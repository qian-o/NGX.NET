namespace Streamline.NET;

public static unsafe partial class SL
{
    /// <summary>
    /// Downloads the matching Windows x64 runtime from NVIDIA's official Streamline
    /// release into the specified directory, preserving the downloaded files' licenses.
    /// An intact matching installation is reused without network access.
    /// </summary>
    /// <remarks>
    /// For commercial products, use binaries authorized by NVIDIA for your application.
    /// </remarks>
    /// <param name="directory">Absolute path to the directory in which to install the native runtime.</param>
    public static void DownloadRuntime(string directory)
    {
        RuntimeDownloader.Shared.DownloadAsync(directory, CancellationToken.None).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Downloads the matching Windows x64 runtime from NVIDIA's official Streamline
    /// release into the specified directory, preserving the downloaded files' licenses.
    /// An intact matching installation is reused without network access.
    /// </summary>
    /// <remarks>
    /// For commercial products, use binaries authorized by NVIDIA for your application.
    /// </remarks>
    /// <param name="directory">Absolute path to the directory in which to install the native runtime.</param>
    /// <param name="cancellationToken">Cancels the download or installation.</param>
    public static Task DownloadRuntimeAsync(string directory, CancellationToken cancellationToken = default)
    {
        return RuntimeDownloader.Shared.DownloadAsync(directory, cancellationToken);
    }
}
