using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace Streamline.NET;

internal sealed partial class RuntimeDownloader
{
    private static readonly string[] requiredFiles =
    [
        "NvLowLatencyVk.dll",
        "nvngx_deepdvc.dll",
        "nvngx_dlss.dll",
        "nvngx_dlssd.dll",
        "nvngx_dlssg.dll",
        "sl.common.dll",
        "sl.deepdvc.dll",
        "sl.directsr.dll",
        "sl.dlss_d.dll",
        "sl.dlss_g.dll",
        "sl.dlss.dll",
        "sl.interposer.dll",
        "sl.nis.dll",
        "sl.nvperf.dll",
        "sl.pcl.dll",
        "sl.reflex.dll",
        "nis.license.txt",
        "nvngx_dlss.license.txt",
        "reflex.license.txt",
        "Licenses/external/json/LICENSE.MIT",
        "Licenses/external/ngx-sdk/license.txt",
        "Licenses/external/nsight-sdk/SystemsGraphics/LICENSE.txt",
        "Licenses/external/reflex-sdk-vk/reflex.license.txt",
        "Licenses/3rd-party-licenses.md",
        "Licenses/license.txt",
        "Licenses/NVIDIA Nsight Graphics SDK License (Apache 2.0).txt",
        "Licenses/NVIDIA Nsight Perf SDK License (28Sept2022).pdf"
    ];

    private static async Task<List<RuntimeFile>> ExtractAsync(string archivePath, string staging, CancellationToken cancellationToken)
    {
        using ZipArchive archive = ZipFile.OpenRead(archivePath);
        HashSet<string> paths = new(StringComparer.OrdinalIgnoreCase);
        List<RuntimeFile> files = [];

        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string path = NormalizeRelativePath(entry.FullName.TrimEnd('/', '\\'));

            if (entry.FullName.EndsWith('/') || entry.FullName.EndsWith('\\'))
            {
                continue;
            }

            string? relative = SelectFile(path);

            if (relative is null)
            {
                continue;
            }

            // Unix symbolic links have file type 0120000 in the upper attribute word.
            if ((entry.ExternalAttributes >> 16 & 0xF000) == 0xA000 || !paths.Add(relative))
            {
                throw new InvalidDataException("The Streamline archive contains a link or duplicate installation path: " + relative);
            }

            string destination = Path.Combine(staging, relative);
            CheckPath(staging, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            using Stream source = entry.Open();
            string hash = await CopyAndHashAsync(source, destination, entry.Length, cancellationToken).ConfigureAwait(false);
            files.Add(new(relative, entry.Length, hash));
        }

        ValidateFileSet(files);

        return [.. files.OrderBy(file => file.Path, StringComparer.Ordinal)];
    }

    private static string? SelectFile(string path)
    {
        const string runtimePrefix = "bin/x64/";

        if (path.StartsWith(runtimePrefix, StringComparison.Ordinal))
        {
            string name = path[runtimePrefix.Length..];

            return !name.Contains('/') && (name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) || IsLicenseFile(name))
                ? name : null;
        }

        string[] segments = path.Split('/');

        if (segments.Any(segment => segment.Equals("bin", StringComparison.OrdinalIgnoreCase)
            || segment.Equals("development", StringComparison.OrdinalIgnoreCase)
            || segment.Equals("debug", StringComparison.OrdinalIgnoreCase)))
        {
            return null;
        }

