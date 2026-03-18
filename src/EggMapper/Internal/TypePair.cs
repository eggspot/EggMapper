using System.Runtime.CompilerServices;

namespace EggMapper.Internal;

internal readonly struct TypePair : IEquatable<TypePair>
{
    public readonly Type SourceType;
    public readonly Type DestinationType;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TypePair(Type sourceType, Type destinationType)
    {
        SourceType = sourceType;
        DestinationType = destinationType;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(TypePair other) =>
        SourceType == other.SourceType && DestinationType == other.DestinationType;

    public override bool Equals(object? obj) =>
        obj is TypePair other && Equals(other);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode()
    {
        // Use XOR with rotation for better distribution and fewer operations
        // than the multiply-and-add approach, reducing cycles in the hot path.
        int h1 = SourceType?.GetHashCode() ?? 0;
        int h2 = DestinationType?.GetHashCode() ?? 0;
        // Rotate h1 by 16 bits and XOR with h2 for good distribution
        return ((h1 << 16) | ((int)((uint)h1 >> 16))) ^ h2;
    }
}
