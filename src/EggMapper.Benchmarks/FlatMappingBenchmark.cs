using BenchmarkDotNet.Attributes;
using EggMapper.Benchmarks.Configs;
using EggMapper.Benchmarks.Generated;
using EggMapper.Benchmarks.Models;
using Mapster;

namespace EggMapper.Benchmarks;

[MemoryDiagnoser]
[RankColumn]
public class FlatMappingBenchmark
{
    private ModelObject _source = null!;
    private static readonly MapperlyMapper Mapperly = new();
    private static readonly ModelObjectMapper ClassMapper = ModelObjectMapper.Instance;

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

    [Benchmark]
    public ModelDto MapperlyMap() => Mapperly.MapFlat(_source);

    [Benchmark]
    public ModelDto AgileMapper() => AgileObjects.AgileMapper.Mapper.Map(_source).ToANew<ModelDto>();

    // ── Compile-time (Tier 2 & 3) ──────────────────────────────────────────────

    /// <summary>
    /// EggMapper.Generator (Tier 2): [MapTo(typeof(ModelDto))] on ModelObject.
    /// Extension method generated at compile time — zero reflection, zero allocation.
    /// </summary>
    [Benchmark]
    public ModelDto EggMapperGenerator() => _source.ToModelDto();

    /// <summary>
    /// EggMapper.ClassMapper (Tier 3): [EggMapper] partial class mapper.
    /// Partial method implemented at compile time — zero reflection, zero allocation.
    /// </summary>
    [Benchmark]
    public ModelDto EggMapperClassMapper() => ClassMapper.Map(_source);
}
