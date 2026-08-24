using System.Text.Json;
using VVooOverthrown.Protocol.Messages;
using Xunit;

namespace VVooOverthrown.Protocol.Tests;

public sealed class EnvelopeTests
{
    [Fact]
    public void JsonRoundTripPreservesKoreanPayload()
    {
        using var document = JsonDocument.Parse("{\"message\":\"연결됨\"}");
        var expected = new Envelope("status", "request-1", document.RootElement.Clone());

        var json = JsonSerializer.Serialize(expected);
        var actual = JsonSerializer.Deserialize<Envelope>(json);

        Assert.NotNull(actual);
        Assert.Equal(expected.Type, actual.Type);
        Assert.Equal(expected.RequestId, actual.RequestId);
        Assert.Equal("연결됨", actual.Payload.GetProperty("message").GetString());
    }
}

