namespace VVooOverthrown.Helper.Transport;

public sealed class PipeRequest
{
    public string Command { get; set; } = string.Empty;

    public bool Enabled { get; set; }

    public float Value { get; set; }
}

public sealed class PipeResponse
{
    public bool Ok { get; set; }

    public string ErrorCode { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string SessionDecision { get; set; } = string.Empty;

    public string[] Capabilities { get; set; } = Array.Empty<string>();

    public bool GodModeEnabled { get; set; }

    public float TimeScale { get; set; } = 1f;
}
