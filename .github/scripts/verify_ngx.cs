#:project ../../NGX.NET/NGX.NET.csproj
#:property PublishAot=false
#:property PublishTrimmed=false
#:property EnableTrimAnalyzer=false
#:property EnableAotAnalyzer=false
#:property AllowUnsafeBlocks=true

using System.Numerics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using NGX.NET;
using Ngx = NGX.NET.NGX;

// Independent compiled-assembly audit. This does not use the C# emitter or its
// naming/type mapping logic; NGXNativeName and actual import metadata identify APIs.
string root = Path.GetFullPath(args.Length > 0 ? args[0] : ".");
using JsonDocument document = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "NGX.NET.Generator/ast.json")));
Assembly assembly = typeof(Ngx).Assembly;
Dictionary<string, Type> types = assembly.GetTypes().Where(t => t.GetCustomAttribute<NGXNativeNameAttribute>() is not null).ToDictionary(t => t.GetCustomAttribute<NGXNativeNameAttribute>()!.Name);
Dictionary<string, MethodInfo> imports = assembly.GetTypes().SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)).Where(m => m.GetCustomAttribute<LibraryImportAttribute>() is not null).ToDictionary(m => m.GetCustomAttribute<LibraryImportAttribute>()!.EntryPoint!);
HashSet<string> expectedImports = ["NGX_Bridge_Parameter_Reset"];
HashSet<string> opaqueTypes = document.RootElement.GetProperty("platforms").EnumerateObject().SelectMany(p => p.Value.GetProperty("records").EnumerateArray()).Where(r => r.GetProperty("opaque").GetBoolean()).Select(r => r.GetProperty("name").GetString()!).ToHashSet();
int checks = 0;

void Require(bool condition, string message)
{
    checks++;
    if (!condition) throw new InvalidDataException(message);
}

// API groups retain their backend/feature names. Every exported data or helper
// type uses the library prefix, including source-generated nested array types.
foreach (Type type in assembly.GetExportedTypes().Where(t => !t.IsDefined(typeof(CompilerGeneratedAttribute))))
{
    bool apiGroup = type.DeclaringType == typeof(Ngx) && type.IsAbstract && type.IsSealed;
    if (!apiGroup) Require(type.Name.StartsWith("NGX", StringComparison.Ordinal), "Missing NGX type prefix: " + type.FullName);
    Require(!type.Name.StartsWith("NGXNGX", StringComparison.Ordinal), "Repeated NGX type prefix: " + type.FullName);
}

// Each native entry point has one public signature. Check the compiled surface
// rather than relying on the generator to avoid emitting convenience overloads.
const BindingFlags publicDeclaredMethods = BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;
foreach (MethodInfo method in imports.Values)
{
    string context = method.DeclaringType!.FullName + "." + method.Name;
    Require(method.DeclaringType.GetMethods(publicDeclaredMethods).Count(m => m.Name == method.Name) == 1, "Managed overload for native entry point " + context);
    foreach (Type signatureType in method.GetParameters().Select(p => p.ParameterType).Prepend(method.ReturnType))
    {
        Require(!signatureType.IsByRef && !signatureType.IsByRefLike, "Managed reference or span in native signature " + context);
    }
}

// Constants are exposed as properties, and top-level macro/runtime helpers are
// independent of SDK entry points. All other API-group methods must be imports.
HashSet<string> standaloneHelpers = [nameof(Ngx.Succeeded), nameof(Ngx.Failed), nameof(Ngx.ThrowIfFailed), nameof(Ngx.ArrayLength)];
foreach (Type type in assembly.GetExportedTypes().Where(t => t == typeof(Ngx) || t.DeclaringType == typeof(Ngx) && t.IsAbstract && t.IsSealed))
{
    foreach (MethodInfo method in type.GetMethods(publicDeclaredMethods).Where(m => !m.IsSpecialName))
    {
        if (type == typeof(Ngx) && standaloneHelpers.Contains(method.Name)) continue;
        Require(method.GetCustomAttribute<LibraryImportAttribute>() is not null, "Managed wrapper in SDK API group " + type.FullName + "." + method.Name);
    }
}

