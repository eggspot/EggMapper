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
        return type.GetInterfaces().Any(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
    }

    public static Type? GetCollectionElementType(Type type)
    {
        if (type.IsArray) return type.GetElementType();
        var iface = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)
            ? type
            : type.GetInterfaces().FirstOrDefault(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        return iface?.GetGenericArguments()[0];
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
