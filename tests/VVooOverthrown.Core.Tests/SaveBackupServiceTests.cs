using VVooOverthrown.Core.Saves;
using Xunit;

namespace VVooOverthrown.Core.Tests;

public sealed class SaveBackupServiceTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "VVooOverthrown.Backup.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task BackupCopiesEveryFileAndRecordsVerifiedHashes()
    {
        var source = Path.Combine(_root, "source");
        var destination = Path.Combine(_root, "backups");
        Directory.CreateDirectory(Path.Combine(source, "Config"));
        await File.WriteAllTextAsync(Path.Combine(source, "Config", "Config.bdf"), "settings");
        await File.WriteAllTextAsync(Path.Combine(source, "Player.log"), "log");

        var result = await new SaveBackupService().CreateAsync(source, destination, default);

        Assert.Equal(2, result.Files.Count);
        Assert.All(result.Files, file => Assert.Equal(64, file.Sha256.Length));
        Assert.Equal("settings", await File.ReadAllTextAsync(Path.Combine(result.BackupRoot, "Config", "Config.bdf")));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}

