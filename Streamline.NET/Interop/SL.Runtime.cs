namespace Streamline.NET;

public static unsafe partial class SL
{
    /// <summary>
    /// Ensures the selected Windows x64 runtime files exist, downloading missing files
    /// from NVIDIA's official Streamline release. Existing files are preserved.
    /// </summary>
    /// <remarks>
    /// This method does not initialize Streamline or configure its library path.
    /// Production binaries are preferred; development binaries are used when those are all the SDK provides.
    /// For commercial products, use binaries authorized by NVIDIA for your application.
    /// </remarks>
    /// <param name="directory">Absolute path to the native runtime directory.</param>
    /// <param name="options">Features to prepare.</param>
    public static void EnsureRuntime(string directory, RuntimeOptions options)
    {
        RuntimeDownloader.Shared.EnsureAsync(directory, options, CancellationToken.None).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Ensures the selected Windows x64 runtime files exist, downloading missing files
    /// from NVIDIA's official Streamline release. Existing files are preserved.
    /// </summary>
    /// <remarks>
    /// This method does not initialize Streamline or configure its library path.
    /// Production binaries are preferred; development binaries are used when those are all the SDK provides.
    /// For commercial products, use binaries authorized by NVIDIA for your application.
    /// </remarks>
    /// <param name="directory">Absolute path to the native runtime directory.</param>
    /// <param name="options">Features to prepare.</param>
    /// <param name="cancellationToken">Cancels the download or installation.</param>
    public static Task EnsureRuntimeAsync(string directory, RuntimeOptions options, CancellationToken cancellationToken = default)
    {
        return RuntimeDownloader.Shared.EnsureAsync(directory, options, cancellationToken);
    }
}