int Size(Type type) => type.IsEnum ? Marshal.SizeOf(Enum.GetUnderlyingType(type)) : type.IsPointer || type.IsFunctionPointer || type == typeof(nint) || type == typeof(nuint) ? IntPtr.Size : Marshal.SizeOf(type);

void VerifyType(JsonElement native, Type managed, string context, bool behindPointer = false)
{
    string kind = native.GetProperty("kind").GetString()!;
    if (kind == "VOID")
    {
        Require(managed == typeof(void), context + " return type");
        return;
    }

    if (kind is "RECORD" or "ENUM")
    {
        string name = native.GetProperty("name").GetString()!;
        Require(managed.GetCustomAttribute<NGXNativeNameAttribute>()?.Name == name, context + " native type identity");
        if (behindPointer && opaqueTypes.Contains(name)) return;
    }

    int nativeSize = kind is "LVALUEREFERENCE" or "RVALUEREFERENCE" ? IntPtr.Size : native.GetProperty("size").GetInt32();
    Require(Size(managed) == nativeSize, context + " ABI size");
    Type? scalar = kind switch
    {
        "BOOL" => typeof(NGXBool8),
        "CHAR_S" or "CHAR_U" or "SCHAR" => typeof(sbyte),
        "UCHAR" => typeof(byte),
        "SHORT" => typeof(short),
        "USHORT" => typeof(ushort),
        "INT" => typeof(int),
        "UINT" => typeof(uint),
        "LONG" => nativeSize == 8 ? typeof(long) : typeof(int),
        "ULONG" => nativeSize == 8 ? typeof(ulong) : typeof(uint),
        "LONGLONG" => typeof(long),
        "ULONGLONG" => typeof(ulong),
        "FLOAT" => typeof(float),
        "DOUBLE" => typeof(double),
        _ => null
    };
    if (scalar is not null) Require(managed == scalar || managed == typeof(nuint) && scalar == typeof(ulong), context + " scalar type");
    if (kind is "POINTER" or "LVALUEREFERENCE" or "RVALUEREFERENCE")
    {
        JsonElement element = native.GetProperty("element");
        string elementKind = element.GetProperty("kind").GetString()!;
        if (elementKind is not ("FUNCTIONPROTO" or "FUNCTIONNOPROTO"))
        {
            if (managed == typeof(nint))
            {
                Require(elementKind == "RECORD" && opaqueTypes.Contains(element.GetProperty("name").GetString()!) && !element.GetProperty("name").GetString()!.StartsWith("NVSDK_NGX_", StringComparison.Ordinal), context + " external opaque handle");
            }
            else
            {
                Require(managed.IsPointer, context + " pointer type");
                if (elementKind == "WCHAR") Require(managed.GetElementType() == typeof(void), context + " platform wchar_t pointer");
                else VerifyType(element, managed.GetElementType()!, context + " pointee", true);
            }
        }
    }
    if (kind == "POINTER" && native.GetProperty("element").GetProperty("kind").GetString() == "FUNCTIONPROTO")
    {
        Require(managed.IsFunctionPointer && managed.IsUnmanagedFunctionPointer, context + " unmanaged callback");
        Require(managed.GetFunctionPointerCallingConventions().All(c => c == typeof(CallConvCdecl)), context + " callback modifiers");
        JsonElement function = native.GetProperty("element");
        VerifyType(function.GetProperty("result"), managed.GetFunctionPointerReturnType(), context);
        JsonElement[] nativeArguments = [.. function.GetProperty("arguments").EnumerateArray()];
        Type[] managedArguments = managed.GetFunctionPointerParameterTypes();
        Require(nativeArguments.Length == managedArguments.Length, context + " callback argument count");
        for (int i = 0; i < nativeArguments.Length; i++) VerifyType(nativeArguments[i], managedArguments[i], context);
    }
}

