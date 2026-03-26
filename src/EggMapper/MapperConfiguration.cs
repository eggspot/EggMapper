using System.Collections.Concurrent;
using EggMapper.Internal;

namespace EggMapper;

public sealed class MapperConfiguration
{
    // Generation counter: each MapperConfiguration gets a unique id.
    // All Mapper instances from the same config share this generation,
    // so FastCache stays valid when IMapper is registered as scoped.
    private static int _globalGeneration;
    internal int Generation { get; }

    private readonly ConcurrentDictionary<TypePair, Func<object, object?, ResolutionContext, object>> _compiledMaps = new();
    private readonly Dictionary<TypePair, TypeMap> _typeMaps = new();

    // Frozen (read-only) snapshot built after all maps are compiled.
    // Regular Dictionary reads are significantly faster than ConcurrentDictionary
    // because they require no volatile loads or atomic operations.
    internal Dictionary<TypePair, Func<object, object?, ResolutionContext, object>> FrozenMaps = null!;

    // Ctx-free typed delegates (Func<TSource, TDestination>) for flat maps that do
    // not need a ResolutionContext at call time.  These eliminate all boxing and the
    // ctx/dest parameters from the per-item call in Map<> and MapList<>.
    internal Dictionary<TypePair, Delegate> FrozenCtxFreeMaps = null!;

    // Ctx-free compiled list delegates: Func<IList<TSource>, List<TDestination>>
    // Inlines the entire collection + element mapping loop — zero per-element delegate call.
    internal Dictionary<TypePair, Delegate> FrozenCtxFreeListMaps = null!;

    // Patch delegates: Func<TSrc, TDest, TDest> — copies only non-null/HasValue props.
    internal Dictionary<TypePair, Delegate> FrozenPatchMaps = null!;

    internal Func<System.Reflection.PropertyInfo, bool>? ShouldMapProperty { get; private set; }

    internal int DefaultMaxDepth { get; private set; }

    // Global type converters: Func<TSource, TDest> indexed by (TSource, TDest) type pair.
    // Inlined into expression trees during compilation — zero runtime overhead.
    private readonly Dictionary<TypePair, Delegate> _globalConverters;

    // Open generic templates: (srcGenericDef, destGenericDef) → TypeMap with open types.
    private readonly Dictionary<(Type, Type), TypeMap> _openGenericRegistrations;

    // Bundles both delegates for a compiled closed generic pair into one cache entry,
    // reducing on-demand lookups from two ConcurrentDictionary reads to one.
    private sealed class OpenGenericEntry(
        Func<object, object?, ResolutionContext, object> boxed,
        Delegate? ctxFree)
    {
        public readonly Func<object, object?, ResolutionContext, object> Boxed = boxed;
        public readonly Delegate? CtxFree = ctxFree;
    }

    // On-demand compiled delegates for closed generic pairs — single dict, bundled entry.
    private readonly ConcurrentDictionary<TypePair, OpenGenericEntry> _runtimeOpenGenericEntries = new();

    // Lazy list/patch delegates — compiled on first MapList<>/Patch<> call.
    // Keeping startup proportional to Map<> call sites, not total registered pairs.
    private readonly ConcurrentDictionary<TypePair, Delegate> _runtimeListMaps  = new();
    private readonly ConcurrentDictionary<TypePair, Delegate> _runtimePatchMaps = new();

