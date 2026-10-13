using System.Reflection;

namespace NGX.NET;

internal static class NativeLoader
{
    private static readonly Lock @lock = new();
    private static readonly string? Rid = (OperatingSystem.IsWindows(), OperatingSystem.IsLinux(), RuntimeInformation.ProcessArchitecture) switch
    {
        (true, _, Architecture.X64) => "win-x64",
        (true, _, Architecture.Arm64) => "win-arm64",
        (_, true, Architecture.X64) => "linux-x64",
        (_, true, Architecture.Arm64) => "linux-arm64",
        _ => null
    };

    private static readonly string? FileName = (OperatingSystem.IsWindows(), OperatingSystem.IsLinux()) switch
    {
        (true, _) => "ngx-bridge.dll",
        (_, true) => "libngx-bridge.so",
        _ => null
    };

    private static bool isRegistered;

    internal static string RuntimeDirectory { get; } = GetRuntimeDirectory();

    internal static void Register()
    {
        using Lock.Scope _ = @lock.EnterScope();

        if (isRegistered)
        {
            return;
        }

        try
        {
            NativeLibrary.SetDllImportResolver(typeof(Ngx).Assembly, Resolve);
        }
        catch (InvalidOperationException)
        {
            // The application has already registered an assembly resolver.
        }

        isRegistered = true;
    }

    private static string GetRuntimeDirectory()
    {
        if (Rid is null)
        {
            return AppContext.BaseDirectory;
        }

        string directory = Path.Combine(AppContext.BaseDirectory, "runtimes", Rid, "native");

        return Directory.Exists(directory) ? directory : AppContext.BaseDirectory;
    }

    private static nint Resolve(string name, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (name is not Ngx.LibraryName)
        {
            return 0;
        }

        if (FileName is null)
        {
            throw new PlatformNotSupportedException("NGX supports Windows and Linux.");
        }

        if (Rid is null)
        {
            throw new PlatformNotSupportedException("NGX supports x64 and arm64.");
        }

        return Load(Path.Combine(AppContext.BaseDirectory, "runtimes", Rid, "native", FileName), Path.Combine(AppContext.BaseDirectory, FileName), FileName);
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
