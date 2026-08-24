using System.Security.Cryptography;

namespace VVooOverthrown.Core.Build;

public sealed class GameBuildValidator
{
    public async Task<BuildValidationResult> ValidateAsync(
        string gameRoot,
        SupportedBuildProfile profile,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var normalizedRoot = Path.GetFullPath(gameRoot);
        var mismatches = new List<string>();

        foreach (var (relativePath, expectedHash) in profile.ExpectedHashes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var path = Path.GetFullPath(Path.Combine(normalizedRoot, relativePath));
            if (!IsInsideRoot(normalizedRoot, path) || !File.Exists(path))
            {
                mismatches.Add(relativePath);
                continue;
            }

            await using var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                1024 * 1024,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            var hashBytes = await SHA256.HashDataAsync(stream, cancellationToken);
            var actualHash = Convert.ToHexString(hashBytes);
            if (!actualHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase))
            {
                mismatches.Add(relativePath);
            }
        }

        return new BuildValidationResult(profile.Name, mismatches);
    }

    private static bool IsInsideRoot(string root, string candidate)
    {
        var rootWithSeparator = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                                Path.DirectorySeparatorChar;
        return candidate.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase);
    }
}

