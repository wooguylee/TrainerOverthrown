using System.Collections.ObjectModel;

namespace VVooOverthrown.Core.Build;

public sealed class SupportedBuildProfile
{
    public static SupportedBuildProfile Current { get; } = new(
        "Unity 6000.1.10f1 / build 47de9a83d3aa4f279a4c30664db3df1f",
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Overthrown.exe"] = "41A3938AEC61589E85C14FC16394D558B84A568B218799C7981A2936B68D2B1D",
            ["GameAssembly.dll"] = "28FFF76B50ED06FC0343EC218B9465AABBE927B40655D6E36F5A5DFEE7B15B1A",
            ["Overthrown_Data\\il2cpp_data\\Metadata\\global-metadata.dat"] = "D6B15EB0DAA94C16E818619872CF313544BAF81A46D68AFE23DA45334E56BA3B"
        });

    public SupportedBuildProfile(string name, IReadOnlyDictionary<string, string> expectedHashes)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Build profile name is required.", nameof(name));
        }

        ArgumentNullException.ThrowIfNull(expectedHashes);
        if (expectedHashes.Count == 0)
        {
            throw new ArgumentException("At least one expected file hash is required.", nameof(expectedHashes));
        }

        var copy = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (relativePath, hash) in expectedHashes)
        {
            ValidateRelativePath(relativePath);
            if (hash.Length != 64 || !hash.All(Uri.IsHexDigit))
            {
                throw new ArgumentException($"Invalid SHA-256 for {relativePath}.", nameof(expectedHashes));
            }

            copy.Add(relativePath, hash.ToUpperInvariant());
        }

        Name = name;
        ExpectedHashes = new ReadOnlyDictionary<string, string>(copy);
    }

    public string Name { get; }

    public IReadOnlyDictionary<string, string> ExpectedHashes { get; }

    private static void ValidateRelativePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) ||
            Path.IsPathRooted(relativePath) ||
            relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Contains(".."))
        {
            throw new ArgumentException($"Unsafe build profile path: {relativePath}", nameof(relativePath));
        }
    }
}

