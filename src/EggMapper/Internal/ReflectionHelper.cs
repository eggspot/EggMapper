using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;

namespace EggMapper.Internal;

internal static class ReflectionHelper
{
    private static readonly HashSet<Type> NumericTypes = new()
    {
        typeof(byte), typeof(sbyte), typeof(short), typeof(ushort),
        typeof(int), typeof(uint), typeof(long), typeof(ulong),
        typeof(float), typeof(double), typeof(decimal)
    };

    // Per-type caches — computed once, reused across all map compilations.
    // Eliminates repeated GetInterfaces() calls (each allocates an array) for
    // every property type encountered during startup compilation.
    private static readonly ConcurrentDictionary<Type, bool> _isDictCache = new();
    private static readonly ConcurrentDictionary<Type, bool> _isCollCache = new();
    private static readonly ConcurrentDictionary<Type, (Type Key, Type Value)> _dictKvCache = new();
    private static readonly ConcurrentDictionary<Type, Type?> _collElemCache = new();

    public static bool IsNumericType(Type type) => NumericTypes.Contains(type);

    public static bool IsNullableType(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);

    public static Type GetUnderlyingType(Type type) =>
        Nullable.GetUnderlyingType(type) ?? type;

    public static bool IsDictionaryType(Type type) =>
        _isDictCache.GetOrAdd(type, ComputeIsDictionaryType);

    private static bool ComputeIsDictionaryType(Type type)
    {
        if (!type.IsGenericType && type.GetInterfaces().Length == 0) return false;
        if (type.IsGenericType)
        {
            var def = type.GetGenericTypeDefinition();
            if (def == typeof(IDictionary<,>) || def == typeof(Dictionary<,>)) return true;
        }
        var interfaces = type.GetInterfaces();
        for (int i = 0; i < interfaces.Length; i++)
        {
            var iface = interfaces[i];
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                return true;
        }
        return false;
    }

    public static (Type KeyType, Type ValueType) GetDictionaryKeyValueTypes(Type type) =>
        _dictKvCache.GetOrAdd(type, ComputeDictionaryKeyValueTypes);

    private static (Type, Type) ComputeDictionaryKeyValueTypes(Type type)
    {
        if (type.IsGenericType)
        {
            var def = type.GetGenericTypeDefinition();
            if (def == typeof(IDictionary<,>) || def == typeof(Dictionary<,>))
            {
                var args = type.GetGenericArguments();
                return (args[0], args[1]);
            }
        }
        var interfaces = type.GetInterfaces();
        for (int i = 0; i < interfaces.Length; i++)
        {
            var iface = interfaces[i];
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IDictionary<,>))
            {
                var args = iface.GetGenericArguments();
                return (args[0], args[1]);
            }
        }
        throw new InvalidOperationException($"{TypeNameHelper.Readable(type)} is not a dictionary type");
    }

    public static bool IsCollectionType(Type type) =>
        _isCollCache.GetOrAdd(type, ComputeIsCollectionType);

    private static bool ComputeIsCollectionType(Type type)
    {
        if (type == typeof(string)) return false;
        // IsDictionaryType result is already cached from the first call
        if (IsDictionaryType(type)) return false;
        if (type.IsArray) return true;
        // Check if the type itself is a generic collection interface (IEnumerable<T>, IList<T>, etc.)
        if (type.IsGenericType)
        {
            var def = type.GetGenericTypeDefinition();
            if (def == typeof(IEnumerable<>) || def == typeof(IList<>) || def == typeof(ICollection<>))
                return true;
        }
        var interfaces = type.GetInterfaces();
        for (int i = 0; i < interfaces.Length; i++)
        {
            var iface = interfaces[i];
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return true;
        }
        return false;
    }

    public static Type? GetCollectionElementType(Type type) =>
        _collElemCache.GetOrAdd(type, ComputeCollectionElementType);

    private static Type? ComputeCollectionElementType(Type type)
    {
        if (type.IsArray) return type.GetElementType();
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            return type.GetGenericArguments()[0];
        var interfaces = type.GetInterfaces();
        for (int i = 0; i < interfaces.Length; i++)
        {
            var iface = interfaces[i];
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return iface.GetGenericArguments()[0];
        }
        return null;
    }

    public static bool IsAssignable(Type sourceType, Type destinationType)
    {
        if (destinationType.IsAssignableFrom(sourceType)) return true;
        var srcUnderlying = GetUnderlyingType(sourceType);
        var destUnderlying = GetUnderlyingType(destinationType);
        if (srcUnderlying == destUnderlying) return true;
        return false;
    }

    public static bool HasFlattenedSource(string destPropName, TypeDetails srcDetails)
    {
        var readable = srcDetails.ReadableProperties;
        for (int i = 0; i < readable.Length; i++)
        {
            var srcProp = readable[i];
            var navName = srcProp.Name;
            if (!destPropName.StartsWith(navName, StringComparison.OrdinalIgnoreCase)) continue;
            var remainder = destPropName.Substring(navName.Length);
            if (remainder.Length == 0) continue;
            var nestedDetails = TypeDetails.Get(srcProp.PropertyType);
            var nestedProps = nestedDetails.ReadableProperties;
            for (int j = 0; j < nestedProps.Length; j++)
            {
                if (string.Equals(remainder, nestedProps[j].Name, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            // Recursive: check deeper levels (e.g., AddressCityName → Address.City.Name)
            if (HasFlattenedSource(remainder, nestedDetails))
                return true;
        }
        return false;
    }
}
