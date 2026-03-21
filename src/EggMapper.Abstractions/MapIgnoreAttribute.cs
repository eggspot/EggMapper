using System;

namespace EggMapper
{
    /// <summary>
    /// Instructs EggMapper.Generator to skip this source property when generating the mapping.
    /// The corresponding destination property (if any) will remain at its default value.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class MapIgnoreAttribute : Attribute { }
}
