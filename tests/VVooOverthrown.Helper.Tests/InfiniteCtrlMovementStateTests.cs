using VVooOverthrown.Helper.Runtime;
using Xunit;

namespace VVooOverthrown.Helper.Tests;

public sealed class InfiniteCtrlMovementStateTests
{
    [Fact]
    public void EnableUsesHundredTimesAndDisableRestoresCapturedFactor()
    {
        var state = new InfiniteCtrlMovementState();

        var enabledFactor = state.Enable(2f);
        var disabled = state.TryDisable(out var restoredFactor);

        Assert.Equal(100f, enabledFactor);
        Assert.True(disabled);
        Assert.Equal(2f, restoredFactor);
        Assert.False(state.Enabled);
    }

    [Fact]
    public void EnabledStateMakesElapsedDashTimerReadyWithoutReducingIt()
    {
        var state = new InfiniteCtrlMovementState();
        state.Enable(1f);

        Assert.Equal(0.8f, state.ReadyDashTimer(0.2f, 0.8f));
        Assert.Equal(1.2f, state.ReadyDashTimer(1.2f, 0.8f));
    }

    [Fact]
    public void RepeatedEnablePreservesFirstRestoreFactor()
    {
        var state = new InfiniteCtrlMovementState();

        state.Enable(2f);
        state.Enable(100f);
        state.TryDisable(out var restoredFactor);

        Assert.Equal(2f, restoredFactor);
    }

    [Fact]
    public void RemotePlayerDashTimerIsNeverChanged()
    {
        var state = new InfiniteCtrlMovementState();
        state.Enable(1f);

        var timer = state.ReadyDashTimer(0.2f, 0.8f, isLocalPlayer: false);

        Assert.Equal(0.2f, timer);
    }
}