    public MapperConfiguration(Action<IMapperConfigurationExpression> configure)
    {
        Generation = System.Threading.Interlocked.Increment(ref _globalGeneration);
        var expr = new MapperConfigurationExpression();
        configure(expr);
        ShouldMapProperty = expr.GetShouldMapProperty();
        DefaultMaxDepth = expr.GetDefaultMaxDepth();
        _globalConverters = expr.GetGlobalConverters();
        _openGenericRegistrations = expr.GetOpenGenericTypeMaps();

        foreach (var typeMap in expr.GetTypeMaps())
        {
            typeMap.ShouldMapProperty = ShouldMapProperty;
            var key = new TypePair(typeMap.SourceType, typeMap.DestinationType);
            _typeMaps[key] = typeMap;
        }

        // Resolve IncludeAllDerived: for each base map with the flag set,
        // find derived type maps and auto-set their BaseMapTypePair.
        ResolveIncludeAllDerived();

        var orderedMaps = TopologicalOrder(_typeMaps).ToList();
        var ctxFree = new Dictionary<TypePair, Delegate>(orderedMaps.Count);

        // ── Phase 1: ctx-free compilation in parallel ────────────────────────
        // TryBuildCtxFreeDelegate reads only _typeMaps (immutable at this point)
        // and writes only to its own TypeMap.MappingExpression — fully thread-safe.
        var parallelResults =
            new ConcurrentDictionary<TypePair, (Delegate CtxFree, Func<object, object?, ResolutionContext, object> Boxed)>();

        Parallel.ForEach(orderedMaps, typeMap =>
        {
            if (typeMap.ConvertUsingFunc != null) return;
            var key = new TypePair(typeMap.SourceType, typeMap.DestinationType);
            var ctxFreeWithDest = Execution.ExpressionBuilder.TryBuildCtxFreeDelegate(
                typeMap, _typeMaps, _globalConverters);
            if (ctxFreeWithDest != null)
            {
                var boxed = Execution.ExpressionBuilder.CreateBoxedWrapper(
                    typeMap.SourceType, typeMap.DestinationType, ctxFreeWithDest);
                parallelResults[key] = (ctxFreeWithDest, boxed);
            }
        });

        // ── Phase 2: commit results; sequential fallback for complex maps ────
        // Maps that couldn't take ctx-free path (AfterMap, conditions, inheritance…)
        // may need earlier delegates in _compiledMaps — topological order is required.
        foreach (var typeMap in orderedMaps)
        {
            var key = new TypePair(typeMap.SourceType, typeMap.DestinationType);

            if (typeMap.ConvertUsingFunc != null)
            {
                _compiledMaps[key] = typeMap.ConvertUsingFunc;
                typeMap.MappingDelegate = typeMap.ConvertUsingFunc;
            }
            else if (parallelResults.TryGetValue(key, out var r))
            {
                ctxFree[key] = r.CtxFree;
                _compiledMaps[key] = r.Boxed;
                typeMap.MappingDelegate = r.Boxed;
            }
            else
            {
                var compiled = Execution.ExpressionBuilder.BuildMappingDelegate(
                    typeMap, _typeMaps, _compiledMaps, DefaultMaxDepth, _globalConverters);
                _compiledMaps[key] = compiled;
                typeMap.MappingDelegate = compiled;
            }
            // List and patch delegates are compiled lazily on first MapList<>/Patch<> call.
        }

        // Snapshot: plain Dictionary is faster than ConcurrentDictionary for reads.
        FrozenMaps = new Dictionary<TypePair, Func<object, object?, ResolutionContext, object>>(_compiledMaps);
        FrozenCtxFreeMaps = ctxFree;
        // Empty: list/patch are compiled lazily via GetOrCompileListDelegate / GetOrCompilePatchDelegate.
        FrozenCtxFreeListMaps = new Dictionary<TypePair, Delegate>(0);
        FrozenPatchMaps       = new Dictionary<TypePair, Delegate>(0);
    }

    private static IEnumerable<TypeMap> TopologicalOrder(Dictionary<TypePair, TypeMap> typeMaps)
    {
        var visited = new HashSet<TypePair>();
        var result  = new List<TypeMap>(typeMaps.Count);

        void Visit(TypeMap map)
        {
            var key = new TypePair(map.SourceType, map.DestinationType);
            if (!visited.Add(key)) return;

            var srcDetails  = TypeDetails.Get(map.SourceType);
            var destDetails = TypeDetails.Get(map.DestinationType);

            foreach (var destProp in destDetails.WritableProperties)
            {
                if (!srcDetails.ReadableByName.TryGetValue(destProp.Name, out var srcProp)) continue;

                var nestedPair = new TypePair(srcProp.PropertyType, destProp.PropertyType);
                if (typeMaps.TryGetValue(nestedPair, out var nestedMap))
                    Visit(nestedMap);
            }

            result.Add(map);
        }

        foreach (var kvp in typeMaps) Visit(kvp.Value);
        return result;
    }

    private void ResolveIncludeAllDerived()
    {
        // Find all base maps with IncludeAllDerived flag
        var baseMaps = _typeMaps.Values
            .Where(m => m.IncludeAllDerivedFlag)
            .ToList();

        foreach (var baseMap in baseMaps)
        {
            var basePair = new TypePair(baseMap.SourceType, baseMap.DestinationType);

            // Find derived maps whose source/dest types inherit from the base map types
            foreach (var kvp in _typeMaps)
            {
                var derivedMap = kvp.Value;
                if (derivedMap == baseMap) continue;
                if (derivedMap.BaseMapTypePair.HasValue) continue; // already has a base

                if (baseMap.SourceType.IsAssignableFrom(derivedMap.SourceType) &&
                    baseMap.DestinationType.IsAssignableFrom(derivedMap.DestinationType))
                {
                    derivedMap.BaseMapTypePair = basePair;
                }
            }
        }
    }

