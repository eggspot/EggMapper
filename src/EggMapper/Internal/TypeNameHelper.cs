using System.Text;

namespace EggMapper.Internal;

/// <summary>
/// Produces human-readable type names for error messages.
/// <c>List&lt;DeliveryProvider&gt;</c> instead of <c>List`1</c>.
/// </summary>
internal static class TypeNameHelper
{
    /// <summary>
    /// Returns a readable short name: <c>List&lt;DeliveryProvider&gt;</c>, <c>Dictionary&lt;String, Int32&gt;</c>,
    /// <c>Nullable&lt;Int32&gt;</c> (or <c>Int32?</c> shorthand), etc.
    /// Uses short names (no namespace) for brevity.
    /// </summary>
    internal static string Readable(Type type)
    {
        if (type == null) return "(null)";

        // Nullable<T> → T?
        var underlying = Nullable.GetUnderlyingType(type);
        if (underlying != null)
            return Readable(underlying) + "?";

        // Non-generic: just use Name
        if (!type.IsGenericType)
            return type.IsArray ? Readable(type.GetElementType()!) + "[]" : type.Name;

        // Generic: strip the `N suffix and append <T1, T2, ...>
        var def = type.GetGenericTypeDefinition();
        var baseName = def.Name;
        var backtick = baseName.IndexOf('`');
        if (backtick > 0) baseName = baseName.Substring(0, backtick);

        var args = type.GetGenericArguments();
        var sb = new StringBuilder(baseName.Length + args.Length * 12);
        sb.Append(baseName);
        sb.Append('<');
        for (int i = 0; i < args.Length; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(Readable(args[i]));
        }
        sb.Append('>');
        return sb.ToString();
    }

    /// <summary>
    /// Formats a mapping pair: <c>DeliveryProvider -> DeliveryProviderResponse</c>.
    /// </summary>
    internal static string Pair(Type source, Type dest) =>
        $"{Readable(source)} -> {Readable(dest)}";
}
