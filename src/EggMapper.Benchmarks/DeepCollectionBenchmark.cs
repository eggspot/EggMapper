using BenchmarkDotNet.Attributes;
using EggMapper.Benchmarks.Configs;
using EggMapper.Benchmarks.Models;
using Mapster;

namespace EggMapper.Benchmarks;

/// <summary>
/// Benchmarks mapping a collection of objects that each contain nested objects,
/// combining the deep-type and collection scenarios.
/// </summary>
[MemoryDiagnoser]
[RankColumn]
public class DeepCollectionBenchmark
{
    private List<Customer> _source = null!;

    [GlobalSetup]
    public void Setup()
    {
        MapsterConfig.Configure();
        _source = Enumerable.Range(1, 100).Select(i => new Customer
        {
            Id = i,
            Name = $"Customer {i}",
            HomeAddress = new Address
            {
                Street = $"{i} Home St",
                City = "HomeTown",
                State = "HM",
                Zip = $"{10000 + i}",
                Country = "US"
            },
            WorkAddress = new Address
            {
                Street = $"{i} Work Ave",
                City = "WorkCity",
                State = "WK",
                Zip = $"{20000 + i}",
                Country = "US"
            }
        }).ToList();
    }

    [Benchmark(Baseline = true)]
    public List<CustomerDTO> Manual()
    {
        var result = new List<CustomerDTO>(_source.Count);
        for (int i = 0; i < _source.Count; i++)
        {
            var s = _source[i];
            result.Add(new CustomerDTO
            {
                Id = s.Id,
                Name = s.Name,
                HomeAddress = new AddressDTO
                {
                    Street = s.HomeAddress.Street,
                    City = s.HomeAddress.City,
                    State = s.HomeAddress.State,
                    Zip = s.HomeAddress.Zip,
                    Country = s.HomeAddress.Country
                },
                WorkAddress = new AddressDTO
                {
                    Street = s.WorkAddress.Street,
                    City = s.WorkAddress.City,
                    State = s.WorkAddress.State,
                    Zip = s.WorkAddress.Zip,
                    Country = s.WorkAddress.Country
                }
            });
        }
        return result;
    }

    [Benchmark]
    public List<CustomerDTO> EggMapper() =>
        EggMapperConfig.Mapper.MapList<Customer, CustomerDTO>(_source);

    [Benchmark]
    public List<CustomerDTO> AutoMapper() =>
        AutoMapperConfig.Mapper.Map<List<CustomerDTO>>(_source);

    [Benchmark]
    public List<CustomerDTO> Mapster() => _source.Adapt<List<CustomerDTO>>();

    [Benchmark]
    public List<CustomerDTO> MapperlyMap() => new MapperlyMapper().MapCustomerList(_source);

    [Benchmark]
    public List<CustomerDTO> AgileMapper() =>
        AgileObjects.AgileMapper.Mapper.Map(_source).ToANew<List<CustomerDTO>>();
}
