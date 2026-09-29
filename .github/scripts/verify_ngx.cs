#:project ../../NGX.NET/NGX.NET.csproj
#:property PublishAot=false
#:property PublishTrimmed=false
#:property EnableTrimAnalyzer=false
#:property EnableAotAnalyzer=false

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

foreach (JsonProperty platform in document.RootElement.GetProperty("platforms").EnumerateObject())
{
    string rid = platform.Name;
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
            if (nativeType.GetProperty("kind").GetString() != "CONSTANTARRAY") VerifyType(nativeType, managed.FieldType, name + "::" + fieldName);
            else Require(Size(managed.FieldType) == nativeType.GetProperty("size").GetInt32(), rid + " array storage " + name + "::" + fieldName);
        }
    }

    foreach (JsonElement enumeration in platform.Value.GetProperty("enums").EnumerateArray())
    {
        string name = enumeration.GetProperty("name").GetString()!;
        Require(types.TryGetValue(name, out Type? type), rid + " missing enum " + name);
        Dictionary<string, FieldInfo> members = type!.GetFields(BindingFlags.Public | BindingFlags.Static).ToDictionary(f => f.GetCustomAttribute<NGXNativeNameAttribute>()!.Name);
        foreach (JsonElement member in enumeration.GetProperty("values").EnumerateArray())
        {
            string memberName = member.GetProperty("name").GetString()!;
            Require(members.TryGetValue(memberName, out FieldInfo? field), "missing enum member " + memberName);
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
Require(Marshal.SizeOf<NGXBool8>() == 1, "bool8 size");
NGXDLSSGOptEvalParams defaults = new();
Require(defaults.MultiFrameCount == 1 && defaults.MultiFrameIndex == 1 && defaults.MinRelativeLinearDepthObjectSeparation == 40, "official FG defaults");
Console.WriteLine($"Verified {checks} compiled API/ABI checks across four RIDs; {imports.Count} imports.");

delegate ReadOnlySpan<byte> SpanGetter();
