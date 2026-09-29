using System.IO.Compression;

namespace Streamline.NET;

internal sealed partial class RuntimeDownloader
{
    private static string[] GetRequiredFiles(RuntimeOptions options)
    {
        if (options.RenderAPI is not (RenderAPI.D3D11 or RenderAPI.D3D12 or RenderAPI.Vulkan))
        {
            throw new ArgumentOutOfRangeException(nameof(options), "The rendering API is not supported.");
        }

        List<string> files = ["sl.interposer.dll", "sl.common.dll"];

        foreach (uint feature in options.GetFeatures())
        {
            string[] dependencies = feature switch
            {
                SL.FeatureDLSS => ["sl.dlss.dll", "nvngx_dlss.dll"],
                SL.FeatureDLSSRR => ["sl.dlss_d.dll", "nvngx_dlssd.dll"],
                SL.FeatureDLSSG => ["sl.dlss_g.dll", "nvngx_dlssg.dll"],
                SL.FeatureReflex => options.RenderAPI == RenderAPI.Vulkan
                    ? ["sl.reflex.dll", "NvLowLatencyVk.dll"] : ["sl.reflex.dll"],
                SL.FeaturePCL => ["sl.pcl.dll"],
                SL.FeatureNIS => ["sl.nis.dll"],
                SL.FeatureDeepDVC => ["sl.deepdvc.dll", "nvngx_deepdvc.dll"],
                SL.FeatureDirectSR => ["sl.directsr.dll"],
                SL.FeatureNvPerf => ["sl.nvperf.dll"],
                _ => throw new ArgumentOutOfRangeException(nameof(options))
            };
            files.AddRange(dependencies);
        }

        return [.. files];
    }

    private static List<string> GetMissingFiles(string directory, IEnumerable<string> required, CancellationToken cancellationToken)
    {
        List<string> missing = [];

        foreach (string name in required)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CheckPath(directory, name);
            string path = Path.Combine(directory, name);

            if (Directory.Exists(path))
            {
                throw new IOException("A directory occupies a required Streamline library path: " + path);
            }

            if (!File.Exists(path))
            {
                missing.Add(name);
            }
            else if (new FileInfo(path).Length == 0)
            {
                throw new InvalidDataException("An existing Streamline library is empty. Replace or remove it before ensuring the runtime: " + path);
            }
        }

        return missing;
    }

    private static async Task<List<string>> ExtractAsync(string archivePath, string staging, List<string> missing, CancellationToken cancellationToken)
    {
        using ZipArchive archive = ZipFile.OpenRead(archivePath);
        HashSet<string> required = new(missing, StringComparer.OrdinalIgnoreCase);
        Dictionary<string, (ZipArchiveEntry Entry, bool Development)> selected = new(StringComparer.OrdinalIgnoreCase);

        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string path = NormalizeRelativePath(entry.FullName.TrimEnd('/', '\\'));

            if (entry.FullName.EndsWith('/') || entry.FullName.EndsWith('\\'))
            {
                continue;
            }

            string? relative = SelectFile(path, required, out bool development);

            if (relative is null)
            {
                continue;
            }

            // Unix symbolic links have file type 0120000 in the upper attribute word.
            if ((entry.ExternalAttributes >> 16 & 0xF000) == 0xA000)
            {
                throw new InvalidDataException("The Streamline archive contains a symbolic link: " + relative);
            }

            if (selected.TryGetValue(relative, out (ZipArchiveEntry Entry, bool Development) existing))
            {
                if (existing.Development == development)
                {
                    throw new InvalidDataException("The Streamline archive contains a duplicate installation path: " + relative);
                }

                if (!existing.Development)
                {
                    continue;
                }
            }

            selected[relative] = (entry, development);
        }

        foreach (string name in missing)
        {
            if (!selected.TryGetValue(name, out (ZipArchiveEntry Entry, bool Development) file) || file.Entry.Length == 0)
            {
                throw new InvalidDataException($"The official Streamline SDK does not contain the required library '{name}'. " +
                    "Provide it in the runtime directory or check https://github.com/NVIDIA-RTX/Streamline/releases.");
            }
        }

        List<string> files = [];

        foreach ((string path, (ZipArchiveEntry entry, _)) in selected.OrderBy(item => item.Key, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            string destination = Path.Combine(staging, path);
            CheckPath(staging, path);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            using Stream source = entry.Open();
            await CopyAndHashAsync(source, destination, entry.Length, cancellationToken).ConfigureAwait(false);
            files.Add(path);
        }

        return files;
    }

    private static string? SelectFile(string path, HashSet<string> required, out bool development)
    {
        const string runtimePrefix = "bin/x64/";
        const string developmentPrefix = "bin/x64/development/";
        development = path.StartsWith(developmentPrefix, StringComparison.Ordinal);

        if (path.StartsWith(runtimePrefix, StringComparison.Ordinal))
        {
            string name = path[(development ? developmentPrefix.Length : runtimePrefix.Length)..];

            if (name.Contains('/'))
            {
                return null;
            }

            return required.TryGetValue(name, out string? canonical) ? canonical : IsLicenseFile(name) ? name : null;
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
}
