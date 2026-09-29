using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;

namespace Streamline.NET;

internal sealed partial class RuntimeDownloader(HttpClient client)
{
    private const int BufferSize = 64 * 1024;

    private const int MaximumRedirects = 5;

    private static readonly SemaphoreSlim installationGate = new(1, 1);

    private static readonly string release = $"v{SL.VersionMajor}.{SL.VersionMinor}.{SL.VersionPatch}";

    private static readonly string assetName = $"streamline-sdk-{release}.zip";

    internal static RuntimeDownloader Shared { get; } = new(new HttpClient(new SocketsHttpHandler
    {
        AllowAutoRedirect = false
    }));

    internal async Task EnsureAsync(string directory, RuntimeOptions options, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        if (!Path.IsPathFullyQualified(directory))
        {
            throw new ArgumentException("The runtime destination must be an absolute directory path.", nameof(directory));
        }

        string destination = Path.GetFullPath(directory);

        if (File.Exists(destination))
        {
            throw new ArgumentException("The runtime destination must be a directory, not a file.", nameof(directory));
        }

        string[] required = GetRequiredFiles(options);
        await installationGate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            List<string> missing = GetMissingFiles(destination, required, cancellationToken);

            if (missing.Count == 0)
            {
                return;
            }

            StreamlineLibrary.CheckRuntimeUpdate();
            string workspace = CreateWorkspace(destination);
            bool preserveRecovery = false;
            Exception? operationFailure = null;

            try
            {
                Asset asset = await GetAssetAsync(cancellationToken).ConfigureAwait(false);
                string archive = Path.Combine(workspace, "runtime.zip");

                using (HttpResponseMessage response = await GetAsync(asset.Url, cancellationToken).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();

                    if (response.Content.Headers.ContentLength is long length && length != asset.Size)
                    {
                        throw new InvalidDataException("The Streamline archive length does not match the official release metadata.");
                    }

                    using Stream source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                    string hash = await CopyAndHashAsync(source, archive, asset.Size, cancellationToken).ConfigureAwait(false);

                    if (hash != asset.Sha256)
                    {
                        throw new InvalidDataException("The Streamline archive SHA256 does not match the official release metadata.");
                    }
                }

                string staging = Path.Combine(workspace, "staging");
                Directory.CreateDirectory(staging);
                List<string> files = await ExtractAsync(archive, staging, missing, cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();

                StreamlineLibrary.PublishRuntime(() => Publish(destination, staging, workspace, required, files,
                    cancellationToken, ref preserveRecovery));
            }
            catch (Exception exception)
            {
                operationFailure = exception;

                throw;
            }
            finally
            {
                if (!preserveRecovery)
                {
                    try
                    {
                        Directory.Delete(workspace, recursive: true);
                    }
                    catch (Exception cleanupFailure) when (cleanupFailure is IOException or UnauthorizedAccessException)
                    {
                        if (operationFailure is null)
                        {
                            throw new IOException($"The Streamline runtime was installed, but temporary files could not be removed from '{workspace}'.", cleanupFailure);
                        }

                        // Preserve the original failure, especially its cancellation semantics.
                        operationFailure.Data["StreamlineRuntimeRecoveryDirectory"] = workspace;
                        operationFailure.Data["StreamlineRuntimeCleanupError"] = cleanupFailure;
                    }
                }
            }
        }
        finally
        {
            installationGate.Release();
        }
    }

    private async Task<Asset> GetAssetAsync(CancellationToken cancellationToken)
    {
        Uri metadata = new($"https://api.github.com/repos/NVIDIA-RTX/Streamline/releases/tags/{release}");
        using HttpResponseMessage response = await GetAsync(metadata, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        using Stream source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using JsonDocument document = await JsonDocument.ParseAsync(source, cancellationToken: cancellationToken).ConfigureAwait(false);
        JsonElement root = document.RootElement;

        if (root.GetProperty("tag_name").GetString() != release)
        {
            throw new InvalidDataException("The official Streamline release does not match this binding version.");
        }

        Asset? selected = null;

        foreach (JsonElement item in root.GetProperty("assets").EnumerateArray())
        {
            if (item.GetProperty("name").GetString() != assetName)
            {
                continue;
            }

            string expectedUrl = $"https://github.com/NVIDIA-RTX/Streamline/releases/download/{release}/{assetName}";
            string? digest = item.GetProperty("digest").GetString();
            long size = item.GetProperty("size").GetInt64();

            if (selected is not null || item.GetProperty("browser_download_url").GetString() != expectedUrl
                || size <= 0 || digest is null || !digest.StartsWith("sha256:", StringComparison.Ordinal)
                || !IsSha256(digest[7..]))
            {
                throw new InvalidDataException("The official Streamline archive is missing valid download metadata.");
            }

            selected = new(new(expectedUrl), size, digest[7..].ToLowerInvariant());
        }

        return selected ?? throw new InvalidDataException($"The official Streamline release does not contain {assetName}.");
    }

    private async Task<HttpResponseMessage> GetAsync(Uri url, CancellationToken cancellationToken)
    {
        for (int redirects = 0; redirects <= MaximumRedirects; redirects++)
        {
            if (url.Scheme != Uri.UriSchemeHttps || !url.IsDefaultPort || !string.IsNullOrEmpty(url.UserInfo)
                || url.Host is not ("api.github.com" or "github.com" or "release-assets.githubusercontent.com" or "objects.githubusercontent.com"))
            {
                throw new InvalidDataException("Streamline downloads must remain on official GitHub release endpoints.");
            }

            using HttpRequestMessage request = new(HttpMethod.Get, url);
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("Streamline.NET", release[1..]));
            HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode is not (HttpStatusCode.MovedPermanently or HttpStatusCode.Redirect
                or HttpStatusCode.SeeOther or HttpStatusCode.TemporaryRedirect or HttpStatusCode.PermanentRedirect))
            {
                return response;
            }

            Uri? location = response.Headers.Location;
            response.Dispose();

            if (location is null)
            {
                throw new InvalidDataException("The Streamline download redirect has no destination.");
            }

            url = location.IsAbsoluteUri ? location : new(url, location);
        }

        throw new InvalidDataException("The Streamline download exceeded the redirect limit.");
    }

    private static async Task<string> CopyAndHashAsync(Stream source, string destination, long expectedLength, CancellationToken cancellationToken)
    {
        using FileStream output = new(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None, BufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        byte[] buffer = new byte[BufferSize];
        long total = 0;
        int count;

        while ((count = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) != 0)
        {
            if (count > expectedLength - total)
            {
                throw new InvalidDataException("A Streamline download entry exceeds its declared size.");
            }

            hash.AppendData(buffer, 0, count);
            await output.WriteAsync(buffer.AsMemory(0, count), cancellationToken).ConfigureAwait(false);
            total += count;
        }

        if (total != expectedLength)
        {
            throw new InvalidDataException("A Streamline download entry is shorter than its declared size.");
        }

        await output.FlushAsync(cancellationToken).ConfigureAwait(false);

        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }

    private static bool IsSha256(string hash)
    {
        return hash.Length == SHA256.HashSizeInBytes * 2 && hash.All(char.IsAsciiHexDigit);
    }

    private sealed record Asset(Uri Url, long Size, string Sha256);
}
