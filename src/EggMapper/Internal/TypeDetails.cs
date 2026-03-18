using System.Collections.Concurrent;
using System.Reflection;

namespace EggMapper.Internal;

internal sealed class TypeDetails
{
    public Type Type { get; }
    public PropertyInfo[] ReadableProperties { get; }
    public PropertyInfo[] WritableProperties { get; }
    public ConstructorInfo[] Constructors { get; }

    private static readonly ConcurrentDictionary<Type, TypeDetails> Cache = new();

    public static TypeDetails Get(Type type) => Cache.GetOrAdd(type, t => new TypeDetails(t));

    private TypeDetails(Type type)
    {
        Type = type;
        ReadableProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead).ToArray();
        WritableProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite).ToArray();
        Constructors = type.GetConstructors();
    }
}
