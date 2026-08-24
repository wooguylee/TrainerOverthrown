using System.IO.Pipes;
using VVooOverthrown.Helper.Transport;
using Xunit;

namespace VVooOverthrown.Helper.Tests;

public sealed class HelperPipeServerTests
{
    [Fact]
    public async Task NotifiesWhenConnectedClientDisconnects()
    {
        var pipeName = $"VVooOverthrown.test.{Guid.NewGuid():N}";
        var disconnected = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        using var server = new HelperPipeServer(
            pipeName,
            _ => Task.FromResult(new PipeResponse { Ok = true }),
            () =>
            {
                disconnected.TrySetResult();
                return Task.CompletedTask;
            });
        server.Start();
        await using var client = new NamedPipeClientStream(
            ".",
            pipeName,
            PipeDirection.InOut,
            PipeOptions.Asynchronous);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await client.ConnectAsync(timeout.Token);
        await client.DisposeAsync();

        await disconnected.Task.WaitAsync(timeout.Token);
        Assert.True(disconnected.Task.IsCompletedSuccessfully);
    }
}
