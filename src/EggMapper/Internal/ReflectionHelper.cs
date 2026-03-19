using System.Collections;
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

    public static bool IsNumericType(Type type) => NumericTypes.Contains(type);

    public static bool IsNullableType(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);

    public static Type GetUnderlyingType(Type type) =>
        Nullable.GetUnderlyingType(type) ?? type;

    public static bool IsCollectionType(Type type)
    {
        if (type == typeof(string)) return false;
        if (type.IsArray) return true;
        var interfaces = type.GetInterfaces();
        for (int i = 0; i < interfaces.Length; i++)
        {
            var iface = interfaces[i];
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return true;
        }
        return false;
    }

    public static Type? GetCollectionElementType(Type type)
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
        foreach (var srcProp in srcDetails.ReadableProperties)
        {
            if (!destPropName.StartsWith(srcProp.Name, StringComparison.OrdinalIgnoreCase)) continue;
            var remainder = destPropName.Substring(srcProp.Name.Length);
            if (string.IsNullOrEmpty(remainder)) continue;
            var nestedDetails = TypeDetails.Get(srcProp.PropertyType);
            if (nestedDetails.ReadableProperties.Any(p =>
                string.Equals(p.Name, remainder, StringComparison.OrdinalIgnoreCase)))
                return true;
        }
        return false;
    }
}
