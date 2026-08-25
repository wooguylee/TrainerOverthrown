using VVooOverthrown.Core.Transport;
using VVooOverthrown.Helper.Transport;
using Xunit;

namespace VVooOverthrown.Core.Tests;

public sealed class TrainerPipeClientTests
{
    [Fact]
    public async Task ExchangesExpandedContractWithCurrentUserPipe()
    {
        var pid = Random.Shared.Next(100_000, 999_999);
        using var server = new HelperPipeServer(
            $"VVooOverthrown.{pid}",
            request => Task.FromResult(new PipeResponse
            {
                Ok = request.Command == "inventoryQuery" && request.ResourceType == 18 && request.Amount == 250,
                TestModeEnabled = true,
                SessionDecision = "Uncertain",
                OfflineMode = false,
                ConnectionCount = 2,
                InventoryAmount = 987,
                SelectedResourceType = request.ResourceType,
                Capabilities = ["inventory.resource"],
            }));
        server.Start();
        await using var client = new TrainerPipeClient();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await client.ConnectAsync(pid, timeout.Token);
        var response = await client.SendAsync(
            new PipeRequest
            {
                Command = "inventoryQuery",
                ResourceType = 18,
                Amount = 250,
            }, timeout.Token);

        Assert.True(response.Ok);
        Assert.True(response.TestModeEnabled);
        Assert.Equal("Uncertain", response.SessionDecision);
        Assert.False(response.OfflineMode);
        Assert.Equal(2, response.ConnectionCount);
        Assert.Equal(18, response.SelectedResourceType);
        Assert.Equal(987, response.InventoryAmount);
        Assert.Contains("inventory.resource", response.Capabilities);
    }
}
