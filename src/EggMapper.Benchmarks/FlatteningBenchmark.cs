using BenchmarkDotNet.Attributes;
using EggMapper.Benchmarks.Configs;
using EggMapper.Benchmarks.Models;
using Mapster;

namespace EggMapper.Benchmarks;

[MemoryDiagnoser]
[RankColumn]
public class FlatteningBenchmark
{
    private FlatteningSource _source = null!;

    private static readonly global::EggMapper.MapperConfiguration EggConfig = new(cfg =>
        cfg.CreateMap<FlatteningSource, FlatteningDest>());

    private static readonly global::EggMapper.IMapper EggMapper = EggConfig.CreateMapper();

    private static readonly AutoMapper.IMapper AutoMap;

    static FlatteningBenchmark()
    {
        var amConfig = new AutoMapper.MapperConfiguration(cfg =>
            cfg.CreateMap<FlatteningSource, FlatteningDest>());
        AutoMap = amConfig.CreateMapper();

        TypeAdapterConfig<FlatteningSource, FlatteningDest>.NewConfig();
    }

    [GlobalSetup]
    public void Setup()
    {
        _source = new FlatteningSource
        {
            Id = 1,
            Title = "Test",
            Address = new FlatteningAddress
            {
                Street = "123 Main St",
                City = "Springfield",
                State = "IL",
                Zip = "62701"
            },
            Contact = new FlatteningContact
            {
                Email = "test@example.com",
                Phone = "555-1234"
            }
        };
    }

    [Benchmark(Baseline = true)]
    public FlatteningDest Manual() => new FlatteningDest
    {
        Id = _source.Id,
        Title = _source.Title,
        AddressStreet = _source.Address.Street,
        AddressCity = _source.Address.City,
        AddressState = _source.Address.State,
        AddressZip = _source.Address.Zip,
        ContactEmail = _source.Contact.Email,
        ContactPhone = _source.Contact.Phone
    };

    [Benchmark]
    public FlatteningDest EggMap() => EggMapper.Map<FlatteningSource, FlatteningDest>(_source);

    [Benchmark]
    public FlatteningDest AutoMapper() => AutoMap.Map<FlatteningDest>(_source);

    [Benchmark]
    public FlatteningDest Mapster() => _source.Adapt<FlatteningDest>();
}
