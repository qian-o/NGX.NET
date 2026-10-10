using System.Reflection;

namespace NGX.NET;

internal static class NativeLoader
{
    private const int Unregistered = 0;
    private const int Registering = 1;
    private const int Registered = 2;

    private static readonly string? rid = (OperatingSystem.IsWindows(), OperatingSystem.IsLinux(), RuntimeInformation.ProcessArchitecture) switch
    {
        (true, _, Architecture.X64) => "win-x64",
        (true, _, Architecture.Arm64) => "win-arm64",
        (_, true, Architecture.X64) => "linux-x64",
        (_, true, Architecture.Arm64) => "linux-arm64",
        _ => null
    };

    private static readonly string? file = true switch
    {
        _ when OperatingSystem.IsWindows() => "ngx-bridge.dll",
        _ when OperatingSystem.IsLinux() => "libngx-bridge.so",
        _ => null
    };

    private static int registrationState;

    internal static string RuntimeDirectory { get; } = GetRuntimeDirectory();

    internal static void Register()
    {
        if (Interlocked.CompareExchange(ref registrationState, Registering, Unregistered) is Unregistered)
        {
            try
            {
                NativeLibrary.SetDllImportResolver(typeof(Ngx).Assembly, Resolve);
            }
            catch (InvalidOperationException)
            {
                // The application has already registered an assembly resolver.
            }

            Volatile.Write(ref registrationState, Registered);

            return;
        }

        SpinWait wait = new();
        while (Volatile.Read(ref registrationState) is not Registered)
        {
            wait.SpinOnce();
        }
    }

    private static string GetRuntimeDirectory()
    {
        if (rid is null)
        {
            return AppContext.BaseDirectory;
        }

        string directory = Path.Combine(AppContext.BaseDirectory, "runtimes", rid, "native");

        return Directory.Exists(directory) ? directory : AppContext.BaseDirectory;
    }

    private static nint Resolve(string name, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (name is not Ngx.LibraryName)
        {
            return 0;
        }

        if (file is null)
        {
            throw new PlatformNotSupportedException("NGX supports Windows and Linux.");
        }

        if (rid is null)
        {
            throw new PlatformNotSupportedException("NGX supports x64 and arm64.");
        }

        return Load(Path.Combine(AppContext.BaseDirectory, "runtimes", rid, "native", file), Path.Combine(AppContext.BaseDirectory, file), file);
    }

    private static nint Load(params ReadOnlySpan<string> paths)
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
