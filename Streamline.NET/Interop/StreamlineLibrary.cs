using System.Reflection;
using System.Runtime.InteropServices;

namespace Streamline.NET;

internal static class StreamlineLibrary
{
    internal const string ImportName = "Streamline.NET.Native";

    private const string InterposerName = "sl.interposer.dll";

    private static readonly object sync = new();

    private static string? libraryDirectory;

    private static nint module;

    static StreamlineLibrary()
    {
        NativeLibrary.SetDllImportResolver(typeof(StreamlineLibrary).Assembly, Resolve);
    }

    internal static void Register()
    {
        // Entering this type registers the resolver without loading the SDK.
    }

    internal static void SetPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!Path.IsPathFullyQualified(path))
        {
            throw new ArgumentException("The Streamline library path must be an absolute directory path.", nameof(path));
        }

        lock (sync)
        {
            if (module != 0)
            {
                throw new InvalidOperationException("The Streamline library has already been loaded.");
            }

            libraryDirectory = path;
        }
    }

    internal static nint GetExport(string name)
    {
        return NativeLibrary.GetExport(Load(), name);
    }

    private static nint Resolve(string name, Assembly assembly, DllImportSearchPath? searchPath)
    {
        return name == ImportName ? Load() : 0;
    }

    private static nint Load()
    {
        lock (sync)
        {
            if (module != 0)
            {
                return module;
            }

            if (libraryDirectory is null)
            {
                throw new InvalidOperationException("Call SL.SetLibraryPath before invoking the Streamline SDK.");
            }

            module = NativeLibrary.Load(Path.Combine(libraryDirectory, InterposerName));

            return module;
        }
    }
}
