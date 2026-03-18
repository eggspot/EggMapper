using System.Collections.Concurrent;
using EggMapper.Internal;

namespace EggMapper;

public sealed class MapperConfiguration
{
    private readonly ConcurrentDictionary<TypePair, Func<object, object?, ResolutionContext, object>> _compiledMaps = new();
    private readonly Dictionary<TypePair, TypeMap> _typeMaps = new();

    public MapperConfiguration(Action<IMapperConfigurationExpression> configure)
    {
        var expr = new MapperConfigurationExpression();
        configure(expr);

        foreach (var typeMap in expr.GetTypeMaps())
        {
            var key = new TypePair(typeMap.SourceType, typeMap.DestinationType);
            _typeMaps[key] = typeMap;
        }

        foreach (var kvp in _typeMaps)
            CompileMap(kvp.Value);
    }

    private void CompileMap(TypeMap typeMap)
    {
        var key = new TypePair(typeMap.SourceType, typeMap.DestinationType);
        if (_compiledMaps.ContainsKey(key)) return;

        var compiledDelegate = Execution.ExpressionBuilder.BuildMappingDelegate(typeMap, _typeMaps, _compiledMaps);
        _compiledMaps[key] = compiledDelegate;
        typeMap.MappingDelegate = compiledDelegate;
    }

    public IMapper CreateMapper() => new Mapper(this);

    internal Func<object, object?, ResolutionContext, object>? GetMapDelegate(TypePair typePair)
    {
        _compiledMaps.TryGetValue(typePair, out var del);
        return del;
    }

    internal Dictionary<TypePair, TypeMap> TypeMaps => _typeMaps;

    public void AssertConfigurationIsValid()
    {
        var errors = new List<string>();
        foreach (var kvp in _typeMaps)
        {
            var typeMap = kvp.Value;
            var destDetails = TypeDetails.Get(typeMap.DestinationType);
            var srcDetails = TypeDetails.Get(typeMap.SourceType);

            foreach (var destProp in destDetails.WritableProperties)
            {
                var propMap = typeMap.PropertyMaps.FirstOrDefault(p =>
                    p.DestinationProperty.Name == destProp.Name);

                if (propMap?.Ignored == true) continue;
                if (propMap?.HasUseValue == true) continue;
                if (propMap?.CustomResolver != null) continue;

                var sourceMemberName = propMap?.SourceMemberName ?? destProp.Name;
                var srcProp = srcDetails.ReadableProperties.FirstOrDefault(p =>
                    string.Equals(p.Name, sourceMemberName, StringComparison.OrdinalIgnoreCase));

                if (srcProp == null && !ReflectionHelper.HasFlattenedSource(destProp.Name, srcDetails))
                {
                    errors.Add(
                        $"Unmapped destination member '{typeMap.DestinationType.Name}.{destProp.Name}' " +
                        $"in map {typeMap.SourceType.Name} -> {typeMap.DestinationType.Name}");
                }
            }
        }

        if (errors.Count > 0)
            throw new InvalidOperationException(
                "EggMapper configuration is invalid:\n" + string.Join("\n", errors));
    }
}