// Reviewed SDK fields only: unrelated float arrays and pointers remain strict.
Dictionary<string, (Type Type, int[] Shape)> frameMathFields = new()
{
    ["cameraViewToClip"] = (typeof(Matrix4x4), [4, 4]),
    ["clipToCameraView"] = (typeof(Matrix4x4), [4, 4]),
    ["clipToLensClip"] = (typeof(Matrix4x4), [4, 4]),
    ["clipToPrevClip"] = (typeof(Matrix4x4), [4, 4]),
    ["prevClipToClip"] = (typeof(Matrix4x4), [4, 4]),
    ["jitterOffset"] = (typeof(Vector2), [2]),
    ["mvecScale"] = (typeof(Vector2), [2]),
    ["cameraPinholeOffset"] = (typeof(Vector2), [2]),
    ["cameraPos"] = (typeof(Vector3), [3]),
    ["cameraUp"] = (typeof(Vector3), [3]),
    ["cameraRight"] = (typeof(Vector3), [3]),
    ["cameraFwd"] = (typeof(Vector3), [3])
};
HashSet<string> reconstructionMathRecords =
[
    "NVSDK_NGX_D3D11_DLSSD_Eval_Params", "NVSDK_NGX_D3D12_DLSSD_Eval_Params",
    "NVSDK_NGX_CUDA_DLSSD_Eval_Params", "NVSDK_NGX_VK_DLSSD_Eval_Params"
];

bool VerifyMathField(JsonElement native, Type managed, string record, string field)
{
    string context = record + "::" + field;
    if (record == "NVSDK_NGX_DLSSG_Opt_Eval_Params" && frameMathFields.TryGetValue(field, out var math))
    {
        Require(managed == math.Type, context + " typed math field");
        Require(Size(managed) == native.GetProperty("size").GetInt32(), context + " math storage size");
        foreach (int length in math.Shape)
        {
            Require(native.GetProperty("kind").GetString() == "CONSTANTARRAY", context + " native array rank");
            Require(native.GetProperty("count").GetInt32() == length, context + " native array dimension");
            native = native.GetProperty("element");
        }
        Require(native.GetProperty("kind").GetString() == "FLOAT" && native.GetProperty("size").GetInt32() == sizeof(float), context + " native float element");
        return true;
    }

    if (reconstructionMathRecords.Contains(record) && field is "pInWorldToViewMatrix" or "pInViewToClipMatrix")
    {
        Require(managed == typeof(Matrix4x4).MakePointerType(), context + " reviewed matrix pointer");
        Require(native.GetProperty("kind").GetString() == "POINTER" && native.GetProperty("size").GetInt32() == IntPtr.Size, context + " native pointer size");
        JsonElement element = native.GetProperty("element");
        Require(element.GetProperty("kind").GetString() == "FLOAT" && element.GetProperty("size").GetInt32() == sizeof(float), context + " native float pointer");
        return true;
    }

    return false;
}

MathAbiChecks.Verify(Require);

