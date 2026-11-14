using BenchmarkDotNet.Attributes;
using EggMapper.Benchmarks.Configs;
using EggMapper.Benchmarks.Models;
using Mapster;

namespace EggMapper.Benchmarks;

[MemoryDiagnoser]
[RankColumn]
public class MapIntoExistingBenchmark
{
    private ModelObject _source = null!;
    private readonly ModelDto _destination = new();

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
    public ModelDto Manual()
    {
        var d = _destination;
        var s = _source;
        d.Id = s.Id;
        d.Name = s.Name;
        d.BaseDate = s.BaseDate;
        d.Description = s.Description;
        d.Value = s.Value;
        d.Active = s.Active;
        d.Email = s.Email;
        d.Phone = s.Phone;
        d.Status = s.Status;
        d.Country = s.Country;
        return d;
    }

    [Benchmark]
    public ModelDto EggMapper() => EggMapperConfig.Mapper.Map(_source, _destination);

    [Benchmark]
    public ModelDto AutoMapper() => AutoMapperConfig.Mapper.Map(_source, _destination);

    [Benchmark]
    public ModelDto Mapster() => _source.Adapt(_destination);
}
