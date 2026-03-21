using System;

namespace EggMapper
{
    /// <summary>
    /// Overrides the default by-name property matching for a single source property.
    /// The generator maps this source property to the specified destination member instead.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class MapPropertyAttribute : Attribute
    {
        /// <summary>The name of the destination property to map this source property to.</summary>
        public string DestinationMember { get; }

        /// <param name="destinationMember">The name of the destination property.</param>
        public MapPropertyAttribute(string destinationMember)
        {
            DestinationMember = destinationMember ?? throw new ArgumentNullException(nameof(destinationMember));
        }
    }
}
