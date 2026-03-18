namespace EggMapper.Internal;

internal readonly struct TypePair : IEquatable<TypePair>
{
    public readonly Type SourceType;
    public readonly Type DestinationType;

    public TypePair(Type sourceType, Type destinationType)
    {
        SourceType = sourceType;
        DestinationType = destinationType;
    }

    public bool Equals(TypePair other) =>
        SourceType == other.SourceType && DestinationType == other.DestinationType;

    public override bool Equals(object? obj) =>
        obj is TypePair other && Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine(SourceType, DestinationType);
}
