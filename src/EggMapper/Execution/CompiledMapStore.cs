using System.Collections.Concurrent;
using EggMapper.Internal;

namespace EggMapper.Execution;

internal sealed class CompiledMapStore
{
    private readonly ConcurrentDictionary<TypePair, Func<object, object?, ResolutionContext, object>> _maps = new();
    private readonly MapperConfiguration _configuration;

    internal CompiledMapStore(MapperConfiguration configuration)
    {
        _configuration = configuration;
    }

    internal void Add(TypePair key, Func<object, object?, ResolutionContext, object> map) => _maps[key] = map;

    internal bool TryGetCompiled(TypePair key, out Func<object, object?, ResolutionContext, object> map)
        => _maps.TryGetValue(key, out map!);

    internal bool TryGetOrCompile(TypePair key, out Func<object, object?, ResolutionContext, object> map)
    {
        if (_maps.TryGetValue(key, out map!))
            return true;
        return _configuration.TryGetOrCompile(key) && _maps.TryGetValue(key, out map!);
    }

    internal bool TryGetOrCompileForBuild(TypePair key, out Func<object, object?, ResolutionContext, object> map)
    {
        try
        {
            return TryGetOrCompile(key, out map);
        }
        catch (Exception ex) when (!ex.IsFatal())
        {
            map = null!;
            return false;
        }
    }
}
