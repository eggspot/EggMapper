using BenchmarkDotNet.Attributes;
using EggMapper.Benchmarks.Configs;
using EggMapper.Benchmarks.Models;
using Mapster;

namespace EggMapper.Benchmarks;

[MemoryDiagnoser]
[RankColumn]
public class ComplexTypeBenchmark
{
    private Foo _source = null!;

    [GlobalSetup]
    public void Setup()
    {
        MapsterConfig.Configure();
        _source = new Foo
        {
            FooId = 1,
            FooName = "Root Foo",
            Inner = new InnerFoo { InnerFooId = 10, InnerFooName = "Inner One" },
            InnerFoos = new List<InnerFoo>
            {
                new InnerFoo { InnerFooId = 11, InnerFooName = "Inner Two" },
                new InnerFoo { InnerFooId = 12, InnerFooName = "Inner Three" },
                new InnerFoo { InnerFooId = 13, InnerFooName = "Inner Four" }
            }
        };
    }

    [Benchmark(Baseline = true)]
    public FooDest Manual()
    {
        var innerFoos = new List<InnerFooDest>(_source.InnerFoos.Count);
        for (int i = 0; i < _source.InnerFoos.Count; i++)
        {
            var x = _source.InnerFoos[i];
            innerFoos.Add(new InnerFooDest
            {
                InnerFooId = x.InnerFooId,
                InnerFooName = x.InnerFooName
            });
        }
        return new FooDest
        {
            FooId = _source.FooId,
            FooName = _source.FooName,
            Inner = new InnerFooDest
            {
                InnerFooId = _source.Inner.InnerFooId,
                InnerFooName = _source.Inner.InnerFooName
            },
            InnerFoos = innerFoos
        };
    }

    [Benchmark]
    public FooDest EggMapper() => EggMapperConfig.Mapper.Map<Foo, FooDest>(_source);

    [Benchmark]
    public FooDest AutoMapper() => AutoMapperConfig.Mapper.Map<FooDest>(_source);

    [Benchmark]
    public FooDest Mapster() => _source.Adapt<FooDest>();
}
