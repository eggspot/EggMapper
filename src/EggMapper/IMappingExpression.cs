using System.Linq.Expressions;

namespace EggMapper;

/// <summary>
/// Configures the map from <typeparamref name="TSource"/> to <typeparamref name="TDestination"/> returned by
/// <c>CreateMap</c>. Members with the same name (case-insensitive) and flattened names (<c>CustomerName</c> from
/// <c>Customer.Name</c>) map without configuration.
/// </summary>
public interface IMappingExpression<TSource, TDestination>
{
    /// <summary>
    /// Configures one destination member, for example
    /// <c>.ForMember(d =&gt; d.Total, o =&gt; o.MapFrom(s =&gt; s.Items.Sum(i =&gt; i.Price)))</c> or <c>o.Ignore()</c>.
    /// </summary>
    IMappingExpression<TSource, TDestination> ForMember<TMember>(
        Expression<Func<TDestination, TMember>> destinationMember,
        Action<IMemberConfigurationExpression<TSource, TDestination, TMember>> memberOptions);

    /// <summary>
    /// Configures a top-level destination member with <c>MapFrom</c> or <c>Ignore</c>. Nested paths such as
    /// <c>d =&gt; d.Address.City</c> are not supported: configure the nested type's own map instead.
    /// </summary>
    IMappingExpression<TSource, TDestination> ForPath<TMember>(
        Expression<Func<TDestination, TMember>> destinationMember,
        Action<IPathConfigurationExpression<TSource, TDestination, TMember>> pathOptions);

    /// <summary>
    /// Also registers the map from <typeparamref name="TDestination"/> to <typeparamref name="TSource"/>, matching
    /// members by name only: <c>MapFrom</c> expressions are not inverted and flattened members are not unflattened.
    /// Configure that direction with its own <c>CreateMap</c> when it needs more. Returns this (forward) map.
    /// </summary>
    IMappingExpression<TSource, TDestination> ReverseMap();

    /// <summary>Creates the destination with <paramref name="ctor"/> instead of a constructor EggMapper picks.</summary>
    IMappingExpression<TSource, TDestination> ConstructUsing(Func<TSource, TDestination> ctor);
    IMappingExpression<TSource, TDestination> ConstructUsing(Func<TSource, ResolutionContext, TDestination> ctor);

    /// <summary>Runs <paramref name="beforeFunction"/> on the source and the new destination before members are mapped.</summary>
    IMappingExpression<TSource, TDestination> BeforeMap(Action<TSource, TDestination> beforeFunction);
    IMappingExpression<TSource, TDestination> BeforeMap(Action<TSource, TDestination, ResolutionContext> beforeFunction);

    /// <summary>Runs <paramref name="afterFunction"/> on the source and the destination after all members are mapped.</summary>
    IMappingExpression<TSource, TDestination> AfterMap(Action<TSource, TDestination> afterFunction);
    IMappingExpression<TSource, TDestination> AfterMap(Action<TSource, TDestination, ResolutionContext> afterFunction);

    /// <summary>
    /// Inherits the member configuration of the registered <c>CreateMap&lt;TSourceBase, TDestinationBase&gt;()</c>.
    /// Members configured on this map win.
    /// </summary>
    IMappingExpression<TSource, TDestination> IncludeBase<TSourceBase, TDestinationBase>();

    /// <summary>
    /// Makes every registered map whose source and destination derive from this map's types inherit this map's member
    /// configuration, as if each called <c>IncludeBase</c>. It does not choose a derived map at run time:
    /// <c>Map&lt;Base, BaseDto&gt;</c> always produces a <c>BaseDto</c>.
    /// </summary>
    IMappingExpression<TSource, TDestination> IncludeAllDerived();

    /// <summary>Stops mapping nested objects of this pair deeper than <paramref name="depth"/> levels (self-referencing types).</summary>
    IMappingExpression<TSource, TDestination> MaxDepth(int depth);

    /// <summary>
    /// Replaces member-by-member mapping for this pair with <paramref name="converter"/>.
    /// </summary>
    IMappingExpression<TSource, TDestination> ConvertUsing(Func<TSource, TDestination> converter);
    IMappingExpression<TSource, TDestination> ConvertUsing(Func<TSource, TDestination?, TDestination> converter);
    IMappingExpression<TSource, TDestination> ConvertUsing(Func<TSource, TDestination?, ResolutionContext, TDestination> converter);
    IMappingExpression<TSource, TDestination> ConvertUsing(ITypeConverter<TSource, TDestination> converter);
    IMappingExpression<TSource, TDestination> ConvertUsing<TConverter>() where TConverter : ITypeConverter<TSource, TDestination>, new();
    IMappingExpression<TSource, TDestination> ConvertUsing(Type converterType);

    IMappingExpression<TSource, TDestination> ForAllMembers(
        Action<IMemberConfigurationExpression<TSource, TDestination, object>> memberOptions);

    /// <summary>
    /// Applies <paramref name="memberOptions"/> to every writable destination property that has
    /// not already been explicitly configured via <c>ForMember</c>.  Useful for patterns such as
    /// "ignore all unmapped members" without having to enumerate them individually.
    /// </summary>
    IMappingExpression<TSource, TDestination> ForAllOtherMembers(
        Action<IMemberConfigurationExpression<TSource, TDestination, object>> memberOptions);

    /// <summary>
    /// Adds an inline validation rule that runs after all properties are mapped.
    /// If <paramref name="predicate"/> returns false for the mapped destination value,
    /// <paramref name="errorMessage"/> is collected and thrown as part of a
    /// <see cref="MappingValidationException"/> once all rules have been evaluated.
    /// Maps with validation rules are routed to the flexible delegate path.
    /// </summary>
    IMappingExpression<TSource, TDestination> Validate<TMember>(
        Expression<Func<TDestination, TMember>> destinationMember,
        Func<TMember, bool> predicate,
        string errorMessage);
}

/// <summary>
/// Non-generic mapping expression for CreateMap(Type, Type) scenarios.
/// </summary>
public interface IMappingExpression
{
    IMappingExpression ForMember(string destinationMember, Action<IMemberConfigurationExpression> memberOptions);
    IMappingExpression IncludeAllDerived();
    IMappingExpression ConvertUsing(Type converterType);
}

/// <summary>
/// Non-generic member configuration for string-based ForMember.
/// </summary>
public interface IMemberConfigurationExpression
{
    void MapFrom(string sourceMemberName);
    void Ignore();
}
