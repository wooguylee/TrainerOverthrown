using System.IO.Pipes;
using System.Text.Json;

namespace VVooOverthrown.Helper.Transport;

public sealed class HelperPipeServer : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _pipeName;
    private readonly Func<PipeRequest, Task<PipeResponse>> _handler;
    private readonly Func<Task>? _onDisconnected;
    private readonly CancellationTokenSource _stop = new();
    private Task? _runTask;

    public HelperPipeServer(
        string pipeName,
        Func<PipeRequest, Task<PipeResponse>> handler,
        Func<Task>? onDisconnected = null)
    {
        _pipeName = pipeName;
        _handler = handler;
        _onDisconnected = onDisconnected;
    }

    public void Start() => _runTask = Task.Run(RunAsync);

    private async Task RunAsync()
    {
        while (!_stop.IsCancellationRequested)
        {
            var connected = false;
            try
            {
                await using var pipe = new NamedPipeServerStream(
                    _pipeName,
                    PipeDirection.InOut,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                await pipe.WaitForConnectionAsync(_stop.Token).ConfigureAwait(false);
                connected = true;
                using var reader = new StreamReader(pipe, leaveOpen: true);
                using var writer = new StreamWriter(pipe, leaveOpen: true) { AutoFlush = true };
                while (pipe.IsConnected && !_stop.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync().ConfigureAwait(false);
                    if (line is null)
                    {
                        break;
                    }

                    PipeResponse response;
                    try
                    {
                        var request = JsonSerializer.Deserialize<PipeRequest>(line, JsonOptions)
                                      ?? throw new InvalidDataException("요청 JSON이 비어 있습니다.");
                        response = await _handler(request).ConfigureAwait(false);
                    }
                    catch (Exception exception)
                    {
                        response = new PipeResponse
                        {
                            Ok = false,
                            ErrorCode = "INVALID_REQUEST",
                            Message = exception.Message,
                        };
                    }

                    await writer.WriteLineAsync(JsonSerializer.Serialize(response, JsonOptions))
                        .ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (_stop.IsCancellationRequested)
            {
                break;
            }
            catch
            {
                if (!_stop.IsCancellationRequested)
                {
                    await Task.Delay(250, _stop.Token).ConfigureAwait(false);
                }
            }
            finally
            {
                if (connected && !_stop.IsCancellationRequested && _onDisconnected is not null)
                {
                    try
                    {
                        await _onDisconnected().ConfigureAwait(false);
                    }
                    catch
                    {
                        // The server must keep accepting a new local app connection.
                    }
                }
            }
        }
    }

    public void Dispose()
    {
        _stop.Cancel();
        try
        {
            _runTask?.Wait(TimeSpan.FromSeconds(1));
        }
        catch
        {
            // Shutdown is best-effort; all Unity state is reset by RuntimeHost.
        }
        _stop.Dispose();
    }
}
