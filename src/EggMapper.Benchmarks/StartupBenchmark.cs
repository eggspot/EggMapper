using BenchmarkDotNet.Attributes;
using EggMapper.Benchmarks.Models;
using Mapster;

namespace EggMapper.Benchmarks;

[MemoryDiagnoser]
[RankColumn]
public class StartupBenchmark
{
    [Benchmark(Baseline = true)]
    public global::EggMapper.IMapper EggMapperStartup()
    {
        var config = new global::EggMapper.MapperConfiguration(cfg =>
        {
            cfg.CreateMap<ModelObject, ModelDto>();
            cfg.CreateMap<Customer, CustomerDTO>();
            cfg.CreateMap<Address, AddressDTO>();
            cfg.CreateMap<Foo, FooDest>();
            cfg.CreateMap<InnerFoo, InnerFooDest>();
        });
        return config.CreateMapper();
    }

    [Benchmark]
    public global::AutoMapper.IMapper AutoMapperStartup()
    {
        var config = new global::AutoMapper.MapperConfiguration(
            (global::AutoMapper.IMapperConfigurationExpression cfg) =>
            {
                cfg.CreateMap<ModelObject, ModelDto>();
                cfg.CreateMap<Customer, CustomerDTO>();
                cfg.CreateMap<Address, AddressDTO>();
                cfg.CreateMap<Foo, FooDest>();
                cfg.CreateMap<InnerFoo, InnerFooDest>();
            }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        return config.CreateMapper();
    }

    [Benchmark]
    public TypeAdapterConfig MapsterStartup()
    {
        var config = new TypeAdapterConfig();
        config.NewConfig<ModelObject, ModelDto>();
        config.NewConfig<Customer, CustomerDTO>();
        config.NewConfig<Address, AddressDTO>();
        config.NewConfig<Foo, FooDest>();
        config.NewConfig<InnerFoo, InnerFooDest>();
        return config;
    }
}
