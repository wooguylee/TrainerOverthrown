namespace VVooOverthrown.Core.Installation;

public sealed class InstallManifest
{
    public string Owner { get; init; } = "VVooOverthrown";

    public string BuildProfile { get; init; } = string.Empty;

    public DateTimeOffset InstalledAtUtc { get; init; }

    public List<InstalledFile> Files { get; init; } = [];
}

public sealed class InstalledFile
{
    public string RelativePath { get; init; } = string.Empty;

    public string Sha256 { get; init; } = string.Empty;

    public long Length { get; init; }
}

public sealed class InstallResult
{
    public InstallResult(bool succeeded, IReadOnlyList<string> installedFiles)
    {
        Succeeded = succeeded;
        InstalledFiles = installedFiles;
    }

    public bool Succeeded { get; }

    public IReadOnlyList<string> InstalledFiles { get; }
}

public sealed class RemovalResult
{
    public RemovalResult(IReadOnlyList<string> removedFiles, IReadOnlyList<string> preservedModifiedFiles)
    {
        RemovedFiles = removedFiles;
        PreservedModifiedFiles = preservedModifiedFiles;
    }

    public IReadOnlyList<string> RemovedFiles { get; }

    public IReadOnlyList<string> PreservedModifiedFiles { get; }
}

