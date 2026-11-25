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
        if (Rid is null || FileName is null || File.Exists(Path.Combine(AppContext.BaseDirectory, FileName)))
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

        if (RuntimeDirectory == AppContext.BaseDirectory)
        {
            return Load(assembly, searchPath, Path.Combine(AppContext.BaseDirectory, FileName), FileName);
        }

        return Load(assembly, searchPath, Path.Combine(RuntimeDirectory, FileName), Path.Combine(AppContext.BaseDirectory, FileName), FileName);
    }

    private static nint Load(Assembly assembly, DllImportSearchPath? searchPath, params ReadOnlySpan<string> paths)
    {
        foreach (string path in paths)
        {
            if (Path.IsPathRooted(path))
            {
                if (File.Exists(path) && NativeLibrary.TryLoad(path, out nint handle))
                {
                    return handle;
                }
            }
            else if (NativeLibrary.TryLoad(path, assembly, searchPath, out nint handle))
            {
                return handle;
            }
        }

        return 0;
    }
}
