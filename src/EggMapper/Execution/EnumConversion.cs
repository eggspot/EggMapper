using System.Linq.Expressions;
using System.Reflection;
using EggMapper.Internal;

namespace EggMapper.Execution;

internal static class EnumConversion
{
    public static bool IsEnumPair(Type sourceType, Type destinationType)
    {
        var sourceEnum = Nullable.GetUnderlyingType(sourceType) ?? sourceType;
        var destinationEnum = Nullable.GetUnderlyingType(destinationType) ?? destinationType;
        return sourceEnum.IsEnum && destinationEnum.IsEnum && sourceEnum != destinationEnum;
    }

    public static Expression? TryBuild(Expression source, Type destinationType, TypeConversionRegistry? registry)
    {
        var sourceType = source.Type;
        var sourceEnum = Nullable.GetUnderlyingType(sourceType) ?? sourceType;
        var destinationEnum = Nullable.GetUnderlyingType(destinationType) ?? destinationType;
        var pair = new TypePair(sourceEnum, destinationEnum);
        if (registry != null && registry.HasConvertUsing(pair))
            return null;

        var mapping = registry?.GetEnumPairMapping(pair) ?? EnumPairMapping.For(EnumMappingMode.ByValue);

        if (sourceType != sourceEnum)
        {
            var converted = BuildCore(Expression.Property(source, "Value"), sourceEnum, destinationEnum, mapping);
            var hasValue = Expression.Property(source, "HasValue");
            return destinationType == destinationEnum
                ? Expression.Condition(hasValue, converted, Expression.Default(destinationType))
                : Expression.Condition(hasValue, Expression.Convert(converted, destinationType), Expression.Constant(null, destinationType));
        }

        var core = BuildCore(source, sourceEnum, destinationEnum, mapping);
        return destinationType == destinationEnum ? core : Expression.Convert(core, destinationType);
    }

    private static Expression BuildCore(Expression value, Type sourceEnum, Type destinationEnum, EnumPairMapping mapping)
    {
        var wideType = WideType(sourceEnum);
        var byValue = ToEnum(Expression.Convert(value, Enum.GetUnderlyingType(sourceEnum)), destinationEnum);
        var targets = ResolveTargets(sourceEnum, destinationEnum, mapping);
        if (targets == null)
            return byValue;

        if (sourceEnum.IsDefined(typeof(FlagsAttribute), false) && mapping.Mode == EnumMappingMode.ByName)
            return BuildFlags(value, sourceEnum, destinationEnum, targets, byValue);

        var cases = new List<SwitchCase>(targets.Count);
        foreach (var target in targets)
            cases.Add(Expression.SwitchCase(
                Expression.Constant(target.Value, destinationEnum),
                Expression.Constant(Widen(target.Key, wideType), wideType)));
        return Expression.Switch(Widened(value, sourceEnum), byValue, cases.ToArray());
    }

    private static Dictionary<object, object>? ResolveTargets(Type sourceEnum, Type destinationEnum, EnumPairMapping mapping)
    {
        if (mapping.Mode == EnumMappingMode.ByValue && mapping.Values.Count == 0)
            return null;

        var destinationNames = Enum.GetNames(destinationEnum);
        var targets = new Dictionary<object, object>();
        var seenValues = new HashSet<object>();
        var identity = true;
        foreach (var name in Enum.GetNames(sourceEnum))
        {
            var sourceValue = Enum.Parse(sourceEnum, name);
            var sourceUnderlying = Convert.ChangeType(sourceValue, Enum.GetUnderlyingType(sourceEnum));
            if (!seenValues.Add(sourceUnderlying)) continue;
            var canonicalName = Enum.GetName(sourceEnum, sourceValue)!;

            object? target = null;
            if (mapping.Values.TryGetValue(sourceValue, out var explicitTarget))
                target = explicitTarget;
            else if (mapping.Mode == EnumMappingMode.ByName)
                target = FindByName(destinationEnum, destinationNames, canonicalName);

            if (target == null) continue;
            targets[sourceValue] = target;
            if (Widen(sourceValue, typeof(decimal)) is var a && Widen(target, typeof(decimal)) is var b && !Equals(a, b))
                identity = false;
        }
        return identity && mapping.Values.Count == 0 ? null : targets;
    }

    private static object? FindByName(Type destinationEnum, string[] destinationNames, string name)
    {
        foreach (var candidate in destinationNames)
            if (string.Equals(candidate, name, StringComparison.Ordinal))
                return Enum.Parse(destinationEnum, candidate);
        foreach (var candidate in destinationNames)
            if (string.Equals(candidate, name, StringComparison.OrdinalIgnoreCase))
                return Enum.Parse(destinationEnum, candidate);
        return null;
    }

    private static Expression BuildFlags(Expression value, Type sourceEnum, Type destinationEnum,
        Dictionary<object, object> targets, Expression byValue)
    {
        var memberCount = Enum.GetValues(sourceEnum).Cast<object>()
            .Select(v => Convert.ChangeType(v, Enum.GetUnderlyingType(sourceEnum))).Distinct().Count();
        if (targets.Count != memberCount)
            return byValue;

        var wideType = WideType(sourceEnum);
        var destinationWide = WideType(destinationEnum);
        var sourceBits = Expression.Variable(wideType, "flags");
        var result = Expression.Variable(destinationWide, "mapped");
        object knownMask = Widen(0, wideType);
        var statements = new List<Expression>
        {
            Expression.Assign(sourceBits, Widened(value, sourceEnum)),
            Expression.Assign(result, Expression.Constant(Widen(0, destinationWide), destinationWide))
        };
        foreach (var target in targets)
        {
            var bits = Widen(target.Key, wideType);
            knownMask = wideType == typeof(ulong) ? (object)((ulong)knownMask | (ulong)bits) : (long)knownMask | (long)bits;
            var bitsConstant = Expression.Constant(bits, wideType);
            statements.Add(Expression.IfThen(
                Expression.Equal(Expression.And(sourceBits, bitsConstant), bitsConstant),
                Expression.OrAssign(result, Expression.Constant(Widen(target.Value, destinationWide), destinationWide))));
        }
        var unknownBits = Expression.NotEqual(
            Expression.And(sourceBits, Expression.Not(Expression.Constant(knownMask, wideType))),
            Expression.Constant(Widen(0, wideType), wideType));
        statements.Add(Expression.Condition(unknownBits, byValue,
            ToEnum(result, destinationEnum)));
        return Expression.Block(destinationEnum, new[] { sourceBits, result }, statements);
    }

    private static Type WideType(Type enumType) => Enum.GetUnderlyingType(enumType) == typeof(ulong) ? typeof(ulong) : typeof(long);

    private static Expression Widened(Expression value, Type enumType)
        => Expression.Convert(Expression.Convert(value, Enum.GetUnderlyingType(enumType)), WideType(enumType));

    private static object Widen(object value, Type wideType)
    {
        if (value is Enum) value = Convert.ChangeType(value, Enum.GetUnderlyingType(value.GetType()));
        if (wideType == typeof(decimal)) return Convert.ToDecimal(value);
        return wideType == typeof(ulong) ? unchecked((ulong)Convert.ToDecimal(value)) : (object)Convert.ToInt64(value);
    }

    private static Expression ToEnum(Expression numeric, Type destinationEnum)
        => Expression.Convert(Expression.Convert(numeric, Enum.GetUnderlyingType(destinationEnum)), destinationEnum);
}
