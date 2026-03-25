namespace EggMapper;

public sealed class MappingException : Exception
{
    public Type? SourceType { get; }
    public Type? DestinationType { get; }
    public string? MemberName { get; }

    public MappingException(string message) : base(message) { }

    public MappingException(string message, Exception innerException)
        : base(message, innerException) { }

    public MappingException(Type sourceType, Type destinationType, Exception innerException)
        : base($"Error mapping {sourceType.Name} to {destinationType.Name}: {innerException.Message}", innerException)
    {
        SourceType = sourceType;
        DestinationType = destinationType;
    }

    public MappingException(Type sourceType, Type destinationType, string memberName, Exception innerException)
        : base($"Error mapping {sourceType.Name} to {destinationType.Name}, member '{memberName}': {innerException.Message}", innerException)
    {
        SourceType = sourceType;
        DestinationType = destinationType;
        MemberName = memberName;
    }

    public MappingException(Type sourceType, Type destinationType, string message)
        : base($"Error mapping {sourceType.Name} to {destinationType.Name}: {message}")
    {
        SourceType = sourceType;
        DestinationType = destinationType;
    }
}
