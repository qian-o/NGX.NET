using System.Reflection;
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
        NativeLibrary.SetDllImportResolver(typeof(Ngx).Assembly, Resolve);
    }

    /// <summary>
    /// Directory containing the bridge and packaged NVIDIA feature libraries.
    /// Pass this directory in NGXFeatureCommonInfo when initializing NGX.
    /// </summary>
    public static string RuntimeDirectory
    {
        get
        {
            string os;

            if (OperatingSystem.IsWindows())
            {
                os = "win";
            }
            else if (OperatingSystem.IsLinux())
            {
                os = "linux";
            }
            else
            {
                throw new PlatformNotSupportedException("NGX supports Windows and Linux.");
            }

            string arch = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 => "x64",
                Architecture.Arm64 => "arm64",
                _ => throw new PlatformNotSupportedException("NGX supports x64 and arm64.")
            };
            string directory = Path.Combine(AppContext.BaseDirectory, "runtimes", $"{os}-{arch}", "native");

            return Directory.Exists(directory) ? directory : AppContext.BaseDirectory;
        }
    }

    /// <summary>
    /// Applies the official NVSDK_NGX_SUCCEED macro. NGX success is not zero.
    /// </summary>
    public static bool Succeeded(NGXResult result)
    {
        return ((uint)result & 0xFFF00000u) is not (uint)NGXResult.Fail;
    }

    /// <summary>
    /// Applies the official NVSDK_NGX_FAILED macro.
    /// </summary>
    public static bool Failed(NGXResult result)
    {
        return !Succeeded(result);
    }

    /// <summary>
    /// Throws an NGXException only when the official failure predicate is true.
    /// </summary>
    public static void ThrowIfFailed(NGXResult result, [CallerArgumentExpression(nameof(result))] string? operation = null)
    {
        if (Failed(result))
        {
            throw new NGXException(result, operation);
        }
    }

    /// <summary>
    /// Counts elements, equivalent to NVSDK_NGX_ARRAY_LEN for a managed span.
    /// </summary>
    public static nuint ArrayLength<T>(ReadOnlySpan<T> values)
    {
        return (nuint)values.Length;
    }

    private static nint Resolve(string name, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (name is not LibraryName)
        {
            return 0;
        }

        string file = OperatingSystem.IsWindows() ? "ngx-bridge.dll" : "libngx-bridge.so";

        return NativeLibrary.Load(Path.Combine(RuntimeDirectory, file), assembly, searchPath);
    }

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