    public IMapper CreateMapper() => new Mapper(this);

    /// <summary>
    /// Returns (compiling on first call) the ctx-free list delegate for <paramref name="key"/>.
    /// Thread-safe: uses ConcurrentDictionary.GetOrAdd for atomic first-compile.
    /// </summary>
    internal Delegate? GetOrCompileListDelegate(TypePair key)
    {
        if (_runtimeListMaps.TryGetValue(key, out var cached)) return cached;
        if (!_typeMaps.TryGetValue(key, out var typeMap)) return null;
        var del = Execution.ExpressionBuilder.TryBuildCtxFreeListDelegate(typeMap, _typeMaps, _globalConverters);
        if (del == null) return null;
        return _runtimeListMaps.GetOrAdd(key, del);
    }

    /// <summary>
    /// Returns (compiling on first call) the patch delegate for <paramref name="key"/>.
    /// Thread-safe: uses ConcurrentDictionary.GetOrAdd for atomic first-compile.
    /// </summary>
    internal Delegate? GetOrCompilePatchDelegate(TypePair key)
    {
        if (_runtimePatchMaps.TryGetValue(key, out var cached)) return cached;
        if (!_typeMaps.TryGetValue(key, out var typeMap)) return null;
        var del = Execution.ExpressionBuilder.TryBuildPatchDelegate(typeMap);
        if (del == null) return null;
        return _runtimePatchMaps.GetOrAdd(key, del);
    }

    /// <summary>
    /// Attempts to find (or compile on first call) a delegate for a closed generic pair whose
    /// open generic definition was registered via <c>CreateMap(typeof(T&lt;&gt;), typeof(U&lt;&gt;))</c>.
    /// Thread-safe: uses ConcurrentDictionary for on-demand compiled results.
    /// </summary>
    internal bool TryGetOrCompileOpenGenericMap(TypePair key,
        out Func<object, object?, ResolutionContext, object>? boxedDel,
        out Delegate? ctxFreeDel)
    {
        // Fast path: single ConcurrentDictionary lookup for the bundled entry.
        if (_runtimeOpenGenericEntries.TryGetValue(key, out var cached))
        {
            boxedDel   = cached.Boxed;
            ctxFreeDel = cached.CtxFree;
            return true;
        }

        boxedDel   = null;
        ctxFreeDel = null;

        var template = FindOpenGenericTemplate(key.SourceType, key.DestinationType);
        if (template == null)
            return false;

        // GetOrAdd ensures only one thread compiles per type pair.
        var entry = _runtimeOpenGenericEntries.GetOrAdd(key,
            k => CompileOpenGenericEntry(template, k.SourceType, k.DestinationType));
        boxedDel   = entry.Boxed;
        ctxFreeDel = entry.CtxFree;
        return true;
    }

    private OpenGenericEntry CompileOpenGenericEntry(TypeMap template, Type srcType, Type destType)
    {
        var closedTypeMap = CloseGenericTypeMap(template, srcType, destType);
        closedTypeMap.ShouldMapProperty = ShouldMapProperty;

        // ConvertUsing overrides all property mapping — no expression tree needed.
        if (closedTypeMap.ConvertUsingFunc != null)
            return new OpenGenericEntry(closedTypeMap.ConvertUsingFunc, null);

        // Try ctx-free path first (avoids boxing and ResolutionContext allocation).
        var ctxFreeResult = Execution.ExpressionBuilder.TryBuildCtxFreeDelegate(
            closedTypeMap, _typeMaps, _globalConverters);
        if (ctxFreeResult != null)
        {
            var boxed = Execution.ExpressionBuilder.CreateBoxedWrapper(srcType, destType, ctxFreeResult);
            return new OpenGenericEntry(boxed, ctxFreeResult);
        }

        // Fall back to flexible delegate path.
        var compiled = Execution.ExpressionBuilder.BuildMappingDelegate(
            closedTypeMap, _typeMaps, _compiledMaps, DefaultMaxDepth, _globalConverters);
        return new OpenGenericEntry(compiled, null);
    }

