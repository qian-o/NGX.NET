using System.Text;

namespace Streamline.NET.Generator;

internal sealed partial class CSharpEmitter
{
    public int Generate()
    {
        LoadReviewedImplementations();
        foreach (NativeDeclaration declaration in snapshot.Declarations)
        {
            if (declaration.Classification is "test" or "plugin-template" or "implementation")
            {
                Record(declaration, "Excluded: " + declaration.Reason);
                continue;
            }

            if (declaration.Kind == "ENUM_DECL")
            {
                if (declaration.MappedAs == "int")
                {
                    EmitVulkanConstants(declaration);
                }
                else
                {
                    EmitEnum(declaration);
                }
            }
            else if (declaration.IsRecord)
            {
                if (!declaration.Definition)
                {
                    Record(declaration, "Opaque borrowed native address");
                }
                else if (declaration.Name is "FrameToken" or "IAllocator")
                {
                    EmitBorrowedHandle(declaration);
                }
                else
                {
                    EmitStruct(declaration);
                }
            }
            else if (declaration.Kind == "VAR_DECL")
            {
                EmitConstant(declaration);
            }
            else if (declaration.Kind == "FUNCTION_DECL" && declaration.Name.StartsWith("sl", StringComparison.Ordinal)
                     && !declaration.QualifiedName.Contains("::", StringComparison.Ordinal))
            {
                EmitFunction(declaration);
            }
            else if (declaration.Kind is "TYPE_ALIAS_DECL" or "TYPEDEF_DECL")
            {
                if (!declaration.Name.StartsWith("PFun_", StringComparison.Ordinal) && !declaration.Name.StartsWith("PFunOn", StringComparison.Ordinal))
                {
                    Record(declaration, "Underlying native type in signatures and fields");
                }
            }
            else if (TryRecordManual(declaration))
            {
                // Source hashes require a fresh review when upstream inline bodies change.
            }
            else if (declaration.Name == "to_underlying" && declaration.Kind == "FUNCTION_TEMPLATE")
            {
                Record(declaration, "C# cast to the extracted enum underlying integer type");
            }
            else if (TryEmitHelper(declaration))
            {
                // The helper emitter records the exact semantic translation.
            }
            else
            {
                // The recursive coverage pass reports every remaining declaration.
            }
        }

        EmitSecurityData();
        EmitPublicMacros();
        RecordCallbackAliases();
        foreach (NativeDeclaration declaration in snapshot.Declarations)
        {
            CompleteCoverage(declaration);
        }
        CheckMacros();
        if (unhandled.Count > 0)
        {
            foreach (string item in unhandled)
            {
                Console.Error.WriteLine("Unhandled: " + item);
            }
            Console.Error.WriteLine($"Generation stopped: {unhandled.Count} declarations or macros have no verified implementation.");
            return 1;
        }
        WriteFiles();
        WriteCoverage();
        Console.WriteLine($"Source: {snapshot.Source.Release} {snapshot.Source.Commit}");
        Console.WriteLine($"Generated {files.Count} files; 0 unclassified, 0 unhandled declarations.");
        return 0;
    }

    private void EmitVulkanConstants(NativeDeclaration declaration)
    {
        Record(declaration, "32-bit signed native enum storage");
        foreach (NativeDeclaration value in declaration.Children)
        {
            StringBuilder builder = File("Vulkan", "SL.Constants");
            string name = VulkanConstantName(value.Name);
            builder.AppendLine();
            Comment(builder, value, "    ");
            builder.AppendLine($"    public const int {name} = {value.Value};");
            Record(value, "SL." + name);
        }
    }

    private void EmitBorrowedHandle(NativeDeclaration declaration)
    {
        string name = declaration.Name;
        StringBuilder builder = File("Core", name);
        builder.AppendLine();
        Comment(builder, declaration);
        builder.AppendLine($"public readonly unsafe partial struct {name}(nint handle)");
        builder.AppendLine("{");
        builder.AppendLine("    /// <summary>Borrowed native object address. Copying this wrapper does not transfer ownership.</summary>");
        builder.AppendLine("    public readonly nint Handle = handle;");
        builder.AppendLine();

        if (name == "FrameToken")
        {
            EmitTypeId(builder, declaration);
            int slot = snapshot.Abi.VirtualSlots.Single(pair => pair.Key.StartsWith("frameToken:", StringComparison.Ordinal)).Value;
            builder.AppendLine("    /// <summary>Reads the frame index through the native virtual method. The SDK object must remain valid.</summary>");
            builder.AppendLine($"    public uint FrameIndex => ((delegate* unmanaged[MemberFunction]<nint, uint>)(*(nint**)Handle)[{slot}])(Handle);");
            builder.AppendLine();
            builder.AppendLine("    /// <summary>Invokes the native frame-index conversion.</summary>");
            builder.AppendLine("    public static implicit operator uint(FrameToken value) => value.FrameIndex;");
            foreach (NativeDeclaration child in declaration.Children.Where(child => child.Kind is "CXX_BASE_SPECIFIER" or "CONSTRUCTOR" or "CONVERSION_FUNCTION"))
            {
                Record(child, "FrameToken borrowed object address and virtual frame-index access; native construction belongs to the SDK");
            }
        }
        else
        {
            int allocate = snapshot.Abi.VirtualSlots.Single(pair => pair.Key.Contains("::allocate(", StringComparison.Ordinal)).Value;
            int free = snapshot.Abi.VirtualSlots.Single(pair => pair.Key.Contains("::free(", StringComparison.Ordinal)).Value;
            builder.AppendLine("    /// <summary>Allocates the requested byte count using the original native allocator.</summary>");
            builder.AppendLine($"    public void* Allocate(uint nBytes) => ((delegate* unmanaged[MemberFunction]<nint, uint, void*>)(*(nint**)Handle)[{allocate}])(Handle, nBytes);");
            builder.AppendLine();
            builder.AppendLine("    /// <summary>Frees memory previously allocated by this same allocator.</summary>");
            builder.AppendLine($"    public void Free(void* memory) => ((delegate* unmanaged[MemberFunction]<nint, void*, void>)(*(nint**)Handle)[{free}])(Handle, memory);");
            int destroy = snapshot.Abi.VirtualSlots.Single(pair => pair.Key.Contains("::~", StringComparison.Ordinal)).Value;
            if (!snapshot.Abi.AllocatorDestructor.Contains("i32 noundef 0", StringComparison.Ordinal))
            {
                throw new InvalidDataException("Unrecognized allocator destructor dispatch contract.");
            }
            builder.AppendLine();
            builder.AppendLine("    /// <summary>Explicitly invokes the native virtual destructor without freeing object storage. Requires the caller's ownership authority; invalidates the object.</summary>");
            builder.AppendLine("    public void Destroy()");
            builder.AppendLine("    {");
            builder.AppendLine($"        ((delegate* unmanaged[MemberFunction]<nint, uint, nint>)(*(nint**)Handle)[{destroy}])(Handle, 0);");
            builder.AppendLine("    }");
            foreach (NativeDeclaration child in declaration.Children.Where(child => child.Kind is "CXX_METHOD" or "DESTRUCTOR"))
            {
                Record(child, "IAllocator." + (child.Kind == "DESTRUCTOR" ? "Destroy" : TypeMapper.PascalCase(child.Name)));
            }
        }

        builder.AppendLine("}");
        Record(declaration, name + " borrowed address wrapper");
    }
}
