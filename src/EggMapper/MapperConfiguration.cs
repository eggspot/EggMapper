using System.Collections.Concurrent;
using EggMapper.Internal;

namespace EggMapper;

public sealed class MapperConfiguration
{
    private readonly ConcurrentDictionary<TypePair, Func<object, object?, ResolutionContext, object>> _compiledMaps = new();
    private readonly Dictionary<TypePair, TypeMap> _typeMaps = new();

    // Frozen (read-only) snapshot built after all maps are compiled.
    // Regular Dictionary reads are significantly faster than ConcurrentDictionary
    // because they require no volatile loads or atomic operations.
    internal Dictionary<TypePair, Func<object, object?, ResolutionContext, object>> FrozenMaps = null!;

    // Ctx-free typed delegates (Func<TSource, TDestination>) for flat maps that do
    // not need a ResolutionContext at call time.  These eliminate all boxing and the
    // ctx/dest parameters from the per-item call in Map<> and MapList<>.
    internal Dictionary<TypePair, Delegate> FrozenCtxFreeMaps = null!;

    public MapperConfiguration(Action<IMapperConfigurationExpression> configure)
    {
        var expr = new MapperConfigurationExpression();
        configure(expr);

        foreach (var typeMap in expr.GetTypeMaps())
        {
            var key = new TypePair(typeMap.SourceType, typeMap.DestinationType);
            _typeMaps[key] = typeMap;
        }

        foreach (var typeMap in TopologicalOrder(_typeMaps))
            CompileMap(typeMap);

        // Snapshot: no further writes will occur to _compiledMaps after construction.
        FrozenMaps = new Dictionary<TypePair, Func<object, object?, ResolutionContext, object>>(_compiledMaps);

        // Build ctx-free typed delegates for eligible maps.
        var ctxFree = new Dictionary<TypePair, Delegate>();
        foreach (var kvp in _typeMaps)
        {
            var del = Execution.ExpressionBuilder.TryBuildCtxFreeDelegate(kvp.Value);
            if (del != null)
                ctxFree[kvp.Key] = del;
        }
        FrozenCtxFreeMaps = ctxFree;
    }

    private static IEnumerable<TypeMap> TopologicalOrder(Dictionary<TypePair, TypeMap> typeMaps)
    {
        var visited = new HashSet<TypePair>();
        var result  = new List<TypeMap>(typeMaps.Count);

        void Visit(TypeMap map)
        {
            var key = new TypePair(map.SourceType, map.DestinationType);
            if (!visited.Add(key)) return;

            var srcDetails  = TypeDetails.Get(map.SourceType);
            var destDetails = TypeDetails.Get(map.DestinationType);

            foreach (var destProp in destDetails.WritableProperties)
            {
                var srcProp = srcDetails.ReadableProperties.FirstOrDefault(p =>
                    string.Equals(p.Name, destProp.Name, StringComparison.OrdinalIgnoreCase));
                if (srcProp == null) continue;

                var nestedPair = new TypePair(srcProp.PropertyType, destProp.PropertyType);
                if (typeMaps.TryGetValue(nestedPair, out var nestedMap))
                    Visit(nestedMap);
            }

            result.Add(map);
        }

        foreach (var kvp in typeMaps) Visit(kvp.Value);
        return result;
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
        FrozenMaps.TryGetValue(typePair, out var del);
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
