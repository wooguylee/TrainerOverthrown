using VVooOverthrown.Core.Discovery;
using Xunit;

namespace VVooOverthrown.Core.Tests;

public sealed class GameLocatorTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "VVooOverthrown.GameLocator.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void AcceptsRootContainingExpectedExecutableAndDataDirectory()
    {
        Directory.CreateDirectory(Path.Combine(_root, "Overthrown_Data"));
        File.WriteAllText(Path.Combine(_root, "Overthrown.exe"), "fixture");

        var result = GameLocator.ValidateRoot(_root);

        Assert.True(result.IsValid);
        Assert.Equal(Path.GetFullPath(_root), result.GameRoot);
    }

    [Fact]
    public void RejectsDirectoryWithoutExpectedExecutable()
    {
        Directory.CreateDirectory(_root);

        var result = GameLocator.ValidateRoot(_root);

        Assert.False(result.IsValid);
        Assert.Equal("Overthrown.exe를 찾을 수 없습니다.", result.ErrorMessage);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}

