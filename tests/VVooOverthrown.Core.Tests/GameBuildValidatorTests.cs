using System.Security.Cryptography;
using VVooOverthrown.Core.Build;
using Xunit;

namespace VVooOverthrown.Core.Tests;

public sealed class GameBuildValidatorTests : IDisposable
{
    private readonly string _gameRoot = Path.Combine(
        Path.GetTempPath(),
        "VVooOverthrown.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task AcceptsExactSupportedFiles()
    {
        var profile = await CreateFixtureAsync();

        var result = await new GameBuildValidator().ValidateAsync(_gameRoot, profile, default);

        Assert.True(result.IsSupported);
        Assert.Empty(result.Mismatches);
    }

    [Fact]
    public async Task RejectsChangedGameAssembly()
    {
        var profile = await CreateFixtureAsync();
        await File.AppendAllTextAsync(Path.Combine(_gameRoot, "GameAssembly.dll"), "changed");

        var result = await new GameBuildValidator().ValidateAsync(_gameRoot, profile, default);

        Assert.False(result.IsSupported);
        Assert.Contains("GameAssembly.dll", result.Mismatches);
    }

    [Fact]
    public async Task RejectsMissingMetadata()
    {
        var profile = await CreateFixtureAsync();
        File.Delete(Path.Combine(
            _gameRoot,
            "Overthrown_Data",
            "il2cpp_data",
            "Metadata",
            "global-metadata.dat"));

        var result = await new GameBuildValidator().ValidateAsync(_gameRoot, profile, default);

        Assert.False(result.IsSupported);
        Assert.Contains("Overthrown_Data\\il2cpp_data\\Metadata\\global-metadata.dat", result.Mismatches);
    }

    public void Dispose()
    {
        if (Directory.Exists(_gameRoot))
        {
            Directory.Delete(_gameRoot, recursive: true);
        }
    }

    private async Task<SupportedBuildProfile> CreateFixtureAsync()
    {
        var files = new Dictionary<string, byte[]>
        {
            ["Overthrown.exe"] = "exe"u8.ToArray(),
            ["GameAssembly.dll"] = "assembly"u8.ToArray(),
            ["Overthrown_Data\\il2cpp_data\\Metadata\\global-metadata.dat"] = "metadata"u8.ToArray()
        };

        var hashes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (relativePath, contents) in files)
        {
            var path = Path.Combine(_gameRoot, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllBytesAsync(path, contents);
            hashes[relativePath] = Convert.ToHexString(SHA256.HashData(contents));
        }

        return new SupportedBuildProfile("fixture", hashes);
    }
}

