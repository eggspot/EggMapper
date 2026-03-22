using BenchmarkDotNet.Attributes;
using EggMapper.Benchmarks.Models;
using Mapster;

namespace EggMapper.Benchmarks;

/// <summary>
/// Measures the total time from zero to having mapped one instance of every registered type pair.
/// This is the real-world "cold start" cost: config creation + first Map() call per type pair.
///
/// Unlike StartupBenchmark (which only measures config construction), this benchmark exposes the
/// lazy-compilation cost that AutoMapper and Mapster defer to first use. EggMapper pays upfront
/// at config time; competitors pay here instead.
/// </summary>
[MemoryDiagnoser]
[RankColumn]
public class ColdStartBenchmark
{
    private static readonly ModelObject _model = new()
    {
        Id = 1, Name = "test", BaseDate = 20230101, Value = 1.5,
        Active = true, Email = "a@b.com", Status = 2, Country = "US"
    };

    private static readonly Customer _customer = new()
    {
        Id = 1, Name = "Alice",
        HomeAddress = new() { Street = "1 Main St", City = "Boston", State = "MA", Zip = "02101", Country = "US" },
        WorkAddress = new() { Street = "2 Work Ave", City = "Boston", State = "MA", Zip = "02102", Country = "US" }
    };

    private static readonly Foo _foo = new()
    {
        FooId = 1, FooName = "foo",
        Inner = new() { InnerFooId = 2, InnerFooName = "inner" },
        InnerFoos = [new() { InnerFooId = 3, InnerFooName = "inner2" }]
    };

    [Benchmark(Baseline = true)]
    public ModelDto EggMapper()
    {
        var config = new global::EggMapper.MapperConfiguration(cfg =>
        {
            cfg.CreateMap<ModelObject, ModelDto>();
            cfg.CreateMap<Customer, CustomerDTO>();
            cfg.CreateMap<Address, AddressDTO>();
            cfg.CreateMap<Foo, FooDest>();
            cfg.CreateMap<InnerFoo, InnerFooDest>();
        });
        var mapper = config.CreateMapper();
        // All delegates already compiled — these calls are instant
        mapper.Map<Customer, CustomerDTO>(_customer);
        mapper.Map<Foo, FooDest>(_foo);
        return mapper.Map<ModelObject, ModelDto>(_model);
    }

    [Benchmark]
    public ModelDto AutoMapper()
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
        var mapper = config.CreateMapper();
        // These calls trigger lazy compilation for each type pair
        mapper.Map<CustomerDTO>(_customer);   // compiles Customer+Address maps
        mapper.Map<FooDest>(_foo);            // compiles Foo+InnerFoo maps
        return mapper.Map<ModelDto>(_model);  // compiles ModelObject map
    }

    [Benchmark]
    public ModelDto Mapster()
    {
        var config = new TypeAdapterConfig();
        config.NewConfig<ModelObject, ModelDto>();
        config.NewConfig<Customer, CustomerDTO>();
        config.NewConfig<Address, AddressDTO>();
        config.NewConfig<Foo, FooDest>();
        config.NewConfig<InnerFoo, InnerFooDest>();
        // These calls trigger lazy compilation for each type pair
        _customer.Adapt<CustomerDTO>(config);
        _foo.Adapt<FooDest>(config);
        return _model.Adapt<ModelDto>(config);
    }
}