foreach (JsonProperty platform in document.RootElement.GetProperty("platforms").EnumerateObject())
{
    string rid = platform.Name;
    int mathFields = 0;
    foreach (JsonElement record in platform.Value.GetProperty("records").EnumerateArray())
    {
        if (record.GetProperty("opaque").GetBoolean()) continue;
        string name = record.GetProperty("name").GetString()!;
        Require(types.TryGetValue(name, out Type? type), rid + " missing structure " + name);
        Require(Marshal.SizeOf(type!) == record.GetProperty("size").GetInt32(), rid + " sizeof " + name);
        Dictionary<string, FieldInfo> fields = type!.GetFields().Where(f => f.GetCustomAttribute<NGXNativeNameAttribute>() is not null).ToDictionary(f => f.GetCustomAttribute<NGXNativeNameAttribute>()!.Name);
        Require(fields.Count == record.GetProperty("fields").GetArrayLength(), rid + " field count " + name);
        foreach (JsonElement field in record.GetProperty("fields").EnumerateArray())
        {
            string fieldName = field.GetProperty("name").GetString()!;
            Require(fields.TryGetValue(fieldName, out FieldInfo? managed), rid + " missing field " + name + "::" + fieldName);
            Require(Marshal.OffsetOf(type, managed!.Name).ToInt64() * 8 == field.GetProperty("offset").GetInt64(), rid + " offsetof " + name + "::" + fieldName);
            JsonElement nativeType = field.GetProperty("type");
            if (VerifyMathField(nativeType, managed.FieldType, name, fieldName)) mathFields++;
            else if (nativeType.GetProperty("kind").GetString() != "CONSTANTARRAY") VerifyType(nativeType, managed.FieldType, name + "::" + fieldName);
            else Require(Size(managed.FieldType) == nativeType.GetProperty("size").GetInt32(), rid + " array storage " + name + "::" + fieldName);
        }
    }

    Require(mathFields == (rid.StartsWith("win-", StringComparison.Ordinal) ? 20 : 16), rid + " reviewed math field coverage");

    foreach (JsonElement enumeration in platform.Value.GetProperty("enums").EnumerateArray())
    {
        string name = enumeration.GetProperty("name").GetString()!;
        Require(types.TryGetValue(name, out Type? type), rid + " missing enum " + name);
        Dictionary<string, FieldInfo> members = type!.GetFields(BindingFlags.Public | BindingFlags.Static).ToDictionary(f => f.GetCustomAttribute<NGXNativeNameAttribute>()!.Name);
        foreach (JsonElement member in enumeration.GetProperty("values").EnumerateArray())
        {
            string memberName = member.GetProperty("name").GetString()!;
            Require(members.TryGetValue(memberName, out FieldInfo? field), "missing enum member " + memberName);
            Require(memberName.Replace("_", "", StringComparison.Ordinal).EndsWith(field!.Name, StringComparison.Ordinal), "native enum capitalization " + memberName);
            Require(unchecked((uint)Convert.ToInt64(field!.GetRawConstantValue())) == unchecked((uint)member.GetProperty("value").GetInt64()), rid + " enum bits " + memberName);
        }
    }

    HashSet<string> exports = platform.Value.GetProperty("exports").EnumerateArray().Select(e => e.GetString()!).ToHashSet();
    foreach (JsonElement function in platform.Value.GetProperty("functions").EnumerateArray())
    {
        string name = function.GetProperty("export").GetString()!;
        expectedImports.Add(name);
        Require(exports.Contains(name), rid + " missing native export " + name);
        Require(imports.TryGetValue(name, out MethodInfo? method), rid + " missing compiled import " + name);
        VerifyType(function.GetProperty("result"), method!.ReturnType, name);
        Require(method.GetCustomAttribute<UnmanagedCallConvAttribute>()!.CallConvs!.Contains(typeof(CallConvCdecl)), name + " calling convention");
        ParameterInfo[] parameters = method.GetParameters();
        JsonElement[] nativeParameters = [.. function.GetProperty("parameters").EnumerateArray()];
        Require(parameters.Length == nativeParameters.Length, name + " parameter count");
        for (int i = 0; i < parameters.Length; i++) VerifyType(nativeParameters[i].GetProperty("type"), parameters[i].ParameterType, name + " argument " + i);
    }

    foreach (JsonProperty alias in platform.Value.GetProperty("aliases").EnumerateObject())
    {
        JsonElement native = alias.Value;
        if (native.GetProperty("kind").GetString() != "POINTER" || native.GetProperty("element").GetProperty("kind").GetString() != "FUNCTIONPROTO") continue;
        Require(types.TryGetValue(alias.Name, out Type? type), "missing callback " + alias.Name);
        VerifyType(native, type!.GetField("Pointer")!.FieldType, alias.Name);
    }

    JsonElement parameter = platform.Value.GetProperty("records").EnumerateArray().Single(r => r.GetProperty("name").GetString() == "NVSDK_NGX_Parameter");
    if (parameter.TryGetProperty("members", out JsonElement virtualMembers))
    {
        Require(virtualMembers.GetArrayLength() == 17, "Parameter virtual member inventory");
        foreach (JsonElement member in virtualMembers.EnumerateArray()) Require(imports.ContainsKey(member.GetProperty("binding").GetString()!), "Missing C++ member adapter");
    }

    Dictionary<string, PropertyInfo> strings = typeof(Ngx).GetProperties(BindingFlags.Static | BindingFlags.Public).Where(p => p.GetCustomAttribute<NGXNativeNameAttribute>() is not null).ToDictionary(p => p.GetCustomAttribute<NGXNativeNameAttribute>()!.Name);
    foreach (JsonElement macro in platform.Value.GetProperty("macros").EnumerateArray())
    {
        string[] tokens = [.. macro.GetProperty("tokens").EnumerateArray().Select(t => t.GetString()!)];
        if (tokens.Length == 0 || !tokens.All(t => t.StartsWith('"'))) continue;
        string nativeName = macro.GetProperty("name").GetString()!;
        Require(strings.TryGetValue(nativeName, out PropertyInfo? property), "Missing parameter constant " + nativeName);
        List<byte> bytes = [];
        foreach (string literal in tokens)
        {
            for (int i = 1; i < literal.Length - 1; i++)
            {
                char c = literal[i];
                if (c == '\\')
                {
                    c = literal[++i];
                    if (c == 'x')
                    {
                        int start = ++i;
                        while (i < literal.Length - 1 && Uri.IsHexDigit(literal[i])) i++;
                        bytes.Add(Convert.ToByte(literal[start..i], 16));
                        i--;
                        continue;
                    }
                    c = c switch { 'n' => '\n', 'r' => '\r', 't' => '\t', '0' => '\0', _ => c };
                }
                bytes.Add(checked((byte)c));
            }
        }
        ReadOnlySpan<byte> actual = property!.GetMethod!.CreateDelegate<SpanGetter>()();
        Require(actual.SequenceEqual(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(bytes)), "Parameter constant bytes " + nativeName);
    }

    Dictionary<string, FieldInfo> numbers = typeof(Ngx).GetFields(BindingFlags.Static | BindingFlags.Public).Where(f => f.GetCustomAttribute<NGXNativeNameAttribute>() is not null).ToDictionary(f => f.GetCustomAttribute<NGXNativeNameAttribute>()!.Name);
    foreach (JsonElement macro in platform.Value.GetProperty("macros").EnumerateArray().Where(m => m.GetProperty("name").GetString() is "NVSDK_NGX_VERSION_API_MACRO" or "NVSDK_NGX_DLSS_DEBUG_OVERLAY_VALUE_UNSET"))
    {
        string name = macro.GetProperty("name").GetString()!;
        string literal = string.Concat(macro.GetProperty("tokens").EnumerateArray().Select(t => t.GetString()));
        long expected = literal.StartsWith("0x", StringComparison.Ordinal) ? Convert.ToInt64(literal[2..], 16) : long.Parse(literal, System.Globalization.CultureInfo.InvariantCulture);
        Require(numbers.TryGetValue(name, out FieldInfo? field), "Missing numeric macro " + name);
        Require(Convert.ToInt64(field!.GetRawConstantValue()) == expected, "Numeric macro value " + name);
    }

    foreach (JsonElement binary in platform.Value.GetProperty("binaries").EnumerateArray())
    {
        string path = Path.Combine(root, "native", rid, binary.GetProperty("name").GetString()!);
        string checksum = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path)));
        Require(checksum == binary.GetProperty("sha256").GetString(), "binary checksum " + path);
    }
}

