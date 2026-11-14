using BenchmarkDotNet.Attributes;
using EggMapper.Benchmarks.Configs;
using EggMapper.Benchmarks.Models;

namespace EggMapper.Benchmarks;

[MemoryDiagnoser]
[RankColumn]
public class ObjectSourceBenchmark
{
    private object _source = null!;
    private object _proxySource = null!;
    private int _turn;

    public sealed class ModelObjectProxy : ModelObject
    {
    }

    [GlobalSetup]
    public void Setup()
    {
        _source = Create(new ModelObject());
        _proxySource = Create(new ModelObjectProxy());
    }

    private static ModelObject Create(ModelObject model)
    {
        model.Id = 1;
        model.Name = "Test User";
        model.BaseDate = 20240101;
        model.Description = "A test description";
        model.Value = 42.5;
        model.Active = true;
        model.Email = "test@example.com";
        model.Phone = "555-1234";
        model.Status = 1;
        model.Country = "US";
        return model;
    }

    [Benchmark(Baseline = true)]
    public ModelDto Manual()
    {
        var s = (ModelObject)_source;
        return new ModelDto
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
        };
    }

    [Benchmark]
    public ModelDto EggMapper() => EggMapperConfig.Mapper.Map<ModelDto>(_source);

    [Benchmark]
    public ModelDto AutoMapper() => AutoMapperConfig.Mapper.Map<ModelDto>(_source);

    [Benchmark]
    public ModelDto EggMapperDerivedSource() => EggMapperConfig.Mapper.Map<ModelDto>(_proxySource);

    [Benchmark]
    public ModelDto EggMapperAlternatingSources() =>
        EggMapperConfig.Mapper.Map<ModelDto>((_turn++ & 1) == 0 ? _source : _proxySource);
}
