using System.Reflection;
using System.Text;

namespace EggMapper.Internal;

internal sealed class UnmappedMap
{
    public UnmappedMap(TypeMap typeMap, List<PropertyInfo> members)
    {
        TypeMap = typeMap;
        Members = members;
    }

    public TypeMap TypeMap { get; }
    public List<PropertyInfo> Members { get; }
}

internal static class ConfigurationValidationMessage
{
    internal const int MaxMembersPerMap = 10;
    internal const int MaxMaps = 20;
    private const int MaxClosestMembers = 3;

    internal static string Build(IReadOnlyList<UnmappedMap> unmappedMaps)
    {
        var totalMembers = unmappedMaps.Sum(m => m.Members.Count);
        var headers = PairHeaders(unmappedMaps);
        var ordered = unmappedMaps
            .Select((map, index) => (Map: map, Header: headers[index]))
            .OrderBy(x => x.Header, StringComparer.Ordinal)
            .ToList();

        var sb = new StringBuilder();
        sb.Append("EggMapper configuration is invalid: ")
          .Append(Count(totalMembers, "unmapped destination member"))
          .Append(" in ")
          .Append(Count(unmappedMaps.Count, "map"))
          .Append('.');

        foreach (var (map, header) in ordered.Take(MaxMaps))
        {
            sb.Append('\n').Append('\n').Append(header);
            AppendReverseMapNotes(sb, map.TypeMap);
            var sourceDetails = TypeDetails.Get(map.TypeMap.SourceType);
            foreach (var member in map.Members.Take(MaxMembersPerMap))
                AppendMember(sb, map.TypeMap, sourceDetails, member);
            if (map.Members.Count > MaxMembersPerMap)
                sb.Append("\n  +").Append(map.Members.Count - MaxMembersPerMap).Append(" more in this map");
        }

        if (ordered.Count > MaxMaps)
            sb.Append("\n\n+").Append(Count(ordered.Count - MaxMaps, "more map"));

        return sb.ToString();
    }

    private static void AppendMember(StringBuilder sb, TypeMap typeMap, TypeDetails sourceDetails, PropertyInfo member)
    {
        sb.Append("\n  Unmapped destination member '")
          .Append(typeMap.DestinationType.Name).Append('.').Append(member.Name)
          .Append("' in map ")
          .Append(typeMap.SourceType.Name).Append(" -> ").Append(typeMap.DestinationType.Name);

        var ignore = $".ForMember(d => d.{member.Name}, o => o.Ignore())";
        var suggestions = UnmappedMemberSuggestions.For(sourceDetails, member, MaxClosestMembers);
        if (suggestions.ConfidentMatch is { } match)
        {
            sb.Append("\n    Fix: .ForMember(d => d.").Append(member.Name)
              .Append(", o => o.MapFrom(s => s.").Append(match.Path).Append("))")
              .Append("\n     or: ").Append(ignore);
            return;
        }

        if (suggestions.ClosestMembers.Count > 0)
        {
            sb.Append("\n    Options: ")
              .Append(string.Join(", ", suggestions.ClosestMembers.Select(c => $"s.{c.Path} ({TypeNameHelper.Readable(c.Type)})")));
        }
        sb.Append("\n    Configure: .ForMember(d => d.").Append(member.Name)
          .Append(", o => o.MapFrom(s => ...)) or ").Append(ignore);
    }

    private static void AppendReverseMapNotes(StringBuilder sb, TypeMap typeMap)
    {
        if (typeMap.ReverseMapOrigin is not { } origin) return;

        var forward = $"CreateMap<{TypeNameHelper.Readable(origin.SourceType)}, {TypeNameHelper.Readable(origin.DestinationType)}>()";
        var reverse = $"CreateMap<{TypeNameHelper.Readable(typeMap.SourceType)}, {TypeNameHelper.Readable(typeMap.DestinationType)}>()";
        sb.Append("\n  Created by .ReverseMap() on ").Append(forward)
          .Append(", which maps by name only. Configure this direction with its own ").Append(reverse).Append('.');
        if (typeMap.IgnoredExplicitCreateMap)
        {
            sb.Append("\n  A ").Append(reverse)
              .Append(" in a profile was ignored because this ReverseMap() registered the pair first; remove .ReverseMap() from ")
              .Append(forward).Append(" to use it.");
        }
    }

    private static List<string> PairHeaders(IReadOnlyList<UnmappedMap> unmappedMaps)
    {
        var shortHeaders = unmappedMaps
            .Select(m => TypeNameHelper.Pair(m.TypeMap.SourceType, m.TypeMap.DestinationType))
            .ToList();
        var duplicated = new HashSet<string>(
            shortHeaders.GroupBy(h => h, StringComparer.Ordinal).Where(g => g.Count() > 1).Select(g => g.Key),
            StringComparer.Ordinal);

        return unmappedMaps
            .Select((m, i) => duplicated.Contains(shortHeaders[i])
                ? Qualified(m.TypeMap.SourceType) + " -> " + Qualified(m.TypeMap.DestinationType)
                : shortHeaders[i])
            .ToList();
    }

    private static string Qualified(Type type)
    {
        var name = TypeNameHelper.Readable(type);
        for (var declaring = type.DeclaringType; declaring != null; declaring = declaring.DeclaringType)
            name = TypeNameHelper.Readable(declaring) + "." + name;
        return string.IsNullOrEmpty(type.Namespace) ? name : type.Namespace + "." + name;
    }

    private static string Count(int count, string noun) => count == 1 ? $"1 {noun}" : $"{count} {noun}s";
}
