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

    private static void Publish(string directory, string staging, string workspace, string[] required, List<string> files,
        CancellationToken cancellationToken, ref bool preserveRecovery)
    {
        List<string> installed = [];
        List<string> createdDirectories = [];

        try
        {
            foreach (string path in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                CheckPath(directory, path);
                string target = Path.Combine(directory, path);

                if (File.Exists(target))
                {
                    continue;
                }

                EnsureDirectory(Path.GetDirectoryName(target)!, createdDirectories);
                File.Move(Path.Combine(staging, path), target);
                installed.Add(path);
            }

            if (GetMissingFiles(directory, required, cancellationToken).Count != 0)
            {
                throw new IOException("A required Streamline library disappeared during installation.");
            }
        }
        catch (Exception failure)
        {
            List<Exception> rollbackFailures = [];

            foreach (string path in installed.AsEnumerable().Reverse())
            {
                try
                {
                    CheckPath(directory, path);
                    File.Delete(Path.Combine(directory, path));
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

                throw new IOException($"Streamline installation rollback could not finish. Temporary files have been preserved at '{workspace}'.",
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
}
