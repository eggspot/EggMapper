using System.Linq.Expressions;
using System.Reflection;
using EggMapper.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace EggMapper;

internal sealed class MapperConfigurationExpression : IMapperConfigurationExpression
{
    private readonly Dictionary<TypePair, TypeMap> _typeMaps = new();

    public IMappingExpression<TSource, TDestination> CreateMap<TSource, TDestination>()
    {
        var typeMap = new TypeMap
        {
            SourceType = typeof(TSource),
            DestinationType = typeof(TDestination)
        };
        var key = new TypePair(typeof(TSource), typeof(TDestination));
        _typeMaps[key] = typeMap;
        return new MappingExpression<TSource, TDestination>(typeMap, RegisterTypeMap);
    }

    private void RegisterTypeMap(TypeMap typeMap)
    {
        var key = new TypePair(typeMap.SourceType, typeMap.DestinationType);
        if (!_typeMaps.ContainsKey(key))
            _typeMaps[key] = typeMap;
    }

    public void AddProfile<TProfile>() where TProfile : Profile, new()
        => AddProfile(new TProfile());

    public void AddProfile(Profile profile)
    {
        foreach (var typeMap in profile.GetTypeMaps())
            RegisterTypeMap(typeMap);
    }

    public void AddProfiles(IEnumerable<Assembly> assemblies)
    {
        foreach (var assembly in assemblies)
        {
            var profileTypes = assembly.GetTypes()
                .Where(t => !t.IsAbstract && typeof(Profile).IsAssignableFrom(t) && t != typeof(Profile));
            foreach (var profileType in profileTypes)
            {
                if (Activator.CreateInstance(profileType) is Profile profile)
                    AddProfile(profile);
            }
        }
    }

    public void AddMaps(params Assembly[] assemblies) => AddProfiles(assemblies);
    public void AddMaps(IEnumerable<Assembly> assemblies) => AddProfiles(assemblies);

    internal IEnumerable<TypeMap> GetTypeMaps() => _typeMaps.Values;
}

internal sealed class MappingExpression<TSource, TDestination> : IMappingExpression<TSource, TDestination>
{
    private readonly TypeMap _typeMap;
    private readonly Action<TypeMap> _registerTypeMap;

    internal MappingExpression(TypeMap typeMap, Action<TypeMap> registerTypeMap)
    {
        _typeMap = typeMap;
        _registerTypeMap = registerTypeMap;
    }

    private PropertyMap GetOrCreatePropertyMap(PropertyInfo destProp)
    {
        var existing = _typeMap.PropertyMaps.FirstOrDefault(p => p.DestinationProperty.Name == destProp.Name);
        if (existing != null) return existing;
        var propMap = new PropertyMap { DestinationProperty = destProp };
        _typeMap.PropertyMaps.Add(propMap);
        return propMap;
    }

    private static string GetMemberName<T, TMember>(Expression<Func<T, TMember>> expression)
    {
        if (expression.Body is MemberExpression member)
            return member.Member.Name;
        if (expression.Body is UnaryExpression { Operand: MemberExpression unaryMember })
            return unaryMember.Member.Name;
        throw new ArgumentException($"Expression is not a member access: {expression}");
    }

    public IMappingExpression<TSource, TDestination> ForMember<TMember>(
        Expression<Func<TDestination, TMember>> destinationMember,
        Action<IMemberConfigurationExpression<TSource, TDestination, TMember>> memberOptions)
    {
        var memberName = GetMemberName(destinationMember);
        var destDetails = TypeDetails.Get(typeof(TDestination));
        var destProp = destDetails.WritableProperties.FirstOrDefault(p => p.Name == memberName)
            ?? throw new InvalidOperationException(
                $"Property '{memberName}' not found or not writable on '{typeof(TDestination).Name}'");
        var propMap = GetOrCreatePropertyMap(destProp);
        var expr = new MemberConfigurationExpression<TSource, TDestination, TMember>(propMap);
        memberOptions(expr);
        return this;
    }

