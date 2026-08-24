using System.Security.Cryptography;
using VVooOverthrown.Core.Build;
using VVooOverthrown.Core.Installation;
using Xunit;

namespace VVooOverthrown.Core.Tests;

public sealed class PayloadInstallerTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "VVooOverthrown.Installer.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task InstallWritesPayloadAndOwnedManifest()
    {
        var (gameRoot, payloadRoot, profile) = await CreateFixtureAsync();
        var installer = new PayloadInstaller(new GameBuildValidator(), () => false);

        var result = await installer.InstallAsync(gameRoot, payloadRoot, profile, default);

        Assert.True(result.Succeeded);
        Assert.Equal("plugin", await File.ReadAllTextAsync(Path.Combine(gameRoot, "BepInEx", "plugins", "VVooOverthrown", "plugin.dll")));
        Assert.True(File.Exists(PayloadInstaller.GetManifestPath(gameRoot)));
    }

    [Fact]
    public async Task FailedInstallRollsBackFilesCopiedEarlier()
    {
        var (gameRoot, payloadRoot, profile) = await CreateFixtureAsync();
        await File.WriteAllTextAsync(Path.Combine(payloadRoot, "a-first.txt"), "first");
        await File.WriteAllTextAsync(Path.Combine(payloadRoot, "z-collision"), "file");
        Directory.CreateDirectory(Path.Combine(gameRoot, "z-collision"));
        var installer = new PayloadInstaller(new GameBuildValidator(), () => false);

        await Assert.ThrowsAnyAsync<IOException>(
            () => installer.InstallAsync(gameRoot, payloadRoot, profile, default));

        Assert.False(File.Exists(Path.Combine(gameRoot, "a-first.txt")));
        Assert.False(File.Exists(PayloadInstaller.GetManifestPath(gameRoot)));
    }

    [Fact]
    public async Task RemovePreservesInstalledFileChangedByUser()
    {
        var (gameRoot, payloadRoot, profile) = await CreateFixtureAsync();
        var installer = new PayloadInstaller(new GameBuildValidator(), () => false);
        await installer.InstallAsync(gameRoot, payloadRoot, profile, default);
        var installedPlugin = Path.Combine(gameRoot, "BepInEx", "plugins", "VVooOverthrown", "plugin.dll");
        await File.AppendAllTextAsync(installedPlugin, "-user-change");

        var result = await installer.RemoveAsync(gameRoot, default);

        Assert.True(File.Exists(installedPlugin));
        Assert.Contains(installedPlugin, result.PreservedModifiedFiles);
        Assert.False(File.Exists(PayloadInstaller.GetManifestPath(gameRoot)));
    }

    [Fact]
    public async Task InstallRefusesWhenGameIsRunning()
    {
        var (gameRoot, payloadRoot, profile) = await CreateFixtureAsync();
        var installer = new PayloadInstaller(new GameBuildValidator(), () => true);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => installer.InstallAsync(gameRoot, payloadRoot, profile, default));

        Assert.Equal("게임이 실행 중일 때는 설치할 수 없습니다.", exception.Message);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private async Task<(string GameRoot, string PayloadRoot, SupportedBuildProfile Profile)> CreateFixtureAsync()
    {
        var gameRoot = Path.Combine(_root, "game");
        var payloadRoot = Path.Combine(_root, "payload");
        var gameFiles = new Dictionary<string, byte[]>
        {
            ["Overthrown.exe"] = "exe"u8.ToArray(),
            ["GameAssembly.dll"] = "assembly"u8.ToArray(),
            ["Overthrown_Data\\il2cpp_data\\Metadata\\global-metadata.dat"] = "metadata"u8.ToArray()
        };
        var hashes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (relativePath, contents) in gameFiles)
        {
            var path = Path.Combine(gameRoot, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllBytesAsync(path, contents);
            hashes[relativePath] = Convert.ToHexString(SHA256.HashData(contents));
        }

        var plugin = Path.Combine(payloadRoot, "BepInEx", "plugins", "VVooOverthrown", "plugin.dll");
        Directory.CreateDirectory(Path.GetDirectoryName(plugin)!);
        await File.WriteAllTextAsync(plugin, "plugin");
        return (gameRoot, payloadRoot, new SupportedBuildProfile("fixture", hashes));
    }
}

