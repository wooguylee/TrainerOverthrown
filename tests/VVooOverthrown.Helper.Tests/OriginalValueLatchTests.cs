using VVooOverthrown.Helper.Runtime;
using Xunit;

namespace VVooOverthrown.Helper.Tests;

public sealed class OriginalValueLatchTests
{
    [Fact]
    public void CapturesOnceAndRestoresTheOriginalValue()
    {
        var latch = new OriginalValueLatch<bool>();

        latch.Capture(true);
        latch.Capture(false);

        Assert.True(latch.TryTake(out var original));
        Assert.True(original);
        Assert.False(latch.TryTake(out _));
    }
}
