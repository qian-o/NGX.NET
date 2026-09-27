using System.Text.Json.Serialization;

namespace Streamline.NET.Generator;

internal sealed class InterfaceSnapshot
{
    public int SchemaVersion { get; set; }

    public SnapshotSource Source { get; set; } = new();

    public List<NativeDeclaration> Declarations { get; set; } = [];

    public List<NativeMacro> Macros { get; set; } = [];

    public List<string> Exports { get; set; } = [];

    public NativeAbi Abi { get; set; } = new();

    public NativeSecurityData Security { get; set; } = new();
}

internal sealed class NativeSecurityData
{
    public List<NativeDeclaration> Types { get; set; } = [];

    public Dictionary<string, string> Constants { get; set; } = [];
}

internal sealed class NativeAbi
{
    public Dictionary<string, int> VirtualSlots { get; set; } = [];

    public string ResourceAllocateCallback { get; set; } = "";

    public string AllocatorDestructor { get; set; } = "";
}

internal sealed class SnapshotSource
{
    public string Repository { get; set; } = "";

    public string Release { get; set; } = "";

    public string Commit { get; set; } = "";
}

internal sealed class NativeDeclaration
{
    public string Id { get; set; } = "";

    public string Kind { get; set; } = "";

    public string Name { get; set; } = "";

    public string QualifiedName { get; set; } = "";

    public string File { get; set; } = "";

    public int Line { get; set; }

    public string Classification { get; set; } = "";

    public string? Reason { get; set; }

    public string Comment { get; set; } = "";

    public string Source { get; set; } = "";

    public string Access { get; set; } = "";

    public bool Definition { get; set; }

    public bool Deprecated { get; set; }

    public string? DeprecationMessage { get; set; }

    public bool Flags { get; set; }

    public string? MappedAs { get; set; }

    public bool Virtual { get; set; }

    public bool Static { get; set; }

    public bool ConstMethod { get; set; }

    public int CallingConvention { get; set; }

    public string? Value { get; set; }

    public long OffsetBits { get; set; }

    public int? BitWidth { get; set; }

    public NativeType Type { get; set; } = new();

    public NativeType? UnderlyingType { get; set; }

    public NativeType? ResultType { get; set; }

    public List<NativeDeclaration> Children { get; set; } = [];

    public List<NativeDeclaration> LayoutFields { get; set; } = [];

    public List<NativeExpression> Expressions { get; set; } = [];

    public ParameterContract Contract { get; set; } = new();

    [JsonIgnore]
    public IEnumerable<NativeDeclaration> Fields => Children.Where(child => child.Kind == "FIELD_DECL");

    [JsonIgnore]
    public IEnumerable<NativeDeclaration> Parameters => Children.Where(child => child.Kind == "PARM_DECL");

    [JsonIgnore]
    public bool IsRecord => Kind is "STRUCT_DECL" or "CLASS_DECL" or "UNION_DECL";
}

internal sealed class ParameterContract
{
    public string Direction { get; set; } = "unspecified";

    public string Lifetime { get; set; } = "native-contract";

    public string Convenience { get; set; } = "raw";

    public string? CountParameter { get; set; }

    public string? Encoding { get; set; }

    public string Evidence { get; set; } = "";
}

internal sealed class NativeType
{
    public string Spelling { get; set; } = "";

    public string Canonical { get; set; } = "";

    public string Kind { get; set; } = "";

    public string? Declaration { get; set; }

    public int Size { get; set; }

    public int Alignment { get; set; }

    public bool Const { get; set; }

    public long Count { get; set; }

    public int CallingConvention { get; set; }

    public NativeType? Element { get; set; }

    public NativeType? Result { get; set; }

    public List<NativeType> Parameters { get; set; } = [];
}

internal sealed class NativeExpression
{
    public string Kind { get; set; } = "";

    public string Text { get; set; } = "";

    public string Type { get; set; } = "";

    public string? Value { get; set; }

    public List<NativeExpression> Children { get; set; } = [];
}

internal sealed class NativeMacro
{
    public string Id { get; set; } = "";

    public string Name { get; set; } = "";

    public string File { get; set; } = "";

    public string Body { get; set; } = "";

    public string Classification { get; set; } = "";
}
