namespace Streamline.NET;

internal sealed partial class RuntimeDownloader
{
    private static string CreateWorkspace(string destination)
    {
        string parent = Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(destination)) ?? destination;

        while (!Directory.Exists(parent))
        {
            parent = Path.GetDirectoryName(parent) ?? throw new DirectoryNotFoundException("No parent directory exists for the Streamline runtime destination.");
        }

        string workspace = Path.Combine(parent, ".streamline-download-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workspace);

        return workspace;
    }

    private static void Publish(string directory, string staging, string workspace, Installation? previous, Installation installation,
        CancellationToken cancellationToken, ref bool preserveRecovery)
    {
        string backup = Path.Combine(workspace, "backup");
        // A spelling change must also remove the old managed path on case-sensitive hosts.
        HashSet<string> currentFiles = installation.Files.Select(file => file.Path).ToHashSet(StringComparer.Ordinal);
        List<string> obsoleteFiles = previous is null ? [] : [.. previous.Files.Select(file => file.Path).Where(path => !currentFiles.Contains(path))];
        List<Replacement> replacements = [];
        List<string> createdDirectories = [];

        try
        {
            foreach (string path in obsoleteFiles.Concat(installation.Files.Select(file => file.Path)).Append(ManifestName))
            {
                cancellationToken.ThrowIfCancellationRequested();
                CheckPath(directory, path);
                string target = Path.Combine(directory, path);
                string saved = Path.Combine(backup, path);
                string staged = Path.Combine(staging, path);
                bool replacing = currentFiles.Contains(path) || path == ManifestName;
                Replacement replacement = new(target, saved);
                replacements.Add(replacement);

                if (File.Exists(target))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(saved)!);
                    File.Move(target, saved);
                    replacement.OriginalMoved = true;
                }

                if (replacing)
                {
                    EnsureDirectory(Path.GetDirectoryName(target)!, createdDirectories);
                    File.Move(staged, target);
                    replacement.Installed = true;
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
        }
        catch (Exception failure)
        {
            List<Exception> rollbackFailures = [];

            foreach (Replacement replacement in replacements.AsEnumerable().Reverse())
            {
                try
                {
                    CheckPath(directory, Path.GetRelativePath(directory, replacement.Target));

                    if (replacement.Installed)
                    {
                        File.Delete(replacement.Target);
                    }

                    if (replacement.OriginalMoved)
                    {
                        File.Move(replacement.Backup, replacement.Target);
                    }
                }
                catch (Exception rollbackFailure) when (rollbackFailure is IOException or UnauthorizedAccessException)
                {
                    rollbackFailures.Add(rollbackFailure);
                }
            }

            foreach (string path in createdDirectories.AsEnumerable().Reverse())
            {
                try
                {
                    CheckLink(path);

                    if (Directory.Exists(path) && !Directory.EnumerateFileSystemEntries(path).Any())
                    {
                        Directory.Delete(path);
                    }
                }
                catch (Exception rollbackFailure) when (rollbackFailure is IOException or UnauthorizedAccessException)
                {
                    rollbackFailures.Add(rollbackFailure);
                }
            }

            if (rollbackFailures.Count > 0)
            {
                preserveRecovery = true;
                rollbackFailures.Insert(0, failure);

                throw new IOException($"Streamline installation rollback could not finish. Recovery files have been preserved at '{workspace}'.",
                    new AggregateException(rollbackFailures));
            }

            throw;
        }
    }

    private static void EnsureDirectory(string directory, List<string> created)
    {
        if (Directory.Exists(directory))
        {
            return;
        }

        EnsureDirectory(Path.GetDirectoryName(directory) ?? throw new DirectoryNotFoundException(directory), created);
        Directory.CreateDirectory(directory);
        created.Add(directory);
    }

    private sealed class Replacement(string target, string backup)
    {
        internal string Target { get; } = target;

        internal string Backup { get; } = backup;

        internal bool OriginalMoved { get; set; }

        internal bool Installed { get; set; }
    }
}
