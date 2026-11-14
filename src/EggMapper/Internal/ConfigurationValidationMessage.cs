using System.Reflection;
using System.Text;

namespace EggMapper.Internal;

internal sealed class InvalidMap
{
    public InvalidMap(TypeMap typeMap, List<PropertyInfo> members)
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

    internal static string Build(IReadOnlyList<InvalidMap> invalidMaps)
    {
        var mapsWithUnmappedMembers = invalidMaps.Count(m => m.Members.Count > 0);
        var totalMembers = invalidMaps.Sum(m => m.Members.Count);
        var duplicatedMaps = invalidMaps.Count(m => m.TypeMap.AlsoDefinedIn?.Count > 0);
        var headers = PairHeaders(invalidMaps);
        var ordered = invalidMaps
            .Select((map, index) => (Map: map, Header: headers[index]))
            .OrderBy(x => x.Header, StringComparer.Ordinal)
            .ToList();

        var problems = new List<string>();
        if (totalMembers > 0)
            problems.Add($"{Count(totalMembers, "unmapped destination member")} in {Count(mapsWithUnmappedMembers, "map")}");
        if (duplicatedMaps > 0)
            problems.Add($"{Count(duplicatedMaps, "map")} configured in more than one place");

        var sb = new StringBuilder();
        sb.Append("EggMapper configuration is invalid: ").Append(string.Join("; ", problems)).Append('.');

        foreach (var (map, header) in ordered.Take(MaxMaps))
        {
            sb.Append('\n').Append('\n').Append(header);
            AppendDuplicateDefinitions(sb, map.TypeMap);
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

    private static void AppendDuplicateDefinitions(StringBuilder sb, TypeMap typeMap)
    {
        if (typeMap.AlsoDefinedIn is not { Count: > 0 } others) return;

        var createMap = $"CreateMap<{TypeNameHelper.Readable(typeMap.SourceType)}, {TypeNameHelper.Readable(typeMap.DestinationType)}>()";
        var used = typeMap.DefinedInProfile ?? MapperConfigurationExpression.RootConfigurationName;
        var all = new List<string> { used };
        all.AddRange(others);
        sb.Append("\n  ").Append(createMap).Append(" is configured in ").Append(JoinNames(all))
          .Append("; only the map in ").Append(used)
          .Append(" is used. Keep one, or move the other's ForMember calls into it.");
    }

    private static string JoinNames(List<string> names) =>
        names.Count == 2
            ? names[0] + " and " + names[1]
            : string.Join(", ", names.Take(names.Count - 1)) + " and " + names[names.Count - 1];

    private static void AppendReverseMapNotes(StringBuilder sb, TypeMap typeMap)
    {
        if (typeMap.ReverseMapOrigin is not { } origin) return;

        var forward = $"CreateMap<{TypeNameHelper.Readable(origin.SourceType)}, {TypeNameHelper.Readable(origin.DestinationType)}>()";
        var reverse = $"CreateMap<{TypeNameHelper.Readable(typeMap.SourceType)}, {TypeNameHelper.Readable(typeMap.DestinationType)}>()";
        sb.Append("\n  Created by .ReverseMap() on ").Append(forward)
          .Append(", which maps by name only. Configure this direction with its own ").Append(reverse).Append('.');
    }

    private static List<string> PairHeaders(IReadOnlyList<InvalidMap> unmappedMaps)
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
