namespace EggMapper;

/// <summary>Configures how one enum pair is mapped. Used with <c>ConvertUsingEnumMapping</c>.</summary>
/// <typeparam name="TSourceEnum">Source enum type.</typeparam>
/// <typeparam name="TDestinationEnum">Destination enum type.</typeparam>
public sealed class EnumMappingOptions<TSourceEnum, TDestinationEnum>
    where TSourceEnum : struct, Enum
    where TDestinationEnum : struct, Enum
{
    internal EnumMappingMode Mode { get; private set; } = EnumMappingMode.ByValue;
    internal Dictionary<TSourceEnum, TDestinationEnum> Values { get; } = new();

    /// <summary>Maps members with the same name (exact, then ignoring case); unmatched values map by value.</summary>
    public EnumMappingOptions<TSourceEnum, TDestinationEnum> MapByName()
    {
        Mode = EnumMappingMode.ByName;
        return this;
    }

    /// <summary>Maps by the underlying numeric value (the default).</summary>
    public EnumMappingOptions<TSourceEnum, TDestinationEnum> MapByValue()
    {
        Mode = EnumMappingMode.ByValue;
        return this;
    }

    /// <summary>Maps <paramref name="source"/> to <paramref name="destination"/>, overriding the name or value rule.</summary>
    public EnumMappingOptions<TSourceEnum, TDestinationEnum> MapValue(TSourceEnum source, TDestinationEnum destination)
    {
        Values[source] = destination;
        return this;
    }

    internal Execution.EnumPairMapping ToPairMapping()
    {
        var values = new Dictionary<object, object>(Values.Count);
        foreach (var entry in Values)
            values[entry.Key] = entry.Value;
        return new Execution.EnumPairMapping(Mode, values);
    }
}
