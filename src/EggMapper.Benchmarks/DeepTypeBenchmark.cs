using BenchmarkDotNet.Attributes;
using EggMapper.Benchmarks.Configs;
using EggMapper.Benchmarks.Models;
using Mapster;

namespace EggMapper.Benchmarks;

[MemoryDiagnoser]
[RankColumn]
public class DeepTypeBenchmark
{
    private Customer _source = null!;

    [GlobalSetup]
    public void Setup()
    {
        MapsterConfig.Configure();
        _source = new Customer
        {
            Id = 1,
            Name = "Jane Doe",
            HomeAddress = new Address
            {
                Street = "123 Main St",
                City = "Springfield",
                State = "IL",
                Zip = "62701",
                Country = "US"
            },
            WorkAddress = new Address
            {
                Street = "456 Office Blvd",
                City = "Springfield",
                State = "IL",
                Zip = "62702",
                Country = "US"
            }
        };
    }

    [Benchmark(Baseline = true)]
    public CustomerDTO Manual() => new CustomerDTO
    {
        Id = _source.Id,
        Name = _source.Name,
        HomeAddress = new AddressDTO
        {
            Street = _source.HomeAddress.Street,
            City = _source.HomeAddress.City,
            State = _source.HomeAddress.State,
            Zip = _source.HomeAddress.Zip,
            Country = _source.HomeAddress.Country
        },
        WorkAddress = new AddressDTO
        {
            Street = _source.WorkAddress.Street,
            City = _source.WorkAddress.City,
            State = _source.WorkAddress.State,
            Zip = _source.WorkAddress.Zip,
            Country = _source.WorkAddress.Country
        }
    };

    [Benchmark]
    public CustomerDTO EggMapper() => EggMapperConfig.Mapper.Map<Customer, CustomerDTO>(_source);

    [Benchmark]
    public CustomerDTO AutoMapper() => AutoMapperConfig.Mapper.Map<CustomerDTO>(_source);

    [Benchmark]
    public CustomerDTO Mapster() => _source.Adapt<CustomerDTO>();
}
