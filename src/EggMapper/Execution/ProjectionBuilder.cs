using System.Linq.Expressions;
using System.Reflection;
using EggMapper.Internal;

namespace EggMapper.Execution;

internal static class ProjectionBuilder
{
    internal static Expression<Func<TSrc, TDest>> Build<TSrc, TDest>(MapperConfiguration config)
    {
        var srcParam = Expression.Parameter(typeof(TSrc), "src");
        var body = BuildExpression(typeof(TSrc), typeof(TDest), srcParam, config);
        return Expression.Lambda<Func<TSrc, TDest>>(body, srcParam);
    }

    private static Expression BuildExpression(
        Type srcType, Type destType, Expression srcExpr, MapperConfiguration config)
    {
        var key = new TypePair(srcType, destType);
        config.TypeMaps.TryGetValue(key, out var typeMap);

        var destDetails = TypeDetails.Get(destType);
        var srcDetails = TypeDetails.Get(srcType);
        var bindings = new List<MemberBinding>();

        var propMaps = typeMap?.EffectivePropertyMaps ?? typeMap?.PropertyMaps;

        foreach (var destProp in destDetails.WritableProperties)
        {
            var propMap = propMaps?.FirstOrDefault(p => p.DestinationProperty.Name == destProp.Name);
            if (propMap?.Ignored == true) continue;

            Expression? valueExpr;

            if (propMap?.MapFromExpression != null)
            {
                var replacer = new ParameterReplacer(propMap.MapFromExpression.Parameters[0], srcExpr);
                valueExpr = replacer.Visit(propMap.MapFromExpression.Body);
                if (valueExpr!.Type != destProp.PropertyType)
                    valueExpr = Expression.Convert(valueExpr, destProp.PropertyType);
            }
            else if (propMap?.HasUseValue == true)
            {
                valueExpr = Expression.Constant(propMap.UseValue, destProp.PropertyType);
            }
            else
            {
                var srcMemberName = propMap?.SourceMemberName ?? destProp.Name;
                if (srcDetails.ReadableByName.TryGetValue(srcMemberName, out var srcProp))
                {
                    var srcAccess = Expression.Property(srcExpr, srcProp);
                    var nestedKey = new TypePair(srcProp.PropertyType, destProp.PropertyType);

                    if (srcProp.PropertyType != destProp.PropertyType &&
                        config.TypeMaps.ContainsKey(nestedKey) &&
                        !EnumConversion.IsEnumPair(srcProp.PropertyType, destProp.PropertyType))
                    {
                        valueExpr = BuildExpression(srcProp.PropertyType, destProp.PropertyType,
                            srcAccess, config);
                    }
                    else if (srcProp.PropertyType == destProp.PropertyType)
                    {
                        valueExpr = srcAccess;
                    }
                    else if (destProp.PropertyType.IsAssignableFrom(srcProp.PropertyType))
                    {
                        valueExpr = Expression.Convert(srcAccess, destProp.PropertyType);
                    }
                    else if (EnumConversion.IsEnumPair(srcProp.PropertyType, destProp.PropertyType))
                    {
                        valueExpr = EnumConversion.TryBuild(srcAccess, destProp.PropertyType, null)!;
                    }
                    else
                    {
                        continue;
                    }
                }
                else
                {
                    valueExpr = TryBuildFlattenedExpr(destProp.Name, destProp.PropertyType,
                        srcExpr, srcDetails);
                    if (valueExpr == null) continue;
                }
            }

            bindings.Add(Expression.Bind(destProp, valueExpr));
        }

        if (destDetails.ParameterlessCtor != null)
            return Expression.MemberInit(Expression.New(destDetails.ParameterlessCtor), bindings);

        var best = FindBestCtor(destDetails, srcDetails)
            ?? throw new InvalidOperationException(
                $"ProjectTo: no suitable constructor found for '{TypeNameHelper.Readable(destType)}'.");

        var ctorArgs = BuildCtorArgExpressions(best.Params, srcExpr, srcDetails);

        var members = best.Params
            .Select(p => (MemberInfo?)GetMatchingProperty(destDetails, p))
            .ToArray();

        var newExpr = Expression.New(best.Ctor, ctorArgs, members!);

        var ctorParamNames = new HashSet<string>(
            best.Params.Select(p => p.Name ?? ""), StringComparer.OrdinalIgnoreCase);
        var extraBindings = bindings
            .Where(b => !ctorParamNames.Contains(b.Member.Name))
            .ToList();

        return extraBindings.Count > 0
            ? (Expression)Expression.MemberInit(newExpr, extraBindings)
            : newExpr;
    }

