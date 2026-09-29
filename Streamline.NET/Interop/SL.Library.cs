namespace Streamline.NET;

/// <summary>
/// Application-facing Streamline functions and constants.
/// </summary>
public static unsafe partial class SL
{
    /// <summary>
    /// Configures the absolute directory containing the Streamline runtime libraries.
    /// The directory must contain sl.interposer.dll.
    /// The interposer is loaded on the first SDK call and retained until process exit.
    /// This does not change plugin search paths, initialize the SDK, or verify the file signature.
    /// </summary>
    /// <param name="libraryPath">Absolute path to the directory containing the application-supplied libraries.</param>
    /// <exception cref="ArgumentException">The path is empty, not absolute, or refers to an existing file.</exception>
    /// <exception cref="DllNotFoundException">The directory does not contain sl.interposer.dll.</exception>
    /// <exception cref="InvalidOperationException">The library has already been loaded.</exception>
    public static void SetLibraryPath(string libraryPath)
    {
        StreamlineLibrary.SetPath(libraryPath);
    }
}
