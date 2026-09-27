namespace Streamline.NET;

/// <summary>Application-facing Streamline functions and constants.</summary>
public static unsafe partial class SL
{
    /// <summary>
    /// Configures the absolute path of the Streamline interposer. The file is loaded
    /// on the first SDK call and retained until process exit. This does not change
    /// plugin search paths, initialize the SDK, or verify the file signature.
    /// </summary>
    /// <param name="libraryPath">Absolute path to the application-supplied library file.</param>
    /// <exception cref="ArgumentException">The path is empty or not absolute.</exception>
    /// <exception cref="InvalidOperationException">The library has already been loaded.</exception>
    public static void SetLibraryPath(string libraryPath)
    {
        StreamlineLibrary.SetPath(libraryPath);
    }
}