    /// <summary>
    /// Finds an open-generic template matching the given source/dest types.
    /// Handles three patterns:
    ///   1. Both open generic: ApiResponse&lt;&gt; → ApiResponseDto&lt;&gt;
    ///   2. Source non-generic (interface/class), dest open generic: ISequenceIdEntity → Id&lt;&gt;
    ///   3. Source open generic, dest non-generic (rare)
    /// For pattern 2, also checks if srcType implements the template's source interface.
    /// </summary>
    private TypeMap? FindOpenGenericTemplate(Type srcType, Type destType)
    {
        // Pattern 1: both generic — standard case
        if (srcType.IsGenericType && destType.IsGenericType)
        {
            var srcDef  = srcType.GetGenericTypeDefinition();
            var destDef = destType.GetGenericTypeDefinition();
            if (_openGenericRegistrations.TryGetValue((srcDef, destDef), out var t1))
                return t1;
        }

        // Pattern 2: source is concrete/interface, dest is generic (e.g., ISequenceIdEntity → Id<>)
        if (destType.IsGenericType)
        {
            var destDef = destType.GetGenericTypeDefinition();
            // Try exact source type first
            if (_openGenericRegistrations.TryGetValue((srcType, destDef), out var t2))
                return t2;
            // Walk source interfaces (e.g., Product implements ISequenceIdEntity)
            foreach (var iface in srcType.GetInterfaces())
            {
                if (_openGenericRegistrations.TryGetValue((iface, destDef), out var ti))
                    return ti;
            }
            // Walk source base types
            for (var bt = srcType.BaseType; bt != null && bt != typeof(object); bt = bt.BaseType)
            {
                if (_openGenericRegistrations.TryGetValue((bt, destDef), out var tb))
                    return tb;
                // Base type might also be generic
                if (bt.IsGenericType && _openGenericRegistrations.TryGetValue((bt.GetGenericTypeDefinition(), destDef), out var tbg))
                    return tbg;
            }
        }

        // Pattern 3: source is generic, dest is concrete (rare)
        if (srcType.IsGenericType)
        {
            var srcDef = srcType.GetGenericTypeDefinition();
            if (_openGenericRegistrations.TryGetValue((srcDef, destType), out var t3))
                return t3;
        }

        return null;
    }

    private static TypeMap CloseGenericTypeMap(TypeMap template, Type srcType, Type destType)
    {
        // Close open-generic ConvertUsing converter type if present
        Func<object, object?, ResolutionContext, object>? convertFunc = template.ConvertUsingFunc;
        if (convertFunc == null && template.OpenGenericConverterType != null)
        {
            // Determine the generic argument for the converter.
            // For pattern ISequenceIdEntity → Id<T>, the dest is Id<Product>, so T = Product.
            var genArgs = destType.IsGenericType ? destType.GetGenericArguments()
                        : srcType.IsGenericType ? srcType.GetGenericArguments()
                        : Array.Empty<Type>();
            if (genArgs.Length > 0)
            {
                try
                {
                    var closedConverterType = template.OpenGenericConverterType.MakeGenericType(genArgs);
                    var converter = Activator.CreateInstance(closedConverterType)!;
                    var method = closedConverterType.GetMethod("Convert")
                        ?? throw new InvalidOperationException(
                            $"Type {TypeNameHelper.Readable(closedConverterType)} does not have a Convert method.");
                    convertFunc = (src, dest, ctx) => method.Invoke(converter, new[] { src, dest, ctx })!;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Failed to close generic converter {TypeNameHelper.Readable(template.OpenGenericConverterType)} " +
                        $"for {TypeNameHelper.Pair(srcType, destType)}", ex);
                }
            }
        }

        var closedMap = new TypeMap
        {
            SourceType                = srcType,
            DestinationType           = destType,
            ConvertUsingFunc          = convertFunc,
            CustomConstructor         = template.CustomConstructor,
            CustomConstructorWithCtx  = template.CustomConstructorWithCtx,
            BeforeMapAction           = template.BeforeMapAction,
            AfterMapAction            = template.AfterMapAction,
            BeforeMapCtxAction        = template.BeforeMapCtxAction,
            AfterMapCtxAction         = template.AfterMapCtxAction,
            MaxDepth                  = template.MaxDepth,
            IncludeAllDerivedFlag     = template.IncludeAllDerivedFlag,
            BaseMapTypePair           = template.BaseMapTypePair,
            ValidationRules           = template.ValidationRules,
        };

