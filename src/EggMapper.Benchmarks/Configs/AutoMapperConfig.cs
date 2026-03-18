using AutoMapper;
using EggMapper.Benchmarks.Models;

namespace EggMapper.Benchmarks.Configs;

public static class AutoMapperConfig
{
    public static readonly IMapper Mapper;

    static AutoMapperConfig()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<ModelObject, ModelDto>();
            cfg.CreateMap<Customer, CustomerDTO>();
            cfg.CreateMap<Address, AddressDTO>();
            cfg.CreateMap<Foo, FooDest>();
            cfg.CreateMap<InnerFoo, InnerFooDest>();
        });
        Mapper = config.CreateMapper();
    }
}
