using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NGX.NET;

/// <summary>
/// Direct access to NVIDIA NGX. Serialize SDK calls and keep GPU resources alive
/// until their submitted work completes. Feature handles and parameter maps must
/// be released with the matching backend's ReleaseFeature and DestroyParameters.
/// </summary>
public static unsafe partial class NGX
{
    internal const string LibraryName = "ngx-bridge";

    /// <summary>
    /// Directory containing the bridge and packaged NVIDIA feature libraries.
    /// Pass this directory in FeatureCommonInfo when initializing NGX.
    /// </summary>
    public static string RuntimeDirectory
    {
        get
        {
            string os = OperatingSystem.IsWindows() ? "win" : OperatingSystem.IsLinux() ? "linux" : throw new PlatformNotSupportedException("NGX supports Windows and Linux.");
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

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2255", Justification = "Registers the native bridge resolver before generated P/Invoke calls.")]
    [ModuleInitializer]
    internal static void InitializeResolver()
    {
        NativeLibrary.SetDllImportResolver(typeof(NGX).Assembly, Resolve);
    }

    private static nint Resolve(string name, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (name != LibraryName)
        {
            return 0;
        }

        string file = OperatingSystem.IsWindows() ? "ngx-bridge.dll" : "libngx-bridge.so";

        return NativeLibrary.Load(Path.Combine(RuntimeDirectory, file), assembly, searchPath);
    }

    /// <summary>
    /// Applies the official NVSDK_NGX_SUCCEED macro. NGX success is not zero.
    /// </summary>
    public static bool Succeeded(Result result) => ((uint)result & 0xFFF00000u) != (uint)Result.Fail;

    /// <summary>
    /// Applies the official NVSDK_NGX_FAILED macro.
    /// </summary>
    public static bool Failed(Result result) => !Succeeded(result);

    /// <summary>
    /// Throws an NGXException only when the official failure predicate is true.
    /// </summary>
    public static void ThrowIfFailed(Result result, [CallerArgumentExpression(nameof(result))] string? operation = null)
    {
        if (Failed(result))
        {
            throw new NGXException(result, operation);
        }
    }

    /// <summary>
    /// Counts elements, equivalent to NVSDK_NGX_ARRAY_LEN for a managed span.
    /// </summary>
    public static nuint ArrayLength<T>(ReadOnlySpan<T> values) => (nuint)values.Length;

    public static partial class Parameter
    {
        /// <summary>
        /// Resets a parameter map through the SDK's C++ Reset member.
        /// </summary>
        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_Parameter_Reset")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void Reset(global::NGX.NET.Parameter* parameters);
    }
}
