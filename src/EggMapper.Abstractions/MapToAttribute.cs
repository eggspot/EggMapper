using System;

namespace EggMapper
{
    /// <summary>
    /// Instructs EggMapper.Generator to generate a compile-time extension method
    /// <c>source.ToTDestination()</c> and <c>source.ToTDestinationList()</c> for this type.
    /// </summary>
    /// <remarks>
    /// Apply multiple <see cref="MapToAttribute"/> on the same source type to generate
    /// mappings to several destination types.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
    public sealed class MapToAttribute : Attribute
    {
        /// <summary>The destination type to map to.</summary>
        public Type DestinationType { get; }

        /// <param name="destinationType">The destination type to map to.</param>
        public MapToAttribute(Type destinationType)
        {
            DestinationType = destinationType ?? throw new ArgumentNullException(nameof(destinationType));
        }
    }
}
