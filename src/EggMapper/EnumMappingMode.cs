namespace EggMapper;

/// <summary>How a value of one enum type is mapped to a different enum type.</summary>
public enum EnumMappingMode
{
    /// <summary>Maps by the underlying numeric value (a cast).</summary>
    ByValue,

    /// <summary>
    /// Maps to the destination member with the same name (exact match first, then ignoring case); values whose name has
    /// no destination member fall back to the numeric value. This is AutoMapper's default enum behavior.
    /// </summary>
    ByName
}
