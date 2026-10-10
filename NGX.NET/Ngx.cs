using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NGX.NET;

/// <summary>
/// Direct access to NVIDIA NGX. Serialize SDK calls and keep GPU resources alive
/// until their submitted work completes. Feature handles and parameter maps must
/// be released with the matching backend's ReleaseFeature and DestroyParameters.
/// </summary>
public static unsafe partial class Ngx
{
    internal const string LibraryName = "ngx-bridge";

    static Ngx()
    {
        string os;
        string file;
        if (OperatingSystem.IsWindows())
        {
            os = "win";
            file = "ngx-bridge.dll";
        }
        else if (OperatingSystem.IsLinux())
        {
            os = "linux";
            file = "libngx-bridge.so";
        }
        else
        {
            throw new PlatformNotSupportedException("NGX supports Windows and Linux.");
        }

        string architecture = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.Arm64 => "arm64",
            _ => throw new PlatformNotSupportedException("NGX supports x64 and arm64.")
        };
        string directory = Path.Combine(AppContext.BaseDirectory, "runtimes", $"{os}-{architecture}", "native");
        RuntimeDirectory = Directory.Exists(directory) ? directory : AppContext.BaseDirectory;
        nint library = Load(Path.Combine(directory, file), Path.Combine(AppContext.BaseDirectory, file), file);
        NativeLibrary.SetDllImportResolver(typeof(Ngx).Assembly, (name, _, _) => name is LibraryName ? library : 0);

        static nint Load(params string[] paths)
        {
            foreach (string path in paths)
            {
                if (NativeLibrary.TryLoad(path, out nint handle))
                {
                    return handle;
                }
            }

            return 0;
        }
    }

    /// <summary>
    /// Directory containing the packaged NVIDIA feature libraries.
    /// Pass this directory in NGXFeatureCommonInfo when initializing NGX.
    /// </summary>
    public static string RuntimeDirectory { get; }

    public static partial class Parameter
    {
        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_Parameter_Reset")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial void ResetNative(nint parameters);

        /// <summary>
        /// Resets a parameter map through the SDK's C++ Reset member.
        /// </summary>
        public static void Reset(NGXParameter parameters)
        {
            if (parameters.IsNull)
            {
                throw new ArgumentException("A non-null NGX parameter handle is required.", nameof(parameters));
            }

            ResetNative(parameters.Value);
            NgxLifetime.ReleaseParameters(parameters.Value);
        }
    }
}
