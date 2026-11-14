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

    private static readonly ConcurrentDictionary<(Type Dest, Type Src), ConstructorInfo?> CollectionWrapperCtors = new();

    internal static ConstructorInfo? FindCollectionWrapperCtor(Type destType, Type sourceType) =>
        CollectionWrapperCtors.GetOrAdd((destType, sourceType), key =>
        {
            var direct = key.Dest.GetConstructor(new[] { key.Src });
            if (direct != null) return direct;

            var ifaces = key.Src.GetInterfaces();
            for (int i = 0; i < ifaces.Length; i++)
            {
                if (!ifaces[i].IsGenericType) continue;
                var ctor = key.Dest.GetConstructor(new[] { ifaces[i] });
                if (ctor != null) return ctor;
            }
            for (int i = 0; i < ifaces.Length; i++)
            {
                if (ifaces[i].IsGenericType) continue;
                var ctor = key.Dest.GetConstructor(new[] { ifaces[i] });
                if (ctor != null) return ctor;
            }
            return null;
        });

    private static readonly MethodInfo EnumParseMethod =
        typeof(Enum).GetMethod("Parse", new[] { typeof(Type), typeof(string), typeof(bool) })!;
    private static readonly MethodInfo ObjectToStringMethod =
        typeof(object).GetMethod("ToString", Type.EmptyTypes)!;
    private static readonly MethodInfo CreateEmptyCollectionMethod =
        typeof(ExpressionBuilder).GetMethod(nameof(CreateEmptyCollection),
            BindingFlags.NonPublic | BindingFlags.Static)!;

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
        var setter = prop.GetSetMethod()
            ?? throw new InvalidOperationException(
                $"Property '{prop.DeclaringType?.Name}.{prop.Name}' has no public setter and cannot be mapped.");
        var objParam = Expression.Parameter(typeof(object), "obj");
        var valParam = Expression.Parameter(typeof(object), "val");
        var declaringType = prop.DeclaringType!;
        Expression instance = declaringType.IsValueType
            ? Expression.Unbox(objParam, declaringType)
            : Expression.Convert(objParam, declaringType);
        var valCast = Expression.Convert(valParam, prop.PropertyType);
        var call = Expression.Call(instance, setter, valCast);
        return Expression.Lambda<Action<object, object?>>(call, objParam, valParam).Compile();
    }

    private static NewExpression NewWithoutArguments(Type type, ConstructorInfo? parameterlessCtor)
        => parameterlessCtor != null ? Expression.New(parameterlessCtor) : Expression.New(type);

    private static Func<object, object> BuildFactory(TypeMap typeMap)
    {
        if (typeMap.CustomConstructor != null)
            return typeMap.CustomConstructor;

        var destType = typeMap.DestinationType;
        var destDetails = TypeDetails.Get(destType);
        var defaultCtor = destDetails.ParameterlessCtor;

        if (destDetails.CanConstructWithoutArguments)
        {
            var srcParam = Expression.Parameter(typeof(object), "_");
            var newExpr = NewWithoutArguments(destType, defaultCtor);
            var boxed = Expression.Convert(newExpr, typeof(object));
            return Expression.Lambda<Func<object, object>>(boxed, srcParam).Compile();
        }

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
            ?? throw new MappingException($"Cannot create instance of {TypeNameHelper.Readable(destType)}");
    }

    public static Func<object, object?, ResolutionContext, object> BuildMappingDelegate(
        TypeMap typeMap,
        Dictionary<TypePair, TypeMap> allTypeMaps,
        CompiledMapStore compiledMaps,
        int defaultMaxDepth = 32,
        Dictionary<TypePair, Delegate>? globalConverters = null)
    {
        if (TryBuildTypedDelegate(typeMap, allTypeMaps, compiledMaps, out var fastDel, globalConverters))
            return fastDel!;

        return BuildFlexibleDelegate(typeMap, allTypeMaps, compiledMaps, defaultMaxDepth);
    }

    public static Delegate? TryBuildCtxFreeDelegate(
        TypeMap typeMap,
        Dictionary<TypePair, TypeMap>? allTypeMaps = null,
        Dictionary<TypePair, Delegate>? globalConverters = null)
    {
        if (typeMap.ConvertUsingFunc != null) return null;
        if (typeMap.ShouldMapProperty != null) return null;
        if (typeMap.BeforeMapAction != null) return null;
        if (typeMap.AfterMapAction  != null) return null;
        if (typeMap.BeforeMapCtxAction != null) return null;
        if (typeMap.AfterMapCtxAction  != null) return null;
        if (typeMap.MaxDepth > 0) return null;
        if (typeMap.BaseMapTypePair.HasValue) return null;
        if (typeMap.ValidationRules?.Count > 0) return null;
        if (typeMap.CustomConstructorWithCtx != null) return null;
        if (ReflectionHelper.IsCollectionType(typeMap.SourceType)) return null;
        if (ReflectionHelper.IsCollectionType(typeMap.DestinationType)) return null;

        var srcType  = typeMap.SourceType;
        var destType = typeMap.DestinationType;

        var destDetailsCf = TypeDetails.Get(destType);
        var defaultCtor = destDetailsCf.ParameterlessCtor;
        (ConstructorInfo Ctor, ParameterInfo[] Params)? bestCtorCf = null;
        if (!destDetailsCf.CanConstructWithoutArguments && typeMap.CustomConstructor == null)
        {
            var srcDetailsCf = TypeDetails.Get(srcType);
            bestCtorCf = FindBestConstructor(destDetailsCf, srcDetailsCf);
            if (bestCtorCf == null && !destType.IsValueType) return null;
        }

        var srcDetails  = TypeDetails.Get(srcType);
        var destDetails = TypeDetails.Get(destType);

        var srcParam  = Expression.Parameter(srcType,  "src");
        var destParam = Expression.Parameter(destType, "existingDest");
        var dVar      = Expression.Variable(destType, "d");

        var stmts = new List<Expression>(typeMap.PropertyMaps.Count + destDetails.WritableProperties.Length + 2);

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
            newDestExpr = NewWithoutArguments(destType, defaultCtor);

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

        foreach (var propMap in typeMap.PropertyMaps)
        {
            processedDestProps.Add(propMap.DestinationProperty.Name);
            if (propMap.Ignored) continue;

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

                var assignExpr = TryBuildCtxFreeAssign(srcParam, dVar, srcProp, propMap.DestinationProperty, allTypeMaps, globalConverters);
                if (assignExpr == null) return null;
                stmts.Add(assignExpr);
            }
        }

        Dictionary<string, (PropertyInfo NavProp, List<(PropertyInfo NestedProp, PropertyInfo DestProp)> Items)>? flatGroups = null;

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
                    flatGroups ??= new Dictionary<string, (PropertyInfo NavProp, List<(PropertyInfo NestedProp, PropertyInfo DestProp)> Items)>(StringComparer.OrdinalIgnoreCase);
                    if (!flatGroups.TryGetValue(navProp.Name, out var group))
                        flatGroups[navProp.Name] = group = (navProp, new List<(PropertyInfo, PropertyInfo)>());
                    group.Items.Add((nestedProp, destProp));
                    continue;
                }
                if (ReflectionHelper.HasFlattenedSource(destProp.Name, srcDetails))
                    return null;
                var emptyInit = BuildEnsureCollectionInitializedExpr(dVar, destProp);
                if (emptyInit != null) stmts.Add(emptyInit);
                continue;
            }

            var assignExpr = TryBuildCtxFreeAssign(srcParam, dVar, srcProp, destProp, allTypeMaps, globalConverters);
            if (assignExpr == null) return null;
            stmts.Add(assignExpr);
        }

        if (flatGroups != null)
            foreach (var g in flatGroups.Values)
                EmitGroupedFlattenedAssigns(srcParam, dVar, g.NavProp, g.Items, stmts);

        stmts.Add(dVar);

        var body     = Expression.Block(new[] { dVar }, stmts);
        var funcType = typeof(Func<,,>).MakeGenericType(srcType, destType, destType);
        var lambda = Expression.Lambda(funcType, body, srcParam, destParam);
        typeMap.MappingExpression = lambda;
        return lambda.Compile();
    }

    public static Delegate? TryBuildCtxFreeListDelegate(
        TypeMap elemTypeMap,
        Dictionary<TypePair, TypeMap>? allTypeMaps = null,
        Dictionary<TypePair, Delegate>? globalConverters = null)
    {
        var srcElem = elemTypeMap.SourceType;
        var destElem = elemTypeMap.DestinationType;

        if (elemTypeMap.BeforeMapAction != null || elemTypeMap.AfterMapAction != null) return null;
        if (elemTypeMap.MaxDepth > 0 || elemTypeMap.BaseMapTypePair.HasValue) return null;
        if (ReflectionHelper.IsCollectionType(srcElem) || ReflectionHelper.IsCollectionType(destElem)) return null;

        var destElemDetails = TypeDetails.Get(destElem);
        var destCtor = destElemDetails.ParameterlessCtor;
        (ConstructorInfo Ctor, ParameterInfo[] Params)? bestCtorList = null;
        if (!destElemDetails.CanConstructWithoutArguments && elemTypeMap.CustomConstructor == null)
        {
            var srcElemDetails = TypeDetails.Get(srcElem);
            bestCtorList = FindBestConstructor(destElemDetails, srcElemDetails);
            if (bestCtorList == null && !destElem.IsValueType) return null;
        }

        var srcDetails = TypeDetails.Get(srcElem);
        var destDetails = TypeDetails.Get(destElem);

        var elemSrcParam = Expression.Parameter(srcElem, "es");
        var elemDestVar = Expression.Variable(destElem, "ed");
        var elemStmts = new List<Expression>(elemTypeMap.PropertyMaps.Count + destDetails.WritableProperties.Length + 2);

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
            newDestExpr = NewWithoutArguments(destElem, destCtor);
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
                var a = TryBuildCtxFreeAssign(elemSrcParam, elemDestVar, sp, propMap.DestinationProperty, allTypeMaps, globalConverters);
                if (a == null) return null;
                elemStmts.Add(a);
            }
        }

        Dictionary<string, (PropertyInfo NavProp, List<(PropertyInfo NestedProp, PropertyInfo DestProp)> Items)>? elemFlatGroups = null;

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
                    elemFlatGroups ??= new Dictionary<string, (PropertyInfo NavProp, List<(PropertyInfo NestedProp, PropertyInfo DestProp)> Items)>(StringComparer.OrdinalIgnoreCase);
                    if (!elemFlatGroups.TryGetValue(navProp.Name, out var group))
                        elemFlatGroups[navProp.Name] = group = (navProp, new List<(PropertyInfo, PropertyInfo)>());
                    group.Items.Add((nestedProp, destProp));
                    continue;
                }
                if (ReflectionHelper.HasFlattenedSource(destProp.Name, srcDetails)) return null;
                continue;
            }

            var assign = TryBuildCtxFreeAssign(elemSrcParam, elemDestVar, srcProp, destProp, allTypeMaps, globalConverters);
            if (assign == null) return null;
            elemStmts.Add(assign);
        }

        if (elemFlatGroups != null)
            foreach (var g in elemFlatGroups.Values)
                EmitGroupedFlattenedAssigns(elemSrcParam, elemDestVar, g.NavProp, g.Items, elemStmts);

        elemStmts.Add(elemDestVar);

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

        var continueLabel = Expression.Label("cont");
        if (!srcElem.IsValueType)
        {
            var nullAddDefault = Expression.Block(
                Expression.Call(listVar, addMethod, Expression.Default(destElem)),
                Expression.PostIncrementAssign(iVar),
                Expression.Goto(continueLabel));
            loopBodyStmts.Add(Expression.IfThen(
                Expression.ReferenceEqual(
                    Expression.Convert(elemSrcParam, typeof(object)),
                    Expression.Constant(null, typeof(object))),
                nullAddDefault));
        }

        for (int idx = 0; idx < elemStmts.Count - 1; idx++)
            loopBodyStmts.Add(elemStmts[idx]);
        loopBodyStmts.Add(Expression.Call(listVar, addMethod, elemDestVar));
        loopBodyStmts.Add(Expression.PostIncrementAssign(iVar));
        loopBodyStmts.Add(Expression.Label(continueLabel));

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

    private static Expression? TryBuildCtxFreeAssign(
        ParameterExpression srcParam,
        ParameterExpression dVar,
        PropertyInfo srcProp,
        PropertyInfo destProp,
        Dictionary<TypePair, TypeMap>? allTypeMaps = null,
        Dictionary<TypePair, Delegate>? globalConverters = null)
    {
        var srcType    = srcProp.PropertyType;
        var destType   = destProp.PropertyType;
        var srcAccess  = Expression.Property(srcParam, srcProp);
        var destAccess = Expression.Property(dVar, destProp);

        if (ReflectionHelper.IsCollectionType(srcType) && ReflectionHelper.IsCollectionType(destType))
        {
            if (allTypeMaps == null) return null;
            return TryBuildCtxFreeCollectionAssign(srcParam, dVar, srcProp, destProp, allTypeMaps, globalConverters);
        }
        if (ReflectionHelper.IsCollectionType(srcType) || ReflectionHelper.IsCollectionType(destType))
            return null;

        if (ReflectionHelper.IsDictionaryType(srcType) || ReflectionHelper.IsDictionaryType(destType))
        {
            if (!ReflectionHelper.IsDictionaryType(srcType) || !ReflectionHelper.IsDictionaryType(destType))
                return null;
            return TryBuildCtxFreeDictionaryAssign(srcParam, dVar, srcProp, destProp);
        }

        if (srcType == destType)
            return Expression.Assign(destAccess, srcAccess);

        if (globalConverters != null)
        {
            var converterPair = new TypePair(srcType, destType);
            if (globalConverters.TryGetValue(converterPair, out var converterDel))
            {
                var funcType = typeof(Func<,>).MakeGenericType(srcType, destType);
                return Expression.Assign(destAccess,
                    Expression.Invoke(Expression.Constant(converterDel, funcType), srcAccess));
            }
        }

        if (destType.IsAssignableFrom(srcType))
            return Expression.Assign(destAccess, Expression.Convert(srcAccess, destType));

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

        if (destUnderlying != null
            && (srcType == destUnderlying || destUnderlying.IsAssignableFrom(srcType)))
        {
            return Expression.Assign(destAccess, Expression.Convert(srcAccess, destType));
        }

        {
            var srcCore  = srcUnderlying  ?? srcType;
            var destCore = destUnderlying ?? destType;
            if (srcCore.IsEnum && destType == typeof(string))
                return BuildEnumToStringAssign(srcAccess, destAccess, srcUnderlying);
            if (srcType == typeof(string) && destCore.IsEnum)
                return BuildStringToEnumAssign(srcAccess, destAccess, destType, destCore);
        }

        if (!srcType.IsValueType && allTypeMaps != null)
        {
            var nested = TryBuildCtxFreeNestedAssign(srcParam, dVar, srcProp, destProp, allTypeMaps, globalConverters);
            if (nested != null) return nested;
        }

        if (!srcType.IsValueType)
            return null;

        if (destType.IsValueType)
        {
            try   { return Expression.Assign(destAccess, Expression.Convert(srcAccess, destType)); }
            catch { return null; }
        }

        return null;
    }

    private static Expression BuildEnumToStringAssign(
        Expression srcAccess, Expression destAccess, Type? srcNullableUnderlying)
    {
        Expression nameExpr;
        if (srcNullableUnderlying != null)
        {
            nameExpr = Expression.Condition(
                Expression.Property(srcAccess, "HasValue"),
                Expression.Call(
                    Expression.Convert(Expression.Property(srcAccess, "Value"), typeof(object)),
                    ObjectToStringMethod),
                Expression.Constant(null, typeof(string)));
        }
        else
        {
            nameExpr = Expression.Call(
                Expression.Convert(srcAccess, typeof(object)),
                ObjectToStringMethod);
        }
        return Expression.Assign(destAccess, nameExpr);
    }

    private static Expression BuildStringToEnumAssign(
        Expression srcAccess, Expression destAccess, Type destType, Type destEnumCore)
    {
        Expression parseExpr = Expression.Convert(
            Expression.Call(EnumParseMethod,
                Expression.Constant(destEnumCore),
                srcAccess,
                Expression.Constant(true)),
            destEnumCore);

        if (destType != destEnumCore)
            parseExpr = Expression.Convert(parseExpr, destType);

        var nullOrEmpty = Expression.OrElse(
            Expression.ReferenceEqual(srcAccess, Expression.Constant(null, typeof(string))),
            Expression.Equal(
                Expression.Property(srcAccess, nameof(string.Length)),
                Expression.Constant(0)));

        return Expression.Assign(destAccess,
            Expression.Condition(nullOrEmpty, Expression.Default(destType), parseExpr));
    }

    private static Expression? TryBuildCtxFreeNestedAssign(
        ParameterExpression srcParam,
        ParameterExpression dVar,
        PropertyInfo srcProp,
        PropertyInfo destProp,
        Dictionary<TypePair, TypeMap> allTypeMaps,
        Dictionary<TypePair, Delegate>? globalConverters = null)
    {
        var srcType = srcProp.PropertyType;
        var destType = destProp.PropertyType;
        var typePair = new TypePair(srcType, destType);

        if (!allTypeMaps.TryGetValue(typePair, out var childTypeMap))
            return null;

        if (childTypeMap.BeforeMapAction != null || childTypeMap.AfterMapAction != null) return null;
        if (childTypeMap.MaxDepth > 0 || childTypeMap.BaseMapTypePair.HasValue) return null;
        for (int i = 0; i < childTypeMap.PropertyMaps.Count; i++)
        {
            var pm = childTypeMap.PropertyMaps[i];
            if (pm.Condition != null || pm.FullCondition != null || pm.PreCondition != null
                || pm.HasNullSubstitute || pm.CustomResolver != null)
                return null;
        }

        if (srcType == destType) return null;

        var childDestDetailsCfn = TypeDetails.Get(destType);
        var childDestCtor = childDestDetailsCfn.ParameterlessCtor;
        (ConstructorInfo Ctor, ParameterInfo[] Params)? bestCtorCfn = null;
        if (!childDestDetailsCfn.CanConstructWithoutArguments && childTypeMap.CustomConstructor == null)
        {
            var childSrcDetailsCfn = TypeDetails.Get(srcType);
            bestCtorCfn = FindBestConstructor(childDestDetailsCfn, childSrcDetailsCfn);
            if (bestCtorCfn == null && !destType.IsValueType) return null;
        }

        var childSrcDetails = TypeDetails.Get(srcType);
        var childDestDetails = TypeDetails.Get(destType);

        var srcAccess = Expression.Property(srcParam, srcProp);
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
            newExpr = NewWithoutArguments(destType, childDestCtor);
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
                var assign = TryBuildSimpleInlineAssign(childSrcVar, childDestVar, cSrcProp, pm.DestinationProperty, globalConverters);
                if (assign == null) return null;
                innerStmts.Add(assign);
            }
        }

        foreach (var cDestProp in childDestDetails.WritableProperties)
        {
            if (processedProps.Contains(cDestProp.Name)) continue;
            processedProps.Add(cDestProp.Name);

            childSrcDetails.ReadableByName.TryGetValue(cDestProp.Name, out var cSrcProp);
            if (cSrcProp == null)
            {
                var emptyInit = BuildEnsureCollectionInitializedExpr(childDestVar, cDestProp);
                if (emptyInit != null) innerStmts.Add(emptyInit);
                continue;
            }

            var assign = TryBuildSimpleInlineAssign(childSrcVar, childDestVar, cSrcProp, cDestProp, globalConverters);
            if (assign == null) return null;
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

    private static Expression? TryBuildCtxFreeCollectionAssign(
        ParameterExpression srcParam,
        ParameterExpression dVar,
        PropertyInfo srcProp,
        PropertyInfo destProp,
        Dictionary<TypePair, TypeMap> allTypeMaps,
        Dictionary<TypePair, Delegate>? globalConverters = null)
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

        var elemTypePair = new TypePair(srcElem, destElem);
        Expression? elemMapExpr = null;

        if (srcElem == destElem)
        {
        }
        else if (allTypeMaps.TryGetValue(elemTypePair, out var elemTypeMap))
        {
            var elemCtor = TypeDetails.Get(destElem).ParameterlessCtor;
            if (elemTypeMap.BeforeMapAction != null || elemTypeMap.AfterMapAction != null) return null;
            if (elemTypeMap.MaxDepth > 0 || elemTypeMap.BaseMapTypePair.HasValue) return null;
            for (int i = 0; i < elemTypeMap.PropertyMaps.Count; i++)
            {
                var pm = elemTypeMap.PropertyMaps[i];
                if (pm.Condition != null || pm.FullCondition != null || pm.PreCondition != null
                    || pm.HasNullSubstitute || pm.CustomResolver != null)
                    return null;
            }

            var elemSrcParam = Expression.Parameter(srcElem, "es");
            var elemDestVar = Expression.Variable(destElem, "ed");
            var elemStmts = new List<Expression>();
            Expression newElemExpr;
            if (TypeDetails.Get(destElem).CanConstructWithoutArguments)
                newElemExpr = NewWithoutArguments(destElem, elemCtor);
            else
            {
                var srcElemDetails2 = TypeDetails.Get(srcElem);
                var destElemDetails2 = TypeDetails.Get(destElem);
                var bestCtorCol = FindBestConstructor(destElemDetails2, srcElemDetails2);
                if (bestCtorCol == null && !destElem.IsValueType) return null;
                newElemExpr = bestCtorCol == null
                    ? Expression.New(destElem)
                    : Expression.New(bestCtorCol.Value.Ctor,
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
                    var a = TryBuildSimpleInlineAssign(elemSrcParam, elemDestVar, sp, pm.DestinationProperty, globalConverters);
                    if (a == null) return null;
                    elemStmts.Add(a);
                }
            }

            foreach (var dp in elemDestDetails.WritableProperties)
            {
                if (processed.Contains(dp.Name)) continue;
                processed.Add(dp.Name);
                elemSrcDetails.ReadableByName.TryGetValue(dp.Name, out var sp);
                if (sp == null)
                {
                    var emptyInit = BuildEnsureCollectionInitializedExpr(elemDestVar, dp);
                    if (emptyInit != null) elemStmts.Add(emptyInit);
                    continue;
                }
                var a = TryBuildSimpleInlineAssign(elemSrcParam, elemDestVar, sp, dp, globalConverters);
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
            return null;
        }

        var srcAccess = Expression.Property(srcParam, srcProp);
        var collVar = Expression.Variable(ilistSrcType, "col_" + srcProp.Name);
        var listVar = Expression.Variable(listDestType, "lst_" + destProp.Name);
        var iVar = Expression.Variable(typeof(int), "i_" + srcProp.Name);
        var countVar = Expression.Variable(typeof(int), "cnt_" + srcProp.Name);
        var breakLabel = Expression.Label("brk_" + srcProp.Name);

        var icolType = typeof(ICollection<>).MakeGenericType(srcElem);
        var countProp = icolType.GetProperty("Count")!;
        var itemProp = _ilistIndexerCache.GetOrAdd(ilistSrcType, t =>
        {
            var props = t.GetProperties();
            for (int pi = 0; pi < props.Length; pi++)
            {
                var idxParams = props[pi].GetIndexParameters();
                if (idxParams.Length == 1 && idxParams[0].ParameterType == typeof(int))
                    return props[pi];
            }
            return null;
        });
        if (itemProp == null) return null;
        var listCtor = listDestType.GetConstructor(new[] { typeof(int) })!;
        var addMethod = listDestType.GetMethod("Add")!;

        var elemAccess = Expression.MakeIndex(collVar, itemProp, new[] { (Expression)iVar });

        Expression addExpr;
        if (elemMapExpr != null)
        {
            var compiledElemMap = Expression.Invoke(elemMapExpr, elemAccess);
            addExpr = Expression.Call(listVar, addMethod, compiledElemMap);
        }
        else
        {
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

        return Expression.IfThenElse(
            Expression.ReferenceNotEqual(
                Expression.Convert(srcAccess, typeof(object)),
                Expression.Constant(null, typeof(object))),
            loop,
            Expression.Assign(Expression.Property(dVar, destProp),
                BuildEmptyCollectionExpr(destType, destElem)));
    }

    private static bool TryBuildTypedDelegate(
        TypeMap typeMap,
        Dictionary<TypePair, TypeMap> allTypeMaps,
        CompiledMapStore compiledMaps,
        out Func<object, object?, ResolutionContext, object>? result,
        Dictionary<TypePair, Delegate>? globalConverters = null)
    {
        result = null;

        if (typeMap.BeforeMapAction != null || typeMap.BeforeMapCtxAction != null) return false;
        if (typeMap.AfterMapAction != null || typeMap.AfterMapCtxAction != null) return false;
        if (typeMap.MaxDepth > 0) return false;
        if (typeMap.BaseMapTypePair.HasValue) return false;
        if (typeMap.ConvertUsingFunc != null) return false;
        if (typeMap.ShouldMapProperty != null) return false;
        if (typeMap.ValidationRules?.Count > 0) return false;
        if (typeMap.CustomConstructorWithCtx != null) return false;
        if (ReflectionHelper.IsCollectionType(typeMap.SourceType)) return false;
        if (ReflectionHelper.IsCollectionType(typeMap.DestinationType)) return false;

        var srcType  = typeMap.SourceType;
        var destType = typeMap.DestinationType;

        var destDetailsT = TypeDetails.Get(destType);
        var defaultCtor = destDetailsT.ParameterlessCtor;
        (ConstructorInfo Ctor, ParameterInfo[] Params)? bestCtorT = null;
        if (!destDetailsT.CanConstructWithoutArguments && typeMap.CustomConstructor == null)
        {
            var srcDetailsT = TypeDetails.Get(srcType);
            bestCtorT = FindBestConstructor(destDetailsT, srcDetailsT);
            if (bestCtorT == null && !destType.IsValueType) return false;
        }

        var srcDetails  = TypeDetails.Get(srcType);
        var destDetails = TypeDetails.Get(destType);

        var srcParam  = Expression.Parameter(typeof(object), "src");
        var destParam = Expression.Parameter(typeof(object), "dest");
        var ctxParam  = Expression.Parameter(typeof(ResolutionContext), "ctx");

        var sVar = Expression.Variable(srcType,  "s");
        var dVar = Expression.Variable(destType, "d");

        var stmts = new List<Expression>(typeMap.PropertyMaps.Count + destDetails.WritableProperties.Length + 3);

        stmts.Add(Expression.Assign(sVar, Expression.Convert(srcParam, srcType)));

        Expression newDestExpr;
        if (typeMap.CustomConstructor != null)
            newDestExpr = Expression.Convert(
                Expression.Invoke(Expression.Constant(typeMap.CustomConstructor), srcParam),
                destType);
        else if (bestCtorT != null)
            newDestExpr = Expression.New(bestCtorT.Value.Ctor,
                BuildCtorArgExpressions(bestCtorT.Value.Params, sVar, srcDetails));
        else
            newDestExpr = NewWithoutArguments(destType, defaultCtor);

        stmts.Add(Expression.Assign(dVar,
            Expression.Condition(
                Expression.ReferenceNotEqual(destParam, Expression.Constant(null, typeof(object))),
                Expression.Convert(destParam, destType),
                newDestExpr)));

        var processedDestProps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var propMap in typeMap.PropertyMaps)
        {
            processedDestProps.Add(propMap.DestinationProperty.Name);
            if (propMap.Ignored) continue;

            if (propMap.Condition != null || propMap.FullCondition != null
                || propMap.PreCondition != null || propMap.HasNullSubstitute
                || propMap.UseDestinationValue)
                return false;

            if (propMap.CustomResolver != null || propMap.ContextResolver != null
                || propMap.ValueResolverFactory != null) return false;

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
                catch { return false; }
                continue;
            }

            if (propMap.SourceMemberName != null)
            {
                srcDetails.ReadableByName.TryGetValue(propMap.SourceMemberName, out var srcProp);
                if (srcProp == null) continue;

                var assignExpr = TryBuildTypedAssign(
                    sVar, dVar, srcProp, propMap.DestinationProperty,
                    allTypeMaps, compiledMaps, ctxParam, globalConverters);
                if (assignExpr == null) return false;
                stmts.Add(assignExpr);
            }
        }

        foreach (var destProp in destDetails.WritableProperties)
        {
            if (processedDestProps.Contains(destProp.Name)) continue;
            processedDestProps.Add(destProp.Name);

            srcDetails.ReadableByName.TryGetValue(destProp.Name, out var srcProp);
            if (srcProp == null)
            {
                var flatExpr = TryBuildTypedFlattenedAssign(sVar, dVar, destProp, srcDetails);
                if (flatExpr != null)
                {
                    stmts.Add(flatExpr);
                    continue;
                }

                if (ReflectionHelper.HasFlattenedSource(destProp.Name, srcDetails))
                    return false;
                var emptyInit = BuildEnsureCollectionInitializedExpr(dVar, destProp);
                if (emptyInit != null) stmts.Add(emptyInit);
                continue;
            }

            var assignExpr = TryBuildTypedAssign(
                sVar, dVar, srcProp, destProp, allTypeMaps, compiledMaps, ctxParam, globalConverters);
            if (assignExpr == null) return false;
            stmts.Add(assignExpr);
        }

        stmts.Add(Expression.Convert(dVar, typeof(object)));

        var body   = Expression.Block(new[] { sVar, dVar }, stmts);
        var lambda = Expression.Lambda<Func<object, object?, ResolutionContext, object>>(
            body, srcParam, destParam, ctxParam);
        typeMap.MappingExpression = lambda;

        result = lambda.Compile();
        return true;
    }

    private static Expression? TryBuildTypedAssign(
        ParameterExpression sVar,
        ParameterExpression dVar,
        PropertyInfo srcProp,
        PropertyInfo destProp,
        Dictionary<TypePair, TypeMap> allTypeMaps,
        CompiledMapStore compiledMaps,
        ParameterExpression ctxParam,
        Dictionary<TypePair, Delegate>? globalConverters = null)
    {
        var srcType    = srcProp.PropertyType;
        var destType   = destProp.PropertyType;
        var srcAccess  = Expression.Property(sVar, srcProp);
        var destAccess = Expression.Property(dVar, destProp);

        if (ReflectionHelper.IsDictionaryType(srcType) || ReflectionHelper.IsDictionaryType(destType))
        {
            if (!ReflectionHelper.IsDictionaryType(srcType) || !ReflectionHelper.IsDictionaryType(destType))
                return null;
            return TryBuildCtxFreeDictionaryAssign(sVar, dVar, srcProp, destProp);
        }

        if (ReflectionHelper.IsCollectionType(srcType) && ReflectionHelper.IsCollectionType(destType))
        {
            var srcElem  = ReflectionHelper.GetCollectionElementType(srcType);
            var destElem = ReflectionHelper.GetCollectionElementType(destType);
            if (srcElem == null || destElem == null) return null;

            var elemPair = new TypePair(srcElem, destElem);

            var ilistSrcType = typeof(IList<>).MakeGenericType(srcElem);
            var listDestType = typeof(List<>).MakeGenericType(destElem);
            if (ilistSrcType.IsAssignableFrom(srcType)
                && destType.IsAssignableFrom(listDestType)
                && compiledMaps.TryGetOrCompileForBuild(elemPair, out var elemDel))
            {
                var typedMethod = _mapListTypedMethod.MakeGenericMethod(srcElem, destElem);
                var callExpr = Expression.Call(
                    typedMethod,
                    Expression.Convert(srcAccess, ilistSrcType),
                    Expression.Constant(elemDel),
                    ctxParam);

                return Expression.IfThenElse(
                    Expression.ReferenceNotEqual(
                        Expression.Convert(srcAccess, typeof(object)),
                        Expression.Constant(null, typeof(object))),
                    Expression.Assign(destAccess, Expression.Convert(callExpr, destType)),
                    Expression.Assign(destAccess, BuildEmptyCollectionExpr(destType, destElem)));
            }

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

            return Expression.IfThenElse(
                Expression.ReferenceNotEqual(
                    Expression.Convert(srcAccess, typeof(object)),
                    Expression.Constant(null, typeof(object))),
                Expression.Assign(destAccess, Expression.Convert(fallbackCallExpr, destType)),
                Expression.Assign(destAccess, BuildEmptyCollectionExpr(destType, destElem)));
        }

        if (srcType == destType)
            return Expression.Assign(destAccess, srcAccess);

        if (globalConverters != null)
        {
            var converterPair = new TypePair(srcType, destType);
            if (globalConverters.TryGetValue(converterPair, out var converterDel))
            {
                var funcType = typeof(Func<,>).MakeGenericType(srcType, destType);
                return Expression.Assign(destAccess,
                    Expression.Invoke(Expression.Constant(converterDel, funcType), srcAccess));
            }
        }

        if (destType.IsAssignableFrom(srcType))
            return Expression.Assign(destAccess, Expression.Convert(srcAccess, destType));

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

        if (destUnderlying != null
            && (srcType == destUnderlying || destUnderlying.IsAssignableFrom(srcType)))
        {
            return Expression.Assign(destAccess, Expression.Convert(srcAccess, destType));
        }

        var typePair = new TypePair(srcType, destType);
        if (allTypeMaps.ContainsKey(typePair) && !srcType.IsValueType)
        {
            var inlinedExpr = TryBuildInlinedNestedAssign(
                sVar, dVar, srcProp, destProp, allTypeMaps, compiledMaps, ctxParam, globalConverters);
            if (inlinedExpr != null)
                return inlinedExpr;

            Expression callExpr;

            if (compiledMaps.TryGetOrCompileForBuild(typePair, out var childDel))
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

        if (srcType.IsValueType && destType.IsValueType)
        {
            try   { return Expression.Assign(destAccess, Expression.Convert(srcAccess, destType)); }
            catch { return null; }
        }

        return null;
    }

    private static Expression? TryBuildInlinedNestedAssign(
        ParameterExpression sVar,
        ParameterExpression dVar,
        PropertyInfo srcProp,
        PropertyInfo destProp,
        Dictionary<TypePair, TypeMap> allTypeMaps,
        CompiledMapStore compiledMaps,
        ParameterExpression ctxParam,
        Dictionary<TypePair, Delegate>? globalConverters = null)
    {
        var srcType = srcProp.PropertyType;
        var destType = destProp.PropertyType;
        var typePair = new TypePair(srcType, destType);

        if (!allTypeMaps.TryGetValue(typePair, out var childTypeMap))
            return null;

        if (childTypeMap.BeforeMapAction != null) return null;
        if (childTypeMap.AfterMapAction != null) return null;
        if (childTypeMap.MaxDepth > 0) return null;
        if (childTypeMap.BaseMapTypePair.HasValue) return null;
        for (int i = 0; i < childTypeMap.PropertyMaps.Count; i++)
        {
            var pm = childTypeMap.PropertyMaps[i];
            if (pm.Condition != null || pm.FullCondition != null || pm.PreCondition != null
                || pm.HasNullSubstitute || pm.CustomResolver != null)
                return null;
        }

        var childDestDetailsTn = TypeDetails.Get(destType);
        var childDestCtor = childDestDetailsTn.ParameterlessCtor;
        (ConstructorInfo Ctor, ParameterInfo[] Params)? bestCtorTn = null;
        if (!childDestDetailsTn.CanConstructWithoutArguments && childTypeMap.CustomConstructor == null)
        {
            var childSrcDetailsTn = TypeDetails.Get(srcType);
            bestCtorTn = FindBestConstructor(childDestDetailsTn, childSrcDetailsTn);
            if (bestCtorTn == null && !destType.IsValueType) return null;
        }

        var childSrcDetails = TypeDetails.Get(srcType);
        var childDestDetails = TypeDetails.Get(destType);

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
            newChildExpr = NewWithoutArguments(destType, childDestCtor);
        innerStmts.Add(Expression.Assign(childDestVar, newChildExpr));

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

                var assign = TryBuildSimpleInlineAssign(childSrcVar, childDestVar, cSrcProp, pm.DestinationProperty, globalConverters);
                if (assign == null) return null;
                innerStmts.Add(assign);
            }
        }

        foreach (var cDestProp in childDestDetails.WritableProperties)
        {
            if (processedProps.Contains(cDestProp.Name)) continue;
            processedProps.Add(cDestProp.Name);

            childSrcDetails.ReadableByName.TryGetValue(cDestProp.Name, out var cSrcProp);
            if (cSrcProp == null)
            {
                var emptyInit = BuildEnsureCollectionInitializedExpr(childDestVar, cDestProp);
                if (emptyInit != null) innerStmts.Add(emptyInit);
                continue;
            }

            var assign = TryBuildSimpleInlineAssign(childSrcVar, childDestVar, cSrcProp, cDestProp, globalConverters);
            if (assign == null) return null;
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

    private static Expression? TryBuildSimpleInlineAssign(
        ParameterExpression srcVar, ParameterExpression destVar,
        PropertyInfo srcProp, PropertyInfo destProp,
        Dictionary<TypePair, Delegate>? globalConverters = null)
    {
        var srcType = srcProp.PropertyType;
        var destType = destProp.PropertyType;
        var srcAccess = Expression.Property(srcVar, srcProp);
        var destAccess = Expression.Property(destVar, destProp);

        if (ReflectionHelper.IsCollectionType(srcType) || ReflectionHelper.IsCollectionType(destType))
            return null;

        if (srcType == destType)
            return Expression.Assign(destAccess, srcAccess);

        if (globalConverters != null)
        {
            var converterPair = new TypePair(srcType, destType);
            if (globalConverters.TryGetValue(converterPair, out var converterDel))
            {
                var funcType = typeof(Func<,>).MakeGenericType(srcType, destType);
                return Expression.Assign(destAccess,
                    Expression.Invoke(Expression.Constant(converterDel, funcType), srcAccess));
            }
        }

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

        return null;
    }

    private static List<PropertyInfo>? TryGetFlattenedPropertyChain(
        string destName,
        Type destType,
        TypeDetails srcDetails)
    {
        foreach (var srcProp in srcDetails.ReadableProperties)
        {
            if (!destName.StartsWith(srcProp.Name, StringComparison.OrdinalIgnoreCase)) continue;
            var remainder = destName.Substring(srcProp.Name.Length);
            if (string.IsNullOrEmpty(remainder)) continue;

            var nestedDetails = TypeDetails.Get(srcProp.PropertyType);

            if (nestedDetails.ReadableByName.TryGetValue(remainder, out var nestedProp))
            {
                var nestedType = nestedProp.PropertyType;
                if (destType.IsAssignableFrom(nestedType) || (nestedType.IsValueType && destType.IsValueType))
                    return new List<PropertyInfo> { srcProp, nestedProp };
            }

            var deepChain = TryGetFlattenedPropertyChain(remainder, destType, nestedDetails);
            if (deepChain != null)
            {
                deepChain.Insert(0, srcProp);
                return deepChain;
            }
        }
        return null;
    }

    private static (PropertyInfo NavProp, PropertyInfo NestedProp)? TryGetFlattenedPropInfo(
        PropertyInfo destProp,
        TypeDetails srcDetails)
    {
        var chain = TryGetFlattenedPropertyChain(destProp.Name, destProp.PropertyType, srcDetails);
        if (chain == null || chain.Count < 2) return null;
        if (chain.Count == 2) return (chain[0], chain[1]);
        return null;
    }

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
            foreach (var (nestedProp, destProp) in items)
            {
                var nestedAccess = Expression.Property(navAccess, nestedProp);
                var destAccess = Expression.Property(dVar, destProp);
                stmts.Add(BuildFlattenedAssignExpr(nestedAccess, destAccess, nestedProp.PropertyType, destProp.PropertyType));
            }
            return;
        }

        var navVar = Expression.Variable(navType, navProp.Name.ToLowerInvariant() + "_nav");
        var innerStmts = new List<Expression>(items.Count);

        foreach (var (nestedProp, destProp) in items)
        {
            var nestedAccess = Expression.Property(navVar, nestedProp);
            var destAccess = Expression.Property(dVar, destProp);
            innerStmts.Add(BuildFlattenedAssignExpr(nestedAccess, destAccess, nestedProp.PropertyType, destProp.PropertyType));
        }

        stmts.Add(Expression.Block(
            new[] { navVar },
            Expression.Assign(navVar, navAccess),
            Expression.IfThen(
                Expression.ReferenceNotEqual(
                    Expression.Convert(navVar, typeof(object)),
                    Expression.Constant(null, typeof(object))),
                Expression.Block(innerStmts))));
    }

    private static Expression BuildFlattenedAssignExpr(
        Expression srcExpr, Expression destAccess, Type srcType, Type destType)
    {
        if (srcType == destType)
            return Expression.Assign(destAccess, srcExpr);
        if (destType.IsAssignableFrom(srcType))
            return Expression.Assign(destAccess, Expression.Convert(srcExpr, destType));
        try { return Expression.Assign(destAccess, Expression.Convert(srcExpr, destType)); }
        catch { return Expression.Empty(); }
    }

    private static Expression? TryBuildTypedFlattenedAssign(
        ParameterExpression sVar,
        ParameterExpression dVar,
        PropertyInfo destProp,
        TypeDetails srcDetails)
    {
        var chain = TryGetFlattenedPropertyChain(destProp.Name, destProp.PropertyType, srcDetails);
        if (chain == null || chain.Count < 2) return null;

        var destType = destProp.PropertyType;
        var leafType = chain[chain.Count - 1].PropertyType;

        if (!destType.IsAssignableFrom(leafType) && !(leafType.IsValueType && destType.IsValueType))
            return null;

        Expression current = sVar;
        var accessExprs = new List<Expression>(chain.Count);
        for (int i = 0; i < chain.Count; i++)
        {
            current = Expression.Property(current, chain[i]);
            accessExprs.Add(current);
        }

        var destAccess = Expression.Property(dVar, destProp);
        Expression assignExpr;
        if (leafType == destType)
            assignExpr = Expression.Assign(destAccess, current);
        else
        {
            try { assignExpr = Expression.Assign(destAccess, Expression.Convert(current, destType)); }
            catch { return null; }
        }

        for (int i = chain.Count - 2; i >= 0; i--)
        {
            if (chain[i].PropertyType.IsValueType) continue;
            assignExpr = Expression.IfThen(
                Expression.ReferenceNotEqual(
                    Expression.Convert(accessExprs[i], typeof(object)),
                    Expression.Constant(null, typeof(object))),
                assignExpr);
        }

        return assignExpr;
    }

    private static readonly MethodInfo _mapNestedHelperMethod =
        typeof(ExpressionBuilder).GetMethod(nameof(MapNestedHelper),
            BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly MethodInfo _mapCollectionPropHelperMethod =
        typeof(ExpressionBuilder).GetMethod(nameof(MapCollectionPropHelper),
            BindingFlags.NonPublic | BindingFlags.Static)!;

    private static Expression? BuildEnsureCollectionInitializedExpr(Expression destExpr, PropertyInfo destProp)
    {
        var propType = destProp.PropertyType;
        if (propType.IsValueType) return null;
        if (!ReflectionHelper.IsCollectionType(propType)) return null;
        var elemType = ReflectionHelper.GetCollectionElementType(propType);
        if (elemType == null) return null;

        var propAccess = Expression.Property(destExpr, destProp);
        var emptyExpr = BuildEmptyCollectionExpr(propType, elemType);
        var assignEmpty = Expression.Assign(propAccess, emptyExpr);
        var nullCheck = Expression.Equal(propAccess, Expression.Constant(null, propType));
        return Expression.IfThen(nullCheck, assignEmpty);
    }

    private static object CreateEmptyCollection(Type destType)
    {
        var elemType = ReflectionHelper.GetCollectionElementType(destType);
        if (elemType != null)
        {
            if (destType.IsArray) return Array.CreateInstance(elemType, 0);
            var listType = typeof(List<>).MakeGenericType(elemType);
            if (destType.IsAssignableFrom(listType))
                return Activator.CreateInstance(listType)!;
            var hashSetType = typeof(HashSet<>).MakeGenericType(elemType);
            if (destType.IsAssignableFrom(hashSetType))
                return Activator.CreateInstance(hashSetType)!;

            if (destType.GetConstructor(Type.EmptyTypes) == null)
            {
                var emptyList = Activator.CreateInstance(listType)!;
                var ctor = FindCollectionWrapperCtor(destType, listType);
                if (ctor != null)
                {
                    try { return ctor.Invoke(new[] { emptyList }); }
                    catch (TargetInvocationException tie) when (tie.InnerException != null)
                    {
                        throw tie.InnerException;
                    }
                }
                throw new InvalidOperationException(
                    $"Cannot construct empty collection of type '{TypeNameHelper.Readable(destType)}': " +
                    $"no parameterless constructor and no single-argument constructor accepting " +
                    $"List<{TypeNameHelper.Readable(elemType)}> or any of its interfaces.");
            }
        }
        return Activator.CreateInstance(destType)!;
    }

    private static Expression BuildEmptyCollectionExpr(Type destType, Type destElem)
    {
        if (destType.IsArray)
            return Expression.NewArrayBounds(destElem, Expression.Constant(0));

        var listType = typeof(List<>).MakeGenericType(destElem);
        if (destType.IsAssignableFrom(listType))
        {
            var ctor = listType.GetConstructor(Type.EmptyTypes)!;
            return Expression.Convert(Expression.New(ctor), destType);
        }

        var hashSetType = typeof(HashSet<>).MakeGenericType(destElem);
        if (destType.IsAssignableFrom(hashSetType))
        {
            var ctor = hashSetType.GetConstructor(Type.EmptyTypes)!;
            return Expression.Convert(Expression.New(ctor), destType);
        }

        var directCtor = destType.GetConstructor(Type.EmptyTypes);
        if (directCtor != null)
            return Expression.New(directCtor);

        return Expression.Convert(
            Expression.Call(CreateEmptyCollectionMethod, Expression.Constant(destType)),
            destType);
    }

    private static object MapNestedHelper(
        object srcVal,
        CompiledMapStore maps,
        TypePair pair,
        ResolutionContext ctx)
    {
        if (maps.TryGetOrCompile(pair, out var del))
            return del(srcVal, null, ctx);
        return MapOrConvert(srcVal, pair.DestinationType, ctx)!;
    }

    private static object? MapCollectionPropHelper(
        object? srcColl,
        CompiledMapStore maps,
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

    private static readonly ConcurrentDictionary<Type, PropertyInfo?> _ilistIndexerCache = new();

    private static readonly MethodInfo _wrapCtxFreeAsBoxedMethod =
        typeof(ExpressionBuilder).GetMethod(nameof(WrapCtxFreeAsBoxed),
            BindingFlags.NonPublic | BindingFlags.Static)!;

    internal static Func<object, object?, ResolutionContext, object> CreateBoxedWrapper(
        Type srcType, Type destType, Delegate ctxFreeWithDest)
        => (Func<object, object?, ResolutionContext, object>)
            _wrapCtxFreeAsBoxedMethod
                .MakeGenericMethod(srcType, destType)
                .Invoke(null, new object[] { ctxFreeWithDest })!;

    private static Func<object, object?, ResolutionContext, object> WrapCtxFreeAsBoxed<TSrc, TDest>(
        Func<TSrc, TDest, TDest> ctxFree)
        => (src, dest, ctx) => ctxFree((TSrc)src, dest != null ? (TDest)dest : default!)!;

    private static Func<object, object?, ResolutionContext, object> BuildFlexibleDelegate(
        TypeMap typeMap,
        Dictionary<TypePair, TypeMap> allTypeMaps,
        CompiledMapStore compiledMaps,
        int defaultMaxDepth = 32)
    {
        var factory = BuildFactory(typeMap);
        var ctxCtor = typeMap.CustomConstructorWithCtx;
        var srcDetails = TypeDetails.Get(typeMap.SourceType);
        var destDetails = TypeDetails.Get(typeMap.DestinationType);

        var mappingActions = new List<Action<object, object, ResolutionContext>>();
        var mappingActionNames = new List<string>();
        var processedDestProps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var propMap in typeMap.EffectivePropertyMaps ?? typeMap.PropertyMaps)
        {
            var propName = propMap.DestinationProperty.Name;
            processedDestProps.Add(propName);
            if (propMap.Ignored) continue;
            var action = BuildPropertyAction(propMap, srcDetails, compiledMaps);
            if (action != null)
            {
                mappingActions.Add(action);
                mappingActionNames.Add(propName);
            }
        }

        var shouldMapProperty = typeMap.ShouldMapProperty;
        foreach (var destProp in destDetails.WritableProperties)
        {
            if (processedDestProps.Contains(destProp.Name)) continue;
            if (shouldMapProperty != null && !shouldMapProperty(destProp)) continue;
            processedDestProps.Add(destProp.Name);
            var action = BuildConventionAction(destProp, srcDetails, allTypeMaps, compiledMaps);
            if (action != null)
            {
                mappingActions.Add(action);
                mappingActionNames.Add(destProp.Name);
            }
        }

        var actionsArray = mappingActions.ToArray();
        var actionNamesArray = mappingActionNames.ToArray();
        var beforeMap = typeMap.BeforeMapAction;
        var afterMap = typeMap.AfterMapAction;
        var beforeMapCtx = typeMap.BeforeMapCtxAction;
        var afterMapCtx = typeMap.AfterMapCtxAction;
        var maxDepth = typeMap.MaxDepth > 0 ? typeMap.MaxDepth : defaultMaxDepth;
        var validationRules = typeMap.ValidationRules;

        return (object src, object? dest, ResolutionContext ctx) =>
        {
            if (maxDepth > 0 && ctx.Depth >= maxDepth)
                return dest!;

            var typedDest = dest ?? (ctxCtor != null ? ctxCtor(src, ctx) : factory(src));
            int actionIndex = -1;

            try
            {
                beforeMap?.Invoke(src, typedDest);
                beforeMapCtx?.Invoke(src, typedDest, ctx);

                ctx.Depth++;
                try
                {
                    for (int i = 0; i < actionsArray.Length; i++)
                    {
                        actionIndex = i;
                        actionsArray[i](src, typedDest, ctx);
                    }
                }
                finally
                {
                    ctx.Depth--;
                }

                afterMap?.Invoke(src, typedDest);
                afterMapCtx?.Invoke(src, typedDest, ctx);

                if (validationRules != null && validationRules.Count > 0)
                {
                    List<string>? violations = null;
                    for (int i = 0; i < validationRules.Count; i++)
                    {
                        var (predicate, message) = validationRules[i];
                        if (!predicate(typedDest))
                        {
                            violations ??= new List<string>();
                            violations.Add(message);
                        }
                    }
                    if (violations != null)
                        throw new MappingValidationException(violations);
                }
            }
            catch (MappingValidationException)
            {
                throw;
            }
            catch (MappingException)
            {
                throw;
            }
            catch (Exception ex)
            {
                var memberName = actionIndex >= 0 && actionIndex < actionNamesArray.Length
                    ? actionNamesArray[actionIndex]
                    : null;
                if (memberName != null)
                    throw new MappingException(src.GetType(), typedDest.GetType(), memberName, ex);
                throw new MappingException(src.GetType(), typedDest.GetType(), ex);
            }

            return typedDest;
        };
    }

    private static Action<object, object, ResolutionContext>? BuildPropertyAction(
        PropertyMap propMap,
        TypeDetails srcDetails,
        CompiledMapStore compiledMaps)
    {
        if (propMap.Ignored) return null;
        if (propMap.UseDestinationValue) return null;

        var destProp = propMap.DestinationProperty;
        var setter = GetOrBuildSetter(destProp);

        if (propMap.HasUseValue)
        {
            var useVal = propMap.UseValue;
            return (src, dest, ctx) => setter(dest, useVal);
        }

        if (propMap.ValueResolverFactory != null)
        {
            var factory = propMap.ValueResolverFactory;
            var getter = propMap.SourceMemberName != null && srcDetails.ReadableByName.TryGetValue(propMap.SourceMemberName, out var resolverSrcProp)
                ? GetOrBuildGetter(resolverSrcProp)
                : null;
            var destType = destProp.PropertyType;

            return (src, dest, ctx) =>
            {
                if (ctx.ServiceProvider == null)
                    throw new InvalidOperationException(
                        "IMemberValueResolver requires DI. Use services.AddEggMapper() for dependency injection.");
                var resolver = factory(ctx.ServiceProvider);
                object? val;
                try { val = resolver(src, dest, null, ctx); }
                catch (NullReferenceException) { val = null; }
                setter(dest, MapOrConvert(val, destType, ctx));
            };
        }

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

                object? val;
                try { val = resolver(src, dest, null, ctx); }
                catch (NullReferenceException) { val = null; }
                setter(dest, MapOrConvert(val, destType, ctx));
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

                object? val;
                try { val = resolver(src, dest); }
                catch (NullReferenceException) { val = null; }
                if (hasNullSub && val == null) val = nullSub;
                setter(dest, MapOrConvert(val, destType, ctx));
            };
        }

        if (propMap.SourceMemberName != null)
        {
            srcDetails.ReadableByName.TryGetValue(propMap.SourceMemberName, out var srcProp);
            if (srcProp == null) return null;
            return BuildDirectPropAction(srcProp, destProp, propMap, compiledMaps);
        }

        srcDetails.ReadableByName.TryGetValue(destProp.Name, out var conventionSrcProp);
        if (conventionSrcProp != null)
            return BuildDirectPropAction(conventionSrcProp, destProp, propMap, compiledMaps);

        return null;
    }

    private static Action<object, object, ResolutionContext>? BuildConventionAction(
        PropertyInfo destProp,
        TypeDetails srcDetails,
        Dictionary<TypePair, TypeMap> allTypeMaps,
        CompiledMapStore compiledMaps)
    {
        srcDetails.ReadableByName.TryGetValue(destProp.Name, out var srcProp);

        if (srcProp != null)
        {
            var direct = BuildDirectPropAction(srcProp, destProp, null, compiledMaps);
            if (direct != null) return direct;
        }
        else
        {
            var flattened = TryBuildFlattenedAction(destProp, srcDetails, compiledMaps);
            if (flattened != null) return flattened;
        }

        var destPropType = destProp.PropertyType;
        if (!destPropType.IsValueType && ReflectionHelper.IsCollectionType(destPropType))
        {
            var setter = GetOrBuildSetter(destProp);
            var getter = GetOrBuildGetter(destProp);
            return (src, dest, ctx) =>
            {
                if (getter(dest) == null)
                    setter(dest, CreateEmptyCollection(destPropType));
            };
        }

        return null;
    }

    private static Action<object, object, ResolutionContext>? BuildDirectPropAction(
        PropertyInfo srcProp,
        PropertyInfo destProp,
        PropertyMap? propMap,
        CompiledMapStore compiledMaps)
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

        bool noGuards = condition == null && fullCondition == null && preCondition == null && !hasNullSub;

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
                        if (srcVal == null) { setter(dest, CreateEmptyCollection(capturedDestType)); return; }
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
                        setter(dest, hasNullSub ? nullSub : CreateEmptyCollection(capturedDestType));
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

                        if (capturedMaps.TryGetOrCompile(typePair, out var nestedDel))
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
                    if (srcVal == null) { if (hasNullSub) setter(dest, nullSub); return; }

                    if (capturedMaps.TryGetOrCompile(typePair, out var nestedDel))
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

                    if (capturedMaps.TryGetOrCompile(typePair, out var nestedDel))
                    {
                        setter(dest, nestedDel(srcVal, null, ctx));
                        return;
                    }

                    setter(dest, MapOrConvert(srcVal, destType, ctx));
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

                if (capturedMaps.TryGetOrCompile(typePair, out var nestedDel))
                {
                    setter(dest, nestedDel(srcVal, null, ctx));
                    return;
                }

                setter(dest, MapOrConvert(srcVal, destType, ctx));
            };
        }

        {
            var capturedDestType = destType;

            if (noGuards)
                return (src, dest, ctx) => setter(dest, MapOrConvert(getter(src), capturedDestType, ctx));

            return (src, dest, ctx) =>
            {
                if (preCondition != null && !preCondition(src)) return;
                if (condition != null && !condition(src)) return;
                if (fullCondition != null && !fullCondition(src, dest)) return;

                var srcVal = getter(src);
                if (hasNullSub && srcVal == null) { setter(dest, nullSub); return; }
                setter(dest, MapOrConvert(srcVal, capturedDestType, ctx));
            };
        }
    }

    private static Action<object, object, ResolutionContext>? TryBuildFlattenedAction(
        PropertyInfo destProp,
        TypeDetails srcDetails,
        CompiledMapStore compiledMaps)
    {
        var chain = TryGetFlattenedPropertyChain(destProp.Name, destProp.PropertyType, srcDetails);
        if (chain == null || chain.Count < 2) return null;

        var getters = new Func<object, object?>[chain.Count];
        for (int i = 0; i < chain.Count; i++)
            getters[i] = GetOrBuildGetter(chain[i]);

        var setter = GetOrBuildSetter(destProp);
        var destType = destProp.PropertyType;
        var leafType = chain[chain.Count - 1].PropertyType;
        var capturedMaps = compiledMaps;

        return (src, dest, ctx) =>
        {
            object? current = src;
            for (int i = 0; i < getters.Length; i++)
            {
                current = getters[i](current!);
                if (current == null)
                {
                    if (i < getters.Length - 1 || !destType.IsValueType)
                        setter(dest, null);
                    return;
                }
            }

            if (destType.IsAssignableFrom(leafType))
            {
                setter(dest, current);
            }
            else if (capturedMaps.TryGetOrCompile(new TypePair(leafType, destType), out var nestedDel))
            {
                setter(dest, nestedDel(current!, null, ctx));
            }
            else
            {
                setter(dest, MapOrConvert(current, destType, ctx));
            }
        };
    }

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

        var emptyCtor = dictConcreteType.GetConstructor(Type.EmptyTypes)!;
        return Expression.IfThenElse(
            Expression.ReferenceNotEqual(
                Expression.Convert(srcAccess, typeof(object)),
                Expression.Constant(null, typeof(object))),
            Expression.Assign(destAccess,
                Expression.Convert(Expression.New(copyCtor, srcCast), destType)),
            Expression.Assign(destAccess,
                Expression.Convert(Expression.New(emptyCtor), destType)));
    }

    private static Action<object, object, ResolutionContext>? BuildDictionaryAction(
        Func<object, object?> getter,
        Action<object, object?> setter,
        Type srcType,
        Type destType,
        bool noGuards,
        CompiledMapStore compiledMaps,
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
                if (srcVal == null) { setter(dest, Activator.CreateInstance(destDictType)!); return; }
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
                setter(dest, hasNullSub ? nullSub : Activator.CreateInstance(destDictType)!);
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
        CompiledMapStore compiledMaps,
        ResolutionContext ctx)
    {
        compiledMaps.TryGetOrCompile(valPair, out var valMapper);
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
                mappedVal = val != null ? MapOrConvert(val, destValType, ctx) : null;
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
        CompiledMapStore compiledMaps,
        ResolutionContext ctx)
    {
        compiledMaps.TryGetOrCompile(elementTypePair, out var elemMapper);

        int count = source is ICollection col ? col.Count : -1;

        object? MapElement(object? elem)
        {
            if (elem == null) return null;
            if (elemMapper != null) return elemMapper(elem, null, ctx);
            if (destElemType.IsAssignableFrom(srcElemType)) return elem;
            return MapOrConvert(elem, destElemType, ctx);
        }

        if (destCollectionType.IsArray)
        {
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

            var ctor = FindCollectionWrapperCtor(destCollectionType, list.GetType());
            if (ctor != null)
            {
                try { return ctor.Invoke(new[] { (object)list }); }
                catch (TargetInvocationException tie) when (tie.InnerException != null)
                {
                    throw tie.InnerException;
                }
            }

            return list;
        }
    }

    public static Delegate? TryBuildPatchDelegate(TypeMap typeMap)
    {
        if (typeMap.ConvertUsingFunc != null) return null;
        if (typeMap.BaseMapTypePair.HasValue) return null;
        if (ReflectionHelper.IsCollectionType(typeMap.SourceType)) return null;
        if (ReflectionHelper.IsCollectionType(typeMap.DestinationType)) return null;

        var srcType = typeMap.SourceType;
        var destType = typeMap.DestinationType;
        var srcDetails = TypeDetails.Get(srcType);
        var destDetails = TypeDetails.Get(destType);

        var srcParam  = Expression.Parameter(srcType,  "src");
        var destParam = Expression.Parameter(destType, "dest");
        var stmts = new List<Expression>();

        var processedProps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var propMap in typeMap.PropertyMaps)
        {
            processedProps.Add(propMap.DestinationProperty.Name);
            if (propMap.Ignored) continue;

            if (propMap.CustomResolver != null || propMap.ContextResolver != null
                || propMap.ValueResolverFactory != null) continue;

            if (propMap.HasUseValue)
            {
                try
                {
                    stmts.Add(Expression.Assign(
                        Expression.Property(destParam, propMap.DestinationProperty),
                        Expression.Convert(Expression.Constant(propMap.UseValue),
                            propMap.DestinationProperty.PropertyType)));
                }
                catch { }
                continue;
            }

            if (propMap.SourceMemberName != null)
            {
                srcDetails.ReadableByName.TryGetValue(propMap.SourceMemberName, out var srcProp);
                if (srcProp == null) continue;
                var expr = TryBuildPatchAssign(srcParam, destParam, srcProp, propMap.DestinationProperty);
                if (expr != null) stmts.Add(expr);
            }
        }

        foreach (var destProp in destDetails.WritableProperties)
        {
            if (processedProps.Contains(destProp.Name)) continue;
            processedProps.Add(destProp.Name);

            srcDetails.ReadableByName.TryGetValue(destProp.Name, out var srcProp);
            if (srcProp == null) continue;

            var expr = TryBuildPatchAssign(srcParam, destParam, srcProp, destProp);
            if (expr != null) stmts.Add(expr);
        }

        stmts.Add(destParam);
        var body = stmts.Count == 1
            ? (Expression)destParam
            : Expression.Block(stmts);
        var funcType = typeof(Func<,,>).MakeGenericType(srcType, destType, destType);
        return Expression.Lambda(funcType, body, srcParam, destParam).Compile();
    }

    private static Expression? TryBuildPatchAssign(
        ParameterExpression srcParam, ParameterExpression destParam,
        PropertyInfo srcProp, PropertyInfo destProp)
    {
        var srcType   = srcProp.PropertyType;
        var destType  = destProp.PropertyType;
        var srcAccess = Expression.Property(srcParam,  srcProp);
        var destAccess = Expression.Property(destParam, destProp);

        var innerAssign = BuildPatchInnerAssign(srcAccess, destAccess, srcType, destType);
        if (innerAssign == null) return null;

        if (!srcType.IsValueType)
        {
            return Expression.IfThen(
                Expression.ReferenceNotEqual(
                    Expression.Convert(srcAccess, typeof(object)),
                    Expression.Constant(null, typeof(object))),
                innerAssign);
        }

        if (Nullable.GetUnderlyingType(srcType) != null)
        {
            return Expression.IfThen(
                Expression.Property(srcAccess, "HasValue"),
                innerAssign);
        }

        return innerAssign;
    }

    private static Expression? BuildPatchInnerAssign(
        Expression srcAccess, Expression destAccess, Type srcType, Type destType)
    {
        if (ReflectionHelper.IsCollectionType(srcType) || ReflectionHelper.IsCollectionType(destType))
            return null;

        var srcUnderlying  = Nullable.GetUnderlyingType(srcType);
        var destUnderlying = Nullable.GetUnderlyingType(destType);

        if (srcType == destType)
            return Expression.Assign(destAccess, srcAccess);

        if (destType.IsAssignableFrom(srcType))
            return Expression.Assign(destAccess, Expression.Convert(srcAccess, destType));

        if (srcUnderlying != null
            && (destType == srcUnderlying || destType.IsAssignableFrom(srcUnderlying)))
            return Expression.Assign(destAccess,
                Expression.Convert(Expression.Property(srcAccess, "Value"), destType));

        if (destUnderlying != null
            && (srcType == destUnderlying || destUnderlying.IsAssignableFrom(srcType)))
            return Expression.Assign(destAccess, Expression.Convert(srcAccess, destType));

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

        return null;
    }

    private static (ConstructorInfo Ctor, ParameterInfo[] Params)? FindBestConstructor(
        TypeDetails destDetails, TypeDetails srcDetails)
    {
        ConstructorInfo? best = null;
        ParameterInfo[]? bestParams = null;
        int bestScore = 0;

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

    private static object? MapOrConvert(object? value, Type targetType, ResolutionContext ctx)
    {
        if (value == null)
        {
            if (ReflectionHelper.IsCollectionType(targetType))
                return CreateEmptyCollection(targetType);
            if (targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null)
                return Activator.CreateInstance(targetType);
            return null;
        }
        var valueType = value.GetType();
        if (targetType.IsAssignableFrom(valueType)) return value;

        if (ctx.Mapper != null)
        {
            try
            {
                var mapped = ctx.Mapper.Map(value, valueType, targetType);
                if (mapped == null || targetType.IsAssignableFrom(mapped.GetType()))
                    return mapped;
            }
            catch (InvalidOperationException) { }
        }

        return ConvertValue(value, targetType);
    }

    private static object? TryInvokeConversionOperator(object value, Type valueType, Type targetType)
    {
        const System.Reflection.BindingFlags staticPublic =
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static;

        foreach (var searchType in new[] { valueType, targetType })
        {
            foreach (var opName in new[] { "op_Implicit", "op_Explicit" })
            {
                var methods = searchType.GetMethods(staticPublic);
                for (int i = 0; i < methods.Length; i++)
                {
                    var m = methods[i];
                    if (m.Name != opName) continue;
                    if (!targetType.IsAssignableFrom(m.ReturnType)) continue;

                    var ps = m.GetParameters();
                    if (ps.Length != 1) continue;

                    var paramType = ps[0].ParameterType;
                    if (paramType.IsAssignableFrom(valueType))
                    {
                        try { return m.Invoke(null, new[] { value }); }
                        catch { }
                    }
                }
            }
        }
        return null;
    }

    private static object? ConvertValue(object? value, Type targetType)
    {
        if (value == null) return null;
        var valueType = value.GetType();
        if (targetType.IsAssignableFrom(valueType)) return value;

        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlying.IsEnum && value is string strVal)
            return string.IsNullOrEmpty(strVal) ? Enum.ToObject(underlying, 0) : Enum.Parse(underlying, strVal, ignoreCase: true);
        if (valueType.IsEnum && (underlying == typeof(string) || targetType == typeof(string)))
            return value.ToString();

        var result = TryInvokeConversionOperator(value, valueType, underlying);
        if (result != null) return result;

        try
        {
            return Convert.ChangeType(value, underlying);
        }
        catch { }

        var ctor = underlying.GetConstructor(new[] { valueType });
        if (ctor != null)
        {
            try { return ctor.Invoke(new[] { value }); }
            catch { }
        }

        for (var bt = valueType.BaseType; bt != null && bt != typeof(object); bt = bt.BaseType)
        {
            ctor = underlying.GetConstructor(new[] { bt });
            if (ctor != null)
            {
                try { return ctor.Invoke(new[] { value }); }
                catch { }
            }
        }

        foreach (var iface in valueType.GetInterfaces())
        {
            ctor = underlying.GetConstructor(new[] { iface });
            if (ctor != null)
            {
                try { return ctor.Invoke(new[] { value }); }
                catch { }
            }
        }

        return null;
    }
}
