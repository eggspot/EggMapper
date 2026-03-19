using AgileObjects.AgileMapper;
using EggMapper.Benchmarks.Models;

namespace EggMapper.Benchmarks.Configs;

public static class AgileMapperConfig
{
    public static readonly global::AgileObjects.AgileMapper.Api.Configuration.MappingConfigStartingPoint Config;

    static AgileMapperConfig()
    {
        // AgileMapper uses a static Mapper.Map<> API by default
        // No explicit config needed for convention-based mapping
        Config = global::AgileObjects.AgileMapper.Mapper.WhenMapping;
    }
}