// Reflection reports convention modifiers, but the compact ECMA-335 Cdecl
// encoding has no modifiers. Inspect its signature header independently.
using FileStream assemblyStream = File.OpenRead(assembly.Location);
using PEReader pe = new(assemblyStream);
MetadataReader metadata = pe.GetMetadataReader();
foreach (FieldInfo field in assembly.GetTypes().SelectMany(t => t.GetFields()).Where(f => f.FieldType.IsFunctionPointer))
{
    FieldDefinition definition = metadata.GetFieldDefinition((FieldDefinitionHandle)MetadataTokens.Handle(field.MetadataToken));
    BlobReader signature = metadata.GetBlobReader(definition.Signature);
    Require(signature.ReadSignatureHeader().Kind == SignatureKind.Field, "field signature " + field.Name);
    Require(signature.ReadSignatureTypeCode() == SignatureTypeCode.FunctionPointer, "function pointer encoding " + field.Name);
    Require(signature.ReadSignatureHeader().CallingConvention == SignatureCallingConvention.CDecl, "encoded Cdecl callback " + field.Name);
}

Require(expectedImports.SetEquals(imports.Keys), "Compiled imports do not match the native export inventory.");
Require(Ngx.Succeeded(NGXResult.Success) && Ngx.Succeeded(0), "official success predicate");
foreach (NGXResult result in Enum.GetValues<NGXResult>())
{
    bool failed = ((uint)result & 0xFFF00000u) == 0xBAD00000u;
    Require(Ngx.Failed(result) == failed, "result predicate " + result);
    try
    {
        Ngx.ThrowIfFailed(result);
        Require(!failed, "missing exception " + result);
    }
    catch (NGXException exception)
    {
        Require(failed && exception.Result == result, "exception result " + result);
    }
}

