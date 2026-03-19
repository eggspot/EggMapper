using BenchmarkDotNet.Attributes;
using EggMapper.Benchmarks.Configs;
using EggMapper.Benchmarks.Models;
using Mapster;

namespace EggMapper.Benchmarks;

[MemoryDiagnoser]
[RankColumn]
public class LargeCollectionBenchmark
{
    private List<ModelObject> _source = null!;

    [GlobalSetup]
    public void Setup()
    {
        MapsterConfig.Configure();
        _source = Enumerable.Range(1, 1000).Select(i => new ModelObject
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
    public List<ModelDto> Manual()
    {
        var result = new List<ModelDto>(_source.Count);
        for (int i = 0; i < _source.Count; i++)
        {
            var s = _source[i];
            result.Add(new ModelDto
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
            });
        }
        return result;
    }

    [Benchmark]
    public List<ModelDto> EggMapper() =>
        EggMapperConfig.Mapper.MapList<ModelObject, ModelDto>(_source);

    [Benchmark]
    public List<ModelDto> AutoMapper() => AutoMapperConfig.Mapper.Map<List<ModelDto>>(_source);

    [Benchmark]
    public List<ModelDto> Mapster() => _source.Adapt<List<ModelDto>>();

    [Benchmark]
    public List<ModelDto> MapperlyMap() => new MapperlyMapper().MapFlatList(_source);

    [Benchmark]
    public List<ModelDto> AgileMapper() => AgileObjects.AgileMapper.Mapper.Map(_source).ToANew<List<ModelDto>>();
}
