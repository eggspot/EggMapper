using BenchmarkDotNet.Attributes;
using EggMapper.Benchmarks.Configs;
using EggMapper.Benchmarks.Models;
using Mapster;

namespace EggMapper.Benchmarks;

[MemoryDiagnoser]
[RankColumn]
public class CollectionBenchmark
{
    private List<ModelObject> _source = null!;

    [GlobalSetup]
    public void Setup()
    {
        MapsterConfig.Configure();
        _source = Enumerable.Range(1, 100).Select(i => new ModelObject
        {
            Id = i,
            Name = $"User {i}",
            BaseDate = 20240101 + i,
            Description = $"Description {i}",
            Value = i * 1.5,
            Active = i % 2 == 0,
            Email = $"user{i}@example.com",
            Phone = $"555-{i:D4}",
            Status = i % 3,
            Country = "US"
        }).ToList();
    }

    [Benchmark(Baseline = true)]
    public List<ModelDto> Manual() => _source.Select(s => new ModelDto
    {
        Id = s.Id,
        Name = s.Name,
        BaseDate = s.BaseDate,
        Description = s.Description,
        Value = s.Value,
        Active = s.Active,
        Email = s.Email,
        Phone = s.Phone,
        Status = s.Status,
        Country = s.Country
    }).ToList();

    [Benchmark]
    public List<ModelDto> EggMapper() => _source
        .Select(s => EggMapperConfig.Mapper.Map<ModelObject, ModelDto>(s))
        .ToList();

    [Benchmark]
    public List<ModelDto> AutoMapper() => AutoMapperConfig.Mapper.Map<List<ModelDto>>(_source);

    [Benchmark]
    public List<ModelDto> Mapster() => _source.Adapt<List<ModelDto>>();
}
