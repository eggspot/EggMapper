using System.Collections.Concurrent;
using System.Reflection;

namespace EggMapper.Internal;

internal sealed class TypeDetails
{
    public Type Type { get; }
    public PropertyInfo[] ReadableProperties { get; }
    public PropertyInfo[] WritableProperties { get; }
    public ConstructorInfo[] Constructors { get; }

    /// <summary>Cached parameterless constructor — avoids repeated GetConstructor(Type.EmptyTypes) calls.</summary>
    public ConstructorInfo? ParameterlessCtor { get; }

    /// <summary>Case-insensitive name → PropertyInfo index for readable properties. O(1) lookups.</summary>
    public Dictionary<string, PropertyInfo> ReadableByName { get; }

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
        ParameterlessCtor = type.GetConstructor(Type.EmptyTypes);

        ReadableByName = new Dictionary<string, PropertyInfo>(readable.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var p in readable)
            if (!ReadableByName.ContainsKey(p.Name))
                ReadableByName[p.Name] = p;
    }
}
