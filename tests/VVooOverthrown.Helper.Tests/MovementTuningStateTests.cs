using VVooOverthrown.Helper.Runtime;
using Xunit;

namespace VVooOverthrown.Helper.Tests;

public sealed class MovementTuningStateTests
{
    [Fact]
    public void RegularAndSpecialJumpTypesUseIndependentMultipliers()
    {
        var state = new MovementTuningState();
        state.SetRegularJumpMultiplier(2f);
        state.SetSpecialMovementMultiplier(3f);

        Assert.Equal(20f, state.ScaleJumpVelocity(10f, jumpType: 0, isLocalPlayer: true));
        Assert.Equal(20f, state.ScaleJumpVelocity(10f, jumpType: 4, isLocalPlayer: true));
        Assert.Equal(30f, state.ScaleJumpVelocity(10f, jumpType: 5, isLocalPlayer: true));
        Assert.Equal(10f, state.ScaleJumpVelocity(10f, jumpType: 0, isLocalPlayer: false));
    }

    [Fact]
    public void GravityAndResetAreIndependent()
    {
        var state = new MovementTuningState();
        state.SetRegularJumpMultiplier(2f);
        state.SetSpecialMovementMultiplier(3f);
        state.SetGravityMultiplier(4f);

        Assert.Equal(-8f, state.ScaleGravityDelta(-2f, isLocalPlayer: true));
        Assert.Equal(-2f, state.ScaleGravityDelta(-2f, isLocalPlayer: false));

        state.Reset();

        Assert.Equal(1f, state.RegularJumpMultiplier);
        Assert.Equal(1f, state.SpecialMovementMultiplier);
        Assert.Equal(1f, state.GravityMultiplier);
    }

    [Fact]
    public void VariableHeightBonusUsesCurrentJumpGroupAndIgnoresRemotePlayers()
    {
        var state = new MovementTuningState();
        state.SetRegularJumpMultiplier(2f);
        state.SetSpecialMovementMultiplier(3f);

        Assert.Equal(8f, state.ScaleVariableHeightBonus(4f, jumpType: 0, isLocalPlayer: true));
        Assert.Equal(12f, state.ScaleVariableHeightBonus(4f, jumpType: 5, isLocalPlayer: true));
        Assert.Equal(4f, state.ScaleVariableHeightBonus(4f, jumpType: 5, isLocalPlayer: false));
    }
}
