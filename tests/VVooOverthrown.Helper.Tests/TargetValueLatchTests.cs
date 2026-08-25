using VVooOverthrown.Helper.Runtime;
using Xunit;

namespace VVooOverthrown.Helper.Tests;

public sealed class TargetValueLatchTests
{
    [Fact]
    public void ReplacingTargetReplacesCapturedOriginalValue()
    {
        var first = new object();
        var second = new object();
        var latch = new TargetValueLatch<object, float>();

        latch.Capture(first, 0.5f);
        latch.Capture(second, 2f);

        Assert.True(latch.TryTake(out var target, out var value));
        Assert.Same(second, target);
        Assert.Equal(2f, value);
    }

    [Fact]
    public void ReusingTargetKeepsFirstOriginalValue()
    {
        var target = new object();
        var latch = new TargetValueLatch<object, float>();

        latch.Capture(target, 0.5f);
        latch.Capture(target, 2f);

        Assert.True(latch.TryTake(out var capturedTarget, out var value));
        Assert.Same(target, capturedTarget);
        Assert.Equal(0.5f, value);
    }
}
