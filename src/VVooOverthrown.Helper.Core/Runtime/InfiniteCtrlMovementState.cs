namespace VVooOverthrown.Helper.Runtime;

public sealed class InfiniteCtrlMovementState
{
    public const float RecoveryFactor = 100f;

    private float _restoreFactor;

    public bool Enabled { get; private set; }

    public float Enable(float currentFactor)
    {
        if (!Enabled)
        {
            _restoreFactor = currentFactor;
        }

        Enabled = true;
        return RecoveryFactor;
    }

    public bool TryDisable(out float restoreFactor)
    {
        restoreFactor = _restoreFactor;
        if (!Enabled)
        {
            return false;
        }

        Enabled = false;
        return true;
    }

    public float ReadyDashTimer(float current, float cooldown) =>
        Enabled ? Math.Max(current, cooldown) : current;
}
