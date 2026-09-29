namespace Streamline.NET;

/// <summary>
/// Application-facing Streamline functions and constants.
/// </summary>
public static unsafe partial class SL
{
    /// <summary>
    /// Configures the absolute directory containing the Streamline runtime libraries.
    /// The interposer is loaded on the first SDK call and retained until process exit.
    /// </summary>
    /// <param name="libraryPath">Absolute path to the directory containing the application-supplied libraries.</param>
    /// <exception cref="ArgumentException">The path is empty or not absolute.</exception>
    /// <exception cref="InvalidOperationException">The library has already been loaded.</exception>
    public static void SetLibraryPath(string libraryPath)
    {
        StreamlineLibrary.SetPath(libraryPath);
    }
}
