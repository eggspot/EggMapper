using System.Linq.Expressions;

namespace EggMapper;

/// <summary>
/// Options for one destination member, used inside <c>ForMember(d =&gt; d.X, o =&gt; ...)</c>.
/// </summary>
public interface IMemberConfigurationExpression<TSource, TDestination, TMember>
{
    /// <summary>
    /// Sets the member from an expression over the source, for example <c>o.MapFrom(s =&gt; s.Customer.Name)</c>.
    /// <c>ProjectTo</c> translates this expression too.
    /// </summary>
    void MapFrom<TSourceMember>(Expression<Func<TSource, TSourceMember>> mapExpression);

    /// <summary>Sets the member from <c>(source, destination) =&gt; value</c>.</summary>
    void MapFrom<TSourceMember>(Func<TSource, TDestination, TSourceMember> mapFunction);

    /// <summary>Sets the member from <c>(source, destination, currentMemberValue) =&gt; value</c>.</summary>
    void MapFrom<TSourceMember>(Func<TSource, TDestination, TMember, TSourceMember> mapFunction);

    /// <summary>Sets the member from <c>(source, destination, currentMemberValue, context) =&gt; value</c>.</summary>
    void MapFrom<TSourceMember>(Func<TSource, TDestination, TMember, ResolutionContext, TSourceMember> mapFunction);
    /// <summary>
    /// Resolve the destination member from a source property by name.
    /// String-based overload: <c>MapFrom("SourcePropertyName")</c>.
    /// </summary>
    void MapFrom(string sourceMemberName);

    /// <summary>
    /// Sets the member with a resolver implementing
    /// <c>IMemberValueResolver&lt;object, object, TSourceMember, TMember&gt;</c>, given the value of
    /// <paramref name="sourceMember"/>. The resolver is created at map time with its constructor dependencies from DI,
    /// so the mapper must come from <c>services.AddEggMapper(...)</c>; <c>config.CreateMapper()</c> throws.
    /// </summary>
    void MapFrom<TValueResolver, TSourceMember>(Expression<Func<TSource, TSourceMember>> sourceMember)
        where TValueResolver : IMemberValueResolver<object, object, TSourceMember, TMember>;

    /// <summary>Leaves the member unmapped; <c>AssertConfigurationIsValid</c> no longer reports it.</summary>
    void Ignore();

    /// <summary>Maps the member only when <paramref name="condition"/> returns true for the source.</summary>
    void Condition(Func<TSource, bool> condition);

    /// <summary>Maps the member only when <paramref name="condition"/> returns true for the source and destination.</summary>
    void Condition(Func<TSource, TDestination, bool> condition);

    /// <summary>Checks <paramref name="condition"/> before the member's value is resolved; skips the member when false.</summary>
    void PreCondition(Func<TSource, bool> condition);

    /// <summary>Uses <paramref name="nullSubstitute"/> when the resolved source value is null.</summary>
    void NullSubstitute(TMember nullSubstitute);

    /// <summary>Always sets the member to <paramref name="value"/>.</summary>
    void UseValue(TMember value);

    /// <summary>Keeps the destination's existing value instead of mapping the member.</summary>
    void UseDestinationValue();
}