        return IsLicenseFile(segments[^1]) ? "Licenses/" + path : null;
    }

    private static bool IsLicenseFile(string name)
    {
        string extension = Path.GetExtension(name);

        return (name.Contains("license", StringComparison.OrdinalIgnoreCase) || name.Contains("notice", StringComparison.OrdinalIgnoreCase))
            && (extension.Length == 0 || extension.Equals(".txt", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".md", StringComparison.OrdinalIgnoreCase) || extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".mit", StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeRelativePath(string path)
    {
        path = path.Replace('\\', '/');
        string[] segments = path.Split('/');

        foreach (string segment in segments)
        {
            string stem = segment.Split('.')[0];
            bool reserved = stem.Equals("CON", StringComparison.OrdinalIgnoreCase) || stem.Equals("PRN", StringComparison.OrdinalIgnoreCase)
                || stem.Equals("AUX", StringComparison.OrdinalIgnoreCase) || stem.Equals("NUL", StringComparison.OrdinalIgnoreCase)
                || (stem.Length == 4 && (stem.StartsWith("COM", StringComparison.OrdinalIgnoreCase) || stem.StartsWith("LPT", StringComparison.OrdinalIgnoreCase))
                    && stem[3] is >= '1' and <= '9');

            if (segment.Length == 0 || segment is "." or ".." || segment.EndsWith('.') || segment.EndsWith(' ') || reserved
                || segment.Any(character => char.IsControl(character) || "<>:\"|?*".Contains(character)))
            {
                throw new InvalidDataException("Invalid Streamline installation path: " + path);
            }
        }

        return path;
    }

    private static void ValidateFileSet(List<RuntimeFile> files, bool requireCurrentFiles = true)
    {
        HashSet<string> paths = new(StringComparer.OrdinalIgnoreCase);

        foreach (RuntimeFile file in files)
        {
            string path = NormalizeRelativePath(file.Path);
            string name = path[(path.LastIndexOf('/') + 1)..];
            bool managed = path.StartsWith("Licenses/", StringComparison.Ordinal) ? IsLicenseFile(name)
                : !path.Contains('/') && (name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) || IsLicenseFile(name));

            if (!managed || path != file.Path || !paths.Add(path) || file.Size < 0 || !IsSha256(file.Sha256))
            {
                throw new InvalidDataException("Invalid Streamline runtime manifest entry: " + file.Path);
            }
        }

        if (!paths.Contains("sl.interposer.dll") || !paths.Contains("sl.common.dll")
            || !files.Any(file => file.Path.StartsWith("Licenses/", StringComparison.Ordinal)))
        {
            throw new InvalidDataException("The Streamline runtime is missing its interposer, common plugin, or accompanying licenses.");
        }

        if (requireCurrentFiles)
        {
            foreach (string path in requiredFiles)
            {
                if (!paths.Contains(path))
                {
                    throw new InvalidDataException("The Streamline runtime is missing a required file: " + path);
                }
            }
        }
    }

    private static void CheckPath(string directory, string relativePath)
    {
        string path = directory;

        for (string? parent = directory; parent is not null; parent = Path.GetDirectoryName(parent))
        {
            CheckLink(parent);
        }

        foreach (string segment in NormalizeRelativePath(relativePath).Split('/'))
        {
            path = Path.Combine(path, segment);
            CheckLink(path);
        }
    }

    private static void CheckLink(string path)
    {
        try
        {
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            {
                throw new IOException("Streamline runtime installation paths cannot be symbolic links or reparse points: " + path);
            }
        }
        catch (FileNotFoundException)
        {
            // A new installation path has no existing node to validate.
        }
        catch (DirectoryNotFoundException)
        {
            // A new installation path has no existing parent to validate.
        }
    }

    private static async Task<Installation?> ReadInstallationAsync(string directory, CancellationToken cancellationToken)
    {
        string path = Path.Combine(directory, ManifestName);

        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            using FileStream source = new(path, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
            using JsonDocument document = await JsonDocument.ParseAsync(source, cancellationToken: cancellationToken).ConfigureAwait(false);
            JsonElement root = document.RootElement;

            if (root.GetProperty("schema").GetInt32() != 1)
            {
                return null;
            }

            string version = root.GetProperty("release").GetString() ?? "";
            string asset = root.GetProperty("asset").GetString() ?? "";
            string digest = root.GetProperty("archiveSha256").GetString() ?? "";

            if (!version.StartsWith('v') || !Version.TryParse(version[1..], out _) || asset != $"streamline-sdk-{version}.zip" || !IsSha256(digest))
            {
                return null;
            }

            List<RuntimeFile> files = [];

            foreach (JsonElement file in root.GetProperty("files").EnumerateArray())
            {
                files.Add(new(file.GetProperty("path").GetString() ?? "", file.GetProperty("size").GetInt64(), file.GetProperty("sha256").GetString() ?? ""));
            }

            ValidateFileSet(files, requireCurrentFiles: version == release);

            return new(version, asset, digest, files);
        }
        catch (Exception exception) when (exception is JsonException or InvalidDataException or KeyNotFoundException or InvalidOperationException or FormatException or OverflowException)
        {
            // An absent or damaged manifest is repaired from the official archive.
            return null;
        }
    }

    private static async Task<bool> IsCompleteAsync(string directory, Installation installation, CancellationToken cancellationToken)
    {
        foreach (RuntimeFile file in installation.Files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CheckPath(directory, file.Path);
            string path = Path.Combine(directory, file.Path);

            if (!File.Exists(path) || new FileInfo(path).Length != file.Size)
            {
                return false;
            }

            using FileStream source = new(path, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
            byte[] hash = await SHA256.HashDataAsync(source, cancellationToken).ConfigureAwait(false);

            if (!Convert.ToHexStringLower(hash).Equals(file.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    private static async Task WriteInstallationAsync(string directory, Installation installation, CancellationToken cancellationToken)
    {
        using FileStream output = new(Path.Combine(directory, ManifestName), FileMode.CreateNew, FileAccess.Write, FileShare.None, BufferSize, FileOptions.Asynchronous);
        using Utf8JsonWriter writer = new(output);
        writer.WriteStartObject();
        writer.WriteNumber("schema", 1);
        writer.WriteString("release", installation.Release);
        writer.WriteString("asset", installation.Asset);
        writer.WriteString("archiveSha256", installation.ArchiveSha256);
        writer.WriteStartArray("files");

        foreach (RuntimeFile file in installation.Files)
        {
            writer.WriteStartObject();
            writer.WriteString("path", file.Path);
            writer.WriteNumber("size", file.Size);
            writer.WriteString("sha256", file.Sha256);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        await output.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
}
