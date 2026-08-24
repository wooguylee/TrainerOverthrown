using System.Security.Cryptography;
using VVooOverthrown.Helper.Safety;
using Xunit;

namespace VVooOverthrown.Helper.Tests;

public sealed class RuntimeBuildGuardTests
{
    [Fact]
    public void RejectsAChangedSupportedBuildFile()
    {
        var root = Path.Combine(Path.GetTempPath(), $"VVooOverthrown-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var relativePath = "Overthrown.exe";
            var path = Path.Combine(root, relativePath);
            File.WriteAllText(path, "supported");
            var expected = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
            var guard = new RuntimeBuildGuard(
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    [relativePath] = expected,
                });

            Assert.True(guard.IsSupported(root, out _));
            File.WriteAllText(path, "changed");
            Assert.False(guard.IsSupported(root, out var reason));
            Assert.Contains(relativePath, reason, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
