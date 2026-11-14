namespace EggMapper;

internal sealed class TypedMapper<TSource, TDestination> : IMapper<TSource, TDestination>
{
    private readonly IMapper _mapper;
    private readonly Mapper? _concreteMapper;
    private Func<TSource, TDestination, TDestination>? _ctxFreeMap;

    public TypedMapper(IMapper mapper)
    {
        _mapper = mapper;
        _concreteMapper = mapper as Mapper;
    }

    public TDestination Map(TSource source)
    {
        var map = _ctxFreeMap;
        if (map != null && source != null)
            return map(source, default!);
        var result = _mapper.Map<TSource, TDestination>(source);
        _ctxFreeMap ??= _concreteMapper?.GetCachedCtxFreeMap<TSource, TDestination>();
        return result;
    }

    public TDestination Map(TSource source, TDestination destination)
    {
        var map = _ctxFreeMap;
        if (map != null && source != null)
            return map(source, destination);
        var result = _mapper.Map(source, destination);
        _ctxFreeMap ??= _concreteMapper?.GetCachedCtxFreeMap<TSource, TDestination>();
        return result;
    }

    public List<TDestination> MapList(IEnumerable<TSource> source) => _mapper.MapList<TSource, TDestination>(source);

    public TDestination Patch(TSource source, TDestination destination) => _mapper.Patch(source, destination);
}