    public IMappingExpression<TSource, TDestination> ForPath<TMember>(
        Expression<Func<TDestination, TMember>> destinationMember,
        Action<IPathConfigurationExpression<TSource, TDestination, TMember>> pathOptions)
    {
        var memberName = GetMemberName(destinationMember);
        var destDetails = TypeDetails.Get(typeof(TDestination));
        var destProp = destDetails.WritableProperties.FirstOrDefault(p => p.Name == memberName)
            ?? throw new InvalidOperationException(
                $"Property '{memberName}' not found or not writable on '{typeof(TDestination).Name}'");
        var propMap = GetOrCreatePropertyMap(destProp);
        var expr = new PathConfigurationExpression<TSource, TDestination, TMember>(propMap);
        pathOptions(expr);
        return this;
    }

    public IMappingExpression<TSource, TDestination> ReverseMap()
    {
        _typeMap.HasReverseMap = true;
        var reverseTypeMap = new TypeMap
        {
            SourceType = typeof(TDestination),
            DestinationType = typeof(TSource)
        };
        _registerTypeMap(reverseTypeMap);
        return this;
    }

    public IMappingExpression<TSource, TDestination> ConstructUsing(Func<TSource, TDestination> ctor)
    {
        _typeMap.CustomConstructor = src => ctor((TSource)src)!;
        return this;
    }

    public IMappingExpression<TSource, TDestination> BeforeMap(Action<TSource, TDestination> beforeFunction)
    {
        _typeMap.BeforeMapAction = (src, dest) => beforeFunction((TSource)src, (TDestination)dest);
        return this;
    }

    public IMappingExpression<TSource, TDestination> BeforeMap(Action<TSource, TDestination, ResolutionContext> beforeFunction)
    {
        _typeMap.BeforeMapCtxAction = (src, dest, ctx) =>
            beforeFunction((TSource)src, (TDestination)dest, ctx);
        return this;
    }

    public IMappingExpression<TSource, TDestination> AfterMap(Action<TSource, TDestination> afterFunction)
    {
        _typeMap.AfterMapAction = (src, dest) => afterFunction((TSource)src, (TDestination)dest);
        return this;
    }

    public IMappingExpression<TSource, TDestination> AfterMap(Action<TSource, TDestination, ResolutionContext> afterFunction)
    {
        _typeMap.AfterMapCtxAction = (src, dest, ctx) =>
            afterFunction((TSource)src, (TDestination)dest, ctx);
        return this;
    }

    public IMappingExpression<TSource, TDestination> IncludeBase<TSourceBase, TDestinationBase>()
    {
        _typeMap.BaseMapTypePair = new TypePair(typeof(TSourceBase), typeof(TDestinationBase));
        return this;
    }

    public IMappingExpression<TSource, TDestination> IncludeAllDerived()
    {
        _typeMap.IncludeAllDerivedFlag = true;
        return this;
    }

    public IMappingExpression<TSource, TDestination> MaxDepth(int depth)
    {
        _typeMap.MaxDepth = depth;
        return this;
    }

    public IMappingExpression<TSource, TDestination> ConvertUsing(Func<TSource, TDestination> converter)
    {
        _typeMap.ConvertUsingFunc = (src, dest, ctx) => converter((TSource)src)!;
        return this;
    }

    public IMappingExpression<TSource, TDestination> ConvertUsing(Func<TSource, TDestination?, TDestination> converter)
    {
        _typeMap.ConvertUsingFunc = (src, dest, ctx) =>
            converter((TSource)src, dest is TDestination td ? td : default)!;
        return this;
    }

