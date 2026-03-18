using Mapster;
using EggMapper.Benchmarks.Models;

namespace EggMapper.Benchmarks.Configs;

public static class MapsterConfig
{
    public static void Configure()
    {
        TypeAdapterConfig<ModelObject, ModelDto>.NewConfig();
        TypeAdapterConfig<Customer, CustomerDTO>.NewConfig();
        TypeAdapterConfig<Address, AddressDTO>.NewConfig();
        TypeAdapterConfig<Foo, FooDest>.NewConfig();
        TypeAdapterConfig<InnerFoo, InnerFooDest>.NewConfig();
    }
}
