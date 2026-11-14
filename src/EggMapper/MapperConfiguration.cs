using System.Collections.Concurrent;
using EggMapper.Internal;

namespace EggMapper;

public sealed class MapperConfiguration
{
    private static int _globalGeneration;
    internal int Generation { get; }

    private readonly ConcurrentDictionary<TypePair, Func<object, object?, ResolutionContext, object>> _compiledMaps = new();
    private readonly Dictionary<TypePair, TypeMap> _typeMaps = new();

    internal Dictionary<TypePair, Func<object, object?, ResolutionContext, object>> FrozenMaps = null!;

    internal Dictionary<TypePair, Delegate> FrozenCtxFreeMaps = null!;

    internal Dictionary<TypePair, Delegate> FrozenCtxFreeListMaps = null!;

    internal Dictionary<TypePair, Delegate> FrozenPatchMaps = null!;

    internal Func<System.Reflection.PropertyInfo, bool>? ShouldMapProperty { get; private set; }

    internal int DefaultMaxDepth { get; private set; }

    private readonly Dictionary<TypePair, Delegate> _globalConverters;

    private readonly Dictionary<(Type, Type), TypeMap> _openGenericRegistrations;

    private sealed class OpenGenericEntry(
        Func<object, object?, ResolutionContext, object> boxed,
        Delegate? ctxFree)
    {
        public readonly Func<object, object?, ResolutionContext, object> Boxed = boxed;
        public readonly Delegate? CtxFree = ctxFree;
    }

    private readonly ConcurrentDictionary<TypePair, OpenGenericEntry> _runtimeOpenGenericEntries = new();

    private readonly ConcurrentDictionary<TypePair, Delegate> _runtimeListMaps  = new();
    private readonly ConcurrentDictionary<TypePair, Delegate> _runtimePatchMaps = new();

    private readonly ConcurrentDictionary<TypePair, OpenGenericEntry> _runtimeSameTypeMaps = new();

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

        ResolveIncludeAllDerived();

        ResolveIncludeBaseChains();

