using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using Streamline.NET;

internal static unsafe class Program
{
    private static int assertions;

    private static SLResult settingsResult;

    private static bool settingsHeaderValid;

    private static nint settingsNext;

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static SLResult GetSettings(DLSSOptions* options, DLSSOptimalSettings* settings)
    {
        settingsHeaderValid = settings->StructType == DLSSOptimalSettings.TypeId && settings->StructVersion == SL.StructVersion1;
        settingsNext = (nint)settings->Next;
        settings->OptimalRenderWidth = options->OutputWidth / 2;
        settings->OptimalRenderHeight = options->OutputHeight / 2;
        return settingsResult;
    }

    private static void CheckValueReturns(JsonObject snapshot)
    {
        Type featureFunctions = typeof(SL).Assembly.GetType("Streamline.NET.FeatureFunctions", throwOnError: true)!;
        Dictionary<(uint, string), nint> addresses = (Dictionary<(uint, string), nint>)featureFunctions
            .GetField("addresses", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        (uint, string) key = (SL.FeatureDLSS, "slDLSSGetOptimalSettings");
        addresses.Add(key, (nint)(delegate* unmanaged[Cdecl]<DLSSOptions*, DLSSOptimalSettings*, SLResult>)&GetSettings);
        try
        {
            DLSSOptions options = new() { OutputWidth = 1920, OutputHeight = 1080 };
            settingsResult = SLResult.Ok;
            DLSSOptimalSettings settings = SL.DLSS.GetOptimalSettings(in options);
            Assert(settingsHeaderValid && settingsNext == 0, "Value-return overload initializes the native header and an empty chain");
            Assert(settings.OptimalRenderWidth == 960 && settings.OptimalRenderHeight == 540, "Value-return overload returns the native output");
            foreach (SLResult failure in new[] { SLResult.ErrorInvalidParameter, SLResult.WarnOutOfVRAM, (SLResult)(-77) })
            {
                settingsResult = failure;
                try
                {
                    SL.DLSS.GetOptimalSettings(in options);
                    throw new InvalidOperationException("Expected an SDK exception.");
                }
                catch (SLException exception)
                {
                    Assert(exception.Result == failure, "SDK exception preserves the exact result");
                    Assert(exception.NativeFunction == "slDLSSGetOptimalSettings", "SDK exception preserves the native function name");
                }
            }
            DLSSState extension = new();
            settings = new() { Next = (BaseStructure*)(&extension) };
            settingsResult = SLResult.ErrorInvalidParameter;
            SLResult result = SL.DLSS.GetOptimalSettings(in options, ref settings);
            Assert(result == settingsResult && settingsNext == (nint)(&extension), "Reference overload retains caller storage, extension chains and non-throwing SDK results");
            Assert(settings.OptimalRenderWidth == 960, "Reference overload preserves partial output on a non-success result");

            int returnedValues = 0;
            foreach (JsonObject function in snapshot["declarations"]!.AsArray().OfType<JsonObject>().Where(item => item["kind"]!.GetValue<string>() == "FUNCTION_DECL"))
            {
                JsonObject? output = function["children"]!.AsArray().OfType<JsonObject>().SingleOrDefault(parameter => parameter["contract"]?["returnValue"]?.GetValue<bool>() == true);
                if (output is null)
                {
                    continue;
                }
                string nativeName = function["name"]!.GetValue<string>();
                string managedName = nativeName[2..];
                string? group = Path.GetFileName(function["file"]!.GetValue<string>()) switch
                {
                    "sl_dlss.h" => "DLSS",
                    "sl_dlss_d.h" => "DLSSD",
                    "sl_dlss_g.h" => "DLSSG",
                    "sl_deepdvc.h" => "DeepDVC",
                    "sl_directsr.h" => "DirectSR",
                    "sl_reflex.h" => "Reflex",
                    "sl_pcl.h" => "PCL",
                    "sl_nis.h" => "NIS",
                    _ => null
                };
                Type owner = group is null ? typeof(SL) : typeof(SL).GetNestedType(group)!;
                if (group is not null)
                {
                    managedName = managedName[group.Length..];
                }
                string nativeOutputType = output["type"]!["element"]!["declaration"]!.GetValue<string>().Replace("sl::", "", StringComparison.Ordinal);
                Type returnType = typeof(SL).Assembly.GetType("Streamline.NET." + ManagedName(nativeOutputType), throwOnError: true)!;
                Assert(owner.GetMethods(BindingFlags.Public | BindingFlags.Static).Any(method => method.Name == managedName && method.ReturnType == returnType), "Reviewed value-return API exists: " + nativeName);
                returnedValues++;
            }
            Assert(returnedValues == 13, "Only the reviewed output structures have value-return overloads");
            Assert(typeof(SL).GetMethod("UpgradeInterface", [typeof(nint).MakeByRefType()])!.ReturnType == typeof(SLResult), "In-place interface replacement is not converted to a value-return overload");
        }
        finally
        {
            addresses.Remove(key);
        }
    }

    private struct BorrowedObject
    {
        public nint* VTable;
        public uint FrameNumber;
        public uint Allocations;
        public uint Frees;
        public uint DestructorCalls;
        public uint DestructorFlags;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static uint ReadFrame(nint address) => ((BorrowedObject*)address)->FrameNumber;

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static void* Allocate(nint address, uint bytes)
    {
        ((BorrowedObject*)address)->Allocations++;
        return NativeMemory.Alloc(bytes);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static void Free(nint address, void* memory)
    {
        ((BorrowedObject*)address)->Frees++;
        NativeMemory.Free(memory);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static nint Destroy(nint address, uint flags)
    {
        ((BorrowedObject*)address)->DestructorCalls++;
        ((BorrowedObject*)address)->DestructorFlags = flags;
        return address;
    }

    private static void CheckBorrowedObjects(JsonObject snapshot)
    {
        JsonObject slots = snapshot["abi"]!["virtualSlots"]!.AsObject();
        nint* frameTable = stackalloc nint[1];
        frameTable[slots.Single(pair => pair.Key.StartsWith("frameToken:", StringComparison.Ordinal)).Value!.GetValue<int>()] = (nint)(delegate* unmanaged[MemberFunction]<nint, uint>)&ReadFrame;
        BorrowedObject frameObject = new() { VTable = frameTable, FrameNumber = 73 };
        FrameToken frame = new((nint)(&frameObject));
        Assert(frame.FrameIndex == 73 && (uint)frame == 73, "Frame token uses virtual dispatch on the borrowed address");

        nint* allocatorTable = stackalloc nint[3];
        allocatorTable[slots.Single(pair => pair.Key.Contains("::allocate(", StringComparison.Ordinal)).Value!.GetValue<int>()] = (nint)(delegate* unmanaged[MemberFunction]<nint, uint, void*>)&Allocate;
        allocatorTable[slots.Single(pair => pair.Key.Contains("::free(", StringComparison.Ordinal)).Value!.GetValue<int>()] = (nint)(delegate* unmanaged[MemberFunction]<nint, void*, void>)&Free;
        allocatorTable[slots.Single(pair => pair.Key.Contains("::~", StringComparison.Ordinal)).Value!.GetValue<int>()] = (nint)(delegate* unmanaged[MemberFunction]<nint, uint, nint>)&Destroy;
        BorrowedObject allocatorObject = new() { VTable = allocatorTable };
        IAllocator allocator = new((nint)(&allocatorObject));
        SLArray<int> values = new();
        values.CopyFrom(allocator, [1, 2, 3]);
        Assert(values.Size() == 3 && allocatorObject.Allocations == 1, "Original allocator allocates array storage");
        values[1] = 99;
        List<int> copied = [8, 9];
        values.CopyTo(copied);
        Assert(copied.SequenceEqual([1, 99, 3]), "Ref indexer and vector-copy semantics");
        values.CopyFrom(allocator, [4, 5]);
        Assert(allocatorObject.Allocations == 2 && allocatorObject.Frees == 1, "Replacing native array storage releases the old allocation");
        byte* storage = (byte*)(&values);
        foreach (JsonObject field in snapshot["abi"]!["arrayLayout"]!["fields"]!.AsArray().OfType<JsonObject>())
        {
            byte* location = storage + field["offsetBits"]!.GetValue<int>() / 8;
            string name = field["name"]!.GetValue<string>();
            if (name == "m_size")
            {
                Assert(*(uint*)location == 2, "Native array count offset");
            }
            else if (name == "m_pAllocator")
            {
                Assert(*(nint*)location == allocator.Handle, "Native array allocator offset");
            }
            else
            {
                Assert((*(int**)location)[0] == 4, "Native array data offset");
            }
        }
        values.Destroy();
        values.Destroy();
        Assert(values.Size() == 0 && allocatorObject.Frees == 2 && allocatorObject.DestructorCalls == 0, "Array destruction clears storage without destroying its allocator");
        allocator.Destroy();
        Assert(allocatorObject.DestructorCalls == 1 && allocatorObject.DestructorFlags == 0, "Explicit non-deleting virtual destructor call");
    }

    private static void Assert(bool condition, string message)
    {
        assertions++;
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void Throws<T>(Action action) where T : Exception
    {
        try
        {
            action();
        }
        catch (T)
        {
            assertions++;
            return;
        }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }

    private static string Pascal(string value) => char.ToUpperInvariant(value[0]) + value[1..];

    private static string ManagedName(string name)
    {
        return name switch
        {
            "Result" => "SLResult",
            "Boolean" => "SLBoolean",
            "Version" => "SLVersion",
            "uint2" => "UInt2",
            "uint3" => "UInt3",
            "tagRECT" => "Rect",
            "DXGI_FORMAT" => "DXGIFormat",
            _ => Pascal(name)
        };
    }

    private static void CheckLayouts(JsonObject snapshot)
    {
        Assembly assembly = typeof(SL).Assembly;
        int records = 0;
        foreach (JsonObject native in snapshot["declarations"]!.AsArray().OfType<JsonObject>())
        {
            if (native["kind"]!.GetValue<string>() != "STRUCT_DECL" || !native["definition"]!.GetValue<bool>()
                || native["classification"]!.GetValue<string>() is "plugin-template" or "implementation"
                || native["name"]!.GetValue<string>() is "FrameToken" or "IAllocator")
            {
                continue;
            }
            string name = ManagedName(native["name"]!.GetValue<string>());
            Type type = assembly.GetType("Streamline.NET." + name, throwOnError: true)!;
            int expected = native["type"]!["size"]!.GetValue<int>();
            Assert(Marshal.SizeOf(type) == expected, $"{name}: native size {expected}, managed {Marshal.SizeOf(type)}");
            IEnumerable<JsonObject> fields = native["children"]!.AsArray().OfType<JsonObject>();
            JsonObject? union = fields.FirstOrDefault(child => child["kind"]!.GetValue<string>() == "UNION_DECL");
            fields = (union?["children"]!.AsArray().OfType<JsonObject>() ?? fields).Where(child => child["kind"]!.GetValue<string>() == "FIELD_DECL");
            foreach (JsonObject field in fields)
            {
                string fieldName = field["name"]!.GetValue<string>();
                fieldName = field["access"]!.GetValue<string>() == "PRIVATE" ? fieldName : Pascal(fieldName.Replace("_", "", StringComparison.Ordinal));
                fieldName = fieldName switch { "SType" => "SType", "PNext" => "PNext", "InternalFlags" => "InternalFlagsValue", _ => fieldName };
                long nativeOffset = field["offsetBits"]!.GetValue<long>() / 8;
                long managedOffset = Marshal.OffsetOf(type, fieldName).ToInt64();
                Assert(managedOffset == nativeOffset, $"{name}.{fieldName}: native offset {nativeOffset}, managed {managedOffset}");
            }
            records++;
        }
        Assert(sizeof(SLArray<byte>) == snapshot["abi"]!["arrayLayout"]!["size"]!.GetValue<int>(), "Allocator-backed array layout");
        Console.WriteLine($"Compared {records} value-type layouts against Clang output.");
    }

    private static void CheckDefaults()
    {
        DLSSOptions options = new();
        Assert(options.StructType == DLSSOptions.TypeId && options.StructVersion == 3 && options.Next == null, "DLSS structure header");
        Assert(options.Mode == DLSSMode.Off && options.OutputWidth == uint.MaxValue && options.PreExposure == 1, "DLSS defaults");
        Assert(default(DLSSOptions).StructVersion == 0, "CLR default must remain distinct from native construction");
        Constants constants = new();
        Assert(constants.CameraViewToClip.Row[3].W == float.MaxValue && constants.CameraFwd.Z == float.MaxValue, "Nested native constructors");
        ReflexState reflex = new();
        Assert(reflex.FrameReport[63].StructType == ReflexReport.TypeId && reflex.FrameReport2[63].StructType == ReflexReport2.TypeId, "Native constructors inside inline arrays");
        ViewportHandle viewport = new(-1);
        Assert((uint)viewport == uint.MaxValue && viewport.StructType == ViewportHandle.TypeId, "Private viewport value and unchecked conversion");
        Assert((bool)new Bool8(true) && !(bool)new Bool8(false), "Native Boolean");
        Assert(sizeof(SLBoolean) == 1 && SLBoolean.Invalid != SLBoolean.False, "Tri-state native Boolean");
        SLVersion version = new(2, 14, 1);
        Assert(version.ToStr() == "2.14.1" && version > new SLVersion(2, 13, 99), "Native version members");
        Extent extent = new() { Left = 10, Top = 20, Width = 30, Height = 40 };
        Rect rect = extent;
        Assert(rect.Right == 40 && rect.Bottom == 60, "Native rectangle conversion");
        Assert(SL.DLSS.GetModeAsStr(DLSSMode.MaxQuality) == "DLSSMode::eMaxQuality", "Exact native enum text");
        Assert(SL.DLSS.GetModeAsStr((DLSSMode)int.MaxValue) == "Unknown", "Unknown native enum text");
        Assert(SL.DLSS.ResolvePreset(DLSSPreset.PresetJ) == DLSSPreset.PresetJ && SL.DLSS.ResolvePreset((DLSSPreset)int.MaxValue) == DLSSPreset.Default, "Native preset resolution");
        Assert(SL.HasAnyFlags(PreferenceFlags.AllowOTA, PreferenceFlags.AllowOTA | PreferenceFlags.LoadDownloadedPlugins), "Any-bit flag semantics");
    }

    private static void CheckChains()
    {
        DLSSOptions first = new();
        DLSSState second = new();
        first.Next = (BaseStructure*)&second;
        Assert(SL.FindStruct<DLSSState>(&first) == &second, "Chain search returns original address");
        Assert(SL.FindStruct<NISState>(&first) == null, "Missing chain type");
        Assert(SL.FindStruct<NISState, DLSSState>(&first) == null, "Stop type");
        Assert(SL.FindStruct<NISState, DLSSOptions>(&second) == null, "Defined handling of upstream null-tail dereference");
        void** roots = stackalloc void*[2];
        roots[0] = &first;
        roots[1] = &second;
        List<nint> matches = [];
        Assert(SL.FindStructs<DLSSState>(roots, 2, matches) && matches.Count == 2, "Chain-root pointer array and append order");
        Assert(SL.FindStructs<DLSSState>(null, 0, matches) && matches.Count == 2, "Existing matches retained");
    }

    private static void CheckMath()
    {
        Float4x4 identity = new(new(1, 0, 0, 0), new(0, 1, 0, 0), new(0, 0, 1, 0), new(0, 0, 0, 1));
        Float4x4 input = new(new(2, 0, 0, 0), new(0, 4, 0, 0), new(0, 0, 5, 0), new(0, 0, 0, 1));
        Float4x4 result = new();
        SL.MatrixMul(ref result, in identity, in input);
        Assert(result[0].X == 2 && result[1].Y == 4 && result[2].Z == 5, "Matrix multiplication");
        SL.MatrixFullInvert(ref result, in input);
        Assert(result[0].X == 0.5f && result[1].Y == 0.25f && result[2].Z == 0.2f, "Full matrix inversion");
        Float4x4 singular = default;
        SL.MatrixFullInvert(ref result, in singular);
        Assert(result[0].X == 0 && result[3].W == 0, "Native singular-matrix behavior");
        singular = new(new(1, 0, 0, 0), new(0, 1, 0, 0), new(0, 0, 1, 0), new(0, 0, 0, 0));
        SL.MatrixFullInvert(ref result, in singular);
        Assert(result[3].W == 1 && result[0].X == 0, "Singular rank-three input preserves the native unscaled cofactor result");
        Float4x4 affine = new(new(2, 1, 0, 0), new(0, 3, 1, 0), new(1, 0, 4, 0), new(5, 6, 7, 1));
        System.Numerics.Matrix4x4 oracleInput = new(2, 1, 0, 0, 0, 3, 1, 0, 1, 0, 4, 0, 5, 6, 7, 1);
        Assert(System.Numerics.Matrix4x4.Invert(oracleInput, out System.Numerics.Matrix4x4 oracle), "Independent matrix oracle is invertible");
        SL.MatrixFullInvert(ref result, in affine);
        float* actualValues = (float*)(&result);
        float* oracleValues = (float*)(&oracle);
        for (int index = 0; index < 16; index++)
        {
            Assert(MathF.Abs(actualValues[index] - oracleValues[index]) < 0.00001f, "Nondiagonal inverse component " + index);
        }
        Float3 zero = new(0, 0, 0);
        SL.VectorNormalize(ref zero);
        Assert(float.IsNaN(zero.X) && float.IsNaN(zero.Y), "Native zero-vector boundary");
        Float4x4 transposed = SL.Transpose(in input);
        Assert(transposed[1].Y == input[1].Y, "Transpose");
        Constants camera = new()
        {
            CameraRight = new(2, 0, 0),
            CameraFwd = new(0, 0, 3),
            CameraPos = new(0, 0, 0),
            CameraViewToClip = identity
        };
        SL.RecalculateCameraMatrices(ref camera);
        Assert(camera.CameraRight.X == 1 && camera.CameraFwd.Z == 1 && camera.CameraUp.Y == 1, "Explicit camera helper performs the original normalization and cross product");
        Assert(camera.ClipToPrevClip[3].X == 0, "Initial shared camera history");
        camera.CameraPos.X = 1;
        SL.RecalculateCameraMatrices(ref camera);
        Assert(camera.ClipToPrevClip[3].X == 1 && camera.PrevClipToClip[3].X == -1, "Shared previous-camera history and inverse updates");
    }

    private static void CheckVulkan()
    {
        VkPhysicalDeviceVulkan12Features features = SL.GetVkPhysicalDeviceVulkan12Features(["timelineSemaphore", "unknown", "shaderFloat16\0ignored"]);
        Assert(features.TimelineSemaphore == 1 && features.ShaderFloat16 == 1 && features.BufferDeviceAddress == 0, "Vulkan native name matching, including C string terminator");
        Assert(features.SType == SL.VkStructureTypePhysicalDeviceVulkan12Features && features.PNext == null, "Vulkan header");
        VkPhysicalDeviceVulkan12Features supported = new() { SType = features.SType, TimelineSemaphore = 2 };
        SL.GetMergedSupportedVkPhysicalDeviceVulkanFeatures((VkBaseOutStructure*)&features, null, (VkBaseOutStructure*)&supported);
        Assert(features.TimelineSemaphore == 1 && features.ShaderFloat16 == 0, "Vulkan logical support merge");
        VkPhysicalDeviceOpticalFlowFeaturesNV optical = SL.GetVkPhysicalDeviceOpticalFlowNVFeatures(["opticalFlow"]);
        Assert(optical.OpticalFlow == 1, "Optical flow feature data");
    }

    private static int Main(string[] args)
    {
        string root = Path.GetFullPath(args.Length == 0 ? "." : args[0]);
        JsonObject snapshot = JsonNode.Parse(File.ReadAllText(Path.Combine(root, "Streamline.NET.Generator/streamline-ast.json")))!.AsObject();
        CheckLayouts(snapshot);
        CheckDefaults();
        CheckChains();
        CheckMath();
        CheckVulkan();
        CheckBorrowedObjects(snapshot);
        CheckValueReturns(snapshot);
        Assert(!SL.IsSignedByNVIDIA(Path.Combine(root, ".work", "unsigned-missing-file")), "Missing-file signature identity check");
        Assert(!SL.VerifyEmbeddedSignature(Path.Combine(root, ".work", "unsigned-missing-file")), "Missing-file signature trust check");
        Throws<InvalidOperationException>(() => SL.Shutdown());
        Throws<ArgumentException>(() => SL.SetLibraryPath("relative-library"));
        SL.SetLibraryPath(Path.Combine(root, ".work", "nonexistent-streamline-library"));
        Assert(SL.GetResultAsStr(SLResult.Ok) == "Result::eOk", "Pure helper after path configuration does not load the SDK");
        Throws<DllNotFoundException>(() => SL.Shutdown());
        string? systemLibrary = OperatingSystem.IsWindows()
            ? Path.Combine(Environment.SystemDirectory, "kernel32.dll")
            : OperatingSystem.IsMacOS() ? "/usr/lib/libSystem.B.dylib" : null;
        if (systemLibrary is not null)
        {
            // Use an existing OS library with no Streamline exports. No test DLL or
            // NVIDIA runtime asset is downloaded or distributed.
            SL.SetLibraryPath(systemLibrary);
            Throws<EntryPointNotFoundException>(() => SL.Shutdown());
            Throws<InvalidOperationException>(() => SL.SetLibraryPath(systemLibrary));
        }
        Console.WriteLine($"Passed {assertions} assertions.");
        return 0;
    }
}
