using System.Security.Cryptography;
using System.IO;
using VVooOverthrown.App.Services;
using VVooOverthrown.Core.Build;
using Xunit;

namespace VVooOverthrown.App.Tests;

public sealed class TrainerApplicationServiceTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "VVooOverthrown.AppService.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task InstallCreatesBackupAndRefreshShowsInstalledState()
    {
        var gameRoot = Path.Combine(_root, "game");
        var payloadRoot = Path.Combine(_root, "payload");
        var userDataRoot = Path.Combine(_root, "user-data");
        var backupRoot = Path.Combine(_root, "backups");
        var profile = await CreateGameFixtureAsync(gameRoot);
        Directory.CreateDirectory(payloadRoot);
        Directory.CreateDirectory(userDataRoot);
        await File.WriteAllTextAsync(Path.Combine(payloadRoot, "translation.json"), "{}");
        await File.WriteAllTextAsync(Path.Combine(userDataRoot, "Config.bdf"), "settings");
        var service = new TrainerApplicationService(
            profile,
            payloadRoot,
            userDataRoot,
            backupRoot,
            _ => false);

        await service.InstallAsync(gameRoot, default);
        var snapshot = await service.GetSnapshotAsync(gameRoot, default);

        Assert.True(snapshot.Installed);
        Assert.Single(Directory.GetDirectories(backupRoot));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private static async Task<SupportedBuildProfile> CreateGameFixtureAsync(string gameRoot)
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
            var path = Path.Combine(gameRoot, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllBytesAsync(path, contents);
            hashes[relativePath] = Convert.ToHexString(SHA256.HashData(contents));
        }

        return new SupportedBuildProfile("fixture", hashes);
    }
}
