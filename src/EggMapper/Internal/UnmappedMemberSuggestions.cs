using System.Reflection;

namespace EggMapper.Internal;

internal readonly struct SourceMemberSuggestion
{
    public SourceMemberSuggestion(string path, Type type)
    {
        Path = path;
        Type = type;
    }

    public string Path { get; }
    public Type Type { get; }
}

internal readonly struct UnmappedMemberSuggestions
{
    private UnmappedMemberSuggestions(SourceMemberSuggestion? confidentMatch, IReadOnlyList<SourceMemberSuggestion> closestMembers)
    {
        ConfidentMatch = confidentMatch;
        ClosestMembers = closestMembers;
    }

    public SourceMemberSuggestion? ConfidentMatch { get; }
    public IReadOnlyList<SourceMemberSuggestion> ClosestMembers { get; }

    private const int MinimumLengthForTypoMatch = 6;
    private const int MinimumLengthForContainment = 3;

    internal static UnmappedMemberSuggestions For(TypeDetails source, PropertyInfo destination, int maxClosestMembers)
    {
        var candidates = CollectCandidates(source);
        var destName = destination.Name;
        var destType = destination.PropertyType;

        var confident = new List<SourceMemberSuggestion>();
        var nestedNameMatches = new List<SourceMemberSuggestion>();
        foreach (var candidate in candidates)
        {
            var name = LastSegment(candidate.Path);
            var isNested = candidate.Path.IndexOf('.') >= 0;
            if (!IsCompatible(candidate.Type, destType)) continue;

            if (isNested)
            {
                if (string.Equals(name, destName, StringComparison.OrdinalIgnoreCase))
                    nestedNameMatches.Add(candidate);
                continue;
            }

            if (IsSingularPluralPair(name, destName)
                || (destName.Length >= MinimumLengthForTypoMatch
                    && HasSameType(candidate.Type, destType)
                    && OptimalStringAlignmentDistance(name, destName) == 1))
            {
                confident.Add(candidate);
            }
        }

        if (nestedNameMatches.Count == 1)
            confident.Add(nestedNameMatches[0]);

        SourceMemberSuggestion? confidentMatch = confident.Count == 1 ? confident[0] : null;
        var closest = confidentMatch.HasValue
            ? Array.Empty<SourceMemberSuggestion>()
            : RankClosest(candidates, destName, destType, maxClosestMembers);
        return new UnmappedMemberSuggestions(confidentMatch, closest);
    }

    private static List<SourceMemberSuggestion> CollectCandidates(TypeDetails source)
    {
        var candidates = new List<SourceMemberSuggestion>();
        foreach (var property in source.ReadableProperties)
        {
            candidates.Add(new SourceMemberSuggestion(property.Name, property.PropertyType));
            if (!IsNavigable(property.PropertyType)) continue;
            foreach (var nested in TypeDetails.Get(property.PropertyType).ReadableProperties)
                candidates.Add(new SourceMemberSuggestion(property.Name + "." + nested.Name, nested.PropertyType));
        }
        return candidates;
    }

    private static bool IsNavigable(Type type) =>
        type.IsClass
        && type != typeof(string)
        && type != typeof(object)
        && !ReflectionHelper.IsCollectionType(type)
        && !ReflectionHelper.IsDictionaryType(type);

    private static IReadOnlyList<SourceMemberSuggestion> RankClosest(
        List<SourceMemberSuggestion> candidates, string destName, Type destType, int max)
    {
        var maxDistance = Math.Max(2, destName.Length / 3);
        var ranked = new List<(SourceMemberSuggestion Candidate, bool Compatible, int Distance)>();
        foreach (var candidate in candidates)
        {
            var name = LastSegment(candidate.Path);
            var distance = OptimalStringAlignmentDistance(name, destName);
            var contains = Math.Min(name.Length, destName.Length) >= MinimumLengthForContainment
                && (destName.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf(destName, StringComparison.OrdinalIgnoreCase) >= 0);
            if (distance > maxDistance && !contains) continue;
            ranked.Add((candidate, IsCompatible(candidate.Type, destType), contains ? Math.Min(distance, 1) : distance));
        }

        return ranked
            .OrderByDescending(r => r.Compatible)
            .ThenBy(r => r.Distance)
            .ThenBy(r => r.Candidate.Path.Length)
            .ThenBy(r => r.Candidate.Path, StringComparer.Ordinal)
            .Take(max)
            .Select(r => r.Candidate)
            .ToList();
    }

    private static string LastSegment(string path)
    {
        var dot = path.LastIndexOf('.');
        return dot < 0 ? path : path.Substring(dot + 1);
    }

    private static bool IsSingularPluralPair(string a, string b)
    {
        if (a.Length == b.Length) return false;
        var (shorter, longer) = a.Length < b.Length ? (a, b) : (b, a);
        if (!longer.StartsWith(shorter, StringComparison.OrdinalIgnoreCase)) return false;
        var suffix = longer.Substring(shorter.Length);
        return suffix.Equals("s", StringComparison.OrdinalIgnoreCase)
            || suffix.Equals("es", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasSameType(Type source, Type destination) =>
        ReflectionHelper.GetUnderlyingType(source) == ReflectionHelper.GetUnderlyingType(destination);

    private static bool IsCompatible(Type source, Type destination)
    {
        if (ReflectionHelper.IsAssignable(source, destination)) return true;
        var src = ReflectionHelper.GetUnderlyingType(source);
        var dest = ReflectionHelper.GetUnderlyingType(destination);
        if (ReflectionHelper.IsNumericType(src) && ReflectionHelper.IsNumericType(dest)) return true;
        if ((src.IsEnum || src == typeof(string)) && (dest.IsEnum || dest == typeof(string))) return true;
        return ReflectionHelper.IsCollectionType(src) && ReflectionHelper.IsCollectionType(dest);
    }

    internal static int OptimalStringAlignmentDistance(string a, string b)
    {
        a = a.ToUpperInvariant();
        b = b.ToUpperInvariant();
        var d = new int[a.Length + 1, b.Length + 1];
        for (var i = 0; i <= a.Length; i++) d[i, 0] = i;
        for (var j = 0; j <= b.Length; j++) d[0, j] = j;
        for (var i = 1; i <= a.Length; i++)
        {
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                if (i > 1 && j > 1 && a[i - 1] == b[j - 2] && a[i - 2] == b[j - 1])
                    d[i, j] = Math.Min(d[i, j], d[i - 2, j - 2] + 1);
            }
        }
        return d[a.Length, b.Length];
    }
}
