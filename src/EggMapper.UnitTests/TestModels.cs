namespace EggMapper.UnitTests.TestModels;

// ── Flat ──────────────────────────────────────────────────────────────────────
public class FlatSource
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public double Value { get; set; }
    public string Email { get; set; } = "";
    public bool IsActive { get; set; }
}

public class FlatDest
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public double Value { get; set; }
    public string Email { get; set; } = "";
    public bool IsActive { get; set; }
}

// ── Type-conversion ───────────────────────────────────────────────────────────
public class TypeConvSource
{
    public int IntVal { get; set; }
    public int AnotherInt { get; set; }
}

public class TypeConvDest
{
    public long IntVal { get; set; }
    public double AnotherInt { get; set; }
}

// ── Nested ────────────────────────────────────────────────────────────────────
public class AddressSource
{
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
    public string Zip { get; set; } = "";
}

public class AddressDest
{
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
    public string Zip { get; set; } = "";
}

public class PersonSource
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public AddressSource? Address { get; set; }
}

public class PersonDest
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public AddressDest? Address { get; set; }
}

// ── Flattening ────────────────────────────────────────────────────────────────
public class SubObject
{
    public string Name { get; set; } = "";
    public int Count { get; set; }
}

public class FlattenSource
{
    public SubObject? Sub { get; set; }
    public int Value { get; set; }
}

public class FlattenDest
{
    public string? SubName { get; set; }
    public int SubCount { get; set; }
    public int Value { get; set; }
}

// ── Collections ───────────────────────────────────────────────────────────────
public class ItemSource
{
    public int Id { get; set; }
    public string Label { get; set; } = "";
}

public class ItemDest
{
    public int Id { get; set; }
    public string Label { get; set; } = "";
}

public class CollectionSource
{
    public List<ItemSource>? Items { get; set; }
    public ItemSource[]? ItemArray { get; set; }
}

public class CollectionDest
{
    public List<ItemDest>? Items { get; set; }
    public ItemDest[]? ItemArray { get; set; }
}

// ── Enumerations ──────────────────────────────────────────────────────────────
public enum Status { Active = 0, Inactive = 1, Pending = 2 }

[Flags]
public enum Permissions { None = 0, Read = 1, Write = 2, Admin = 4 }

public class EnumSource
{
    public Status Status { get; set; }
    public int Code { get; set; }
    public Permissions Perms { get; set; }
}

public class EnumDest
{
    public Status Status { get; set; }
    public Status CodeAsStatus { get; set; }
    public int StatusAsInt { get; set; }
    public Permissions Perms { get; set; }
}

// ── Self-referencing (MaxDepth) ───────────────────────────────────────────────
public class TreeNode
{
    public string Name { get; set; } = "";
    public TreeNode? Child { get; set; }
}

// ── Constructor / Immutable ───────────────────────────────────────────────────
public class ImmutablePerson
{
    public string Name { get; }
    public int Age { get; }

    public ImmutablePerson(string name, int age)
    {
        Name = name;
        Age = age;
    }
}

public class PersonSourceSimple
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
}

// Mutable record (default ctor, setters)
public record MutablePersonRecord
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
}

// Mixed: parameterised ctor + settable extra prop
public class SemiImmutable
{
    public string Name { get; }
    public int Age { get; }
    public string? Notes { get; set; }

    public SemiImmutable(string name, int age)
    {
        Name = name;
        Age = age;
    }
}

public class SemiImmutableSource
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public string Notes { get; set; } = "";
}

// ── Inheritance ───────────────────────────────────────────────────────────────
public class BaseSource
{
    public string BaseProp { get; set; } = "";
}

public class BaseDest
{
    public string BaseProp { get; set; } = "";
}

public class DerivedSource : BaseSource
{
    public string DerivedProp { get; set; } = "";
}

public class DerivedDest : BaseDest
{
    public string DerivedProp { get; set; } = "";
}

// ── Interface ─────────────────────────────────────────────────────────────────
public interface IAnimal
{
    string Name { get; }
    string Sound { get; }
}

public class Cat : IAnimal
{
    public string Name { get; set; } = "";
    public string Sound { get; set; } = "Meow";
}

public class AnimalDest
{
    public string Name { get; set; } = "";
    public string Sound { get; set; } = "";
}

public class AnimalHolder
{
    public IAnimal? Animal { get; set; }
}

public class AnimalHolderDest
{
    public AnimalDest? Animal { get; set; }
}

// ── ForPath helpers ───────────────────────────────────────────────────────────
public class InnerSource
{
    public string Value { get; set; } = "";
    public int Count { get; set; }
}

public class NestedPathSource
{
    public InnerSource? Inner { get; set; }
    public string TopLevel { get; set; } = "";
}

public class ForPathDest
{
    public string? InnerValue { get; set; }
    public int InnerCount { get; set; }
    public string? TopLevel { get; set; }
}

// ── BeforeMap / AfterMap helpers ──────────────────────────────────────────────
public class SimpleSource
{
    public string Value { get; set; } = "";
}

public class SimpleDest
{
    public string Value { get; set; } = "";
    public string? Extra { get; set; }
}

// ── Profiles ─────────────────────────────────────────────────────────────────
public class FlatMappingProfile : Profile
{
    public FlatMappingProfile()
    {
        CreateMap<FlatSource, FlatDest>();
    }
}

public class PersonMappingProfile : Profile
{
    public PersonMappingProfile()
    {
        CreateMap<AddressSource, AddressDest>();
        CreateMap<PersonSource, PersonDest>();
    }
}
