using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VVooOverthrown.Protocol.Messages;

public sealed class Envelope
{
    [JsonConstructor]
    public Envelope(string type, string requestId, JsonElement payload)
    {
        Type = string.IsNullOrWhiteSpace(type)
            ? throw new ArgumentException("Message type is required.", nameof(type))
            : type;
        RequestId = string.IsNullOrWhiteSpace(requestId)
            ? throw new ArgumentException("Request ID is required.", nameof(requestId))
            : requestId;
        Payload = payload;
    }

    public string Type { get; }

    public string RequestId { get; }

    public JsonElement Payload { get; }
}

