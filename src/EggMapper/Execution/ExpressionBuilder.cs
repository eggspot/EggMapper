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

    public static Func<object, object?, ResolutionContext, object> BuildMappingDelegate(
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

        if (ReflectionHelper.IsCollectionType(srcType) && ReflectionHelper.IsCollectionType(destType))
        {
            var srcElemType = ReflectionHelper.GetCollectionElementType(srcType);
            var destElemType = ReflectionHelper.GetCollectionElementType(destType);

            if (srcElemType != null && destElemType != null)
            {
                var capturedMaps = compiledMaps;
                var elemPair = new TypePair(srcElemType, destElemType);
                var capturedDestType = destType;

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
