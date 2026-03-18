using EggMapper.Benchmarks.Models;

namespace EggMapper.Benchmarks.Configs;

public static class EggMapperConfig
{
    public static readonly global::EggMapper.MapperConfiguration Configuration;
    public static readonly global::EggMapper.IMapper Mapper;

    static EggMapperConfig()
    {
        Configuration = new global::EggMapper.MapperConfiguration(cfg =>
        {
            cfg.CreateMap<ModelObject, ModelDto>();
            cfg.CreateMap<Customer, CustomerDTO>();
            cfg.CreateMap<Address, AddressDTO>();
            cfg.CreateMap<Foo, FooDest>();
            cfg.CreateMap<InnerFoo, InnerFooDest>();
        });
        Mapper = Configuration.CreateMapper();
    }
}
