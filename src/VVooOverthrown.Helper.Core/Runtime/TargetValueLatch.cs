namespace VVooOverthrown.Helper.Runtime;

public sealed class TargetValueLatch<TTarget, TValue>
    where TTarget : class
{
    private TTarget? _target;
    private TValue? _value;
    private bool _captured;

    public void Capture(TTarget target, TValue value)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (_captured && ReferenceEquals(_target, target))
        {
            return;
        }

        _target = target;
        _value = value;
        _captured = true;
    }

    public bool TryTake(out TTarget target, out TValue value)
    {
        if (!_captured)
        {
            target = default!;
            value = default!;
            return false;
        }

        target = _target!;
        value = _value!;
        _target = default;
        _value = default;
        _captured = false;
        return true;
    }
}
