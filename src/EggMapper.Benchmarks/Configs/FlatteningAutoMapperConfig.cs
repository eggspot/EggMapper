using EggMapper.Benchmarks.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace EggMapper.Benchmarks.Configs;

public static class FlatteningAutoMapperConfig
{
    public static readonly global::AutoMapper.IMapper Mapper;

    static FlatteningAutoMapperConfig()
    {
        var config = new global::AutoMapper.MapperConfiguration(
            (global::AutoMapper.IMapperConfigurationExpression cfg) =>
            {
                cfg.CreateMap<FlatteningSource, FlatteningDest>();
            }, NullLoggerFactory.Instance);
        Mapper = config.CreateMapper();
    }
}