// String helpers execute on the CPU without loading NGX or requiring a GPU.
void RequireThrows<TException>(Action action, string message) where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        Require(true, message);
        return;
    }

    Require(false, message);
}

unsafe void VerifyStrings(NGXEncoding encoding, ReadOnlySpan<byte> expected, int terminatorSize)
{
    const string value = "NGX 中文 🚀";
    void* pointer = NGXMarshal.StringToPtr(value, encoding);

    try
    {
        Require(pointer != null, encoding + " allocation");
        Require(new ReadOnlySpan<byte>(pointer, expected.Length).SequenceEqual(expected), encoding + " bytes and terminator without BOM");
        Require(NGXMarshal.PtrToString(pointer, encoding) == value, encoding + " round trip");
    }
    finally
    {
        NGXMarshal.Free(pointer);
    }

    pointer = NGXMarshal.StringToPtr(string.Empty, encoding);

    try
    {
        Require(pointer != null, encoding + " empty string allocation");
        Require(new ReadOnlySpan<byte>(pointer, terminatorSize).IndexOfAnyExcept((byte)0) == -1, encoding + " empty terminator");
        Require(NGXMarshal.PtrToString(pointer, encoding) == string.Empty, encoding + " empty round trip");
    }
    finally
    {
        NGXMarshal.Free(pointer);
    }

    Require(NGXMarshal.StringToPtr(null, encoding) == null, encoding + " null input");
    Require(NGXMarshal.PtrToString(null, encoding) is null, encoding + " null pointer");
    RequireThrows<ArgumentException>(() => NGXMarshal.StringToPtr("before\0after", encoding), encoding + " embedded NUL rejection");
    byte* borrowed = stackalloc byte[expected.Length];
    expected.CopyTo(new Span<byte>(borrowed, expected.Length));
    Require(NGXMarshal.PtrToString(borrowed, encoding) == value, encoding + " borrowed pointer read");
    Require(new ReadOnlySpan<byte>(borrowed, expected.Length).SequenceEqual(expected), encoding + " borrowed bytes unchanged");
    Require(NGXMarshal.PtrToString(borrowed, encoding) == value, encoding + " borrowed pointer remains valid");
}

unsafe
{
    VerifyStrings(NGXEncoding.Utf8, [0x4E, 0x47, 0x58, 0x20, 0xE4, 0xB8, 0xAD, 0xE6, 0x96, 0x87, 0x20, 0xF0, 0x9F, 0x9A, 0x80, 0], 1);
    NGXEncoding invalid = (NGXEncoding)(-1);
    RequireThrows<ArgumentOutOfRangeException>(() => NGXMarshal.StringToPtr("text", invalid), "invalid input encoding");
    RequireThrows<ArgumentOutOfRangeException>(() => NGXMarshal.StringToPtr(null, invalid), "invalid encoding with null input");
    RequireThrows<ArgumentOutOfRangeException>(() => NGXMarshal.PtrToString(null, invalid), "invalid encoding with null pointer");
    byte borrowed = 0;
    nint borrowedAddress = (nint)(&borrowed);
    RequireThrows<ArgumentOutOfRangeException>(() => NGXMarshal.PtrToString((void*)borrowedAddress, invalid), "invalid pointer encoding");
    NGXMarshal.Free(null);
    Require(true, "Free(null)");

    if (OperatingSystem.IsWindows())
    {
        VerifyStrings(NGXEncoding.NativeWide, [0x4E, 0, 0x47, 0, 0x58, 0, 0x20, 0, 0x2D, 0x4E, 0x87, 0x65, 0x20, 0, 0x3D, 0xD8, 0x80, 0xDE, 0, 0], 2);
    }
    else if (OperatingSystem.IsLinux())
    {
        VerifyStrings(NGXEncoding.NativeWide, [0x4E, 0, 0, 0, 0x47, 0, 0, 0, 0x58, 0, 0, 0, 0x20, 0, 0, 0, 0x2D, 0x4E, 0, 0, 0x87, 0x65, 0, 0, 0x20, 0, 0, 0, 0x80, 0xF6, 1, 0, 0, 0, 0, 0], 4);
    }
    else
    {
        RequireThrows<PlatformNotSupportedException>(() => NGXMarshal.StringToPtr("text", NGXEncoding.NativeWide), "unsupported wchar_t input");
        RequireThrows<PlatformNotSupportedException>(() => NGXMarshal.StringToPtr(null, NGXEncoding.NativeWide), "unsupported wchar_t null input");
        RequireThrows<PlatformNotSupportedException>(() => NGXMarshal.PtrToString(null, NGXEncoding.NativeWide), "unsupported wchar_t null pointer");
        RequireThrows<PlatformNotSupportedException>(() => NGXMarshal.PtrToString((void*)borrowedAddress, NGXEncoding.NativeWide), "unsupported wchar_t pointer");
    }
}

