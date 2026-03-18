using BenchmarkDotNet.Attributes;
using EggMapper.Benchmarks.Configs;
using EggMapper.Benchmarks.Models;
using Mapster;

namespace EggMapper.Benchmarks;

[MemoryDiagnoser]
[RankColumn]
public class FlatMappingBenchmark
{
    private ModelObject _source = null!;

    [GlobalSetup]
    public void Setup()
    {
        MapsterConfig.Configure();
        _source = new ModelObject
        {
            Id = 1,
            Name = "Test User",
            BaseDate = 20240101,
            Description = "A test description",
            Value = 42.5,
            Active = true,
            Email = "test@example.com",
            Phone = "555-1234",
            Status = 1,
            Country = "US"
        };
    }

    [Benchmark(Baseline = true)]
    public ModelDto Manual() => new ModelDto
    {
        Id = _source.Id,
        Name = _source.Name,
        BaseDate = _source.BaseDate,
        Description = _source.Description,
        Value = _source.Value,
        Active = _source.Active,
        Email = _source.Email,
        Phone = _source.Phone,
        Status = _source.Status,
        Country = _source.Country
    };

    [Benchmark]
    public ModelDto EggMapper() => EggMapperConfig.Mapper.Map<ModelObject, ModelDto>(_source);

    [Benchmark]
    public ModelDto AutoMapper() => AutoMapperConfig.Mapper.Map<ModelDto>(_source);

    [Benchmark]
    public ModelDto Mapster() => _source.Adapt<ModelDto>();
}