        var orderedMaps = TopologicalOrder(_typeMaps).ToList();
        var ctxFree = new Dictionary<TypePair, Delegate>(orderedMaps.Count);

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
        }

        FrozenMaps = new Dictionary<TypePair, Func<object, object?, ResolutionContext, object>>(_compiledMaps);
        FrozenCtxFreeMaps = ctxFree;
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
        var baseMaps = _typeMaps.Values
            .Where(m => m.IncludeAllDerivedFlag)
            .ToList();

        foreach (var baseMap in baseMaps)
        {
            var basePair = new TypePair(baseMap.SourceType, baseMap.DestinationType);

            foreach (var kvp in _typeMaps)
            {
                var derivedMap = kvp.Value;
                if (derivedMap == baseMap) continue;
                if (derivedMap.BaseMapTypePair.HasValue) continue;

                if (baseMap.SourceType.IsAssignableFrom(derivedMap.SourceType) &&
                    baseMap.DestinationType.IsAssignableFrom(derivedMap.DestinationType))
                {
                    derivedMap.BaseMapTypePair = basePair;
                }
            }
        }
    }

    private void ResolveIncludeBaseChains()
    {
        foreach (var typeMap in _typeMaps.Values)
        {
            if (typeMap.BaseMapTypePair.HasValue)
                typeMap.EffectivePropertyMaps = BuildEffectivePropertyMaps(typeMap);
        }
    }

    private List<PropertyMap> BuildEffectivePropertyMaps(TypeMap typeMap)
    {
        var chain = new List<TypeMap> { typeMap };
        var visited = new HashSet<TypePair> { new TypePair(typeMap.SourceType, typeMap.DestinationType) };
        var current = typeMap;
        while (current.BaseMapTypePair.HasValue)
        {
            var basePair = current.BaseMapTypePair.Value;
            if (!visited.Add(basePair))
                throw new InvalidOperationException(
                    $"Circular IncludeBase() reference detected involving map " +
                    $"{typeMap.SourceType.Name} -> {typeMap.DestinationType.Name}.");
            if (!_typeMaps.TryGetValue(basePair, out var baseTypeMap)) break;
            chain.Add(baseTypeMap);
            current = baseTypeMap;
        }

        var winners = new Dictionary<string, PropertyMap>(StringComparer.OrdinalIgnoreCase);
        foreach (var levelMap in chain)
            foreach (var propMap in levelMap.PropertyMaps)
                if (!winners.ContainsKey(propMap.DestinationProperty.Name))
                    winners[propMap.DestinationProperty.Name] = propMap;

        var ordered = new List<PropertyMap>();
        var emitted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = chain.Count - 1; i >= 0; i--)
        {
            foreach (var propMap in chain[i].PropertyMaps)
            {
                var name = propMap.DestinationProperty.Name;
                if (!ReferenceEquals(winners[name], propMap)) continue;
                if (!emitted.Add(name)) continue;
                ordered.Add(propMap);
            }
        }

        return ordered;
    }

    public IMapper CreateMapper() => new Mapper(this);

    internal Delegate? GetOrCompileListDelegate(TypePair key)
    {
        if (_runtimeListMaps.TryGetValue(key, out var cached)) return cached;
        if (!_typeMaps.TryGetValue(key, out var typeMap)) return null;
        var del = Execution.ExpressionBuilder.TryBuildCtxFreeListDelegate(typeMap, _typeMaps, _globalConverters);
        if (del == null) return null;
        return _runtimeListMaps.GetOrAdd(key, del);
    }

    internal Delegate? GetOrCompilePatchDelegate(TypePair key)
    {
        if (_runtimePatchMaps.TryGetValue(key, out var cached)) return cached;
        if (!_typeMaps.TryGetValue(key, out var typeMap)) return null;
        var del = Execution.ExpressionBuilder.TryBuildPatchDelegate(typeMap);
        if (del == null) return null;
        return _runtimePatchMaps.GetOrAdd(key, del);
    }

    internal bool TryGetOrCompileSameTypeMap(TypePair key,
        out Func<object, object?, ResolutionContext, object>? boxedDel,
        out Delegate? ctxFreeDel)
    {
        if (_runtimeSameTypeMaps.TryGetValue(key, out var cached))
        {
            boxedDel   = cached.Boxed;
            ctxFreeDel = cached.CtxFree;
            return true;
        }

        boxedDel   = null;
        ctxFreeDel = null;

        var type = key.SourceType;
        if (type != key.DestinationType) return false;
        if (type.IsPrimitive || type == typeof(string) || type.IsEnum) return false;
        if (type.IsValueType) return false;
        if (type.IsAbstract || type.IsInterface) return false;
        if (type.GetConstructor(Type.EmptyTypes) == null) return false;
        if (Internal.ReflectionHelper.IsCollectionType(type)) return false;
        if (Internal.ReflectionHelper.IsDictionaryType(type)) return false;

        var entry = _runtimeSameTypeMaps.GetOrAdd(key, k =>
        {
            var syntheticMap = new TypeMap
            {
                SourceType = type,
                DestinationType = type,
                ShouldMapProperty = ShouldMapProperty
            };

            var ctxFree = Execution.ExpressionBuilder.TryBuildCtxFreeDelegate(
                syntheticMap, _typeMaps, _globalConverters);
            if (ctxFree != null)
            {
                var boxed = Execution.ExpressionBuilder.CreateBoxedWrapper(type, type, ctxFree);
                return new OpenGenericEntry(boxed, ctxFree);
            }

            var compiled = Execution.ExpressionBuilder.BuildMappingDelegate(
                syntheticMap, _typeMaps, _compiledMaps, DefaultMaxDepth, _globalConverters);
            return new OpenGenericEntry(compiled, null);
        });

        boxedDel   = entry.Boxed;
        ctxFreeDel = entry.CtxFree;
        return true;
    }

    internal bool TryGetOrCompileOpenGenericMap(TypePair key,
        out Func<object, object?, ResolutionContext, object>? boxedDel,
        out Delegate? ctxFreeDel)
    {
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

        if (closedTypeMap.ConvertUsingFunc != null)
            return new OpenGenericEntry(closedTypeMap.ConvertUsingFunc, null);

        var ctxFreeResult = Execution.ExpressionBuilder.TryBuildCtxFreeDelegate(
            closedTypeMap, _typeMaps, _globalConverters);
        if (ctxFreeResult != null)
        {
            var boxed = Execution.ExpressionBuilder.CreateBoxedWrapper(srcType, destType, ctxFreeResult);
            return new OpenGenericEntry(boxed, ctxFreeResult);
        }

        var compiled = Execution.ExpressionBuilder.BuildMappingDelegate(
            closedTypeMap, _typeMaps, _compiledMaps, DefaultMaxDepth, _globalConverters);
        return new OpenGenericEntry(compiled, null);
    }

    private TypeMap? FindOpenGenericTemplate(Type srcType, Type destType)
    {
        if (srcType.IsGenericType && destType.IsGenericType)
        {
            var srcDef  = srcType.GetGenericTypeDefinition();
            var destDef = destType.GetGenericTypeDefinition();
            if (_openGenericRegistrations.TryGetValue((srcDef, destDef), out var t1))
                return t1;
        }

        if (destType.IsGenericType)
        {
            var destDef = destType.GetGenericTypeDefinition();
            if (_openGenericRegistrations.TryGetValue((srcType, destDef), out var t2))
                return t2;
            foreach (var iface in srcType.GetInterfaces())
            {
                if (_openGenericRegistrations.TryGetValue((iface, destDef), out var ti))
                    return ti;
            }
            for (var bt = srcType.BaseType; bt != null && bt != typeof(object); bt = bt.BaseType)
            {
                if (_openGenericRegistrations.TryGetValue((bt, destDef), out var tb))
                    return tb;
                if (bt.IsGenericType && _openGenericRegistrations.TryGetValue((bt.GetGenericTypeDefinition(), destDef), out var tbg))
                    return tbg;
            }
        }

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
        Func<object, object?, ResolutionContext, object>? convertFunc = template.ConvertUsingFunc;
        if (convertFunc == null && template.OpenGenericConverterType != null)
        {
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

                var propMaps = typeMap.EffectivePropertyMaps ?? typeMap.PropertyMaps;
                var propMap = propMaps.FirstOrDefault(p => p.DestinationProperty.Name == destProp.Name);

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
