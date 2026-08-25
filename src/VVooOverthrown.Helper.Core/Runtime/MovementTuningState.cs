namespace VVooOverthrown.Helper.Runtime;

public sealed class MovementTuningState
{
    public const float DefaultMultiplier = 1f;

    public float RegularJumpMultiplier { get; private set; } = DefaultMultiplier;

    public float SpecialMovementMultiplier { get; private set; } = DefaultMultiplier;

    public float GravityMultiplier { get; private set; } = DefaultMultiplier;

    public void SetRegularJumpMultiplier(float value) => RegularJumpMultiplier = value;

    public void SetSpecialMovementMultiplier(float value) => SpecialMovementMultiplier = value;

    public void SetGravityMultiplier(float value) => GravityMultiplier = value;

    public float ScaleJumpVelocity(float value, int jumpType, bool isLocalPlayer)
    {
        if (!isLocalPlayer)
        {
            return value;
        }

        var multiplier = jumpType is >= 0 and <= 4
            ? RegularJumpMultiplier
            : SpecialMovementMultiplier;
        return value * multiplier;
    }

    public float ScaleGravityDelta(float value, bool isLocalPlayer) =>
        isLocalPlayer ? value * GravityMultiplier : value;

    public float ScaleVariableHeightBonus(float value, int jumpType, bool isLocalPlayer) =>
        ScaleJumpVelocity(value, jumpType, isLocalPlayer);

    public void Reset()
    {
        RegularJumpMultiplier = DefaultMultiplier;
        SpecialMovementMultiplier = DefaultMultiplier;
        GravityMultiplier = DefaultMultiplier;
    }
}
