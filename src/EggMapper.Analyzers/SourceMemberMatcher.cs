using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace EggMapper.Analyzers
{
    internal sealed class SourceMemberSuggestion
    {
        public SourceMemberSuggestion(string path, ITypeSymbol type)
        {
            Path = path;
            Type = type;
        }

        public string Path { get; }
        public ITypeSymbol Type { get; }
    }

    internal static class SourceMemberMatcher
    {
        private const int MinimumLengthForTypoMatch = 6;
        private const int MinimumLengthForContainment = 3;

        public static IReadOnlyList<IPropertySymbol> ReadableProperties(ITypeSymbol type) =>
            InstanceProperties(type).Where(p => p.GetMethod != null).ToList();

        public static IReadOnlyList<IPropertySymbol> WritableProperties(ITypeSymbol type) =>
            InstanceProperties(type)
                .Where(p => p.SetMethod != null && p.SetMethod.DeclaredAccessibility == Accessibility.Public)
                .ToList();

        public static bool HasReadableProperty(ITypeSymbol type, string name) =>
            ReadableProperties(type).Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

        public static bool HasFlattenedSource(string destinationName, ITypeSymbol sourceType)
        {
            foreach (var property in ReadableProperties(sourceType))
            {
                if (!destinationName.StartsWith(property.Name, StringComparison.OrdinalIgnoreCase)) continue;
                var remainder = destinationName.Substring(property.Name.Length);
                if (remainder.Length == 0) continue;
                if (HasReadableProperty(property.Type, remainder)) return true;
                if (HasFlattenedSource(remainder, property.Type)) return true;
            }
            return false;
        }

        public static (SourceMemberSuggestion? ConfidentMatch, IReadOnlyList<SourceMemberSuggestion> Closest) Suggest(
            Compilation compilation, ITypeSymbol sourceType, IPropertySymbol destination, int maxClosest)
        {
            var candidates = CollectCandidates(sourceType);
            var destName = destination.Name;
            var destType = destination.Type;

            var confident = new List<SourceMemberSuggestion>();
            var nestedNameMatches = new List<SourceMemberSuggestion>();
            foreach (var candidate in candidates)
            {
                if (!IsCompatible(compilation, candidate.Type, destType)) continue;
                var name = LastSegment(candidate.Path);
                if (candidate.Path.IndexOf('.') >= 0)
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

            if (confident.Count == 1)
                return (confident[0], Array.Empty<SourceMemberSuggestion>());
            return (null, RankClosest(compilation, candidates, destName, destType, maxClosest));
        }

        private static IEnumerable<IPropertySymbol> InstanceProperties(ITypeSymbol type)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var current = type; current != null; current = current.BaseType)
            {
                foreach (var member in current.GetMembers())
                {
                    if (member is IPropertySymbol property
                        && !property.IsStatic
                        && !property.IsIndexer
                        && property.DeclaredAccessibility == Accessibility.Public
                        && seen.Add(property.Name))
                    {
                        yield return property;
                    }
                }
            }
        }

        private static List<SourceMemberSuggestion> CollectCandidates(ITypeSymbol sourceType)
        {
            var candidates = new List<SourceMemberSuggestion>();
            foreach (var property in ReadableProperties(sourceType))
            {
                candidates.Add(new SourceMemberSuggestion(property.Name, property.Type));
                if (!IsNavigable(property.Type)) continue;
                foreach (var nested in ReadableProperties(property.Type))
                    candidates.Add(new SourceMemberSuggestion(property.Name + "." + nested.Name, nested.Type));
            }
            return candidates;
        }

        private static bool IsNavigable(ITypeSymbol type) =>
            type.TypeKind == TypeKind.Class
            && type.SpecialType != SpecialType.System_String
            && type.SpecialType != SpecialType.System_Object
            && !IsCollection(type);

        public static bool IsCollection(ITypeSymbol type)
        {
            if (type.SpecialType == SpecialType.System_String) return false;
            if (type is IArrayTypeSymbol) return true;
            return type.AllInterfaces.Any(i => i.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)
                || (type is INamedTypeSymbol named && named.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T);
        }

        private static IReadOnlyList<SourceMemberSuggestion> RankClosest(
            Compilation compilation, List<SourceMemberSuggestion> candidates, string destName, ITypeSymbol destType, int max)
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
                ranked.Add((candidate, IsCompatible(compilation, candidate.Type, destType), contains ? Math.Min(distance, 1) : distance));
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
            var shorter = a.Length < b.Length ? a : b;
            var longer = a.Length < b.Length ? b : a;
            if (!longer.StartsWith(shorter, StringComparison.OrdinalIgnoreCase)) return false;
            var suffix = longer.Substring(shorter.Length);
            return suffix.Equals("s", StringComparison.OrdinalIgnoreCase)
                || suffix.Equals("es", StringComparison.OrdinalIgnoreCase);
        }

        private static ITypeSymbol Underlying(ITypeSymbol type) =>
            type is INamedTypeSymbol named && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
                ? named.TypeArguments[0]
                : type;

        private static bool HasSameType(ITypeSymbol source, ITypeSymbol destination) =>
            SymbolEqualityComparer.Default.Equals(Underlying(source), Underlying(destination));

        private static bool IsCompatible(Compilation compilation, ITypeSymbol source, ITypeSymbol destination)
        {
            if (HasSameType(source, destination)) return true;
            var conversion = compilation.ClassifyCommonConversion(source, destination);
            if (conversion.IsIdentity || (conversion.IsImplicit && conversion.IsReference)) return true;
            var src = Underlying(source);
            var dest = Underlying(destination);
            if (IsNumeric(src) && IsNumeric(dest)) return true;
            if (IsEnumOrString(src) && IsEnumOrString(dest)) return true;
            return IsCollection(src) && IsCollection(dest);
        }

        private static bool IsEnumOrString(ITypeSymbol type) =>
            type.TypeKind == TypeKind.Enum || type.SpecialType == SpecialType.System_String;

        private static bool IsNumeric(ITypeSymbol type)
        {
            switch (type.SpecialType)
            {
                case SpecialType.System_Byte:
                case SpecialType.System_SByte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                case SpecialType.System_Decimal:
                    return true;
                default:
                    return false;
            }
        }

        private static int OptimalStringAlignmentDistance(string a, string b)
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
}
