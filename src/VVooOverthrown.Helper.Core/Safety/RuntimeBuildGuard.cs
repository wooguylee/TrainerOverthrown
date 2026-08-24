using System.Security.Cryptography;

namespace VVooOverthrown.Helper.Safety;

public sealed class RuntimeBuildGuard
{
    private readonly IReadOnlyDictionary<string, string> _expectedHashes;

    public RuntimeBuildGuard(IReadOnlyDictionary<string, string> expectedHashes) =>
        _expectedHashes = expectedHashes ?? throw new ArgumentNullException(nameof(expectedHashes));

    public static RuntimeBuildGuard Current { get; } = new(
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Overthrown.exe"] = "41A3938AEC61589E85C14FC16394D558B84A568B218799C7981A2936B68D2B1D",
            ["GameAssembly.dll"] = "28FFF76B50ED06FC0343EC218B9465AABBE927B40655D6E36F5A5DFEE7B15B1A",
            [@"Overthrown_Data\il2cpp_data\Metadata\global-metadata.dat"] =
                "D6B15EB0DAA94C16E818619872CF313544BAF81A46D68AFE23DA45334E56BA3B",
        });

    public bool IsSupported(string gameRoot, out string reason)
    {
        try
        {
            foreach (var entry in _expectedHashes)
            {
                var path = Path.Combine(gameRoot, entry.Key);
                if (!File.Exists(path))
                {
                    reason = $"missing {entry.Key}";
                    return false;
                }

                using var stream = File.OpenRead(path);
                using var sha256 = SHA256.Create();
                var actual = Convert.ToHexString(sha256.ComputeHash(stream));
                if (!actual.Equals(entry.Value, StringComparison.OrdinalIgnoreCase))
                {
                    reason = $"hash mismatch {entry.Key}";
                    return false;
                }
            }
        }
        catch (Exception exception)
        {
            reason = $"fingerprint error {exception.GetType().Name}";
            return false;
        }

        reason = string.Empty;
        return true;
    }
}