        // Copy PropertyMaps: update DestinationProperty to reflect the closed destination type.
        if (template.PropertyMaps.Count > 0)
        {
            var closedDestDetails = Internal.TypeDetails.Get(destType);
            foreach (var pm in template.PropertyMaps)
            {
                var closedDestProp = closedDestDetails.WritableProperties
                    .FirstOrDefault(p => p.Name == pm.DestinationProperty.Name);
                if (closedDestProp == null) continue;

                closedMap.PropertyMaps.Add(new PropertyMap
                {
                    DestinationProperty  = closedDestProp,
                    Ignored              = pm.Ignored,
                    CustomResolver       = pm.CustomResolver,
                    ContextResolver      = pm.ContextResolver,
                    Condition            = pm.Condition,
                    FullCondition        = pm.FullCondition,
                    PreCondition         = pm.PreCondition,
                    NullSubstitute       = pm.NullSubstitute,
                    HasNullSubstitute    = pm.HasNullSubstitute,
                    UseValue             = pm.UseValue,
                    HasUseValue          = pm.HasUseValue,
                    UseDestinationValue  = pm.UseDestinationValue,
                    SourceMemberName     = pm.SourceMemberName,
                    ValueResolverFactory = pm.ValueResolverFactory,
                });
            }
        }

        return closedMap;
    }

    internal Func<object, object?, ResolutionContext, object>? GetMapDelegate(TypePair typePair)
    {
        FrozenMaps.TryGetValue(typePair, out var del);
        return del;
    }

    internal Dictionary<TypePair, TypeMap> TypeMaps => _typeMaps;

    /// <summary>
    /// Builds an <see cref="System.Linq.Expressions.Expression{TDelegate}"/> that projects
    /// <typeparamref name="TSource"/> to <typeparamref name="TDestination"/> using the
    /// registered type map. The expression is suitable for use with LINQ providers (e.g.
    /// EF Core) and is never compiled by EggMapper — pass it directly to
    /// <c>IQueryable.Select()</c> or use the <c>ProjectTo</c> extension method.
    /// </summary>
    public System.Linq.Expressions.Expression<Func<TSource, TDestination>>
        BuildProjection<TSource, TDestination>()
        => Execution.ProjectionBuilder.Build<TSource, TDestination>(this);

    /// <summary>
    /// Returns a human-readable string representation of the compiled expression tree
    /// for the <typeparamref name="TSource"/> → <typeparamref name="TDestination"/> map.
    /// Returns null when no expression tree is available (e.g. flexible delegate path or
    /// ConvertUsing maps).
    /// </summary>
    public string? GetMappingExpressionText<TSource, TDestination>()
    {
        var key = new TypePair(typeof(TSource), typeof(TDestination));
        if (!_typeMaps.TryGetValue(key, out var typeMap))
            return null;
        return typeMap.MappingExpression?.ToString();
    }

    public void AssertConfigurationIsValid()
    {
        var errors = new List<string>();
        foreach (var kvp in _typeMaps)
        {
            var typeMap = kvp.Value;
            var destDetails = TypeDetails.Get(typeMap.DestinationType);
            var srcDetails = TypeDetails.Get(typeMap.SourceType);

            foreach (var destProp in destDetails.WritableProperties)
            {
                if (ShouldMapProperty != null && !ShouldMapProperty(destProp)) continue;

                var propMap = typeMap.PropertyMaps.FirstOrDefault(p =>
                    p.DestinationProperty.Name == destProp.Name);

                if (propMap?.Ignored == true) continue;
                if (propMap?.HasUseValue == true) continue;
                if (propMap?.UseDestinationValue == true) continue;
                if (propMap?.CustomResolver != null) continue;
                if (propMap?.ContextResolver != null) continue;
                if (propMap?.ValueResolverFactory != null) continue;

                var sourceMemberName = propMap?.SourceMemberName ?? destProp.Name;
                srcDetails.ReadableByName.TryGetValue(sourceMemberName, out var srcProp);

                if (srcProp == null && !ReflectionHelper.HasFlattenedSource(destProp.Name, srcDetails))
                {
                    errors.Add(
                        $"Unmapped destination member '{typeMap.DestinationType.Name}.{destProp.Name}' " +
                        $"in map {typeMap.SourceType.Name} -> {typeMap.DestinationType.Name}");
                }
            }
        }

        if (errors.Count > 0)
            throw new InvalidOperationException(
                "EggMapper configuration is invalid:\n" + string.Join("\n", errors));
    }
}
