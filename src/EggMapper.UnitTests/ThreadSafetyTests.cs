using EggMapper;
using EggMapper.UnitTests.TestModels;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

public class ThreadSafetyTests
{
    [Fact]
    public async Task ConcurrentMapping_SameMapper_ProducesCorrectResults()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<FlatSource, FlatDest>();
            cfg.CreateMap<AddressSource, AddressDest>();
            cfg.CreateMap<PersonSource, PersonDest>();
        });
        var mapper = config.CreateMapper();
        var errors = new List<Exception>();
        var tasks = new Task[50];

        for (int t = 0; t < tasks.Length; t++)
        {
            var index = t;
            tasks[t] = Task.Run(() =>
            {
                try
                {
                    for (int i = 0; i < 100; i++)
                    {
                        var src = new FlatSource
                        {
                            Name = $"Thread{index}_Item{i}",
                            Age = index * 100 + i,
                            Value = index + i * 0.1,
                            Email = $"t{index}i{i}@test.com",
                            IsActive = i % 2 == 0
                        };
                        var dest = mapper.Map<FlatSource, FlatDest>(src);
                        if (dest.Name != src.Name || dest.Age != src.Age)
                            throw new Exception($"Mismatch: expected {src.Name}/{src.Age}, got {dest.Name}/{dest.Age}");
                    }
                }
                catch (Exception ex)
                {
                    lock (errors) errors.Add(ex);
                }
            });
        }

        await Task.WhenAll(tasks);
        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ConcurrentMapping_NestedObjects_ProducesCorrectResults()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<AddressSource, AddressDest>();
            cfg.CreateMap<PersonSource, PersonDest>();
        });
        var mapper = config.CreateMapper();
        var errors = new List<Exception>();
        var tasks = new Task[20];

        for (int t = 0; t < tasks.Length; t++)
        {
            var index = t;
            tasks[t] = Task.Run(() =>
            {
                try
                {
                    for (int i = 0; i < 50; i++)
                    {
                        var src = new PersonSource
                        {
                            Name = $"Person{index}_{i}",
                            Age = index + i,
                            Address = new AddressSource
                            {
                                Street = $"Street{index}",
                                City = $"City{i}",
                                Zip = $"{index:D5}"
                            }
                        };
                        var dest = mapper.Map<PersonSource, PersonDest>(src);
                        if (dest.Name != src.Name || dest.Address?.Street != src.Address.Street)
                            throw new Exception("Nested mapping mismatch");
                    }
                }
                catch (Exception ex)
                {
                    lock (errors) errors.Add(ex);
                }
            });
        }

        await Task.WhenAll(tasks);
        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ConcurrentMapList_ProducesCorrectResults()
    {
        var config = new MapperConfiguration(cfg => cfg.CreateMap<FlatSource, FlatDest>());
        var mapper = config.CreateMapper();
        var errors = new List<Exception>();
        var tasks = new Task[20];

        for (int t = 0; t < tasks.Length; t++)
        {
            var index = t;
            tasks[t] = Task.Run(() =>
            {
                try
                {
                    var sources = Enumerable.Range(0, 50).Select(i => new FlatSource
                    {
                        Name = $"T{index}_I{i}",
                        Age = i
                    }).ToList();

                    var results = mapper.MapList<FlatSource, FlatDest>(sources);
                    if (results.Count != 50)
                        throw new Exception($"Expected 50 items, got {results.Count}");
                    for (int i = 0; i < 50; i++)
                    {
                        if (results[i].Name != $"T{index}_I{i}")
                            throw new Exception("MapList mismatch");
                    }
                }
                catch (Exception ex)
                {
                    lock (errors) errors.Add(ex);
                }
            });
        }

        await Task.WhenAll(tasks);
        errors.Should().BeEmpty();
    }
}
