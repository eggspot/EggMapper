using EggMapper.Internal;

namespace EggMapper.Execution;

internal sealed class EnumPairMapping
{
    public EnumPairMapping(EnumMappingMode mode, IReadOnlyDictionary<object, object> values)
    {
        Mode = mode;
        Values = values;
    }

    public EnumMappingMode Mode { get; }
    public IReadOnlyDictionary<object, object> Values { get; }

    public static EnumPairMapping For(EnumMappingMode mode)
        => mode == EnumMappingMode.ByName ? ByNameDefault : ByValueDefault;

    private static readonly EnumPairMapping ByValueDefault = new(EnumMappingMode.ByValue, new Dictionary<object, object>());
    private static readonly EnumPairMapping ByNameDefault = new(EnumMappingMode.ByName, new Dictionary<object, object>());
}

internal sealed class TypeConversionRegistry
{
    private readonly Dictionary<TypePair, Delegate> _converters;
    private readonly Dictionary<TypePair, EnumPairMapping> _enumPairs;
    private readonly HashSet<TypePair> _enumPairsWithConvertUsing;

    public TypeConversionRegistry(
        Dictionary<TypePair, Delegate> converters,
        EnumMappingMode defaultEnumMapping,
        IEnumerable<TypeMap> typeMaps)
    {
        DefaultEnumMapping = defaultEnumMapping;
        _converters = new Dictionary<TypePair, Delegate>(converters);
        _enumPairs = new Dictionary<TypePair, EnumPairMapping>();
        _enumPairsWithConvertUsing = new HashSet<TypePair>();
        foreach (var typeMap in typeMaps)
        {
            if (!typeMap.SourceType.IsEnum || !typeMap.DestinationType.IsEnum) continue;
            var pair = new TypePair(typeMap.SourceType, typeMap.DestinationType);
            if (typeMap.ConvertUsingFunc != null)
            {
                _enumPairsWithConvertUsing.Add(pair);
                _converters.Remove(pair);
            }
            else if (typeMap.EnumMapping != null)
            {
                _enumPairs[pair] = typeMap.EnumMapping;
                _converters.Remove(pair);
            }
        }
    }

    public EnumMappingMode DefaultEnumMapping { get; }

    public bool TryGetValue(TypePair pair, out Delegate converter) => _converters.TryGetValue(pair, out converter!);

    public bool HasConvertUsing(TypePair enumPair) => _enumPairsWithConvertUsing.Contains(enumPair);

    public EnumPairMapping GetEnumPairMapping(TypePair enumPair)
        => _enumPairs.TryGetValue(enumPair, out var mapping) ? mapping : EnumPairMapping.For(DefaultEnumMapping);
}
