namespace VVooOverthrown.Helper.Transport;

public sealed class PipeRequest
{
    public string Command { get; set; } = string.Empty;

    public bool Enabled { get; set; }

    public float Value { get; set; }

    public int ResourceType { get; set; }

    public int Amount { get; set; }
}

public sealed class PipeResponse
{
    public bool Ok { get; set; }

    public string ErrorCode { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string SessionDecision { get; set; } = string.Empty;

    public string[] Capabilities { get; set; } = Array.Empty<string>();

    public bool TestModeEnabled { get; set; }

    public bool OfflineMode { get; set; }

    public bool AuthoritativeHost { get; set; }

    public int ConnectionCount { get; set; } = -1;

    public bool RemoteParticipant { get; set; }

    public bool PlayerReady { get; set; }

    public bool InventoryReady { get; set; }

    public bool KingdomStorageReady { get; set; }

    public bool GodModeEnabled { get; set; }

    public float TimeScale { get; set; } = 1f;

    public float StaminaFactor { get; set; } = 1f;

    public bool InfiniteCtrlMovementEnabled { get; set; }

    public float MovementSpeedMultiplier { get; set; } = 1f;

    public float RegularJumpMultiplier { get; set; } = 1f;

    public float SpecialMovementMultiplier { get; set; } = 1f;

    public float GravityMultiplier { get; set; } = 1f;

    public int SelectedResourceType { get; set; }

    public int InventoryAmount { get; set; }

    public int KingdomAmount { get; set; }
}
