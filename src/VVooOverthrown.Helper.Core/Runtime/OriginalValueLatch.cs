namespace VVooOverthrown.Helper.Runtime;

public sealed class OriginalValueLatch<T>
{
    private bool _captured;
    private T? _value;

    public void Capture(T value)
    {
        if (_captured)
        {
            return;
        }

        _value = value;
        _captured = true;
    }

    public bool TryTake(out T value)
    {
        if (!_captured)
        {
            value = default!;
            return false;
        }

        value = _value!;
        _value = default;
        _captured = false;
        return true;
    }
}
