namespace EggMapper;

/// <summary>
/// Resolves a destination member value from a source member value.
/// Supports DI constructor injection — register the resolver in your DI container.
/// Compatible with AutoMapper's IMemberValueResolver pattern.
/// </summary>
public interface IMemberValueResolver<TSource, TDestination, TSourceMember, TDestMember>
{
    TDestMember Resolve(TSource source, TDestination destination, TSourceMember sourceMember,
        TDestMember destMember, ResolutionContext context);
}
