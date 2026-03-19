using EggMapper.Benchmarks.Models;
using Riok.Mapperly.Abstractions;

namespace EggMapper.Benchmarks.Configs;

[Mapper]
public partial class MapperlyMapper
{
    public partial ModelDto MapFlat(ModelObject source);
    public partial CustomerDTO MapCustomer(Customer source);
    public partial AddressDTO MapAddress(Address source);
    public partial FooDest MapComplex(Foo source);
    public partial InnerFooDest MapInner(InnerFoo source);
    public partial FlatteningDest MapFlattening(FlatteningSource source);
    public partial List<ModelDto> MapFlatList(List<ModelObject> source);
    public partial List<CustomerDTO> MapCustomerList(List<Customer> source);
}
