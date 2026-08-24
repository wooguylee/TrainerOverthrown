using System.IO.Pipes;
using System.Text.Json;
using VVooOverthrown.Helper.Transport;

namespace VVooOverthrown.Core.Transport;

public sealed class TrainerPipeClient : IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly SemaphoreSlim _requestLock = new(1, 1);
    private NamedPipeClientStream? _pipe;
    private StreamReader? _reader;
    private StreamWriter? _writer;

    public bool IsConnected => _pipe?.IsConnected == true;

    public async Task ConnectAsync(int processId, CancellationToken cancellationToken)
    {
        if (processId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(processId));
        }
        if (IsConnected)
        {
            throw new InvalidOperationException("Helper에 이미 연결되어 있습니다.");
        }

        var pipe = new NamedPipeClientStream(
            ".",
            $"VVooOverthrown.{processId}",
            PipeDirection.InOut,
            PipeOptions.Asynchronous);
        try
        {
            await pipe.ConnectAsync(cancellationToken);
            _pipe = pipe;
            _reader = new StreamReader(pipe, leaveOpen: true);
            _writer = new StreamWriter(pipe, leaveOpen: true) { AutoFlush = true };
        }
        catch
        {
            await pipe.DisposeAsync();
            throw;
        }
    }

    public async Task<PipeResponse> SendAsync(
        PipeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (_pipe?.IsConnected != true || _reader is null || _writer is null)
        {
            throw new InvalidOperationException("Helper에 연결되어 있지 않습니다.");
        }

        await _requestLock.WaitAsync(cancellationToken);
        try
        {
            await _writer.WriteLineAsync(JsonSerializer.Serialize(request, JsonOptions));
            var line = await _reader.ReadLineAsync(cancellationToken)
                       ?? throw new EndOfStreamException("Helper 연결이 종료되었습니다.");
            return JsonSerializer.Deserialize<PipeResponse>(line, JsonOptions)
                   ?? throw new InvalidDataException("Helper 응답이 비어 있습니다.");
        }
        finally
        {
            _requestLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_writer is not null)
        {
            await _writer.DisposeAsync();
        }
        _reader?.Dispose();
        if (_pipe is not null)
        {
            await _pipe.DisposeAsync();
        }
        _requestLock.Dispose();
    }
}
