using System.Collections;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using EggMapper.Internal;

namespace EggMapper.Execution;

internal static class ExpressionBuilder
{
    private static readonly ConcurrentDictionary<PropertyInfo, Func<object, object?>> Getters = new();
    private static readonly ConcurrentDictionary<PropertyInfo, Action<object, object?>> Setters = new();

    private static Func<object, object?> GetOrBuildGetter(PropertyInfo prop) =>
        Getters.GetOrAdd(prop, BuildGetter);

    private static Action<object, object?> GetOrBuildSetter(PropertyInfo prop) =>
        Setters.GetOrAdd(prop, BuildSetter);

    private static Func<object, object?> BuildGetter(PropertyInfo prop)
    {
        var param = Expression.Parameter(typeof(object), "obj");
        var declaringType = prop.DeclaringType!;
        var cast = Expression.Convert(param, declaringType);
        var propAccess = Expression.Property(cast, prop);
        var boxed = Expression.Convert(propAccess, typeof(object));
        return Expression.Lambda<Func<object, object?>>(boxed, param).Compile();
    }

    private static Action<object, object?> BuildSetter(PropertyInfo prop)
    {
        var setter = prop.GetSetMethod()!;
        var objParam = Expression.Parameter(typeof(object), "obj");
        var valParam = Expression.Parameter(typeof(object), "val");
        var declaringType = prop.DeclaringType!;
        var cast = Expression.Convert(objParam, declaringType);
        var valCast = Expression.Convert(valParam, prop.PropertyType);
        var call = Expression.Call(cast, setter, valCast);
        return Expression.Lambda<Action<object, object?>>(call, objParam, valParam).Compile();
    }

