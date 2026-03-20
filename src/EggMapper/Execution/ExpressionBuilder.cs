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

    private static readonly MethodInfo EnumParseMethod =
        typeof(Enum).GetMethod("Parse", new[] { typeof(Type), typeof(string), typeof(bool) })!;
    private static readonly MethodInfo ObjectToStringMethod =
        typeof(object).GetMethod("ToString", Type.EmptyTypes)!;

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
        var destDetails = TypeDetails.Get(destType);
        var defaultCtor = destDetails.ParameterlessCtor;

        if (defaultCtor != null)
        {
            var srcParam = Expression.Parameter(typeof(object), "_");
            var newExpr = Expression.New(defaultCtor);
            var boxed = Expression.Convert(newExpr, typeof(object));
            return Expression.Lambda<Func<object, object>>(boxed, srcParam).Compile();
        }

        // Try parameterized constructor matched against source properties
        var srcType = typeMap.SourceType;
        var srcDetails = TypeDetails.Get(srcType);
        var found = FindBestConstructor(destDetails, srcDetails);
        if (found != null)
        {
            var (bestCtor, bestParams) = found.Value;
            var objSrcParam = Expression.Parameter(typeof(object), "src");
            var typedSrcVar = Expression.Variable(srcType, "s");
            var args = BuildCtorArgExpressions(bestParams, typedSrcVar, srcDetails);
            var newExpr = Expression.New(bestCtor, args);
            var boxed = Expression.Convert(newExpr, typeof(object));
            var body = Expression.Block(new[] { typedSrcVar },
                Expression.Assign(typedSrcVar, Expression.Convert(objSrcParam, srcType)),
                boxed);
            return Expression.Lambda<Func<object, object>>(body, objSrcParam).Compile();
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
        ConcurrentDictionary<TypePair, Func<object, object?, ResolutionContext, object>> compiledMaps,
        int defaultMaxDepth = 32)
    {
        // Fast path: compile a single typed Expression tree (no per-property delegate
        // calls, no boxing of value types in the hot loop).  Falls back to the
        // flexible action-array approach when complex features (conditions, hooks,
        // custom resolvers, inheritance) are present.
        if (TryBuildTypedDelegate(typeMap, allTypeMaps, compiledMaps, out var fastDel))
            return fastDel!;

        return BuildFlexibleDelegate(typeMap, allTypeMaps, compiledMaps, defaultMaxDepth);
    }

    /// <summary>
    /// Attempts to compile a strongly-typed <c>Func&lt;TSource, TDestination&gt;</c>
    /// for maps where no <see cref="ResolutionContext"/> is needed at call time
    /// (i.e. flat maps with only directly-assignable properties and no nested
    /// registered type maps, collection properties, conditions, or hooks).
    /// When successful, the returned delegate eliminates all boxing/unboxing and
    /// the ResolutionContext parameter from the per-item call in
    /// <see cref="Mapper.Map{TSource,TDestination}(TSource)"/> and
    /// <see cref="Mapper.MapList{TSource,TDestination}"/>.
    /// Returns <c>null</c> when the ctx-free path is not applicable.
    /// </summary>
    public static Delegate? TryBuildCtxFreeDelegate(
        TypeMap typeMap,
        Dictionary<TypePair, TypeMap>? allTypeMaps = null)
    {
        if (typeMap.ConvertUsingFunc != null) return null;
        if (typeMap.ShouldMapProperty != null) return null;
        if (typeMap.BeforeMapAction != null) return null;
        if (typeMap.AfterMapAction  != null) return null;
        if (typeMap.BeforeMapCtxAction != null) return null;
        if (typeMap.AfterMapCtxAction  != null) return null;
        if (typeMap.MaxDepth > 0) return null;
        if (typeMap.BaseMapTypePair.HasValue) return null;
        if (ReflectionHelper.IsCollectionType(typeMap.SourceType)) return null;
        if (ReflectionHelper.IsCollectionType(typeMap.DestinationType)) return null;

        var srcType  = typeMap.SourceType;
        var destType = typeMap.DestinationType;

        var destDetailsCf = TypeDetails.Get(destType);
        var defaultCtor = destDetailsCf.ParameterlessCtor;
        (ConstructorInfo Ctor, ParameterInfo[] Params)? bestCtorCf = null;
        if (defaultCtor == null && typeMap.CustomConstructor == null)
        {
            var srcDetailsCf = TypeDetails.Get(srcType);
            bestCtorCf = FindBestConstructor(destDetailsCf, srcDetailsCf);
            if (bestCtorCf == null) return null;
        }

        var srcDetails  = TypeDetails.Get(srcType);
        var destDetails = TypeDetails.Get(destType);

        // Expression parameters: (TSource src, TDest existingDest)
        // existingDest == null (or default for value types) → create a new destination
        // existingDest != null                              → map into the existing instance
        var srcParam  = Expression.Parameter(srcType,  "src");
        var destParam = Expression.Parameter(destType, "existingDest");
        var dVar      = Expression.Variable(destType, "d");

        var stmts = new List<Expression>();

        // d = existingDest ?? new TDest()  (or new TDest(matchedArgs) for records)
        Expression newDestExpr;
        if (typeMap.CustomConstructor != null)
            newDestExpr = Expression.Convert(
                Expression.Invoke(Expression.Constant(typeMap.CustomConstructor),
                    Expression.Convert(srcParam, typeof(object))),
                destType);
        else if (bestCtorCf != null)
            newDestExpr = Expression.New(bestCtorCf.Value.Ctor,
                BuildCtorArgExpressions(bestCtorCf.Value.Params, srcParam, srcDetails));
        else
            newDestExpr = Expression.New(defaultCtor!);

        // Value types can never be null so always construct fresh.
        Expression initDest = destType.IsValueType
            ? newDestExpr
            : Expression.Condition(
                Expression.ReferenceNotEqual(
                    Expression.Convert(destParam, typeof(object)),
                    Expression.Constant(null, typeof(object))),
                destParam,
                newDestExpr);

        stmts.Add(Expression.Assign(dVar, initDest));

        var processedDestProps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // ── Explicit PropertyMaps ────────────────────────────────────────────
        foreach (var propMap in typeMap.PropertyMaps)
        {
            processedDestProps.Add(propMap.DestinationProperty.Name);
            if (propMap.Ignored) continue;

            // Runtime guards cannot be expressed without ctx → bail out
            if (propMap.Condition != null || propMap.FullCondition != null
                || propMap.PreCondition != null || propMap.HasNullSubstitute
                || propMap.UseDestinationValue)
                return null;

            if (propMap.CustomResolver != null || propMap.ContextResolver != null
                || propMap.ValueResolverFactory != null) return null;

            if (propMap.HasUseValue)
            {
                try
                {
                    stmts.Add(Expression.Assign(
                        Expression.Property(dVar, propMap.DestinationProperty),
                        Expression.Convert(
                            Expression.Constant(propMap.UseValue),
                            propMap.DestinationProperty.PropertyType)));
                }
                catch { return null; }
                continue;
            }

            if (propMap.SourceMemberName != null)
            {
                srcDetails.ReadableByName.TryGetValue(propMap.SourceMemberName, out var srcProp);
                if (srcProp == null) continue;

                var assignExpr = TryBuildCtxFreeAssign(srcParam, dVar, srcProp, propMap.DestinationProperty, allTypeMaps);
                if (assignExpr == null) return null;
                stmts.Add(assignExpr);
            }
        }

        // ── Convention mapping for remaining writable destination properties ──
        // Flattened assignments are grouped by navigation property so one null check
        // and one local variable covers all fields from the same navigation path.
        var flatGroups = new Dictionary<string, (PropertyInfo NavProp, List<(PropertyInfo NestedProp, PropertyInfo DestProp)> Items)>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var destProp in destDetails.WritableProperties)
        {
            if (processedDestProps.Contains(destProp.Name)) continue;
            processedDestProps.Add(destProp.Name);

            srcDetails.ReadableByName.TryGetValue(destProp.Name, out var srcProp);
            if (srcProp == null)
            {
                var flatInfo = TryGetFlattenedPropInfo(destProp, srcDetails);
                if (flatInfo != null)
                {
                    var (navProp, nestedProp) = flatInfo.Value;
                    if (!flatGroups.TryGetValue(navProp.Name, out var group))
                        flatGroups[navProp.Name] = group = (navProp, new List<(PropertyInfo, PropertyInfo)>());
                    group.Items.Add((nestedProp, destProp));
                    continue;
                }
                if (ReflectionHelper.HasFlattenedSource(destProp.Name, srcDetails))
                    return null;
                continue;
            }

            var assignExpr = TryBuildCtxFreeAssign(srcParam, dVar, srcProp, destProp, allTypeMaps);
            if (assignExpr == null) return null;
            stmts.Add(assignExpr);
        }

        // Emit grouped flattened assignments — one null check + one local var per nav property
        foreach (var g in flatGroups.Values)
            EmitGroupedFlattenedAssigns(srcParam, dVar, g.NavProp, g.Items, stmts);

        // return d
        stmts.Add(dVar);

        var body     = Expression.Block(new[] { dVar }, stmts);
        // Func<TSrc, TDest, TDest> — second param is optional existing destination
        var funcType = typeof(Func<,,>).MakeGenericType(srcType, destType, destType);
        var lambda = Expression.Lambda(funcType, body, srcParam, destParam);
        typeMap.MappingExpression = lambda;
        return lambda.Compile();
    }

    /// <summary>
    /// Builds a compiled <c>Func&lt;IList&lt;TSource&gt;, List&lt;TDestination&gt;&gt;</c>
    /// that maps an entire collection in a single expression tree — no per-element
    /// delegate call overhead. The element mapping is inlined into the loop body.
    /// Returns null when the element mapping can't be fully inlined.
    /// </summary>
    public static Delegate? TryBuildCtxFreeListDelegate(
        TypeMap elemTypeMap,
        Dictionary<TypePair, TypeMap>? allTypeMaps = null)
    {
        var srcElem = elemTypeMap.SourceType;
        var destElem = elemTypeMap.DestinationType;

        // Try building the single-element ctx-free body first
        if (elemTypeMap.BeforeMapAction != null || elemTypeMap.AfterMapAction != null) return null;
        if (elemTypeMap.MaxDepth > 0 || elemTypeMap.BaseMapTypePair.HasValue) return null;
        if (ReflectionHelper.IsCollectionType(srcElem) || ReflectionHelper.IsCollectionType(destElem)) return null;

        var destElemDetails = TypeDetails.Get(destElem);
        var destCtor = destElemDetails.ParameterlessCtor;
        (ConstructorInfo Ctor, ParameterInfo[] Params)? bestCtorList = null;
        if (destCtor == null && elemTypeMap.CustomConstructor == null)
        {
            var srcElemDetails = TypeDetails.Get(srcElem);
            bestCtorList = FindBestConstructor(destElemDetails, srcElemDetails);
            if (bestCtorList == null) return null;
        }

        var srcDetails = TypeDetails.Get(srcElem);
        var destDetails = TypeDetails.Get(destElem);

        // Build element mapping body inline
        var elemSrcParam = Expression.Parameter(srcElem, "es");
        var elemDestVar = Expression.Variable(destElem, "ed");
        var elemStmts = new List<Expression>();

        Expression newDestExpr;
        if (elemTypeMap.CustomConstructor != null)
            newDestExpr = Expression.Convert(
                Expression.Invoke(Expression.Constant(elemTypeMap.CustomConstructor),
                    Expression.Convert(elemSrcParam, typeof(object))),
                destElem);
        else if (bestCtorList != null)
            newDestExpr = Expression.New(bestCtorList.Value.Ctor,
                BuildCtorArgExpressions(bestCtorList.Value.Params, elemSrcParam, srcDetails));
        else
            newDestExpr = Expression.New(destCtor!);
        elemStmts.Add(Expression.Assign(elemDestVar, newDestExpr));

        var processedProps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var propMap in elemTypeMap.PropertyMaps)
        {
            processedProps.Add(propMap.DestinationProperty.Name);
            if (propMap.Ignored) continue;
            if (propMap.Condition != null || propMap.FullCondition != null
                || propMap.PreCondition != null || propMap.HasNullSubstitute
                || propMap.CustomResolver != null)
                return null;

            if (propMap.HasUseValue)
            {
                try
                {
                    elemStmts.Add(Expression.Assign(
                        Expression.Property(elemDestVar, propMap.DestinationProperty),
                        Expression.Convert(Expression.Constant(propMap.UseValue), propMap.DestinationProperty.PropertyType)));
                }
                catch { return null; }
                continue;
            }

            if (propMap.SourceMemberName != null)
            {
                srcDetails.ReadableByName.TryGetValue(propMap.SourceMemberName, out var sp);
                if (sp == null) continue;
                var a = TryBuildCtxFreeAssign(elemSrcParam, elemDestVar, sp, propMap.DestinationProperty, allTypeMaps);
                if (a == null) return null;
                elemStmts.Add(a);
            }
        }

        var elemFlatGroups = new Dictionary<string, (PropertyInfo NavProp, List<(PropertyInfo NestedProp, PropertyInfo DestProp)> Items)>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var destProp in destDetails.WritableProperties)
        {
            if (processedProps.Contains(destProp.Name)) continue;
            processedProps.Add(destProp.Name);

            srcDetails.ReadableByName.TryGetValue(destProp.Name, out var srcProp);
            if (srcProp == null)
            {
                var flatInfo = TryGetFlattenedPropInfo(destProp, srcDetails);
                if (flatInfo != null)
                {
                    var (navProp, nestedProp) = flatInfo.Value;
                    if (!elemFlatGroups.TryGetValue(navProp.Name, out var group))
                        elemFlatGroups[navProp.Name] = group = (navProp, new List<(PropertyInfo, PropertyInfo)>());
                    group.Items.Add((nestedProp, destProp));
                    continue;
                }
                if (ReflectionHelper.HasFlattenedSource(destProp.Name, srcDetails)) return null;
                continue;
            }

            var assign = TryBuildCtxFreeAssign(elemSrcParam, elemDestVar, srcProp, destProp, allTypeMaps);
            if (assign == null) return null;
            elemStmts.Add(assign);
        }

        foreach (var g in elemFlatGroups.Values)
            EmitGroupedFlattenedAssigns(elemSrcParam, elemDestVar, g.NavProp, g.Items, elemStmts);

        elemStmts.Add(elemDestVar);

        // Build the list mapping with List<T> direct indexer (JIT-devirtualized):
        // (List<TSrc> source) => { var list = new List<TDst>(count); for (...) list.Add(map(source[i])); return list; }
        var listSrcType = typeof(List<>).MakeGenericType(srcElem);
        var listDestType = typeof(List<>).MakeGenericType(destElem);
        var srcListParam = Expression.Parameter(listSrcType, "source");

        var listVar = Expression.Variable(listDestType, "result");
        var iVar = Expression.Variable(typeof(int), "i");
        var countVar = Expression.Variable(typeof(int), "count");
        var breakLabel = Expression.Label("brk");

        var countProp = listSrcType.GetProperty("Count")!;
        var itemProp = listSrcType.GetProperty("Item")!;
        var listCtor = listDestType.GetConstructor(new[] { typeof(int) })!;
        var addMethod = listDestType.GetMethod("Add")!;

        var indexAccess = Expression.MakeIndex(srcListParam, itemProp, new[] { (Expression)iVar });

        var loopBodyStmts = new List<Expression>();
        loopBodyStmts.Add(Expression.Assign(elemSrcParam, indexAccess));
        for (int idx = 0; idx < elemStmts.Count - 1; idx++)
            loopBodyStmts.Add(elemStmts[idx]);
        loopBodyStmts.Add(Expression.Call(listVar, addMethod, elemDestVar));
        loopBodyStmts.Add(Expression.PostIncrementAssign(iVar));

        var fullBody = Expression.Block(
            new[] { listVar, iVar, countVar, elemSrcParam, elemDestVar },
            Expression.Assign(countVar, Expression.Property(srcListParam, countProp)),
            Expression.Assign(listVar, Expression.New(listCtor, countVar)),
            Expression.Assign(iVar, Expression.Constant(0)),
            Expression.Loop(
                Expression.IfThenElse(
                    Expression.LessThan(iVar, countVar),
                    Expression.Block(loopBodyStmts),
                    Expression.Break(breakLabel)),
                breakLabel),
            listVar);

        var funcType = typeof(Func<,>).MakeGenericType(listSrcType, listDestType);
        return Expression.Lambda(funcType, fullBody, srcListParam).Compile();
    }

    /// <summary>
    /// Builds a typed assignment expression for a property pair without needing
    /// a <see cref="ResolutionContext"/> parameter.  Returns <c>null</c> when the
    /// assignment would require runtime context (nested registered maps, collection
    /// elements that require mapping, etc.).
    /// </summary>
    private static Expression? TryBuildCtxFreeAssign(
        ParameterExpression srcParam,
        ParameterExpression dVar,
        PropertyInfo srcProp,
        PropertyInfo destProp,
        Dictionary<TypePair, TypeMap>? allTypeMaps = null)
    {
        var srcType    = srcProp.PropertyType;
        var destType   = destProp.PropertyType;
        var srcAccess  = Expression.Property(srcParam, srcProp);
        var destAccess = Expression.Property(dVar, destProp);

        // ── Collection property — try to inline element mapping ──────────────
        if (ReflectionHelper.IsCollectionType(srcType) && ReflectionHelper.IsCollectionType(destType))
        {
            if (allTypeMaps == null) return null;
            return TryBuildCtxFreeCollectionAssign(srcParam, dVar, srcProp, destProp, allTypeMaps);
        }
        if (ReflectionHelper.IsCollectionType(srcType) || ReflectionHelper.IsCollectionType(destType))
            return null;

        // ── Dictionary property ───────────────────────────────────────────────
        if (ReflectionHelper.IsDictionaryType(srcType) || ReflectionHelper.IsDictionaryType(destType))
        {
            if (!ReflectionHelper.IsDictionaryType(srcType) || !ReflectionHelper.IsDictionaryType(destType))
                return null;
            return TryBuildCtxFreeDictionaryAssign(srcParam, dVar, srcProp, destProp);
        }

        // Same type: direct assignment (no boxing, no conversion)
        if (srcType == destType)
            return Expression.Assign(destAccess, srcAccess);

        // Directly assignable (subclass → base, etc.)
        if (destType.IsAssignableFrom(srcType))
            return Expression.Assign(destAccess, Expression.Convert(srcAccess, destType));

        // Nullable<T> → T
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

        // T → Nullable<T>
        if (destUnderlying != null
            && (srcType == destUnderlying || destUnderlying.IsAssignableFrom(srcType)))
        {
            return Expression.Assign(destAccess, Expression.Convert(srcAccess, destType));
        }

        // ── Enum ↔ string auto-conversion ────────────────────────────────────
        {
            var srcCore  = srcUnderlying  ?? srcType;
            var destCore = destUnderlying ?? destType;
            if (srcCore.IsEnum && destType == typeof(string))
                return BuildEnumToStringAssign(srcAccess, destAccess, srcUnderlying);
            if (srcType == typeof(string) && destCore.IsEnum)
                return BuildStringToEnumAssign(srcAccess, destAccess, destType, destCore);
        }

        // ── Nested reference type — try to inline child map ──────────────────
        if (!srcType.IsValueType && allTypeMaps != null)
        {
            var nested = TryBuildCtxFreeNestedAssign(srcParam, dVar, srcProp, destProp, allTypeMaps);
            if (nested != null) return nested;
        }

        if (!srcType.IsValueType)
            return null;

        // Value-type numeric conversion (int → long, float → double, etc.)
        if (destType.IsValueType)
        {
            try   { return Expression.Assign(destAccess, Expression.Convert(srcAccess, destType)); }
            catch { return null; }
        }

        return null;
    }

    /// <summary>Emits: destAccess = srcEnum.ToString() (via object.ToString for virtual dispatch).</summary>
    private static Expression BuildEnumToStringAssign(
        Expression srcAccess, Expression destAccess, Type? srcNullableUnderlying)
    {
        Expression nameExpr;
        if (srcNullableUnderlying != null)
        {
            // Nullable<TEnum> → srcAccess.HasValue ? (object)srcAccess.Value.ToString() : null
            nameExpr = Expression.Condition(
                Expression.Property(srcAccess, "HasValue"),
                Expression.Call(
                    Expression.Convert(Expression.Property(srcAccess, "Value"), typeof(object)),
                    ObjectToStringMethod),
                Expression.Constant(null, typeof(string)));
        }
        else
        {
            // TEnum → ((object)srcAccess).ToString()
            nameExpr = Expression.Call(
                Expression.Convert(srcAccess, typeof(object)),
                ObjectToStringMethod);
        }
        return Expression.Assign(destAccess, nameExpr);
    }

    /// <summary>
    /// Emits: destAccess = string.IsNullOrEmpty(src) ? default : (TEnum)Enum.Parse(typeof(TEnum), src, true)
    /// Handles both plain TEnum and Nullable&lt;TEnum&gt; destinations.
    /// </summary>
    private static Expression BuildStringToEnumAssign(
        Expression srcAccess, Expression destAccess, Type destType, Type destEnumCore)
    {
        // (TEnum)Enum.Parse(typeof(TEnum), srcStr, ignoreCase: true)
        Expression parseExpr = Expression.Convert(
            Expression.Call(EnumParseMethod,
                Expression.Constant(destEnumCore),
                srcAccess,
                Expression.Constant(true)),
            destEnumCore);

        // If dest is Nullable<TEnum>, wrap the parse result
        if (destType != destEnumCore)
            parseExpr = Expression.Convert(parseExpr, destType);

        // Guard: null or empty string → default(destType)
        var nullOrEmpty = Expression.OrElse(
            Expression.ReferenceEqual(srcAccess, Expression.Constant(null, typeof(string))),
            Expression.Equal(
                Expression.Property(srcAccess, nameof(string.Length)),
                Expression.Constant(0)));

        return Expression.Assign(destAccess,
            Expression.Condition(nullOrEmpty, Expression.Default(destType), parseExpr));
    }

    /// <summary>
    /// Builds an inlined nested object mapping for the ctx-free path.
    /// Emits: if (src.Prop != null) { var cd = new TDest(); cd.X = src.Prop.X; ... d.Prop = cd; }
    /// </summary>
    private static Expression? TryBuildCtxFreeNestedAssign(
        ParameterExpression srcParam,
        ParameterExpression dVar,
        PropertyInfo srcProp,
        PropertyInfo destProp,
        Dictionary<TypePair, TypeMap> allTypeMaps)
    {
        var srcType = srcProp.PropertyType;
        var destType = destProp.PropertyType;
        var typePair = new TypePair(srcType, destType);

        if (!allTypeMaps.TryGetValue(typePair, out var childTypeMap))
            return null;

        // Only inline simple child maps
        if (childTypeMap.BeforeMapAction != null || childTypeMap.AfterMapAction != null) return null;
        if (childTypeMap.MaxDepth > 0 || childTypeMap.BaseMapTypePair.HasValue) return null;
        if (childTypeMap.PropertyMaps.Any(pm =>
            pm.Condition != null || pm.FullCondition != null || pm.PreCondition != null
            || pm.HasNullSubstitute || pm.CustomResolver != null))
            return null;

        // Self-referencing guard
        if (srcType == destType) return null;

        var childDestDetailsCfn = TypeDetails.Get(destType);
        var childDestCtor = childDestDetailsCfn.ParameterlessCtor;
        (ConstructorInfo Ctor, ParameterInfo[] Params)? bestCtorCfn = null;
        if (childDestCtor == null && childTypeMap.CustomConstructor == null)
        {
            var childSrcDetailsCfn = TypeDetails.Get(srcType);
            bestCtorCfn = FindBestConstructor(childDestDetailsCfn, childSrcDetailsCfn);
            if (bestCtorCfn == null) return null;
        }

        var childSrcDetails = TypeDetails.Get(srcType);
        var childDestDetails = TypeDetails.Get(destType);

        var srcAccess = Expression.Property(srcParam, srcProp);
        // Hoist the source nav-property read before the null check so the getter
        // is only called once instead of twice (once for check, once for assignment).
        var childSrcVar = Expression.Variable(srcType, "ns_" + srcProp.Name);
        var childDestVar = Expression.Variable(destType, "nd_" + destProp.Name);
        var innerStmts = new List<Expression>();

        Expression newExpr;
        if (childTypeMap.CustomConstructor != null)
            newExpr = Expression.Convert(
                Expression.Invoke(Expression.Constant(childTypeMap.CustomConstructor),
                    Expression.Convert(childSrcVar, typeof(object))),
                destType);
        else if (bestCtorCfn != null)
            newExpr = Expression.New(bestCtorCfn.Value.Ctor,
                BuildCtorArgExpressions(bestCtorCfn.Value.Params, childSrcVar, childSrcDetails));
        else
            newExpr = Expression.New(childDestCtor!);
        innerStmts.Add(Expression.Assign(childDestVar, newExpr));

        var processedProps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var pm in childTypeMap.PropertyMaps)
        {
            processedProps.Add(pm.DestinationProperty.Name);
            if (pm.Ignored) continue;
            if (pm.HasUseValue)
            {
                try
                {
                    innerStmts.Add(Expression.Assign(
                        Expression.Property(childDestVar, pm.DestinationProperty),
                        Expression.Convert(Expression.Constant(pm.UseValue), pm.DestinationProperty.PropertyType)));
                }
                catch { return null; }
                continue;
            }
            if (pm.SourceMemberName != null)
            {
                childSrcDetails.ReadableByName.TryGetValue(pm.SourceMemberName, out var cSrcProp);
                if (cSrcProp == null) continue;
                var assign = TryBuildSimpleInlineAssign(childSrcVar, childDestVar, cSrcProp, pm.DestinationProperty);
                if (assign == null) return null;
                innerStmts.Add(assign);
            }
        }

        foreach (var cDestProp in childDestDetails.WritableProperties)
        {
            if (processedProps.Contains(cDestProp.Name)) continue;
            processedProps.Add(cDestProp.Name);

            childSrcDetails.ReadableByName.TryGetValue(cDestProp.Name, out var cSrcProp);
            if (cSrcProp == null) continue;

            var assign = TryBuildSimpleInlineAssign(childSrcVar, childDestVar, cSrcProp, cDestProp);
            if (assign == null) return null;
            innerStmts.Add(assign);
        }

        innerStmts.Add(Expression.Assign(Expression.Property(dVar, destProp), childDestVar));

        // var ns = src.NavProp; if (ns != null) { var nd = new TDest(); ... d.NavProp = nd; }
        return Expression.Block(
            new[] { childSrcVar },
            Expression.Assign(childSrcVar, srcAccess),
            Expression.IfThen(
                Expression.ReferenceNotEqual(
                    Expression.Convert(childSrcVar, typeof(object)),
                    Expression.Constant(null, typeof(object))),
                Expression.Block(new[] { childDestVar }, innerStmts)));
    }

    /// <summary>
    /// Builds an inlined collection mapping for the ctx-free path.
    /// Emits: if (src.Coll != null) { var list = new List&lt;T&gt;(count); for (...) list.Add(mapElem(...)); d.Coll = list; }
    /// </summary>
    private static Expression? TryBuildCtxFreeCollectionAssign(
        ParameterExpression srcParam,
        ParameterExpression dVar,
        PropertyInfo srcProp,
        PropertyInfo destProp,
        Dictionary<TypePair, TypeMap> allTypeMaps)
    {
        var srcType = srcProp.PropertyType;
        var destType = destProp.PropertyType;
        var srcElem = ReflectionHelper.GetCollectionElementType(srcType);
        var destElem = ReflectionHelper.GetCollectionElementType(destType);
        if (srcElem == null || destElem == null) return null;

        var ilistSrcType = typeof(IList<>).MakeGenericType(srcElem);
        var listDestType = typeof(List<>).MakeGenericType(destElem);
        if (!ilistSrcType.IsAssignableFrom(srcType) || !destType.IsAssignableFrom(listDestType))
            return null;

        // Build element mapping lambda: Func<TSrcElem, TDestElem>
        var elemTypePair = new TypePair(srcElem, destElem);
        Expression? elemMapExpr = null;

        if (srcElem == destElem)
        {
            // Same type — direct copy
            // No element mapping needed, just copy elements
        }
        else if (allTypeMaps.TryGetValue(elemTypePair, out var elemTypeMap))
        {
            // Build an inline element mapper using TryBuildSimpleInlineAssign
            var elemCtor = TypeDetails.Get(destElem).ParameterlessCtor;
            if (elemTypeMap.BeforeMapAction != null || elemTypeMap.AfterMapAction != null) return null;
            if (elemTypeMap.MaxDepth > 0 || elemTypeMap.BaseMapTypePair.HasValue) return null;
            if (elemTypeMap.PropertyMaps.Any(pm =>
                pm.Condition != null || pm.FullCondition != null || pm.PreCondition != null
                || pm.HasNullSubstitute || pm.CustomResolver != null))
                return null;

            // We'll build the mapping as a helper method call
            // Actually, for simplicity and maximum speed, build a Func<TSrcElem, TDestElem>
            var elemSrcParam = Expression.Parameter(srcElem, "es");
            var elemDestVar = Expression.Variable(destElem, "ed");
            var elemStmts = new List<Expression>();
            Expression newElemExpr;
            if (elemCtor != null)
                newElemExpr = Expression.New(elemCtor);
            else
            {
                var srcElemDetails2 = TypeDetails.Get(srcElem);
                var destElemDetails2 = TypeDetails.Get(destElem);
                var bestCtorCol = FindBestConstructor(destElemDetails2, srcElemDetails2);
                if (bestCtorCol == null) return null;
                newElemExpr = Expression.New(bestCtorCol.Value.Ctor,
                    BuildCtorArgExpressions(bestCtorCol.Value.Params, elemSrcParam, srcElemDetails2));
            }
            elemStmts.Add(Expression.Assign(elemDestVar, newElemExpr));

            var elemSrcDetails = TypeDetails.Get(srcElem);
            var elemDestDetails = TypeDetails.Get(destElem);
            var processed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var pm in elemTypeMap.PropertyMaps)
            {
                processed.Add(pm.DestinationProperty.Name);
                if (pm.Ignored) continue;
                if (pm.SourceMemberName != null)
                {
                    elemSrcDetails.ReadableByName.TryGetValue(pm.SourceMemberName, out var sp);
                    if (sp == null) continue;
                    var a = TryBuildSimpleInlineAssign(elemSrcParam, elemDestVar, sp, pm.DestinationProperty);
                    if (a == null) return null;
                    elemStmts.Add(a);
                }
            }

            foreach (var dp in elemDestDetails.WritableProperties)
            {
                if (processed.Contains(dp.Name)) continue;
                processed.Add(dp.Name);
                elemSrcDetails.ReadableByName.TryGetValue(dp.Name, out var sp);
                if (sp == null) continue;
                var a = TryBuildSimpleInlineAssign(elemSrcParam, elemDestVar, sp, dp);
                if (a == null) return null;
                elemStmts.Add(a);
            }

            elemStmts.Add(elemDestVar);
            var elemBody = Expression.Block(new[] { elemDestVar }, elemStmts);
            var elemFuncType = typeof(Func<,>).MakeGenericType(srcElem, destElem);
            elemMapExpr = Expression.Lambda(elemFuncType, elemBody, elemSrcParam);
        }
        else
        {
            return null; // Can't map collection elements without registered map
        }

        // Build: if (src.Coll != null) { var list = new List<T>(src.Coll.Count); for (i...) list.Add(map(src.Coll[i])); d.Coll = list; }
        var srcAccess = Expression.Property(srcParam, srcProp);
        var collVar = Expression.Variable(ilistSrcType, "col_" + srcProp.Name);
        var listVar = Expression.Variable(listDestType, "lst_" + destProp.Name);
        var iVar = Expression.Variable(typeof(int), "i_" + srcProp.Name);
        var countVar = Expression.Variable(typeof(int), "cnt_" + srcProp.Name);
        var breakLabel = Expression.Label("brk_" + srcProp.Name);

        // IList<T>.Count is inherited from ICollection<T>
        var icolType = typeof(ICollection<>).MakeGenericType(srcElem);
        var countProp = icolType.GetProperty("Count")!;
        // IList<T> indexer: this[int index]
        var itemProp = ilistSrcType.GetProperties()
            .FirstOrDefault(p => p.GetIndexParameters().Length == 1
                && p.GetIndexParameters()[0].ParameterType == typeof(int));
        if (itemProp == null) return null;
        var listCtor = listDestType.GetConstructor(new[] { typeof(int) })!;
        var addMethod = listDestType.GetMethod("Add")!;

        // Loop body: get element and add mapped version
        var elemAccess = Expression.MakeIndex(collVar, itemProp, new[] { (Expression)iVar });

        Expression addExpr;
        if (elemMapExpr != null)
        {
            // Use compiled lambda for element mapping
            var compiledElemMap = Expression.Invoke(elemMapExpr, elemAccess);
            addExpr = Expression.Call(listVar, addMethod, compiledElemMap);
        }
        else
        {
            // Same type — direct add
            addExpr = Expression.Call(listVar, addMethod, elemAccess);
        }

        var loop = Expression.Block(
            new[] { collVar, listVar, iVar, countVar },
            Expression.Assign(collVar, Expression.Convert(srcAccess, ilistSrcType)),
            Expression.Assign(countVar, Expression.Property(Expression.Convert(collVar, icolType), countProp)),
            Expression.Assign(listVar, Expression.New(listCtor, countVar)),
            Expression.Assign(iVar, Expression.Constant(0)),
            Expression.Loop(
                Expression.IfThenElse(
                    Expression.LessThan(iVar, countVar),
                    Expression.Block(
                        addExpr,
                        Expression.PostIncrementAssign(iVar)),
                    Expression.Break(breakLabel)),
                breakLabel),
            Expression.Assign(Expression.Property(dVar, destProp), Expression.Convert(listVar, destType)));

        return Expression.IfThen(
            Expression.ReferenceNotEqual(
                Expression.Convert(srcAccess, typeof(object)),
                Expression.Constant(null, typeof(object))),
            loop);
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
        if (typeMap.BeforeMapAction != null || typeMap.BeforeMapCtxAction != null) return false;
        if (typeMap.AfterMapAction != null || typeMap.AfterMapCtxAction != null) return false;
        if (typeMap.MaxDepth > 0) return false;
        if (typeMap.BaseMapTypePair.HasValue) return false;
        if (typeMap.ConvertUsingFunc != null) return false;
        if (typeMap.ShouldMapProperty != null) return false;
        if (ReflectionHelper.IsCollectionType(typeMap.SourceType)) return false;
        if (ReflectionHelper.IsCollectionType(typeMap.DestinationType)) return false;

        var srcType  = typeMap.SourceType;
        var destType = typeMap.DestinationType;

        // Require a parameterless constructor, a custom constructor, or a matched parameterized one
        var destDetailsT = TypeDetails.Get(destType);
        var defaultCtor = destDetailsT.ParameterlessCtor;
        (ConstructorInfo Ctor, ParameterInfo[] Params)? bestCtorT = null;
        if (defaultCtor == null && typeMap.CustomConstructor == null)
        {
            var srcDetailsT = TypeDetails.Get(srcType);
            bestCtorT = FindBestConstructor(destDetailsT, srcDetailsT);
            if (bestCtorT == null) return false;
        }

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

        // d = dest != null ? (TDst)dest : new TDst()  (or new TDst(matchedArgs) for records)
        Expression newDestExpr;
        if (typeMap.CustomConstructor != null)
            newDestExpr = Expression.Convert(
                Expression.Invoke(Expression.Constant(typeMap.CustomConstructor), srcParam),
                destType);
        else if (bestCtorT != null)
            newDestExpr = Expression.New(bestCtorT.Value.Ctor,
                BuildCtorArgExpressions(bestCtorT.Value.Params, sVar, srcDetails));
        else
            newDestExpr = Expression.New(defaultCtor!);

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

            // Conditions, null-substitution, custom/context resolvers, and DI resolvers
            // all need the flexible runtime path — bail out for the whole map.
            if (propMap.Condition != null || propMap.FullCondition != null
                || propMap.PreCondition != null || propMap.HasNullSubstitute
                || propMap.UseDestinationValue)
                return false;

            if (propMap.CustomResolver != null || propMap.ContextResolver != null
                || propMap.ValueResolverFactory != null) return false;

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
                srcDetails.ReadableByName.TryGetValue(propMap.SourceMemberName, out var srcProp);
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

            srcDetails.ReadableByName.TryGetValue(destProp.Name, out var srcProp);
            if (srcProp == null)
            {
                // Try to build an inline flattened assignment expression
                var flatExpr = TryBuildTypedFlattenedAssign(sVar, dVar, destProp, srcDetails);
                if (flatExpr != null)
                {
                    stmts.Add(flatExpr);
                    continue;
                }

                // If there's a flattened source but we can't inline it, bail out
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
        typeMap.MappingExpression = lambda;

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

        // ── Dictionary property ───────────────────────────────────────────────
        if (ReflectionHelper.IsDictionaryType(srcType) || ReflectionHelper.IsDictionaryType(destType))
        {
            if (!ReflectionHelper.IsDictionaryType(srcType) || !ReflectionHelper.IsDictionaryType(destType))
                return null;
            return TryBuildCtxFreeDictionaryAssign(sVar, dVar, srcProp, destProp);
        }

        // ── Collection property ───────────────────────────────────────────────
        if (ReflectionHelper.IsCollectionType(srcType) && ReflectionHelper.IsCollectionType(destType))
        {
            var srcElem  = ReflectionHelper.GetCollectionElementType(srcType);
            var destElem = ReflectionHelper.GetCollectionElementType(destType);
            if (srcElem == null || destElem == null) return null;

            var elemPair = new TypePair(srcElem, destElem);

            // Fast typed path for IList<TSrc> → List<TDest> when element mapper is available
            var ilistSrcType = typeof(IList<>).MakeGenericType(srcElem);
            var listDestType = typeof(List<>).MakeGenericType(destElem);
            if (ilistSrcType.IsAssignableFrom(srcType)
                && destType.IsAssignableFrom(listDestType)
                && compiledMaps.TryGetValue(elemPair, out var elemDel))
            {
                var typedMethod = _mapListTypedMethod.MakeGenericMethod(srcElem, destElem);
                var callExpr = Expression.Call(
                    typedMethod,
                    Expression.Convert(srcAccess, ilistSrcType),
                    Expression.Constant(elemDel),
                    ctxParam);

                return Expression.IfThen(
                    Expression.ReferenceNotEqual(
                        Expression.Convert(srcAccess, typeof(object)),
                        Expression.Constant(null, typeof(object))),
                    Expression.Assign(destAccess, Expression.Convert(callExpr, destType)));
            }

            // Fallback: untyped collection mapping
            var mapsConst   = Expression.Constant(compiledMaps);
            var pairConst   = Expression.Constant(elemPair);
            var destTConst  = Expression.Constant(destType);
            var srcEConst   = Expression.Constant(srcElem);
            var destEConst  = Expression.Constant(destElem);

            var fallbackCallExpr = Expression.Call(
                _mapCollectionPropHelperMethod,
                Expression.Convert(srcAccess, typeof(object)),
                mapsConst, pairConst, destTConst, srcEConst, destEConst,
                ctxParam);

            return Expression.IfThen(
                Expression.ReferenceNotEqual(
                    Expression.Convert(srcAccess, typeof(object)),
                    Expression.Constant(null, typeof(object))),
                Expression.Assign(destAccess, Expression.Convert(fallbackCallExpr, destType)));
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
        var typePair = new TypePair(srcType, destType);
        if (allTypeMaps.ContainsKey(typePair) && !srcType.IsValueType)
        {
            // Try to inline the child map's property assignments directly into the
            // parent expression tree — eliminates delegate call + boxing overhead.
            var inlinedExpr = TryBuildInlinedNestedAssign(
                sVar, dVar, srcProp, destProp, allTypeMaps, compiledMaps, ctxParam);
            if (inlinedExpr != null)
                return inlinedExpr;

            // Fallback: embed compiled delegate (still has boxing overhead)
            Expression callExpr;

            if (compiledMaps.TryGetValue(typePair, out var childDel))
            {
                callExpr = Expression.Invoke(
                    Expression.Constant(childDel),
                    Expression.Convert(srcAccess, typeof(object)),
                    Expression.Constant(null, typeof(object)),
                    ctxParam);
            }
            else
            {
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

    /// <summary>
    /// Builds an inlined nested object mapping — instead of calling a child delegate
    /// (which boxes src/dest), we directly emit the property assignments for the child
    /// type into the parent expression tree. This eliminates delegate overhead and boxing.
    /// Only inlines simple flat child maps (no further nesting, no collections).
    /// </summary>
    private static Expression? TryBuildInlinedNestedAssign(
        ParameterExpression sVar,
        ParameterExpression dVar,
        PropertyInfo srcProp,
        PropertyInfo destProp,
        Dictionary<TypePair, TypeMap> allTypeMaps,
        ConcurrentDictionary<TypePair, Func<object, object?, ResolutionContext, object>> compiledMaps,
        ParameterExpression ctxParam)
    {
        var srcType = srcProp.PropertyType;
        var destType = destProp.PropertyType;
        var typePair = new TypePair(srcType, destType);

        if (!allTypeMaps.TryGetValue(typePair, out var childTypeMap))
            return null;

        // Only inline simple child maps (no hooks, conditions, MaxDepth, inheritance)
        if (childTypeMap.BeforeMapAction != null) return null;
        if (childTypeMap.AfterMapAction != null) return null;
        if (childTypeMap.MaxDepth > 0) return null;
        if (childTypeMap.BaseMapTypePair.HasValue) return null;
        if (childTypeMap.PropertyMaps.Any(pm =>
            pm.Condition != null || pm.FullCondition != null || pm.PreCondition != null
            || pm.HasNullSubstitute || pm.CustomResolver != null))
            return null;

        var childDestDetailsTn = TypeDetails.Get(destType);
        var childDestCtor = childDestDetailsTn.ParameterlessCtor;
        (ConstructorInfo Ctor, ParameterInfo[] Params)? bestCtorTn = null;
        if (childDestCtor == null && childTypeMap.CustomConstructor == null)
        {
            var childSrcDetailsTn = TypeDetails.Get(srcType);
            bestCtorTn = FindBestConstructor(childDestDetailsTn, childSrcDetailsTn);
            if (bestCtorTn == null) return null;
        }

        var childSrcDetails = TypeDetails.Get(srcType);
        var childDestDetails = TypeDetails.Get(destType);

        // Hoist source nav-property read before the null check — one getter call instead of two.
        // Generates: var cs = s.Prop; if (cs != null) { var cd = new TDest(); ... d.Prop = cd; }
        var srcAccess = Expression.Property(sVar, srcProp);
        var childSrcVar = Expression.Variable(srcType, "cs_" + srcProp.Name);
        var childDestVar = Expression.Variable(destType, "cd_" + destProp.Name);

        var innerStmts = new List<Expression>();

        Expression newChildExpr;
        if (childTypeMap.CustomConstructor != null)
            newChildExpr = Expression.Convert(
                Expression.Invoke(Expression.Constant(childTypeMap.CustomConstructor),
                    Expression.Convert(childSrcVar, typeof(object))),
                destType);
        else if (bestCtorTn != null)
            newChildExpr = Expression.New(bestCtorTn.Value.Ctor,
                BuildCtorArgExpressions(bestCtorTn.Value.Params, childSrcVar, childSrcDetails));
        else
            newChildExpr = Expression.New(childDestCtor!);
        innerStmts.Add(Expression.Assign(childDestVar, newChildExpr));

        var processedProps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Process explicit property maps
        foreach (var pm in childTypeMap.PropertyMaps)
        {
            processedProps.Add(pm.DestinationProperty.Name);
            if (pm.Ignored) continue;

            if (pm.HasUseValue)
            {
                try
                {
                    innerStmts.Add(Expression.Assign(
                        Expression.Property(childDestVar, pm.DestinationProperty),
                        Expression.Convert(Expression.Constant(pm.UseValue), pm.DestinationProperty.PropertyType)));
                }
                catch { return null; }
                continue;
            }

            if (pm.SourceMemberName != null)
            {
                childSrcDetails.ReadableByName.TryGetValue(pm.SourceMemberName, out var cSrcProp);
                if (cSrcProp == null) continue;

                var assign = TryBuildSimpleInlineAssign(childSrcVar, childDestVar, cSrcProp, pm.DestinationProperty);
                if (assign == null) return null; // Property needs complex handling; bail out
                innerStmts.Add(assign);
            }
        }

        // Convention mapping for remaining properties
        foreach (var cDestProp in childDestDetails.WritableProperties)
        {
            if (processedProps.Contains(cDestProp.Name)) continue;
            processedProps.Add(cDestProp.Name);

            childSrcDetails.ReadableByName.TryGetValue(cDestProp.Name, out var cSrcProp);
            if (cSrcProp == null) continue;

            var assign = TryBuildSimpleInlineAssign(childSrcVar, childDestVar, cSrcProp, cDestProp);
            if (assign == null) return null; // Property needs complex handling; bail out
            innerStmts.Add(assign);
        }

        innerStmts.Add(Expression.Assign(Expression.Property(dVar, destProp), childDestVar));

        return Expression.Block(
            new[] { childSrcVar },
            Expression.Assign(childSrcVar, srcAccess),
            Expression.IfThen(
                Expression.ReferenceNotEqual(
                    Expression.Convert(childSrcVar, typeof(object)),
                    Expression.Constant(null, typeof(object))),
                Expression.Block(new[] { childDestVar }, innerStmts)));
    }

    /// <summary>
    /// Builds a simple typed property assignment (same type, assignable, or numeric conversion).
    /// Returns null for complex properties (collections, nested maps) — these cause the
    /// inlining to bail out and fall back to the delegate approach.
    /// </summary>
    private static Expression? TryBuildSimpleInlineAssign(
        ParameterExpression srcVar, ParameterExpression destVar,
        PropertyInfo srcProp, PropertyInfo destProp)
    {
        var srcType = srcProp.PropertyType;
        var destType = destProp.PropertyType;
        var srcAccess = Expression.Property(srcVar, srcProp);
        var destAccess = Expression.Property(destVar, destProp);

        // Collections or nested registered maps → bail
        if (ReflectionHelper.IsCollectionType(srcType) || ReflectionHelper.IsCollectionType(destType))
            return null;

        if (srcType == destType)
            return Expression.Assign(destAccess, srcAccess);

        if (destType.IsAssignableFrom(srcType))
            return Expression.Assign(destAccess, Expression.Convert(srcAccess, destType));

        var srcUnderlying = Nullable.GetUnderlyingType(srcType);
        if (srcUnderlying != null && (destType == srcUnderlying || destType.IsAssignableFrom(srcUnderlying)))
            return Expression.Assign(destAccess,
                Expression.Condition(
                    Expression.Property(srcAccess, "HasValue"),
                    Expression.Convert(Expression.Property(srcAccess, "Value"), destType),
                    Expression.Default(destType)));

        var destUnderlying = Nullable.GetUnderlyingType(destType);
        if (destUnderlying != null && (srcType == destUnderlying || destUnderlying.IsAssignableFrom(srcType)))
            return Expression.Assign(destAccess, Expression.Convert(srcAccess, destType));

        // ── Enum ↔ string auto-conversion ────────────────────────────────────
        {
            var srcCore  = srcUnderlying  ?? srcType;
            var destCore = destUnderlying ?? destType;
            if (srcCore.IsEnum && destType == typeof(string))
                return BuildEnumToStringAssign(srcAccess, destAccess, srcUnderlying);
            if (srcType == typeof(string) && destCore.IsEnum)
                return BuildStringToEnumAssign(srcAccess, destAccess, destType, destCore);
        }

        if (srcType.IsValueType && destType.IsValueType)
        {
            try { return Expression.Assign(destAccess, Expression.Convert(srcAccess, destType)); }
            catch { return null; }
        }

        // Non-simple (nested reference type, etc.) — bail out
        return null;
    }

    /// <summary>
    /// Returns the (navigation property, nested property) pair for a flattened destination
    /// property without building an expression. E.g., AddressStreet → (Address, Street).
    /// Returns null when no valid flattening is found or the type conversion is unsupported.
    /// </summary>
    private static (PropertyInfo NavProp, PropertyInfo NestedProp)? TryGetFlattenedPropInfo(
        PropertyInfo destProp,
        TypeDetails srcDetails)
    {
        var destName = destProp.Name;
        var destType = destProp.PropertyType;

        foreach (var srcProp in srcDetails.ReadableProperties)
        {
            if (!destName.StartsWith(srcProp.Name, StringComparison.OrdinalIgnoreCase)) continue;
            var remainder = destName.Substring(srcProp.Name.Length);
            if (string.IsNullOrEmpty(remainder)) continue;

            var nestedDetails = TypeDetails.Get(srcProp.PropertyType);
            nestedDetails.ReadableByName.TryGetValue(remainder, out var nestedProp);
            if (nestedProp == null) continue;

            var nestedType = nestedProp.PropertyType;
            if (!destType.IsAssignableFrom(nestedType))
            {
                if (nestedType.IsValueType && destType.IsValueType)
                    return (srcProp, nestedProp);
                continue;
            }
            return (srcProp, nestedProp);
        }
        return null;
    }

    /// <summary>
    /// Emits flattened assignments for all dest properties sourced from the same navigation
    /// property into <paramref name="stmts"/>. Reference-type navigation properties get a
    /// single null-check block with a local variable — eliminating per-property null checks.
    /// </summary>
    private static void EmitGroupedFlattenedAssigns(
        ParameterExpression srcParam,
        ParameterExpression dVar,
        PropertyInfo navProp,
        List<(PropertyInfo NestedProp, PropertyInfo DestProp)> items,
        List<Expression> stmts)
    {
        var navAccess = Expression.Property(srcParam, navProp);
        var navType = navProp.PropertyType;

        if (navType.IsValueType)
        {
            // No null check needed — just emit assignments directly
            foreach (var (nestedProp, destProp) in items)
            {
                var nestedAccess = Expression.Property(navAccess, nestedProp);
                var destAccess = Expression.Property(dVar, destProp);
                stmts.Add(BuildFlattenedAssignExpr(nestedAccess, destAccess, nestedProp.PropertyType, destProp.PropertyType));
            }
            return;
        }

        // Reference type: one null check, one local var, all assignments inside
        var navVar = Expression.Variable(navType, navProp.Name.ToLowerInvariant() + "_nav");
        var innerStmts = new List<Expression>(items.Count + 1);
        innerStmts.Add(Expression.Assign(navVar, navAccess));

        foreach (var (nestedProp, destProp) in items)
        {
            var nestedAccess = Expression.Property(navVar, nestedProp);
            var destAccess = Expression.Property(dVar, destProp);
            innerStmts.Add(BuildFlattenedAssignExpr(nestedAccess, destAccess, nestedProp.PropertyType, destProp.PropertyType));
        }

        stmts.Add(Expression.IfThen(
            Expression.ReferenceNotEqual(
                Expression.Convert(navAccess, typeof(object)),
                Expression.Constant(null, typeof(object))),
            Expression.Block(new[] { navVar }, innerStmts)));
    }

    private static Expression BuildFlattenedAssignExpr(
        Expression srcExpr, Expression destAccess, Type srcType, Type destType)
    {
        if (srcType == destType)
            return Expression.Assign(destAccess, srcExpr);
        if (destType.IsAssignableFrom(srcType))
            return Expression.Assign(destAccess, Expression.Convert(srcExpr, destType));
        // Numeric value-type conversion
        try { return Expression.Assign(destAccess, Expression.Convert(srcExpr, destType)); }
        catch { return Expression.Empty(); }
    }

    /// <summary>
    /// Builds a typed expression for a flattened property assignment in the typed path.
    /// E.g., dest.AddressStreet = src.Address?.Street
    /// </summary>
    private static Expression? TryBuildTypedFlattenedAssign(
        ParameterExpression sVar,
        ParameterExpression dVar,
        PropertyInfo destProp,
        TypeDetails srcDetails)
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

            var destType = destProp.PropertyType;
            var nestedType = nestedProp.PropertyType;

            // Only handle directly assignable types (avoid complex conversions)
            if (!destType.IsAssignableFrom(nestedType))
            {
                // Try numeric conversion
                if (nestedType.IsValueType && destType.IsValueType)
                {
                    try
                    {
                        var srcAccess = Expression.Property(sVar, srcProp);
                        var nestedAccess = Expression.Property(srcAccess, nestedProp);
                        var destAccess = Expression.Property(dVar, destProp);
                        var converted = Expression.Convert(nestedAccess, destType);

                        if (srcProp.PropertyType.IsValueType)
                            return Expression.Assign(destAccess, converted);

                        return Expression.IfThen(
                            Expression.ReferenceNotEqual(
                                Expression.Convert(srcAccess, typeof(object)),
                                Expression.Constant(null, typeof(object))),
                            Expression.Assign(destAccess, converted));
                    }
                    catch { continue; }
                }
                continue;
            }

            // Build: if (s.Address != null) d.AddressStreet = s.Address.Street;
            {
                var srcAccess = Expression.Property(sVar, srcProp);
                var nestedAccess = Expression.Property(srcAccess, nestedProp);
                var destAccess = Expression.Property(dVar, destProp);

                Expression assignExpr;
                if (nestedType == destType)
                    assignExpr = Expression.Assign(destAccess, nestedAccess);
                else
                    assignExpr = Expression.Assign(destAccess, Expression.Convert(nestedAccess, destType));

                // For value-type intermediate (rare), no null check needed
                if (srcProp.PropertyType.IsValueType)
                    return assignExpr;

                // Reference type intermediate: null-check
                return Expression.IfThen(
                    Expression.ReferenceNotEqual(
                        Expression.Convert(srcAccess, typeof(object)),
                        Expression.Constant(null, typeof(object))),
                    assignExpr);
            }
        }

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

    /// <summary>
    /// Generic typed collection mapping helper — avoids boxing for List→List mappings.
    /// Called from typed expression trees when element mapper delegate is available.
    /// </summary>
    internal static List<TDest> MapListTyped<TSrc, TDest>(
        IList<TSrc> source,
        Func<object, object?, ResolutionContext, object> elemMapper,
        ResolutionContext ctx)
    {
        var count = source.Count;
        var result = new List<TDest>(count);
        for (int i = 0; i < count; i++)
        {
            var item = source[i];
            if (item == null) { result.Add(default!); continue; }
            result.Add((TDest)elemMapper(item, null, ctx));
        }
        return result;
    }

    private static readonly MethodInfo _mapListTypedMethod =
        typeof(ExpressionBuilder).GetMethod(nameof(MapListTyped),
            BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)!;

    // ══════════════════════════════════════════════════════════════════════════
    // Ctx-free → boxed wrapper  (avoids a second Compile() call per eligible map)
    // ══════════════════════════════════════════════════════════════════════════

    private static readonly MethodInfo _wrapCtxFreeAsBoxedMethod =
        typeof(ExpressionBuilder).GetMethod(nameof(WrapCtxFreeAsBoxed),
            BindingFlags.NonPublic | BindingFlags.Static)!;

    /// <summary>
    /// Wraps a ctx-free-with-dest <c>Func&lt;TSrc,TDest,TDest&gt;</c> as a boxed
    /// <c>Func&lt;object,object?,ResolutionContext,object&gt;</c> without a second
    /// expression-tree compilation.  Passes the existing destination through when non-null
    /// so <c>Map(src, existingDest)</c> still updates in place.
    /// </summary>
    internal static Func<object, object?, ResolutionContext, object> CreateBoxedWrapper(
        Type srcType, Type destType, Delegate ctxFreeWithDest)
        => (Func<object, object?, ResolutionContext, object>)
            _wrapCtxFreeAsBoxedMethod
                .MakeGenericMethod(srcType, destType)
                .Invoke(null, new object[] { ctxFreeWithDest })!;

    private static Func<object, object?, ResolutionContext, object> WrapCtxFreeAsBoxed<TSrc, TDest>(
        Func<TSrc, TDest, TDest> ctxFree)
        => (src, dest, ctx) => ctxFree((TSrc)src, dest != null ? (TDest)dest : default!)!;

    // ══════════════════════════════════════════════════════════════════════════
    // Flexible action-array path  (full feature set, used as fallback)
    // ══════════════════════════════════════════════════════════════════════════

    private static Func<object, object?, ResolutionContext, object> BuildFlexibleDelegate(
        TypeMap typeMap,
        Dictionary<TypePair, TypeMap> allTypeMaps,
        ConcurrentDictionary<TypePair, Func<object, object?, ResolutionContext, object>> compiledMaps,
        int defaultMaxDepth = 32)
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

        var shouldMapProperty = typeMap.ShouldMapProperty;
        foreach (var destProp in destDetails.WritableProperties)
        {
            if (processedDestProps.Contains(destProp.Name)) continue;
            if (shouldMapProperty != null && !shouldMapProperty(destProp)) continue;
            processedDestProps.Add(destProp.Name);
            var action = BuildConventionAction(destProp, srcDetails, allTypeMaps, compiledMaps);
            if (action != null)
                mappingActions.Add(action);
        }

        var actionsArray = mappingActions.ToArray();
        var beforeMap = typeMap.BeforeMapAction;
        var afterMap = typeMap.AfterMapAction;
        var beforeMapCtx = typeMap.BeforeMapCtxAction;
        var afterMapCtx = typeMap.AfterMapCtxAction;
        // Per-map MaxDepth takes precedence; fall back to global default as safety net.
        var maxDepth = typeMap.MaxDepth > 0 ? typeMap.MaxDepth : defaultMaxDepth;

        return (object src, object? dest, ResolutionContext ctx) =>
        {
            if (maxDepth > 0 && ctx.Depth >= maxDepth)
                return dest!;

            var typedDest = dest ?? factory(src);

            try
            {
                beforeMap?.Invoke(src, typedDest);
                beforeMapCtx?.Invoke(src, typedDest, ctx);

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

                afterMap?.Invoke(src, typedDest);
                afterMapCtx?.Invoke(src, typedDest, ctx);
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
        if (propMap.UseDestinationValue) return null; // skip — preserve existing value

        var destProp = propMap.DestinationProperty;
        var setter = GetOrBuildSetter(destProp);

        if (propMap.HasUseValue)
        {
            var useVal = propMap.UseValue;
            return (src, dest, ctx) => setter(dest, useVal);
        }

        // DI-injected value resolver (resolved lazily from ServiceProvider)
        if (propMap.ValueResolverFactory != null)
        {
            var factory = propMap.ValueResolverFactory;
            Func<object, object?, object?, ResolutionContext, object?>? cachedResolver = null;
            var getter = propMap.SourceMemberName != null && srcDetails.ReadableByName.TryGetValue(propMap.SourceMemberName, out var resolverSrcProp)
                ? GetOrBuildGetter(resolverSrcProp)
                : null;
            var destType = destProp.PropertyType;

            return (src, dest, ctx) =>
            {
                if (ctx.ServiceProvider == null)
                    throw new InvalidOperationException(
                        "IMemberValueResolver requires DI. Use services.AddEggMapper() for dependency injection.");
                cachedResolver ??= factory(ctx.ServiceProvider);
                var val = cachedResolver(src, dest, null, ctx);
                setter(dest, ConvertValue(val, destType));
            };
        }

        // Context-aware resolver: (src, dest, destMember, ctx) => value
        if (propMap.ContextResolver != null)
        {
            var resolver = propMap.ContextResolver;
            var condition = propMap.Condition;
            var fullCondition = propMap.FullCondition;
            var preCondition = propMap.PreCondition;
            var destType = destProp.PropertyType;

            return (src, dest, ctx) =>
            {
                if (preCondition != null && !preCondition(src)) return;
                if (condition != null && !condition(src)) return;
                if (fullCondition != null && !fullCondition(src, dest)) return;

                var val = resolver(src, dest, null, ctx);
                setter(dest, ConvertValue(val, destType));
            };
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
            srcDetails.ReadableByName.TryGetValue(propMap.SourceMemberName, out var srcProp);
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
        srcDetails.ReadableByName.TryGetValue(destProp.Name, out var srcProp);

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

        // ── Dictionary property ───────────────────────────────────────────────
        if (ReflectionHelper.IsDictionaryType(srcType) || ReflectionHelper.IsDictionaryType(destType))
        {
            if (!ReflectionHelper.IsDictionaryType(srcType) || !ReflectionHelper.IsDictionaryType(destType))
                return null;
            return BuildDictionaryAction(getter, setter, srcType, destType, noGuards, compiledMaps,
                condition, fullCondition, preCondition, hasNullSub, nullSub);
        }

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
            // For self-referencing same-type reference maps (e.g. TreeNode → TreeNode with MaxDepth),
            // we need to check for a registered delegate at runtime. The delegate may not be in
            // compiledMaps yet at compile time for self-referencing types.
            // Only do this for non-primitive reference types where src == dest type — NOT for
            // string/object or base-class assignments, as those are always safe to direct-assign.
            if (!srcType.IsValueType && srcType == destType
                && srcType != typeof(string) && srcType != typeof(object))
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

                        setter(dest, srcVal);
                    };
                }

                return (src, dest, ctx) =>
                {
                    if (preCondition != null && !preCondition(src)) return;
                    if (condition != null && !condition(src)) return;
                    if (fullCondition != null && !fullCondition(src, dest)) return;

                    var srcVal = getter(src);
                    if (hasNullSub && srcVal == null) { setter(dest, nullSub); return; }

                    if (capturedMaps.TryGetValue(typePair, out var nestedDel))
                    {
                        setter(dest, nestedDel(srcVal, null, ctx));
                        return;
                    }

                    setter(dest, srcVal);
                };
            }

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

    // ══════════════════════════════════════════════════════════════════════════
    // Dictionary helpers
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Ctx-free: emits <c>dest.Prop = src.Prop == null ? null : new Dictionary&lt;K,V&gt;(src.Prop)</c>
    /// for same key+value-type dictionaries.
    /// </summary>
    private static Expression? TryBuildCtxFreeDictionaryAssign(
        ParameterExpression srcParam,
        ParameterExpression dVar,
        PropertyInfo srcProp,
        PropertyInfo destProp)
    {
        var srcType = srcProp.PropertyType;
        var destType = destProp.PropertyType;
        var (srcKeyType, srcValType) = ReflectionHelper.GetDictionaryKeyValueTypes(srcType);
        var (destKeyType, destValType) = ReflectionHelper.GetDictionaryKeyValueTypes(destType);

        if (srcKeyType != destKeyType || srcValType != destValType) return null;

        var dictConcreteType = typeof(Dictionary<,>).MakeGenericType(destKeyType, destValType);
        var srcDictIfaceType = typeof(IDictionary<,>).MakeGenericType(srcKeyType, srcValType);
        var copyCtor = dictConcreteType.GetConstructor(new[] { srcDictIfaceType });
        if (copyCtor == null) return null;

        var srcAccess = Expression.Property(srcParam, srcProp);
        var destAccess = Expression.Property(dVar, destProp);

        var srcCast = srcDictIfaceType.IsAssignableFrom(srcType)
            ? (Expression)Expression.Convert(srcAccess, srcDictIfaceType)
            : srcAccess;

        return Expression.IfThen(
            Expression.ReferenceNotEqual(
                Expression.Convert(srcAccess, typeof(object)),
                Expression.Constant(null, typeof(object))),
            Expression.Assign(destAccess,
                Expression.Convert(Expression.New(copyCtor, srcCast), destType)));
    }

    private static Action<object, object, ResolutionContext>? BuildDictionaryAction(
        Func<object, object?> getter,
        Action<object, object?> setter,
        Type srcType,
        Type destType,
        bool noGuards,
        ConcurrentDictionary<TypePair, Func<object, object?, ResolutionContext, object>> compiledMaps,
        Func<object, bool>? condition,
        Func<object, object, bool>? fullCondition,
        Func<object, bool>? preCondition,
        bool hasNullSub,
        object? nullSub)
    {
        var (srcKeyType, srcValType) = ReflectionHelper.GetDictionaryKeyValueTypes(srcType);
        var (destKeyType, destValType) = ReflectionHelper.GetDictionaryKeyValueTypes(destType);

        if (srcKeyType != destKeyType) return null;

        var destDictType = typeof(Dictionary<,>).MakeGenericType(destKeyType, destValType);
        var valPair = new TypePair(srcValType, destValType);
        var capturedMaps = compiledMaps;

        if (noGuards)
        {
            return (src, dest, ctx) =>
            {
                var srcVal = getter(src);
                if (srcVal == null) return;
                setter(dest, MapDictionaryInternal(srcVal, srcValType, destValType, destDictType, valPair, capturedMaps, ctx));
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
            setter(dest, MapDictionaryInternal(srcVal, srcValType, destValType, destDictType, valPair, capturedMaps, ctx));
        };
    }

    private static object MapDictionaryInternal(
        object srcDict,
        Type srcValType,
        Type destValType,
        Type destDictType,
        TypePair valPair,
        ConcurrentDictionary<TypePair, Func<object, object?, ResolutionContext, object>> compiledMaps,
        ResolutionContext ctx)
    {
        compiledMaps.TryGetValue(valPair, out var valMapper);
        var dest = (IDictionary)Activator.CreateInstance(destDictType)!;
        foreach (DictionaryEntry entry in (IDictionary)srcDict)
        {
            var val = entry.Value;
            object? mappedVal;
            if (valMapper != null && val != null)
                mappedVal = valMapper(val, null, ctx);
            else if (destValType.IsAssignableFrom(srcValType))
                mappedVal = val;
            else
                mappedVal = val != null ? ConvertValue(val, destValType) : null;
            dest[entry.Key] = mappedVal;
        }
        return dest;
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

        // Determine count upfront for pre-sizing when possible
        int count = source is ICollection col ? col.Count : -1;

        object? MapElement(object? elem)
        {
            if (elem == null) return null;
            if (elemMapper != null) return elemMapper(elem, null, ctx);
            if (destElemType.IsAssignableFrom(srcElemType)) return elem;
            return ConvertValue(elem, destElemType);
        }

        if (destCollectionType.IsArray)
        {
            // When source is IList, we can pre-size the array and avoid the intermediate list
            if (source is IList srcList)
            {
                var arr = Array.CreateInstance(destElemType, srcList.Count);
                for (int i = 0; i < srcList.Count; i++)
                    arr.SetValue(MapElement(srcList[i]), i);
                return arr;
            }

            var items = count >= 0 ? new List<object?>(count) : new List<object?>();
            foreach (var item in source)
                items.Add(MapElement(item));
            var arr2 = Array.CreateInstance(destElemType, items.Count);
            for (int i = 0; i < items.Count; i++)
                arr2.SetValue(items[i], i);
            return arr2;
        }

        var listType = typeof(List<>).MakeGenericType(destElemType);
        if (destCollectionType.IsAssignableFrom(listType))
        {
            var list = count >= 0
                ? (IList)Activator.CreateInstance(listType, count)!
                : (IList)Activator.CreateInstance(listType)!;

            // Use index-based loop for IList sources to avoid enumerator allocation
            if (source is IList srcList)
            {
                for (int i = 0; i < srcList.Count; i++)
                    list.Add(MapElement(srcList[i]));
                return list;
            }

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
            var list = count >= 0
                ? (IList)Activator.CreateInstance(listType, count)!
                : (IList)Activator.CreateInstance(listType)!;
            foreach (var item in source)
                list.Add(MapElement(item));
            return list;
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Constructor matching helpers (Feature 5: Record / parameterized-ctor support)
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Finds the constructor whose parameter names best match readable source
    /// properties (case-insensitive). Returns null when no non-empty constructor
    /// matches at least one source property (fall back to existing logic).
    /// </summary>
    private static (ConstructorInfo Ctor, ParameterInfo[] Params)? FindBestConstructor(
        TypeDetails destDetails, TypeDetails srcDetails)
    {
        ConstructorInfo? best = null;
        ParameterInfo[]? bestParams = null;
        int bestScore = 0; // require ≥1 matched param

        foreach (var ctor in destDetails.Constructors)
        {
            var parameters = ctor.GetParameters();
            if (parameters.Length == 0) continue;
            int score = 0;
            foreach (var p in parameters)
            {
                if (p.Name != null && srcDetails.ReadableByName.ContainsKey(p.Name))
                    score++;
            }
            if (score > bestScore)
            {
                bestScore = score;
                best = ctor;
                bestParams = parameters;
            }
        }

        return best == null ? null : (best, bestParams!);
    }

    /// <summary>
    /// Builds constructor argument expressions from source properties matched by
    /// parameter name (case-insensitive). Unmatched parameters get Expression.Default.
    /// </summary>
    private static Expression[] BuildCtorArgExpressions(
        ParameterInfo[] ctorParams, Expression typedSrc, TypeDetails srcDetails)
    {
        var args = new Expression[ctorParams.Length];
        for (int i = 0; i < ctorParams.Length; i++)
        {
            var p = ctorParams[i];
            if (p.Name != null && srcDetails.ReadableByName.TryGetValue(p.Name, out var srcProp))
            {
                var srcAccess = Expression.Property(typedSrc, srcProp);
                if (srcProp.PropertyType == p.ParameterType)
                    args[i] = srcAccess;
                else if (p.ParameterType.IsAssignableFrom(srcProp.PropertyType))
                    args[i] = Expression.Convert(srcAccess, p.ParameterType);
                else
                    args[i] = Expression.Default(p.ParameterType);
            }
            else
            {
                args[i] = Expression.Default(p.ParameterType);
            }
        }
        return args;
    }

    private static object? ConvertValue(object? value, Type targetType)
    {
        if (value == null) return null;
        var valueType = value.GetType();
        if (targetType.IsAssignableFrom(valueType)) return value;

        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

        // Enum ↔ string
        if (underlying.IsEnum && value is string strVal)
            return string.IsNullOrEmpty(strVal) ? Enum.ToObject(underlying, 0) : Enum.Parse(underlying, strVal, ignoreCase: true);
        if (valueType.IsEnum && (underlying == typeof(string) || targetType == typeof(string)))
            return value.ToString();

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
