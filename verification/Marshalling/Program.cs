using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using NGX.NET;

namespace Marshalling;

internal static unsafe class Program
{
    private static readonly List<string> passed = [];
    private static readonly Dictionary<string, object> metrics = [];

    private static int Roots => ((System.Collections.IDictionary)typeof(NgxCallbacks).GetField("roots", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!).Count;

    private static int Main(string[] args)
    {
        string root = Path.GetFullPath(args.Length is 0 ? "." : args[0]);
        try
        {
            Run("all generated Native layouts and constructors match the four-target AST", () => CheckLayouts(root));
            Run("enum values and native import names match the SDK", () => CheckEnumsAndImports(root));
            Run("Ngx methods and public data structs expose no pointers or Native types", CheckSurface);
            Run("nested constructor, Unicode paths and union round trip survive compacting GC", CheckDiscovery);
            Run("constructor failure, union discriminator and bounded arrays reject invalid input", CheckInvalidInput);
            Run("fixed-buffer strings respect capacity and read without a terminator", CheckFixedStrings);
            Run("RR resource and matrix pointers survive constructor return and preserve selected views", CheckEvaluation);
            Run("GBuffer arrays preserve null slots and fixed native capacity", CheckGBuffer);
            Run("native callback ABI, UTF-8 strings, exceptions and one-byte cancellation", CheckCallbacks);
            Run("Init ownership is reserved before native entry, committed or rolled back internally", CheckInitialization);
            Run("parameter snapshots preserve failed-call data and release on success or Reset", CheckParameters);
            Run("omitted DLSSG options preserve previously registered matrix storage", CheckFrameGenerationRetention);
            Run("shutdown of the last tracked device releases its backend's parameter snapshots", CheckBackendCleanup);
            Run("CUDA device addresses remain stable across copied public descriptors", CheckCuda);
            Run("SDK defaults preserve the distinction between new and default", CheckDefaults);
            Run("readonly handles preserve layout, equality, hashing and deconstruction", CheckHandles);
            Run("constructor and retained RR conversion allocation measurement", Measure);

            using JsonDocument ast = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "NGX.NET.Generator/ast.json")));
            object result = new
            {
                status = "managed-refactor-verified-native-runtime-unverified",
                sdk = ast.RootElement.GetProperty("source").Clone(),
                runtime = RuntimeInformation.FrameworkDescription,
                host = RuntimeInformation.RuntimeIdentifier,
                tests = passed,
                metrics,
                limitations = new[]
{
                    "Tests exercise the production generated constructors and internal lifetime management, not a NVIDIA GPU.",
                    "Windows/Linux runtime behavior and NativeAOT execution require target-platform validation.",
                    "Constructor-owned RR storage uses native heap allocation; the earlier stack-prototype zero-allocation result does not apply.",
                    "Failed helper calls conservatively retain both old and new snapshots until successful replacement, Parameter.Reset, DestroyParameters, or backend shutdown."
}
            };
            File.WriteAllText(Path.Combine(root, "verification/Marshalling/results.json"), JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }) + "\n");
            Console.WriteLine($"PASS {passed.Count} production validation groups.");

            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);

            return 1;
        }
    }

    private static void Run(string name, Action test)
    {
        test();
        passed.Add(name);
        Console.WriteLine("PASS " + name);
    }

    private static void Assert(bool value, string message)
    {
        if (!value)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void Throws<T>(Action action)
        where T : Exception
    {
        try
        {
            action();
        }
        catch (T)
        {
            return;
        }

        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }

    private static bool ContainsReferences<T>()
    {
        return RuntimeHelpers.IsReferenceOrContainsReferences<T>();
    }

    private static void CheckLayouts(string root)
    {
        using JsonDocument ast = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "NGX.NET.Generator/ast.json")));
        Dictionary<string, (string Type, string Source)> nativeSources = Directory.GetFiles(Path.Combine(root, "NGX.NET/Types/Native"), "*.g.cs").Select(static path => (Type: Path.GetFileName(path).Replace(".g.cs", ""), Source: File.ReadAllText(path))).ToDictionary(static item => Regex.Match(item.Source, @"/// (\w+)").Groups[1].Value);
        int layouts = 0;
        int offsets = 0;
        foreach (JsonProperty platform in ast.RootElement.GetProperty("platforms").EnumerateObject())
        {
            foreach (JsonElement record in platform.Value.GetProperty("records").EnumerateArray())
            {
                if (record.GetProperty("opaque").GetBoolean())
                {
                    continue;
                }

                string name = record.GetProperty("name").GetString()!;
                (string Type, string Source) source = nativeSources[name];
                Type type = typeof(Ngx).Assembly.GetType("NGX.NET." + source.Type, true)!;
                Type managed = typeof(Ngx).Assembly.GetType("NGX.NET." + source.Type[..^6], true)!;
                Assert(type.IsNotPublic && typeof(IDisposable).IsAssignableFrom(type), name + " visibility/disposal");
                Assert(type.GetConstructor([managed.MakeByRefType()]) != null, name + " public-struct constructor");
                Assert(!(bool)typeof(Program).GetMethod(nameof(ContainsReferences), BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(type).Invoke(null, null)!, name + " managed native field");
                Assert(Marshal.SizeOf(type) == record.GetProperty("size").GetInt32(), platform.Name + " size " + name);
                layouts++;

                foreach (JsonElement field in record.GetProperty("fields").EnumerateArray())
                {
                    string fieldName = field.GetProperty("name").GetString()!;
                    string suffix = source.Source[source.Source.IndexOf(name + "::" + fieldName + "\n", StringComparison.Ordinal)..];
                    string managedField = Regex.Match(suffix, @"public [^\n]+ (\w+)(?:\[\d+\])?;").Groups[1].Value;
                    Assert((long)Marshal.OffsetOf(type, managedField) == field.GetProperty("offset").GetInt64() / 8, name + "::" + fieldName);
                    offsets++;
                }
            }
        }

        metrics["nativeTypes"] = nativeSources.Count;
        metrics["astLayoutComparisons"] = layouts;
        metrics["astFieldOffsetComparisons"] = offsets;
    }

    private static void CheckEnumsAndImports(string root)
    {
        using JsonDocument ast = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "NGX.NET.Generator/ast.json")));
        Dictionary<string, (string Type, string Source)> enums = Directory.GetFiles(Path.Combine(root, "NGX.NET/Types"), "*.g.cs").Select(static path => (Type: Path.GetFileName(path).Replace(".g.cs", ""), Source: File.ReadAllText(path))).Where(static item => item.Source.Contains("public enum")).ToDictionary(static item => Regex.Match(item.Source, @"/// (\w+)").Groups[1].Value);
        HashSet<string> imports = [.. typeof(Ngx).GetNestedTypes(BindingFlags.Public).Append(typeof(Ngx)).SelectMany(static type => type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic)).Select(static method => method.GetCustomAttribute<LibraryImportAttribute>()?.EntryPoint).OfType<string>()];
        int enumValues = 0;
        int functions = 0;
        foreach (JsonProperty platform in ast.RootElement.GetProperty("platforms").EnumerateObject())
        {
            foreach (JsonElement item in platform.Value.GetProperty("enums").EnumerateArray())
            {
                (string Type, string Source) source = enums[item.GetProperty("name").GetString()!];
                Type type = typeof(Ngx).Assembly.GetType("NGX.NET." + source.Type, true)!;
                Dictionary<string, string> members = Regex.Matches(source.Source, @"/// (\w+)\s*/// </summary>\s*(\w+)\s*=").ToDictionary(static match => match.Groups[1].Value, static match => match.Groups[2].Value);
                foreach (JsonElement entry in item.GetProperty("values").EnumerateArray())
                {
                    string member = members[entry.GetProperty("name").GetString()!];
                    Assert(unchecked((uint)Convert.ToInt64(Enum.Parse(type, member))) == unchecked((uint)entry.GetProperty("value").GetInt64()), type.Name + "." + member);
                    enumValues++;
                }
            }

            foreach (JsonElement function in platform.Value.GetProperty("functions").EnumerateArray())
            {
                Assert(imports.Contains(function.GetProperty("export").GetString()!), "Missing native import");
                functions++;
            }
        }

        Assert(Enum.IsDefined(NGXEngineType.Custom) && Enum.IsDefined(NGXPerfQualityValue.Dlaa) && Enum.IsDefined(NGXResult.FailFeatureNotSupported), "PascalCase enums");
        metrics["enumValueComparisons"] = enumValues;
        metrics["astImportComparisons"] = functions;
    }

    private static void CheckSurface()
    {
        foreach (Type type in typeof(Ngx).GetNestedTypes(BindingFlags.Public).Append(typeof(Ngx)))
        {
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                SafeType(method.ReturnType);

                foreach (ParameterInfo parameter in method.GetParameters())
                {
                    SafeType(parameter.ParameterType);
                }
            }
        }

        foreach (Type type in typeof(Ngx).Assembly.GetExportedTypes().Where(static type => type.IsValueType && !type.IsEnum))
        {
            foreach (FieldInfo field in type.GetFields())
            {
                SafeType(field.FieldType);
            }
        }

        Assert(typeof(Ngx).Assembly.GetType("NGX.NET.NGX") == null, "Old entry class remains");
    }

    private static void SafeType(Type type)
    {
        Assert(!type.IsPointer && !type.IsFunctionPointer && !type.Name.EndsWith("Native"), "Unsafe public type " + type);

        if (type.HasElementType)
        {
            SafeType(type.GetElementType()!);
        }

        foreach (Type argument in type.GenericTypeArguments)
        {
            SafeType(argument);
        }
    }

    private static NGXFeatureDiscoveryInfo Discovery()
    {
        return new()
        {
            SDKVersion = NGXVersion.Api,
            FeatureID = NGXFeature.RayReconstruction,
            Identifier = new()
            {
                IdentifierType = NGXApplicationIdentifierType.ProjectId,
                V = new()
                {
                    ProjectDesc = new()
                    {
                        ProjectId = "project-测试",
                        EngineType = NGXEngineType.Custom,
                        EngineVersion = "1.0🚀"
                    }
                }
            },
            ApplicationDataPath = "/tmp/中文🚀",
            FeatureInfo = new()
            {
                PathListInfo = new()
                {
                    Paths = ["/runtime/库", "", new string('文', 100_000)]
                }
            }
        };
    }

    private static void CheckDiscovery()
    {
        NGXFeatureDiscoveryInfo value = Discovery();
        NGXFeatureDiscoveryInfoNative native = new(in value);
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        NGXFeatureDiscoveryInfo restored = new(in native);
        Assert(restored.Identifier.V.ProjectDesc!.Value.EngineVersion is "1.0🚀", "UTF-8 nested identity");
        Assert(restored.ApplicationDataPath == value.ApplicationDataPath, "Native-wide data path");
        Assert(restored.FeatureInfo!.Value.PathListInfo.Paths!.SequenceEqual(value.FeatureInfo!.Value.PathListInfo.Paths!), "Nested paths");
        native.Dispose();
        native.Dispose();
        Assert(native.FeatureInfo == null && native.ApplicationDataPath == null, "Native reset");
        Assert(restored.FeatureInfo.Value.PathListInfo.Paths![0] is "/runtime/库", "Managed result independence");
        value.Identifier = new()
        {
            IdentifierType = NGXApplicationIdentifierType.ApplicationId,
            V = new()
            {
                ApplicationId = ulong.MaxValue
            }
        };
        native = new(in value);
        Assert(native.Identifier.V.ApplicationId is ulong.MaxValue, "Union integer arm");
        native.Dispose();
    }

    private static void CheckInvalidInput()
    {
        NGXFeatureDiscoveryInfo value = Discovery();
        value.FeatureInfo = new()
        {
            PathListInfo = new()
            {
                Paths = ["first", null!]
            }
        };
        Throws<ArgumentNullException>(() => new NGXFeatureDiscoveryInfoNative(in value));
        value.Identifier.IdentifierType = NGXApplicationIdentifierType.ApplicationId;
        Throws<ArgumentException>(() => new NGXFeatureDiscoveryInfoNative(in value));
        NGXVKGBuffer buffer = new()
        {
            PInAttrib = new NGXResourceVK?[18]
        };
        Throws<ArgumentException>(() => new NGXVKGBufferNative(in buffer));
        NGXResourceVKUnion union = new()
        {
            ImageViewInfo = new(),
            BufferInfo = new()
        };
        Throws<ArgumentException>(() => new NGXResourceVKUnionNative(in union));
        NGXLoggingInfo logging = new()
        {
            DisableOtherLoggingSinks = true
        };
        Throws<ArgumentException>(() => new NGXLoggingInfoNative(in logging));
        Throws<ArgumentException>(static () => Ngx.Parameter.Reset(default));

        foreach (string bad in new[]
{
            "bad\0path",
            "\uD800"
}

        )
        {
            NGXFeatureDiscoveryInfo invalid = Discovery();
            invalid.FeatureInfo = new()
            {
                PathListInfo = new()
                {
                    Paths = ["first", bad]
                }
            };
            Throws<ArgumentException>(() => new NGXFeatureDiscoveryInfoNative(in invalid));
        }
    }

    private static void CheckFixedStrings()
    {
        NGXVkExtensionProperties value = new()
        {
            ExtensionName = "VK_测试🚀",
            SpecVersion = 123
        };
        NGXVkExtensionPropertiesNative native = new(in value);
        NGXVkExtensionProperties read = new(in native);
        Assert(read.ExtensionName == value.ExtensionName && read.SpecVersion is 123, "Fixed buffer round trip");
        new Span<byte>(native.ExtensionName, 256).Fill((byte)'x');
        read = new(in native);
        Assert(read.ExtensionName!.Length is 256, "Bounded missing terminator");
        value.ExtensionName = new string('x', 256);
        Throws<ArgumentException>(() => new NGXVkExtensionPropertiesNative(in value));
        native.Dispose();
        void* key = NGXMarshal.StringToPtr(Ngx.EParameterReserved00, NGXEncoding.Utf8);
        try
        {
            Assert(new ReadOnlySpan<byte>(key, 3).SequenceEqual(new byte[]
{
35,
0,
0
}), "SDK binary key");
        }
        finally
        {
            NGXMarshal.Free(key);
        }
    }

    private static NGXResourceVK Resource(int index)
    {
        return new()
        {
            Type = NGXResourceVKType.VkImageView,
            ReadWrite = index is 5,
            Resource = new()
            {
                ImageViewInfo = new()
                {
                    Image = 0x1200 + index,
                    ImageView = 0x3400 + index,
                    Width = 128,
                    Height = 64,
                    Format = NGXVkFormat.R16G16B16A16Sfloat,
                    SubresourceRange = new()
                    {
                        AspectMask = 1,
                        BaseMipLevel = 3,
                        LevelCount = 1,
                        BaseArrayLayer = 2,
                        LayerCount = 1
                    }
                }
            }
        };
    }

    private static NGXVKDLSSDEvalParams Frame()
    {
        return new()
        {
            PInDiffuseAlbedo = Resource(0),
            PInSpecularAlbedo = Resource(1),
            PInNormals = Resource(2),
            PInRoughness = Resource(3),
            PInColor = Resource(4),
            PInOutput = Resource(5),
            PInDepth = Resource(6),
            PInMotionVectors = Resource(7),
            PInExposureTexture = Resource(8),
            PInBiasCurrentColorMask = Resource(9),
            PInColorBeforeTransparency = Resource(10),
            PInScreenSpaceSubsurfaceScatteringGuide = Resource(11),
            PInDepthOfFieldGuide = Resource(12),
            PInSpecularHitDistance = Resource(13),
            PInMotionVectorsReflections = Resource(14),
            PInTransparencyLayer = Resource(15),
            PInTransparencyLayerOpacity = Resource(16),
            PInWorldToViewMatrix = Matrix4x4.CreateTranslation(3, 5, 7),
            PInViewToClipMatrix = Matrix4x4.Identity,
            InRenderSubrectDimensions = new()
            {
                Width = 128,
                Height = 64
            },
            InReset = 1
        };
    }

    private static void CheckEvaluation()
    {
        NGXVKDLSSDEvalParams value = Frame();
        NGXVKDLSSDEvalParamsNative native = new(in value);
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        NGXResourceVKNative*[] ports = [native.PInDiffuseAlbedo, native.PInSpecularAlbedo, native.PInNormals, native.PInRoughness, native.PInColor, native.PInOutput, native.PInDepth, native.PInMotionVectors, native.PInExposureTexture, native.PInBiasCurrentColorMask, native.PInColorBeforeTransparency, native.PInScreenSpaceSubsurfaceScatteringGuide, native.PInDepthOfFieldGuide, native.PInSpecularHitDistance, native.PInMotionVectorsReflections, native.PInTransparencyLayer, native.PInTransparencyLayerOpacity];
        for (int i = 0; i < ports.Length; i++)
        {
            Assert(ports[i]->Resource.ImageViewInfo.Image == 0x1200 + i, "Resource port mapping " + i);
        }

        Assert((bool)native.PInOutput->ReadWrite, "Native bool width/value");
        Assert(native.PInColor->Resource.ImageViewInfo.SubresourceRange.BaseMipLevel is 3 && native.PInColor->Resource.ImageViewInfo.SubresourceRange.BaseArrayLayer is 2, "Selected view");
        float* matrix = (float*)native.PInWorldToViewMatrix;
        Assert(matrix[12] == 3 && matrix[13] == 5 && matrix[14] == 7 && matrix[15] == 1, "Native float matrix order");
        NGXVKDLSSDEvalParams read = new(in native);
        native.Dispose();
        Assert(read.PInColor!.Value.Resource.ImageViewInfo!.Value.Image is 0x1204 && read.PInWorldToViewMatrix == value.PInWorldToViewMatrix, "Owned managed copy");
        value.PInExposureTexture = null;
        value.PInWorldToViewMatrix = default(Matrix4x4);
        value.PInViewToClipMatrix = null;
        native = new(in value);
        Assert(native.PInExposureTexture == null && native.PInWorldToViewMatrix != null && native.PInViewToClipMatrix == null, "Optional versus explicit zero");
        native.Dispose();
    }

    private static void CheckGBuffer()
    {
        NGXVKGBuffer value = new()
        {
            PInAttrib = [Resource(0), null, Resource(2)]
        };
        NGXVKGBufferNative native = new(in value);
        Assert(native.PInAttrib[0].Value != null && native.PInAttrib[1].Value == null && native.PInAttrib[16].Value == null, "Sparse array");
        NGXVKGBuffer restored = new(in native);
        Assert(restored.PInAttrib!.Length is 17 && restored.PInAttrib[2]!.Value.Resource.ImageViewInfo!.Value.Image is 0x1202, "Array conversion");
        native.Dispose();
        NGXCUDAGBuffer cuda = new()
        {
            PInAttrib = [42, null, 0]
        };
        NGXCUDAGBufferNative cudaNative = new(in cuda);
        Assert(*cudaNative.PInAttrib[0].Value is 42 && cudaNative.PInAttrib[1].Value == null && *cudaNative.PInAttrib[2].Value is 0, "CUDA scalar pointer presence");
        cudaNative.Dispose();
    }

    private static void Log(nint pointer)
    {
        ReadOnlySpan<byte> bytes = "日志🚀\0"u8;
        fixed (byte* text = bytes)
        {
            ((delegate* unmanaged[Cdecl]<byte*, NGXLoggingLevel, NGXFeature, void>)pointer)(text, NGXLoggingLevel.On, NGXFeature.SuperSampling);
        }
    }

    private static void CheckCallbacks()
    {
        int calls = 0;
        NGXLoggingInfo value = new()
        {
            LoggingCallback = (message, _, _) =>
            {
                Assert(message is "日志🚀", "Callback UTF-8");
                Interlocked.Increment(ref calls);
            }
        };
        NGXLoggingInfoNative native = new(in value);
        nint pointer = native.LoggingCallback;
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        Parallel.For(0, 64, _ => Log(pointer));
        Assert(calls is 64, "Concurrent callback roots");
        native.Dispose();
        Assert(Roots is 0, "Callback cleanup");
        NGXPfnProgressCallback progress = static (float _, ref bool cancel) =>
        {
            cancel = true;

            throw new InvalidOperationException("Expected callback exception");
        };
        pointer = NgxCallbacks.Acquire(progress);
        byte cancelled = 0;
        ((delegate* unmanaged[Cdecl]<float, byte*, void>)pointer)(0.5f, &cancelled);
        Assert(cancelled is 1, "One-byte ref bool and exception barrier");
        NgxCallbacks.Release(pointer);
        bool seen = false;
        NGXPfnParameterSetUI setter = (parameter, name, amount) => seen = parameter.Value is 123 && name is "Width" && amount is 456;
        pointer = Marshal.GetFunctionPointerForDelegate(setter);
        fixed (byte* name = "Width\0"u8)
        {
            ((delegate* unmanaged[Cdecl]<nint, byte*, uint, void>)pointer)(123, name, 456);
        }

        GC.KeepAlive(setter);
        Assert(seen, "Opaque callback handle ABI");
        Assert(Roots is 0, "All callback roots released");
    }

    private static NativeCall Storage()
    {
        NGXFeatureCommonInfo value = new()
        {
            PathListInfo = new()
            {
                Paths = ["/test"]
            },
            LoggingInfo = new()
            {
                LoggingCallback = static (_, _, _) =>
                {
                }
            }
        };
        NGXFeatureCommonInfoNative native = new(in value);
        NativeCall call = new();
        try
        {
            call.Take(ref native);

            return call;
        }
        catch
        {
            call.Dispose();

            throw;
        }
        finally
        {
            native.Dispose();
        }
    }

    private static void CheckInitialization()
    {
        NativeCall? call = Storage();
        NgxLifetime.BeginInitialization("test", 1, call);
        NgxLifetime.EndInitialization("test", 1, true, ref call);
        Assert(call is null && Roots is 1, "Init commit ownership");
        call = Storage();
        NgxLifetime.BeginInitialization("test", 2, call);
        NgxLifetime.EndInitialization("test", 2, false, ref call);
        call!.Dispose();
        Assert(Roots is 1, "Init rollback");
        NgxLifetime.Shutdown("test", 2);
        Assert(Roots is 1, "Different device retained");
        NgxLifetime.Shutdown("test", 1);
        Assert(Roots is 0, "Shutdown cleanup");
    }

    private static void CheckParameters()
    {
        for (int i = 0; i is < 3; i++)
        {
            NativeCall? call = Storage();
            NgxLifetime.BeginParameters(123, "eval", call);
            NgxLifetime.EndParameters(123, "eval", true, i is 2, ref call);
            Assert(call is null && Roots == (i is 2 ? 1 : i + 1), "Failure retention / success replacement");
        }

        NativeCall? cancelled = Storage();
        NgxLifetime.BeginParameters(123, "eval", cancelled);
        NgxLifetime.EndParameters(123, "eval", false, false, ref cancelled);
        cancelled!.Dispose();
        Assert(Roots is 1, "Pre-entry failure retains old data");
        NgxLifetime.ReleaseParameters(123);
        Assert(Roots is 0, "Reset cleanup");
    }

    private static void CheckCuda()
    {
        NGXCUDADevice device = new()
        {
            CudaContext = 123,
            CudaStream = 456
        };
        NGXCUDADeviceNative* first = NgxLifetime.CudaDevice(device);
        NGXCUDADevice copy = device;
        Assert(first == NgxLifetime.CudaDevice(copy), "Stable CUDA storage");
        NgxLifetime.FinishCudaDevice((nint)first, true);
        NgxLifetime.FinishCudaDevice((nint)first, false);
        Assert(first == NgxLifetime.CudaDevice(copy), "Later failure preserves initialized device");
        NgxLifetime.Shutdown("CUDA", (nint)first);
    }

    private static void CheckBackendCleanup()
    {
        NgxLifetime.PrepareParameters();
        NgxLifetime.RegisterParameters("test", 789);
        NativeCall? parameters = Storage();
        NgxLifetime.BeginParameters(789, "eval", parameters);
        NgxLifetime.EndParameters(789, "eval", true, true, ref parameters);

        foreach (nint device in new nint[]
{
            1,
            2
}

        )
        {
            NativeCall? call = Storage();
            NgxLifetime.BeginInitialization("test", device, call);
            NgxLifetime.EndInitialization("test", device, true, ref call);
        }

        Assert(Roots is 3, "Backend ownership setup");
        NgxLifetime.Shutdown("test", 1);
        Assert(Roots is 2, "Another device still uses backend data");
        NgxLifetime.Shutdown("test", 2);
        Assert(Roots is 0, "Last device cleanup");
    }

    private static void CheckFrameGenerationRetention()
    {
        foreach ((bool options, int expected) in new[]
{
            (true, 1),
            (false, 2),
            (false, 2),
            (true, 1)
}

        )
        {
            NativeCall? call = Storage();
            call.HasFrameGenerationOptions = options;
            NgxLifetime.BeginParameters(456, "fg", call);
            NgxLifetime.EndParameters(456, "fg", true, true, ref call);
            Assert(Roots == expected, "Conditional pointer writes discarded old options");
        }

        NgxLifetime.ReleaseParameters(456);
        Assert(Roots is 0, "FG pointer cleanup");
    }

    private static void CheckHandles()
    {
        NGXHandle handle = new(42);
        NGXHandle equalHandle = new(42);
        NGXParameter parameters = new(42);
        NGXParameter equalParameters = new(42);
        handle.Deconstruct(out nint handleValue);
        parameters.Deconstruct(out nint parameterValue);

        Assert(Marshal.SizeOf<NGXHandle>() == nint.Size && Marshal.SizeOf<NGXParameter>() == nint.Size, "Handle layout");
        Assert(typeof(NGXHandle).GetField(nameof(NGXHandle.Value))!.IsInitOnly && typeof(NGXParameter).GetField(nameof(NGXParameter.Value))!.IsInitOnly, "Readonly handle fields");
        Assert(handle == equalHandle && handle != default && handle.Equals((object)equalHandle), "Handle equality");
        Assert(parameters == equalParameters && parameters != default && parameters.Equals((object)equalParameters), "Parameter equality");
        Assert(handle.GetHashCode() == equalHandle.GetHashCode() && parameters.GetHashCode() == equalParameters.GetHashCode(), "Handle hash codes");
        Assert(handleValue is 42 && parameterValue is 42 && default(NGXHandle).IsNull && default(NGXParameter).IsNull, "Handle values");
        Assert(handle.ToString() is "NGXHandle { Value = 42, IsNull = False }", "Handle display");
        Assert(Enum.IsDefined(NGXDLSSGEvalFlags.None) && Enum.IsDefined(NGXFeatureSupportResult.None), "Empty flag values");
    }

    private static void CheckDefaults()
    {
        NGXDLSSGOptEvalParams value = new();
        NGXDLSSGOptEvalParamsNative native = new(in value);
        Assert(native.MultiFrameCount is 1 && native.MultiFrameIndex is 1 && native.MinRelativeLinearDepthObjectSeparation == 40, "SDK defaults");
        value = default;
        native = new(in value);
        Assert(native.MultiFrameCount is 0 && native.MinRelativeLinearDepthObjectSeparation == 0, "Explicit zeros");
        native.Dispose();
    }

    private static void RetainedFrame(NGXVKDLSSDEvalParams value)
    {
        NativeCall? call = new();
        NGXVKDLSSDEvalParamsNative native = new(in value);
        try
        {
            call.Take(ref native);
            NgxLifetime.BeginParameters(321, "measure", call);
            NgxLifetime.EndParameters(321, "measure", true, true, ref call);
        }
        finally
        {
            native.Dispose();
            call?.Dispose();
        }
    }

    private static void Measure()
    {
        NGXVKDLSSDEvalParams value = Frame();
        for (int i = 0; i is < 1000; i++)
        {
            RetainedFrame(value);
        }

        const int Iterations = 10000;
        long before = GC.GetAllocatedBytesForCurrentThread();

        for (int i = 0; i is < Iterations; i++)
        {
            RetainedFrame(value);
        }

        long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        NgxLifetime.ReleaseParameters(321);
        metrics["rrMeasuredCalls"] = Iterations;
        metrics["rrManagedBytesPerRetainedCall"] = (double)bytes / Iterations;
        metrics["rrManagedDescriptionSize"] = Unsafe.SizeOf<NGXVKDLSSDEvalParams>();
        metrics["rrPopulatedNativePointees"] = 19;
        metrics["rrAdditionalNativeRootAllocation"] = 1;
    }
}
