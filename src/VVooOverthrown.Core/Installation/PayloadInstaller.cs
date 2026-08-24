using System.Security.Cryptography;
using System.Text.Json;
using VVooOverthrown.Core.Build;

namespace VVooOverthrown.Core.Installation;

public sealed class PayloadInstaller
{
    private const string ManifestRelativePath = @"BepInEx\VVooOverthrown.install-manifest.json";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly GameBuildValidator _buildValidator;
    private readonly Func<bool> _isGameRunning;

    public PayloadInstaller(GameBuildValidator buildValidator, Func<bool> isGameRunning)
    {
        _buildValidator = buildValidator ?? throw new ArgumentNullException(nameof(buildValidator));
        _isGameRunning = isGameRunning ?? throw new ArgumentNullException(nameof(isGameRunning));
    }

    public static string GetManifestPath(string gameRoot) =>
        Path.Combine(Path.GetFullPath(gameRoot), ManifestRelativePath);

    public async Task<InstallResult> InstallAsync(
        string gameRoot,
        string payloadRoot,
        SupportedBuildProfile profile,
        CancellationToken cancellationToken)
    {
        if (_isGameRunning())
        {
            throw new InvalidOperationException("게임이 실행 중일 때는 설치할 수 없습니다.");
        }

        var build = await _buildValidator.ValidateAsync(gameRoot, profile, cancellationToken);
        if (!build.IsSupported)
        {
            throw new InvalidOperationException(
                $"지원하지 않는 게임 빌드입니다: {string.Join(", ", build.Mismatches)}");
        }

        var normalizedGameRoot = Path.GetFullPath(gameRoot);
        var normalizedPayloadRoot = Path.GetFullPath(payloadRoot);
        if (!Directory.Exists(normalizedPayloadRoot))
        {
            throw new DirectoryNotFoundException(normalizedPayloadRoot);
        }

        var manifestPath = GetManifestPath(normalizedGameRoot);
        if (File.Exists(manifestPath))
        {
            throw new InvalidOperationException("VVooOverthrown payload가 이미 설치되어 있습니다.");
        }

        var createdFiles = new List<string>();
        try
        {
            var manifest = new InstallManifest
            {
                BuildProfile = profile.Name,
                InstalledAtUtc = DateTimeOffset.UtcNow
            };

            foreach (var sourcePath in Directory.EnumerateFiles(normalizedPayloadRoot, "*", SearchOption.AllDirectories)
                         .OrderBy(path => path, StringComparer.Ordinal))
            {
                cancellationToken.ThrowIfCancellationRequested();
                RejectReparsePoint(sourcePath);
                var relativePath = Path.GetRelativePath(normalizedPayloadRoot, sourcePath);
                var destinationPath = ResolveOwnedPath(normalizedGameRoot, relativePath);
                if (File.Exists(destinationPath) || Directory.Exists(destinationPath))
                {
                    throw new IOException($"설치 대상이 이미 존재합니다: {relativePath}");
                }

                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                File.Copy(sourcePath, destinationPath, overwrite: false);
                createdFiles.Add(destinationPath);
                var fileInfo = new FileInfo(destinationPath);
                manifest.Files.Add(new InstalledFile
                {
                    RelativePath = relativePath,
                    Length = fileInfo.Length,
                    Sha256 = await ComputeHashAsync(destinationPath, cancellationToken)
                });
            }

            Directory.CreateDirectory(Path.GetDirectoryName(manifestPath)!);
            var manifestTempPath = manifestPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            await File.WriteAllTextAsync(
                manifestTempPath,
                JsonSerializer.Serialize(manifest, JsonOptions),
                cancellationToken);
            File.Move(manifestTempPath, manifestPath);
            createdFiles.Add(manifestPath);
            return new InstallResult(true, manifest.Files.Select(file => file.RelativePath).ToArray());
        }
        catch
        {
            foreach (var createdFile in createdFiles.AsEnumerable().Reverse())
            {
                if (File.Exists(createdFile))
                {
                    File.Delete(createdFile);
                }
            }

            throw;
        }
    }

    public async Task<RemovalResult> RemoveAsync(string gameRoot, CancellationToken cancellationToken)
    {
        if (_isGameRunning())
        {
            throw new InvalidOperationException("게임이 실행 중일 때는 제거할 수 없습니다.");
        }

        var normalizedGameRoot = Path.GetFullPath(gameRoot);
        var manifestPath = GetManifestPath(normalizedGameRoot);
        if (!File.Exists(manifestPath))
        {
            return new RemovalResult([], []);
        }

        var manifest = JsonSerializer.Deserialize<InstallManifest>(
                           await File.ReadAllTextAsync(manifestPath, cancellationToken),
                           JsonOptions) ??
                       throw new InvalidDataException("설치 manifest를 읽을 수 없습니다.");
        if (!manifest.Owner.Equals("VVooOverthrown", StringComparison.Ordinal))
        {
            throw new InvalidDataException("알 수 없는 설치 manifest 소유자입니다.");
        }

        var removed = new List<string>();
        var preserved = new List<string>();
        foreach (var entry in manifest.Files.OrderByDescending(file => file.RelativePath.Length))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var installedPath = ResolveOwnedPath(normalizedGameRoot, entry.RelativePath);
            if (!File.Exists(installedPath))
            {
                continue;
            }

            var actualHash = await ComputeHashAsync(installedPath, cancellationToken);
            if (new FileInfo(installedPath).Length == entry.Length &&
                actualHash.Equals(entry.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(installedPath);
                removed.Add(installedPath);
            }
            else
            {
                preserved.Add(installedPath);
            }
        }

        File.Delete(manifestPath);
        return new RemovalResult(removed, preserved);
    }

    private static string ResolveOwnedPath(string root, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
        {
            throw new InvalidDataException($"안전하지 않은 설치 경로입니다: {relativePath}");
        }

        var candidate = Path.GetFullPath(Path.Combine(root, relativePath));
        var rootWithSeparator = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                                Path.DirectorySeparatorChar;
        if (!candidate.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"게임 폴더 밖의 설치 경로입니다: {relativePath}");
        }

        return candidate;
    }

    private static void RejectReparsePoint(string path)
    {
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidDataException($"링크 파일은 payload에 포함할 수 없습니다: {path}");
        }
    }

    private static async Task<string> ComputeHashAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            1024 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        return Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken));
    }
}

