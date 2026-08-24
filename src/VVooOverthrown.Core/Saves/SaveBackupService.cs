using System.Security.Cryptography;

namespace VVooOverthrown.Core.Saves;

public sealed class SaveBackupService
{
    public async Task<SaveBackupResult> CreateAsync(
        string sourceRoot,
        string destinationRoot,
        CancellationToken cancellationToken)
    {
        var normalizedSource = Path.GetFullPath(sourceRoot);
        var normalizedDestination = Path.GetFullPath(destinationRoot);
        if (!Directory.Exists(normalizedSource))
        {
            throw new DirectoryNotFoundException(normalizedSource);
        }

        Directory.CreateDirectory(normalizedDestination);
        var backupRoot = Path.Combine(
            normalizedDestination,
            $"Overthrown-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(backupRoot);
        var files = new List<BackupFile>();

        try
        {
            foreach (var sourcePath in Directory.EnumerateFiles(normalizedSource, "*", SearchOption.AllDirectories)
                         .OrderBy(path => path, StringComparer.Ordinal))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if ((File.GetAttributes(sourcePath) & FileAttributes.ReparsePoint) != 0)
                {
                    throw new InvalidDataException($"링크 파일은 백업할 수 없습니다: {sourcePath}");
                }

                var relativePath = Path.GetRelativePath(normalizedSource, sourcePath);
                var destinationPath = Path.Combine(backupRoot, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                File.Copy(sourcePath, destinationPath, overwrite: false);
                var sourceHash = await ComputeHashAsync(sourcePath, cancellationToken);
                var destinationHash = await ComputeHashAsync(destinationPath, cancellationToken);
                if (!sourceHash.Equals(destinationHash, StringComparison.Ordinal))
                {
                    throw new IOException($"백업 해시 검증에 실패했습니다: {relativePath}");
                }

                files.Add(new BackupFile(relativePath, sourceHash));
            }

            return new SaveBackupResult(backupRoot, files);
        }
        catch
        {
            Directory.Delete(backupRoot, recursive: true);
            throw;
        }
    }

    private static async Task<string> ComputeHashAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken));
    }
}

public sealed class SaveBackupResult
{
    public SaveBackupResult(string backupRoot, IReadOnlyList<BackupFile> files)
    {
        BackupRoot = backupRoot;
        Files = files;
    }

    public string BackupRoot { get; }

    public IReadOnlyList<BackupFile> Files { get; }
}

public sealed record BackupFile(string RelativePath, string Sha256);

