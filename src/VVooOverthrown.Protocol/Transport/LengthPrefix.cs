using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace VVooOverthrown.Protocol.Transport;

public static class LengthPrefix
{
    public const int MaxFrameLength = 1024 * 1024;

    public static async Task WriteAsync(
        Stream stream,
        ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken)
    {
        if (payload.Length > MaxFrameLength)
        {
            throw new InvalidDataException($"Frame length {payload.Length} exceeds {MaxFrameLength} bytes.");
        }

        var length = payload.Length;
        var header = new[]
        {
            (byte)length,
            (byte)(length >> 8),
            (byte)(length >> 16),
            (byte)(length >> 24)
        };
        var bytes = payload.ToArray();

        await stream.WriteAsync(header, 0, header.Length, cancellationToken).ConfigureAwait(false);
        await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<byte[]> ReadAsync(
        Stream stream,
        int maximumLength,
        CancellationToken cancellationToken)
    {
        if (maximumLength < 0 || maximumLength > MaxFrameLength)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumLength));
        }

        var header = new byte[4];
        await ReadExactlyAsync(stream, header, cancellationToken).ConfigureAwait(false);
        var length = header[0] |
                     (header[1] << 8) |
                     (header[2] << 16) |
                     (header[3] << 24);
        if (length < 0 || length > maximumLength)
        {
            throw new InvalidDataException($"Frame length {length} exceeds {maximumLength} bytes.");
        }

        var payload = new byte[length];
        await ReadExactlyAsync(stream, payload, cancellationToken).ConfigureAwait(false);
        return payload;
    }

    private static async Task ReadExactlyAsync(
        Stream stream,
        byte[] buffer,
        CancellationToken cancellationToken)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var read = await stream.ReadAsync(
                    buffer,
                    offset,
                    buffer.Length - offset,
                    cancellationToken)
                .ConfigureAwait(false);
            if (read == 0)
            {
                throw new EndOfStreamException("The framed stream ended before the declared payload length.");
            }

            offset += read;
        }
    }
}