    private static Func<object, object> BuildFactory(TypeMap typeMap)
    {
        if (typeMap.CustomConstructor != null)
            return typeMap.CustomConstructor;

        var destType = typeMap.DestinationType;
        var defaultCtor = destType.GetConstructor(Type.EmptyTypes);

        if (defaultCtor != null)
        {
            var srcParam = Expression.Parameter(typeof(object), "_");
            var newExpr = Expression.New(defaultCtor);
            var boxed = Expression.Convert(newExpr, typeof(object));
            return Expression.Lambda<Func<object, object>>(boxed, srcParam).Compile();
        }

        return _ => Activator.CreateInstance(destType)
            ?? throw new MappingException($"Cannot create instance of {destType.Name}");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Public entry point
    // ══════════════════════════════════════════════════════════════════════════

    public static Func<object, object?, ResolutionContext, object> BuildMappingDelegate(
        TypeMap typeMap,
        Dictionary<TypePair, TypeMap> allTypeMaps,
        ConcurrentDictionary<TypePair, Func<object, object?, ResolutionContext, object>> compiledMaps)
    {
        // Fast path: compile a single typed Expression tree (no per-property delegate
        // calls, no boxing of value types in the hot loop).  Falls back to the
        // flexible action-array approach when complex features (conditions, hooks,
        // custom resolvers, inheritance) are present.
        if (TryBuildTypedDelegate(typeMap, allTypeMaps, compiledMaps, out var fastDel))
            return fastDel!;

        return BuildFlexibleDelegate(typeMap, allTypeMaps, compiledMaps);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Typed Expression-tree path  (fast, no boxing for value types)
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Attempts to compile the entire mapping as a single typed Expression block.
    /// This eliminates per-property delegate overhead and boxing of value types,
    /// making flat-mapping performance approach hand-written code.
    /// Returns false (and uses the flexible path) for maps that require runtime
    /// features that can't be inlined into a static expression tree.
    /// </summary>
    private static bool TryBuildTypedDelegate(
        TypeMap typeMap,
        Dictionary<TypePair, TypeMap> allTypeMaps,
        ConcurrentDictionary<TypePair, Func<object, object?, ResolutionContext, object>> compiledMaps,
        out Func<object, object?, ResolutionContext, object>? result)
    {
        result = null;

        // Bail out for features that can't be expressed in a static expression tree
        if (typeMap.BeforeMapAction != null) return false;
        if (typeMap.AfterMapAction != null) return false;
        if (typeMap.MaxDepth > 0) return false;
        if (typeMap.BaseMapTypePair.HasValue) return false;
        if (ReflectionHelper.IsCollectionType(typeMap.SourceType)) return false;
        if (ReflectionHelper.IsCollectionType(typeMap.DestinationType)) return false;

        var srcType  = typeMap.SourceType;
        var destType = typeMap.DestinationType;

        // Require a parameterless constructor (or a pre-configured custom one)
        var defaultCtor = destType.GetConstructor(Type.EmptyTypes);
        if (defaultCtor == null && typeMap.CustomConstructor == null) return false;

        var srcDetails  = TypeDetails.Get(srcType);
        var destDetails = TypeDetails.Get(destType);

        // Expression parameters: (object src, object? dest, ResolutionContext ctx)
        var srcParam  = Expression.Parameter(typeof(object), "src");
        var destParam = Expression.Parameter(typeof(object), "dest");
        var ctxParam  = Expression.Parameter(typeof(ResolutionContext), "ctx");

        var sVar = Expression.Variable(srcType,  "s");
        var dVar = Expression.Variable(destType, "d");

        var stmts = new List<Expression>();

        // s = (TSrc)src
        stmts.Add(Expression.Assign(sVar, Expression.Convert(srcParam, srcType)));

        // d = dest != null ? (TDst)dest : new TDst()
        Expression newDestExpr = typeMap.CustomConstructor != null
            ? Expression.Convert(
                Expression.Invoke(Expression.Constant(typeMap.CustomConstructor), srcParam),
                destType)
            : (Expression)Expression.New(defaultCtor!);

        stmts.Add(Expression.Assign(dVar,
            Expression.Condition(
                Expression.ReferenceNotEqual(destParam, Expression.Constant(null, typeof(object))),
                Expression.Convert(destParam, destType),
                newDestExpr)));

        // Process explicit PropertyMaps (ForMember / Ignore)
        var processedDestProps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var propMap in typeMap.PropertyMaps)
        {
            processedDestProps.Add(propMap.DestinationProperty.Name);
            if (propMap.Ignored) continue;

            // Conditions, null-substitution, and custom resolvers all need the
            // flexible runtime path — bail out for the whole map.
            if (propMap.Condition != null
                || propMap.FullCondition != null
                || propMap.PreCondition != null
                || propMap.HasNullSubstitute)
                return false;

            if (propMap.CustomResolver != null) return false;

            if (propMap.HasUseValue)
            {
                // d.Prop = (TProp)constant
                try
                {
                    stmts.Add(Expression.Assign(
                        Expression.Property(dVar, propMap.DestinationProperty),
                        Expression.Convert(
                            Expression.Constant(propMap.UseValue),
                            propMap.DestinationProperty.PropertyType)));
                }
                catch { return false; }
                continue;
            }

            if (propMap.SourceMemberName != null)
            {
                var srcProp = srcDetails.ReadableProperties.FirstOrDefault(p =>
                    string.Equals(p.Name, propMap.SourceMemberName, StringComparison.OrdinalIgnoreCase));
                if (srcProp == null) continue;

                var assignExpr = TryBuildTypedAssign(
                    sVar, dVar, srcProp, propMap.DestinationProperty,
                    allTypeMaps, compiledMaps, ctxParam);
                if (assignExpr == null) return false;
                stmts.Add(assignExpr);
            }
        }

        // Convention mapping for remaining writable destination properties
        foreach (var destProp in destDetails.WritableProperties)
        {
            if (processedDestProps.Contains(destProp.Name)) continue;
            processedDestProps.Add(destProp.Name);

            var srcProp = srcDetails.ReadableProperties.FirstOrDefault(p =>
                string.Equals(p.Name, destProp.Name, StringComparison.OrdinalIgnoreCase));
            if (srcProp == null)
            {
                // No direct match — if there's a flattened source we cannot express it
                // as an inline expression tree; let the flexible path handle the whole map.
                if (ReflectionHelper.HasFlattenedSource(destProp.Name, srcDetails))
                    return false;
                continue;
            }

            var assignExpr = TryBuildTypedAssign(
                sVar, dVar, srcProp, destProp, allTypeMaps, compiledMaps, ctxParam);
            if (assignExpr == null) return false;
            stmts.Add(assignExpr);
        }

        // return (object)d
        stmts.Add(Expression.Convert(dVar, typeof(object)));

        var body   = Expression.Block(new[] { sVar, dVar }, stmts);
        var lambda = Expression.Lambda<Func<object, object?, ResolutionContext, object>>(
            body, srcParam, destParam, ctxParam);

        result = lambda.Compile();
        return true;
    }

    /// <summary>
    /// Tries to build a typed assignment expression for a single property pair,
    /// avoiding boxing for directly-assignable types (int, bool, double, etc.).
    /// Returns null if the assignment cannot be safely expressed inline, signalling
    /// the caller to fall back to the flexible delegate approach.
    /// </summary>
    private static Expression? TryBuildTypedAssign(
        ParameterExpression sVar,
        ParameterExpression dVar,
        PropertyInfo srcProp,
        PropertyInfo destProp,
        Dictionary<TypePair, TypeMap> allTypeMaps,
        ConcurrentDictionary<TypePair, Func<object, object?, ResolutionContext, object>> compiledMaps,
        ParameterExpression ctxParam)
    {
        var srcType    = srcProp.PropertyType;
        var destType   = destProp.PropertyType;
        var srcAccess  = Expression.Property(sVar, srcProp);
        var destAccess = Expression.Property(dVar, destProp);

        // ── Collection property ───────────────────────────────────────────────
        if (ReflectionHelper.IsCollectionType(srcType) && ReflectionHelper.IsCollectionType(destType))
        {
            var srcElem  = ReflectionHelper.GetCollectionElementType(srcType);
            var destElem = ReflectionHelper.GetCollectionElementType(destType);
            if (srcElem == null || destElem == null) return null;

            var elemPair    = new TypePair(srcElem, destElem);
            var mapsConst   = Expression.Constant(compiledMaps);
            var pairConst   = Expression.Constant(elemPair);
            var destTConst  = Expression.Constant(destType);
            var srcEConst   = Expression.Constant(srcElem);
            var destEConst  = Expression.Constant(destElem);

            var callExpr = Expression.Call(
                _mapCollectionPropHelperMethod,
                Expression.Convert(srcAccess, typeof(object)),
                mapsConst, pairConst, destTConst, srcEConst, destEConst,
                ctxParam);

            // Assign only when the source collection is non-null
            return Expression.IfThen(
                Expression.ReferenceNotEqual(
                    Expression.Convert(srcAccess, typeof(object)),
                    Expression.Constant(null, typeof(object))),
                Expression.Assign(destAccess, Expression.Convert(callExpr, destType)));
        }

        // ── Same type: direct assignment (NO boxing for int/bool/double/etc.) ─
        if (srcType == destType)
            return Expression.Assign(destAccess, srcAccess);

        // ── Directly assignable (e.g. subclass → base class) ─────────────────
        if (destType.IsAssignableFrom(srcType))
            return Expression.Assign(destAccess, Expression.Convert(srcAccess, destType));

        // ── Nullable<T> → T ───────────────────────────────────────────────────
        var srcUnderlying  = Nullable.GetUnderlyingType(srcType);
        var destUnderlying = Nullable.GetUnderlyingType(destType);

        if (srcUnderlying != null
            && (destType == srcUnderlying || destType.IsAssignableFrom(srcUnderlying)))
        {
            return Expression.Assign(destAccess,
                Expression.Condition(
                    Expression.Property(srcAccess, "HasValue"),
                    Expression.Convert(Expression.Property(srcAccess, "Value"), destType),
                    Expression.Default(destType)));
        }

        // ── T → Nullable<T> ───────────────────────────────────────────────────
        if (destUnderlying != null
            && (srcType == destUnderlying || destUnderlying.IsAssignableFrom(srcType)))
        {
            return Expression.Assign(destAccess, Expression.Convert(srcAccess, destType));
        }

        // ── Registered nested TypeMap (reference type source) ─────────────────
        // When the child map is already compiled (guaranteed when the caller uses
        // TopologicalOrder), embed its delegate directly as a constant — zero
        // runtime dictionary lookup in the hot path.
        var typePair = new TypePair(srcType, destType);
        if (allTypeMaps.ContainsKey(typePair) && !srcType.IsValueType)
        {
            Expression callExpr;

            if (compiledMaps.TryGetValue(typePair, out var childDel))
            {
                // Direct embed: Expression.Invoke on a constant delegate — no lookup.
                callExpr = Expression.Invoke(
                    Expression.Constant(childDel),
                    Expression.Convert(srcAccess, typeof(object)),
                    Expression.Constant(null, typeof(object)),
                    ctxParam);
            }
            else
            {
                // Child not yet compiled (should not happen with topological order),
                // fall back to runtime lookup via the ConcurrentDictionary.
                callExpr = Expression.Call(
                    _mapNestedHelperMethod,
                    Expression.Convert(srcAccess, typeof(object)),
                    Expression.Constant(compiledMaps),
                    Expression.Constant(typePair),
                    ctxParam);
            }

            return Expression.IfThen(
                Expression.ReferenceNotEqual(
                    Expression.Convert(srcAccess, typeof(object)),
                    Expression.Constant(null, typeof(object))),
                Expression.Assign(destAccess, Expression.Convert(callExpr, destType)));
        }

        // ── Value-type numeric conversion (int → long, float → double, etc.) ──
        if (srcType.IsValueType && destType.IsValueType)
        {
            try   { return Expression.Assign(destAccess, Expression.Convert(srcAccess, destType)); }
            catch { return null; }
        }

        // Can't express inline — signal fall-back
        return null;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Static helpers called from within compiled expression trees
    // ══════════════════════════════════════════════════════════════════════════

    // Cached MethodInfo references so we don't use GetMethod() in hot paths
    private static readonly MethodInfo _mapNestedHelperMethod =
        typeof(ExpressionBuilder).GetMethod(nameof(MapNestedHelper),
            BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly MethodInfo _mapCollectionPropHelperMethod =
        typeof(ExpressionBuilder).GetMethod(nameof(MapCollectionPropHelper),
            BindingFlags.NonPublic | BindingFlags.Static)!;

    /// <summary>
    /// Called from within a typed expression tree to map a nested object property.
    /// Dictionary lookup happens once per nested object per top-level Map call;
    /// the result is strongly typed after the call so no outer-loop boxing occurs.
    /// </summary>
    private static object MapNestedHelper(
        object srcVal,
        ConcurrentDictionary<TypePair, Func<object, object?, ResolutionContext, object>> maps,
        TypePair pair,
        ResolutionContext ctx)
    {
        if (maps.TryGetValue(pair, out var del))
            return del(srcVal, null, ctx);
        return ConvertValue(srcVal, pair.DestinationType)!;
    }

    /// <summary>
    /// Called from within a typed expression tree to map a collection property.
    /// Delegates to the existing MapCollectionInternal so all collection shapes
    /// (List, Array, HashSet, …) are handled correctly.
    /// </summary>
    private static object? MapCollectionPropHelper(
        object? srcColl,
        ConcurrentDictionary<TypePair, Func<object, object?, ResolutionContext, object>> maps,
        TypePair elemPair,
        Type destCollType,
        Type srcElemType,
        Type destElemType,
        ResolutionContext ctx)
    {
        if (srcColl == null) return null;
        return MapCollectionInternal(
            (IEnumerable)srcColl, destCollType, srcElemType, destElemType, elemPair, maps, ctx);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Flexible action-array path  (full feature set, used as fallback)
    // ══════════════════════════════════════════════════════════════════════════

    private static Func<object, object?, ResolutionContext, object> BuildFlexibleDelegate(
        TypeMap typeMap,
        Dictionary<TypePair, TypeMap> allTypeMaps,
        ConcurrentDictionary<TypePair, Func<object, object?, ResolutionContext, object>> compiledMaps)
    {
        var factory = BuildFactory(typeMap);
        var srcDetails = TypeDetails.Get(typeMap.SourceType);
        var destDetails = TypeDetails.Get(typeMap.DestinationType);

        var mappingActions = new List<Action<object, object, ResolutionContext>>();
        var processedDestProps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (typeMap.BaseMapTypePair.HasValue &&
            allTypeMaps.TryGetValue(typeMap.BaseMapTypePair.Value, out var baseTypeMap))
        {
            foreach (var basePropMap in baseTypeMap.PropertyMaps)
            {
                var propName = basePropMap.DestinationProperty.Name;
                if (processedDestProps.Contains(propName)) continue;
                if (!typeMap.PropertyMaps.Any(p => p.DestinationProperty.Name == propName))
                {
                    processedDestProps.Add(propName);
                    if (basePropMap.Ignored) continue;
                    var action = BuildPropertyAction(basePropMap, srcDetails, compiledMaps);
                    if (action != null)
                        mappingActions.Add(action);
                }
            }
        }

        foreach (var propMap in typeMap.PropertyMaps)
        {
            processedDestProps.Add(propMap.DestinationProperty.Name);
            if (propMap.Ignored) continue;
            var action = BuildPropertyAction(propMap, srcDetails, compiledMaps);
            if (action != null)
                mappingActions.Add(action);
        }

        foreach (var destProp in destDetails.WritableProperties)
        {
            if (processedDestProps.Contains(destProp.Name)) continue;
            processedDestProps.Add(destProp.Name);
            var action = BuildConventionAction(destProp, srcDetails, allTypeMaps, compiledMaps);
            if (action != null)
                mappingActions.Add(action);
        }

        var actionsArray = mappingActions.ToArray();
        var beforeMap = typeMap.BeforeMapAction;
        var afterMap = typeMap.AfterMapAction;
        var maxDepth = typeMap.MaxDepth;

        return (object src, object? dest, ResolutionContext ctx) =>
        {
            var typedDest = dest ?? factory(src);

            try
            {
                beforeMap?.Invoke(src, typedDest);

                if (maxDepth == 0 || ctx.Depth < maxDepth)
                {
                    ctx.Depth++;
                    try
                    {
                        for (int i = 0; i < actionsArray.Length; i++)
                            actionsArray[i](src, typedDest, ctx);
                    }
                    finally
                    {
                        ctx.Depth--;
                    }
                }

                afterMap?.Invoke(src, typedDest);
            }
            catch (MappingException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new MappingException(src.GetType(), typedDest.GetType(), ex);
            }

            return typedDest;
        };
    }

    private static Action<object, object, ResolutionContext>? BuildPropertyAction(
        PropertyMap propMap,
        TypeDetails srcDetails,
        ConcurrentDictionary<TypePair, Func<object, object?, ResolutionContext, object>> compiledMaps)
    {
        if (propMap.Ignored) return null;

        var destProp = propMap.DestinationProperty;
        var setter = GetOrBuildSetter(destProp);

        if (propMap.HasUseValue)
        {
            var useVal = propMap.UseValue;
            return (src, dest, ctx) => setter(dest, useVal);
        }

        if (propMap.CustomResolver != null)
        {
            var resolver = propMap.CustomResolver;
            var condition = propMap.Condition;
            var fullCondition = propMap.FullCondition;
            var preCondition = propMap.PreCondition;
            var nullSub = propMap.NullSubstitute;
            var hasNullSub = propMap.HasNullSubstitute;
            var destType = destProp.PropertyType;

            return (src, dest, ctx) =>
            {
                if (preCondition != null && !preCondition(src)) return;
                if (condition != null && !condition(src)) return;
                if (fullCondition != null && !fullCondition(src, dest)) return;

                var val = resolver(src, dest);
                if (hasNullSub && val == null) val = nullSub;
                setter(dest, ConvertValue(val, destType));
            };
        }

        if (propMap.SourceMemberName != null)
        {
            var srcProp = srcDetails.ReadableProperties.FirstOrDefault(p =>
                string.Equals(p.Name, propMap.SourceMemberName, StringComparison.OrdinalIgnoreCase));
            if (srcProp == null) return null;
            return BuildDirectPropAction(srcProp, destProp, propMap, compiledMaps);
        }

        return null;
    }

    private static Action<object, object, ResolutionContext>? BuildConventionAction(
        PropertyInfo destProp,
        TypeDetails srcDetails,
        Dictionary<TypePair, TypeMap> allTypeMaps,
        ConcurrentDictionary<TypePair, Func<object, object?, ResolutionContext, object>> compiledMaps)
    {
        var srcProp = srcDetails.ReadableProperties.FirstOrDefault(p =>
            string.Equals(p.Name, destProp.Name, StringComparison.OrdinalIgnoreCase));

        if (srcProp != null)
            return BuildDirectPropAction(srcProp, destProp, null, compiledMaps);

        return TryBuildFlattenedAction(destProp, srcDetails, compiledMaps);
    }

    private static Action<object, object, ResolutionContext>? BuildDirectPropAction(
        PropertyInfo srcProp,
        PropertyInfo destProp,
        PropertyMap? propMap,
        ConcurrentDictionary<TypePair, Func<object, object?, ResolutionContext, object>> compiledMaps)
    {
        var getter = GetOrBuildGetter(srcProp);
        var setter = GetOrBuildSetter(destProp);
        var srcType = srcProp.PropertyType;
        var destType = destProp.PropertyType;
        var condition = propMap?.Condition;
        var fullCondition = propMap?.FullCondition;
        var preCondition = propMap?.PreCondition;
        var nullSub = propMap?.NullSubstitute;
        var hasNullSub = propMap?.HasNullSubstitute ?? false;

        // Detect at compile time whether any runtime guards are needed.
        // Convention mappings (propMap == null) always take the fast path; configured
        // mappings without conditions/null-substitution do too.
        bool noGuards = condition == null && fullCondition == null && preCondition == null && !hasNullSub;

        if (ReflectionHelper.IsCollectionType(srcType) && ReflectionHelper.IsCollectionType(destType))
        {
            var srcElemType = ReflectionHelper.GetCollectionElementType(srcType);
            var destElemType = ReflectionHelper.GetCollectionElementType(destType);

            if (srcElemType != null && destElemType != null)
            {
                var capturedMaps = compiledMaps;
                var elemPair = new TypePair(srcElemType, destElemType);
                var capturedDestType = destType;

                if (noGuards)
                {
                    return (src, dest, ctx) =>
                    {
                        var srcVal = getter(src);
                        if (srcVal == null) return;
                        setter(dest, MapCollectionInternal(
                            (IEnumerable)srcVal, capturedDestType, srcElemType, destElemType, elemPair, capturedMaps, ctx));
                    };
                }

                return (src, dest, ctx) =>
                {
                    if (preCondition != null && !preCondition(src)) return;
                    if (condition != null && !condition(src)) return;
                    if (fullCondition != null && !fullCondition(src, dest)) return;

                    var srcVal = getter(src);
                    if (srcVal == null)
                    {
                        if (hasNullSub) setter(dest, nullSub);
                        return;
                    }
                    var mapped = MapCollectionInternal(
                        (IEnumerable)srcVal, capturedDestType, srcElemType, destElemType, elemPair, capturedMaps, ctx);
                    setter(dest, mapped);
                };
            }
        }

        if (destType.IsAssignableFrom(srcType))
        {
            if (noGuards)
                return (src, dest, ctx) => setter(dest, getter(src));

            return (src, dest, ctx) =>
            {
                if (preCondition != null && !preCondition(src)) return;
                if (condition != null && !condition(src)) return;
                if (fullCondition != null && !fullCondition(src, dest)) return;

                var val = getter(src);
                if (hasNullSub && val == null) val = nullSub;
                setter(dest, val);
            };
        }

        if (!srcType.IsValueType || ReflectionHelper.IsNullableType(srcType))
        {
            var capturedMaps = compiledMaps;
            var typePair = new TypePair(srcType, destType);

            if (noGuards)
            {
                return (src, dest, ctx) =>
                {
                    var srcVal = getter(src);
                    if (srcVal == null) return;

                    if (capturedMaps.TryGetValue(typePair, out var nestedDel))
                    {
                        setter(dest, nestedDel(srcVal, null, ctx));
                        return;
                    }

                    setter(dest, ConvertValue(srcVal, destType));
                };
            }

            return (src, dest, ctx) =>
            {
                if (preCondition != null && !preCondition(src)) return;
                if (condition != null && !condition(src)) return;
                if (fullCondition != null && !fullCondition(src, dest)) return;

                var srcVal = getter(src);
                if (srcVal == null)
                {
                    if (hasNullSub) setter(dest, nullSub);
                    return;
                }

                if (capturedMaps.TryGetValue(typePair, out var nestedDel))
                {
                    setter(dest, nestedDel(srcVal, null, ctx));
                    return;
                }

                setter(dest, ConvertValue(srcVal, destType));
            };
        }

        {
            var capturedDestType = destType;

            if (noGuards)
                return (src, dest, ctx) => setter(dest, ConvertValue(getter(src), capturedDestType));

            return (src, dest, ctx) =>
            {
                if (preCondition != null && !preCondition(src)) return;
                if (condition != null && !condition(src)) return;
                if (fullCondition != null && !fullCondition(src, dest)) return;

                var srcVal = getter(src);
                if (hasNullSub && srcVal == null) { setter(dest, nullSub); return; }
                setter(dest, ConvertValue(srcVal, capturedDestType));
            };
        }
    }

    private static Action<object, object, ResolutionContext>? TryBuildFlattenedAction(
        PropertyInfo destProp,
        TypeDetails srcDetails,
        ConcurrentDictionary<TypePair, Func<object, object?, ResolutionContext, object>> compiledMaps)
    {
        var destName = destProp.Name;

        foreach (var srcProp in srcDetails.ReadableProperties)
        {
            if (!destName.StartsWith(srcProp.Name, StringComparison.OrdinalIgnoreCase)) continue;
            var remainder = destName.Substring(srcProp.Name.Length);
            if (string.IsNullOrEmpty(remainder)) continue;

            var nestedDetails = TypeDetails.Get(srcProp.PropertyType);
            var nestedProp = nestedDetails.ReadableProperties.FirstOrDefault(p =>
                string.Equals(p.Name, remainder, StringComparison.OrdinalIgnoreCase));

            if (nestedProp == null) continue;

            var srcGetter = GetOrBuildGetter(srcProp);
            var nestedGetter = GetOrBuildGetter(nestedProp);
            var setter = GetOrBuildSetter(destProp);
            var destType = destProp.PropertyType;
            var nestedType = nestedProp.PropertyType;
            var capturedMaps = compiledMaps;

            return (src, dest, ctx) =>
            {
                var intermediate = srcGetter(src);
                if (intermediate == null) return;

                var val = nestedGetter(intermediate);
                if (val == null)
                {
                    if (!destType.IsValueType) setter(dest, null);
                    return;
                }

                if (destType.IsAssignableFrom(nestedType))
                {
                    setter(dest, val);
                }
                else if (capturedMaps.TryGetValue(new TypePair(nestedType, destType), out var nestedDel))
                {
                    setter(dest, nestedDel(val, null, ctx));
                }
                else
                {
                    setter(dest, ConvertValue(val, destType));
                }
            };
        }

        return null;
    }

    private static object? MapCollectionInternal(
        IEnumerable source,
        Type destCollectionType,
        Type srcElemType,
        Type destElemType,
        TypePair elementTypePair,
        ConcurrentDictionary<TypePair, Func<object, object?, ResolutionContext, object>> compiledMaps,
        ResolutionContext ctx)
    {
        compiledMaps.TryGetValue(elementTypePair, out var elemMapper);

        object? MapElement(object? elem)
        {
            if (elem == null) return null;
            if (elemMapper != null) return elemMapper(elem, null, ctx);
            if (destElemType.IsAssignableFrom(srcElemType)) return elem;
            return ConvertValue(elem, destElemType);
        }

        if (destCollectionType.IsArray)
        {
            var items = new List<object?>();
            foreach (var item in source)
                items.Add(MapElement(item));
            var arr = Array.CreateInstance(destElemType, items.Count);
            for (int i = 0; i < items.Count; i++)
                arr.SetValue(items[i], i);
            return arr;
        }

        var listType = typeof(List<>).MakeGenericType(destElemType);
        if (destCollectionType.IsAssignableFrom(listType))
        {
            var list = (IList)Activator.CreateInstance(listType)!;
            foreach (var item in source)
                list.Add(MapElement(item));
            return list;
        }

        if (destCollectionType.IsGenericType)
        {
            var ctor = destCollectionType.GetConstructor(Type.EmptyTypes);
            if (ctor != null)
            {
                var addMethod = destCollectionType.GetMethod("Add");
                if (addMethod != null)
                {
                    var coll = ctor.Invoke(Array.Empty<object>());
                    foreach (var item in source)
                        addMethod.Invoke(coll, new[] { MapElement(item) });
                    return coll;
                }
            }
        }

        {
            var list = (IList)Activator.CreateInstance(listType)!;
            foreach (var item in source)
                list.Add(MapElement(item));
            return list;
        }
    }

    private static object? ConvertValue(object? value, Type targetType)
    {
        if (value == null) return null;
        var valueType = value.GetType();
        if (targetType.IsAssignableFrom(valueType)) return value;

        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

        try
        {
            return Convert.ChangeType(value, underlying);
        }
        catch
        {
            return null;
        }
    }
}
