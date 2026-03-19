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
        var allProps = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var readable = new List<PropertyInfo>(allProps.Length);
        var writable = new List<PropertyInfo>(allProps.Length);
        for (int i = 0; i < allProps.Length; i++)
        {
            var p = allProps[i];
            if (p.CanRead) readable.Add(p);
            if (p.CanWrite) writable.Add(p);
        }
        ReadableProperties = readable.ToArray();
        WritableProperties = writable.ToArray();
        Constructors = type.GetConstructors();
    }
}