    private static Expression? TryBuildFlattenedExpr(
        string destPropName, Type destPropType, Expression srcExpr, TypeDetails srcDetails)
    {
        foreach (var srcProp in srcDetails.ReadableProperties)
        {
            if (!destPropName.StartsWith(srcProp.Name, StringComparison.OrdinalIgnoreCase))
                continue;
            var remainder = destPropName.Substring(srcProp.Name.Length);
            if (string.IsNullOrEmpty(remainder)) continue;
            var nestedDetails = TypeDetails.Get(srcProp.PropertyType);
            if (!nestedDetails.ReadableByName.TryGetValue(remainder, out var nestedProp))
                continue;
            var access = Expression.Property(Expression.Property(srcExpr, srcProp), nestedProp);
            if (nestedProp.PropertyType == destPropType) return access;
            if (destPropType.IsAssignableFrom(nestedProp.PropertyType))
                return Expression.Convert(access, destPropType);
        }
        return null;
    }

    private static (ConstructorInfo Ctor, ParameterInfo[] Params)? FindBestCtor(
        TypeDetails destDetails, TypeDetails srcDetails)
    {
        ConstructorInfo? best = null;
        ParameterInfo[]? bestParams = null;
        int bestScore = 0;
        foreach (var ctor in destDetails.Constructors)
        {
            var pars = ctor.GetParameters();
            if (pars.Length == 0) continue;
            int score = 0;
            foreach (var p in pars)
                if (p.Name != null && srcDetails.ReadableByName.ContainsKey(p.Name)) score++;
            if (score > bestScore) { bestScore = score; best = ctor; bestParams = pars; }
        }
        return best == null ? null : (best, bestParams!);
    }

    private static Expression[] BuildCtorArgExpressions(
        ParameterInfo[] ctorParams, Expression srcExpr, TypeDetails srcDetails)
    {
        var args = new Expression[ctorParams.Length];
        for (int i = 0; i < ctorParams.Length; i++)
        {
            var p = ctorParams[i];
            if (p.Name != null && srcDetails.ReadableByName.TryGetValue(p.Name, out var srcProp))
            {
                var access = Expression.Property(srcExpr, srcProp);
                if (srcProp.PropertyType == p.ParameterType)
                    args[i] = access;
                else if (p.ParameterType.IsAssignableFrom(srcProp.PropertyType))
                    args[i] = Expression.Convert(access, p.ParameterType);
                else
                    args[i] = Expression.Default(p.ParameterType);
            }
            else
            {
                args[i] = Expression.Default(p.ParameterType);
            }
        }
        return args;
    }

    private static PropertyInfo? GetMatchingProperty(TypeDetails destDetails, ParameterInfo p)
    {
        if (p.Name != null && destDetails.ReadableByName.TryGetValue(p.Name, out var prop))
            return prop;
        return null;
    }
}

internal sealed class ParameterReplacer : ExpressionVisitor
{
    private readonly ParameterExpression _toReplace;
    private readonly Expression _replacement;

    internal ParameterReplacer(ParameterExpression toReplace, Expression replacement)
    {
        _toReplace = toReplace;
        _replacement = replacement;
    }

    protected override Expression VisitParameter(ParameterExpression node)
        => node == _toReplace ? _replacement : base.VisitParameter(node);
}