Require(Marshal.SizeOf<NGXBool8>() == 1, "bool8 size");
NGXDLSSGOptEvalParams defaults = new();
Require(defaults.MultiFrameCount == 1 && defaults.MultiFrameIndex == 1 && defaults.MinRelativeLinearDepthObjectSeparation == 40, "official FG defaults");
Console.WriteLine($"Verified {checks} compiled API/ABI checks across four RIDs; {imports.Count} imports.");

delegate ReadOnlySpan<byte> SpanGetter();

// Shared verbatim with the NativeAOT smoke so each RID exercises the same bytes.
static unsafe class MathAbiChecks
{
    public static void Verify(Action<bool, string> require)
    {
        require(sizeof(Matrix4x4) == 64 && sizeof(Vector2) == 8 && sizeof(Vector3) == 12, "System.Numerics sizes");
        require(!RuntimeHelpers.IsReferenceOrContainsReferences<Matrix4x4>() && !RuntimeHelpers.IsReferenceOrContainsReferences<Vector2>() && !RuntimeHelpers.IsReferenceOrContainsReferences<Vector3>(), "System.Numerics contains no GC references");
        require(sizeof(NGXDLSSGOptEvalParams) == 592 && !RuntimeHelpers.IsReferenceOrContainsReferences<NGXDLSSGOptEvalParams>(), "FG outer native layout");
        Matrix4x4 matrix = new(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
        Matrix4x4 negativeMatrix = Matrix4x4.Negate(matrix);
        Vector2 vector2 = new(17, 18);
        Vector3 vector3 = new(19, 20, 21);
        ReadOnlySpan<float> matrixValues = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16];
        ReadOnlySpan<nint> matrixComponents = [(nint)(&matrix.M11), (nint)(&matrix.M12), (nint)(&matrix.M13), (nint)(&matrix.M14), (nint)(&matrix.M21), (nint)(&matrix.M22), (nint)(&matrix.M23), (nint)(&matrix.M24), (nint)(&matrix.M31), (nint)(&matrix.M32), (nint)(&matrix.M33), (nint)(&matrix.M34), (nint)(&matrix.M41), (nint)(&matrix.M42), (nint)(&matrix.M43), (nint)(&matrix.M44)];
        for (int i = 0; i < matrixComponents.Length; i++) require(matrixComponents[i] - (nint)(&matrix) == i * sizeof(float), "Matrix4x4 row-major component " + i);
        require((byte*)&vector2.X == (byte*)&vector2 && (byte*)&vector2.Y - (byte*)&vector2 == sizeof(float), "Vector2 component offsets");
        require((byte*)&vector3.X == (byte*)&vector3 && (byte*)&vector3.Y - (byte*)&vector3 == sizeof(float) && (byte*)&vector3.Z - (byte*)&vector3 == 2 * sizeof(float), "Vector3 component offsets");
        NGXDLSSGOptEvalParams options = new();

        void Field<T>(ref T field, void* owner, int offset, T value, T negative, ReadOnlySpan<float> expected) where T : unmanaged, IEquatable<T>
        {
            require((byte*)Unsafe.AsPointer(ref field) - (byte*)owner == offset, "FG math field offset " + offset);
            field = value;
            float* native = (float*)((byte*)owner + offset);
            require(new ReadOnlySpan<float>(native, expected.Length).SequenceEqual(expected), "typed write/native read " + offset);
            for (int i = 0; i < expected.Length; i++) native[i] = -expected[i];
            require(field.Equals(negative), "native write/typed read " + offset);
        }

        Field(ref options.CameraViewToClip, &options, 8, matrix, negativeMatrix, matrixValues);
        Field(ref options.ClipToCameraView, &options, 72, matrix, negativeMatrix, matrixValues);
        Field(ref options.ClipToLensClip, &options, 136, matrix, negativeMatrix, matrixValues);
        Field(ref options.ClipToPrevClip, &options, 200, matrix, negativeMatrix, matrixValues);
        Field(ref options.PrevClipToClip, &options, 264, matrix, negativeMatrix, matrixValues);
        Field(ref options.JitterOffset, &options, 328, vector2, -vector2, [17, 18]);
        Field(ref options.MvecScale, &options, 336, vector2, -vector2, [17, 18]);
        Field(ref options.CameraPinholeOffset, &options, 344, vector2, -vector2, [17, 18]);
        Field(ref options.CameraPos, &options, 352, vector3, -vector3, [19, 20, 21]);
        Field(ref options.CameraUp, &options, 364, vector3, -vector3, [19, 20, 21]);
        Field(ref options.CameraRight, &options, 376, vector3, -vector3, [19, 20, 21]);
        Field(ref options.CameraFwd, &options, 388, vector3, -vector3, [19, 20, 21]);
        require(options.MultiFrameCount == 1 && options.MultiFrameIndex == 1 && options.CameraNear == 0 && options.MinRelativeLinearDepthObjectSeparation == 40, "FG neighboring fields preserved");

        void Pointers<T>(ref T parameters, int size, int offset, Matrix4x4* view, Matrix4x4* clip, ReadOnlySpan<float> expected) where T : unmanaged
        {
            require(sizeof(T) == size && !RuntimeHelpers.IsReferenceOrContainsReferences<T>(), typeof(T).Name + " native layout without GC references");
            Matrix4x4** native = (Matrix4x4**)((byte*)Unsafe.AsPointer(ref parameters) + offset);
            require(native[0] == view && native[1] == clip, typeof(T).Name + " native matrix pointer offsets");
            for (int i = 0; i < expected.Length; i++) require(((float*)native[0])[i] == expected[i] && ((float*)native[1])[i] == -expected[i], typeof(T).Name + " native matrix element " + i);
            native[0] = clip;
            native[1] = view;
        }

        NGXD3D11DLSSDEvalParams d3d11 = new() { PInWorldToViewMatrix = &matrix, PInViewToClipMatrix = &negativeMatrix };
        NGXD3D12DLSSDEvalParams d3d12 = new() { PInWorldToViewMatrix = &matrix, PInViewToClipMatrix = &negativeMatrix };
        NGXCUDADLSSDEvalParams cuda = new() { PInWorldToViewMatrix = &matrix, PInViewToClipMatrix = &negativeMatrix };
        NGXVKDLSSDEvalParams vulkan = new() { PInWorldToViewMatrix = &matrix, PInViewToClipMatrix = &negativeMatrix };
        Pointers(ref d3d11, 904, 584, &matrix, &negativeMatrix, matrixValues);
        Pointers(ref d3d12, 904, 584, &matrix, &negativeMatrix, matrixValues);
        Pointers(ref cuda, 632, 360, &matrix, &negativeMatrix, matrixValues);
        Pointers(ref vulkan, 904, 600, &matrix, &negativeMatrix, matrixValues);
        require(d3d11.PInWorldToViewMatrix == &negativeMatrix && d3d11.PInViewToClipMatrix == &matrix, "D3D11 native pointer write/typed read");
        require(d3d12.PInWorldToViewMatrix == &negativeMatrix && d3d12.PInViewToClipMatrix == &matrix, "D3D12 native pointer write/typed read");
        require(cuda.PInWorldToViewMatrix == &negativeMatrix && cuda.PInViewToClipMatrix == &matrix, "CUDA native pointer write/typed read");
        require(vulkan.PInWorldToViewMatrix == &negativeMatrix && vulkan.PInViewToClipMatrix == &matrix, "Vulkan native pointer write/typed read");
    }
}
