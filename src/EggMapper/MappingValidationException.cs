namespace EggMapper;

/// <summary>
/// Thrown when one or more inline validation rules fail during mapping.
/// All violations are collected before throwing so callers see the complete picture.
/// </summary>
public sealed class MappingValidationException : Exception
{
    /// <summary>All validation error messages collected during the mapping pass.</summary>
    public IReadOnlyList<string> Errors { get; }

    public MappingValidationException(IReadOnlyList<string> errors)
        : base("Mapping validation failed:\n" + string.Join("\n", errors.Select(e => $"  - {e}")))
    {
        Errors = errors;
    }
}
