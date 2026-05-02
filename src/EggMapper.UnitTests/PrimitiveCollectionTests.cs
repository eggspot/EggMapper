using EggMapper;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

file class StringChild { public string Name { get; set; } = ""; }
file class StrSrc { public List<StringChild>? Children { get; set; } }
file class StrDest { public IList<string>? Names { get; set; } }

file class EnumChild { public DayOfWeek Day { get; set; } }
file class EnumSrc { public List<EnumChild>? Days { get; set; } }
file class EnumDest { public IList<DayOfWeek>? Days { get; set; } }

file class ListSrc { public List<int> Items { get; set; } = new() { 1, 2, 3 }; }
file class ListDest { public List<int>? Items { get; set; } }

file class IListSrc { public List<int> Items { get; set; } = new() { 1, 2, 3 }; }
file class IListDest { public IList<int>? Items { get; set; } }

file class ArraySrc { public int[] Items { get; set; } = { 1, 2, 3 }; }
file class ArrayDest { public int[]? Items { get; set; } }

file class IEnumSrc { public List<int> Items { get; set; } = new() { 1, 2, 3 }; }
file class IEnumDest { public IEnumerable<int>? Items { get; set; } }

public class PrimitiveCollectionTests
{
    [Fact]
    public void MapFrom_returning_IEnumerable_string_to_IList_string()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<StrSrc, StrDest>()
               .ForMember(d => d.Names, o => o.MapFrom(s =>
                   s.Children != null ? s.Children.Select(c => c.Name) : new List<string>()));
        }).CreateMapper();

        var dest = mapper.Map<StrSrc, StrDest>(new StrSrc
        {
            Children = new() { new StringChild { Name = "Alice" }, new StringChild { Name = "Bob" } }
        });

        dest.Names.Should().BeEquivalentTo(new[] { "Alice", "Bob" });
    }

    [Fact]
    public void MapFrom_returning_IEnumerable_enum_to_IList_enum()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<EnumSrc, EnumDest>()
               .ForMember(d => d.Days, o => o.MapFrom(s =>
                   s.Days != null ? s.Days.Select(c => c.Day) : new List<DayOfWeek>()));
        }).CreateMapper();

        var dest = mapper.Map<EnumSrc, EnumDest>(new EnumSrc
        {
            Days = new() { new EnumChild { Day = DayOfWeek.Monday }, new EnumChild { Day = DayOfWeek.Friday } }
        });

        dest.Days.Should().BeEquivalentTo(new[] { DayOfWeek.Monday, DayOfWeek.Friday });
    }

    [Fact]
    public void Convention_List_int_to_List_int()
    {
        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<ListSrc, ListDest>()).CreateMapper();
        var dest = mapper.Map<ListSrc, ListDest>(new ListSrc());
        dest.Items.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public void Convention_List_int_to_IList_int()
    {
        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<IListSrc, IListDest>()).CreateMapper();
        var dest = mapper.Map<IListSrc, IListDest>(new IListSrc());
        dest.Items.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public void Convention_int_array_to_int_array()
    {
        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<ArraySrc, ArrayDest>()).CreateMapper();
        var dest = mapper.Map<ArraySrc, ArrayDest>(new ArraySrc());
        dest.Items.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public void Convention_List_int_to_IEnumerable_int()
    {
        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<IEnumSrc, IEnumDest>()).CreateMapper();
        var dest = mapper.Map<IEnumSrc, IEnumDest>(new IEnumSrc());
        dest.Items.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public void Direct_Map_List_int_to_List_int()
    {
        // Top-level collection-to-collection map without an explicit CreateMap<List<int>, List<int>>()
        var mapper = new MapperConfiguration(_ => { }).CreateMapper();
        var dest = mapper.Map<List<int>, List<int>>(new List<int> { 1, 2, 3 });
        dest.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public void Direct_Map_List_int_to_IList_int()
    {
        var mapper = new MapperConfiguration(_ => { }).CreateMapper();
        var dest = mapper.Map<List<int>, IList<int>>(new List<int> { 1, 2, 3 });
        dest.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public void Direct_Map_List_int_to_ICollection_int()
    {
        var mapper = new MapperConfiguration(_ => { }).CreateMapper();
        var dest = mapper.Map<List<int>, ICollection<int>>(new List<int> { 1, 2, 3 });
        dest.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public void Direct_Map_int_array_to_List_int()
    {
        var mapper = new MapperConfiguration(_ => { }).CreateMapper();
        var dest = mapper.Map<int[], List<int>>(new[] { 1, 2, 3 });
        dest.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public void Direct_Map_HashSet_int_to_List_int()
    {
        var mapper = new MapperConfiguration(_ => { }).CreateMapper();
        var dest = mapper.Map<HashSet<int>, List<int>>(new HashSet<int> { 1, 2, 3 });
        dest.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public void Direct_Map_IEnumerable_int_to_int_array()
    {
        var mapper = new MapperConfiguration(_ => { }).CreateMapper();
        var dest = mapper.Map<IEnumerable<int>, int[]>(Enumerable.Range(1, 3));
        dest.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public void MapFrom_with_Where_and_Select_to_List_int()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<ListSrc, ListDest>()
               .ForMember(d => d.Items, o => o.MapFrom(s => s.Items.Where(i => i > 1).Select(i => i * 10)));
        }).CreateMapper();

        var dest = mapper.Map<ListSrc, ListDest>(new ListSrc());

        dest.Items.Should().BeEquivalentTo(new[] { 20, 30 });
    }

    [Fact]
    public void Direct_Map_int_array_to_int_array()
    {
        var mapper = new MapperConfiguration(_ => { }).CreateMapper();
        var dest = mapper.Map<int[], int[]>(new[] { 1, 2, 3 });
        dest.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public void Direct_Map_List_int_to_HashSet_int()
    {
        var mapper = new MapperConfiguration(_ => { }).CreateMapper();
        var dest = mapper.Map<List<int>, HashSet<int>>(new List<int> { 1, 2, 3 });
        dest.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public void Direct_Map_List_to_custom_wrapper_with_IEnumerable_ctor()
    {
        var mapper = new MapperConfiguration(_ => { }).CreateMapper();
        var dest = mapper.Map<List<int>, WrappedInts>(new List<int> { 1, 2, 3 });
        dest.Should().NotBeNull();
        dest.AsList().Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public void Direct_Map_List_int_nullable_to_int_nullable_array()
    {
        var mapper = new MapperConfiguration(_ => { }).CreateMapper();
        var dest = mapper.Map<List<int?>, int?[]>(new List<int?> { 1, null, 3 });
        dest.Should().BeEquivalentTo(new int?[] { 1, null, 3 });
    }
}

file class WrappedInts : System.Collections.Generic.IEnumerable<int>
{
    private readonly List<int> _items;
    public WrappedInts(System.Collections.IEnumerable items)
    {
        _items = new List<int>();
        foreach (var i in items) _items.Add((int)i);
    }
    public List<int> AsList() => _items;
    public IEnumerator<int> GetEnumerator() => _items.GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _items.GetEnumerator();
}
