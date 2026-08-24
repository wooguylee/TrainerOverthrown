using VVooOverthrown.Core.Transport;
using VVooOverthrown.Helper.Transport;
using Xunit;

namespace VVooOverthrown.Core.Tests;

public sealed class TrainerPipeClientTests
{
    [Fact]
    public async Task ExchangesStatusWithCurrentUserPipe()
    {
        var pid = Random.Shared.Next(100_000, 999_999);
        using var server = new HelperPipeServer(
            $"VVooOverthrown.{pid}",
            request => Task.FromResult(new PipeResponse
            {
                Ok = request.Command == "status",
                SessionDecision = "Allowed",
                Capabilities = ["player.godMode"],
            }));
        server.Start();
        await using var client = new TrainerPipeClient();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await client.ConnectAsync(pid, timeout.Token);
        var response = await client.SendAsync(
            new PipeRequest { Command = "status" }, timeout.Token);

        Assert.True(response.Ok);
        Assert.Equal("Allowed", response.SessionDecision);
        Assert.Contains("player.godMode", response.Capabilities);
    }
}
