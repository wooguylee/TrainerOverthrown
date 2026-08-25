using VVooOverthrown.Helper.Transport;

namespace VVooOverthrown.Helper.Features;

public static class TrainerRequestValidator
{
    private const int MaxResourceAmount = 1_000_000_000;
    private const float MaxMultiplier = 1000f;

    private static readonly HashSet<string> KnownCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        TrainerCommands.Status,
        TrainerCommands.Reset,
        TrainerCommands.GodMode,
        TrainerCommands.Heal,
        TrainerCommands.StaminaFactor,
        TrainerCommands.InfiniteCtrlMovement,
        TrainerCommands.MovementSpeed,
        TrainerCommands.TimeScale,
        TrainerCommands.RegularJumpMultiplier,
        TrainerCommands.SpecialMovementMultiplier,
        TrainerCommands.GravityMultiplier,
        TrainerCommands.InventoryQuery,
        TrainerCommands.InventorySet,
        TrainerCommands.InventoryAdd,
        TrainerCommands.KingdomQuery,
        TrainerCommands.KingdomSet,
        TrainerCommands.KingdomAdd,
    };

    private static readonly HashSet<string> ReadOnlyCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        TrainerCommands.Status,
        TrainerCommands.InventoryQuery,
        TrainerCommands.KingdomQuery,
    };

    public static bool IsMutation(string command) =>
        !string.IsNullOrWhiteSpace(command) &&
        KnownCommands.Contains(command) &&
        !ReadOnlyCommands.Contains(command);

    public static TrainerValidationResult Validate(PipeRequest request)
    {
        if (request is null || !KnownCommands.Contains(request.Command ?? string.Empty))
        {
            return TrainerValidationResult.Invalid("UNKNOWN_COMMAND", "지원하지 않는 명령입니다.");
        }

        var command = request.Command ?? string.Empty;
        if (IsMultiplierCommand(command))
        {
            return InRange(request.Value, 0f, MaxMultiplier)
                ? TrainerValidationResult.Valid
                : TrainerValidationResult.Invalid("OUT_OF_RANGE", "배율은 0~1000 범위의 유한한 숫자여야 합니다.");
        }

        if (IsResourceCommand(command))
        {
            if (!IsResourceType(request.ResourceType))
            {
                return TrainerValidationResult.Invalid("INVALID_RESOURCE", "선택한 자원 형식은 지원되지 않습니다.");
            }

            if (IsSetCommand(command) && (request.Amount < 0 || request.Amount > MaxResourceAmount))
            {
                return TrainerValidationResult.Invalid("OUT_OF_RANGE", "설정 수량은 0~1,000,000,000 범위여야 합니다.");
            }

            if (IsAddCommand(command) &&
                (request.Amount < -MaxResourceAmount || request.Amount > MaxResourceAmount))
            {
                return TrainerValidationResult.Invalid("OUT_OF_RANGE", "증감 수량은 -1,000,000,000~1,000,000,000 범위여야 합니다.");
            }
        }

        return TrainerValidationResult.Valid;
    }

    private static bool InRange(float value, float minimum, float maximum) =>
        !float.IsNaN(value) && !float.IsInfinity(value) && value >= minimum && value <= maximum;

    private static bool IsMultiplierCommand(string command) =>
        command.Equals(TrainerCommands.TimeScale, StringComparison.OrdinalIgnoreCase) ||
        command.Equals(TrainerCommands.StaminaFactor, StringComparison.OrdinalIgnoreCase) ||
        command.Equals(TrainerCommands.MovementSpeed, StringComparison.OrdinalIgnoreCase) ||
        command.Equals(TrainerCommands.RegularJumpMultiplier, StringComparison.OrdinalIgnoreCase) ||
        command.Equals(TrainerCommands.SpecialMovementMultiplier, StringComparison.OrdinalIgnoreCase) ||
        command.Equals(TrainerCommands.GravityMultiplier, StringComparison.OrdinalIgnoreCase);

    private static bool IsResourceCommand(string command) =>
        command.StartsWith("inventory", StringComparison.OrdinalIgnoreCase) ||
        command.StartsWith("kingdom", StringComparison.OrdinalIgnoreCase);

    private static bool IsSetCommand(string command) =>
        command.EndsWith("Set", StringComparison.OrdinalIgnoreCase);

    private static bool IsAddCommand(string command) =>
        command.EndsWith("Add", StringComparison.OrdinalIgnoreCase);

    private static bool IsResourceType(int resourceType) =>
        resourceType is >= 1 and <= 38 && resourceType is not 9 and not 26;
}

public sealed class TrainerValidationResult
{
    private TrainerValidationResult(bool isValid, string errorCode, string message)
    {
        IsValid = isValid;
        ErrorCode = errorCode;
        Message = message;
    }

    public static TrainerValidationResult Valid { get; } = new(true, string.Empty, string.Empty);

    public bool IsValid { get; }

    public string ErrorCode { get; }

    public string Message { get; }

    public static TrainerValidationResult Invalid(string errorCode, string message) =>
        new(false, errorCode, message);
}