    public IMappingExpression<TSource, TDestination> ForAllMembers(
        Action<IMemberConfigurationExpression<TSource, TDestination, object>> memberOptions)
    {
        var destDetails = TypeDetails.Get(typeof(TDestination));
        foreach (var destProp in destDetails.WritableProperties)
        {
            var propMap = GetOrCreatePropertyMap(destProp);
            var expr = new MemberConfigurationExpression<TSource, TDestination, object>(propMap);
            memberOptions(expr);
        }
        return this;
    }
}

internal sealed class MemberConfigurationExpression<TSource, TDestination, TMember>
    : IMemberConfigurationExpression<TSource, TDestination, TMember>
{
    private readonly PropertyMap _propMap;

    internal MemberConfigurationExpression(PropertyMap propMap)
    {
        _propMap = propMap;
    }

    public void MapFrom<TSourceMember>(Expression<Func<TSource, TSourceMember>> mapExpression)
    {
        var compiled = mapExpression.Compile();
        _propMap.CustomResolver = (src, dest) => compiled((TSource)src);
    }

    public void MapFrom<TSourceMember>(Func<TSource, TDestination, TSourceMember> mapFunction)
    {
        _propMap.CustomResolver = (src, dest) =>
            mapFunction((TSource)src, dest is TDestination td ? td : default!);
    }

    public void MapFrom<TSourceMember>(Func<TSource, TDestination, TMember, TSourceMember> mapFunction)
    {
        _propMap.CustomResolver = (src, dest) =>
            mapFunction((TSource)src, dest is TDestination td ? td : default!, default!);
    }

    public void MapFrom<TSourceMember>(Func<TSource, TDestination, TMember, ResolutionContext, TSourceMember> mapFunction)
    {
        _propMap.ContextResolver = (src, dest, destMember, ctx) =>
            mapFunction((TSource)src, dest is TDestination td ? td : default!,
                destMember is TMember tm ? tm : default!, ctx);
    }

    public void MapFrom<TValueResolver, TSourceMember>(Expression<Func<TSource, TSourceMember>> sourceMember)
        where TValueResolver : IMemberValueResolver<object, object, TSourceMember, TMember>
    {
        var compiled = sourceMember.Compile();
        _propMap.ValueResolverFactory = sp =>
        {
            var resolver = (TValueResolver)ActivatorUtilities.CreateInstance(sp, typeof(TValueResolver));
            return (src, dest, destMember, ctx) =>
            {
                var srcMember = compiled((TSource)src);
                return resolver.Resolve(src, dest!, srcMember, destMember is TMember dm ? dm : default!, ctx);
            };
        };
    }

    public void Ignore() => _propMap.Ignored = true;

    public void Condition(Func<TSource, bool> condition)
    {
        _propMap.Condition = src => condition((TSource)src);
    }

    public void Condition(Func<TSource, TDestination, bool> condition)
    {
        _propMap.FullCondition = (src, dest) =>
            condition((TSource)src, dest is TDestination td ? td : default!);
    }

    public void PreCondition(Func<TSource, bool> condition)
    {
        _propMap.PreCondition = src => condition((TSource)src);
    }

    public void NullSubstitute(TMember nullSubstitute)
    {
        _propMap.NullSubstitute = nullSubstitute;
        _propMap.HasNullSubstitute = true;
    }

    public void UseValue(TMember value)
    {
        _propMap.UseValue = value;
        _propMap.HasUseValue = true;
    }

    public void UseDestinationValue()
    {
        _propMap.UseDestinationValue = true;
    }
}

internal sealed class PathConfigurationExpression<TSource, TDestination, TMember>
    : IPathConfigurationExpression<TSource, TDestination, TMember>
{
    private readonly PropertyMap _propMap;

    internal PathConfigurationExpression(PropertyMap propMap)
    {
        _propMap = propMap;
    }

    public void MapFrom<TSourceMember>(Expression<Func<TSource, TSourceMember>> mapExpression)
    {
        var compiled = mapExpression.Compile();
        _propMap.CustomResolver = (src, dest) => compiled((TSource)src);
    }

    public void Ignore() => _propMap.Ignored = true;
}
