using System.Text;
using VVooOverthrown.Protocol.Transport;
using Xunit;

namespace VVooOverthrown.Protocol.Tests;

public sealed class LengthPrefixTests
{
    [Fact]
    public async Task RoundTripPreservesUtf8Payload()
    {
        await using var stream = new MemoryStream();
        var expected = Encoding.UTF8.GetBytes("{\"message\":\"한글\"}");

        await LengthPrefix.WriteAsync(stream, expected, default);
        stream.Position = 0;

        Assert.Equal(expected, await LengthPrefix.ReadAsync(stream, 1024, default));
    }

    [Fact]
    public async Task ReadRejectsFrameOverMaximumLength()
    {
        await using var stream = new MemoryStream();
        await LengthPrefix.WriteAsync(stream, new byte[32], default);
        stream.Position = 0;

        await Assert.ThrowsAsync<InvalidDataException>(
            () => LengthPrefix.ReadAsync(stream, 16, default));
    }
}
