using VVooOverthrown.Helper.Features;
using VVooOverthrown.Helper.Transport;
using Xunit;

namespace VVooOverthrown.Helper.Tests;

public sealed class TrainerRequestValidatorTests
{
    [Theory]
    [InlineData(TrainerCommands.Status, false)]
    [InlineData(TrainerCommands.Reset, true)]
    [InlineData(TrainerCommands.InventoryQuery, false)]
    [InlineData(TrainerCommands.KingdomQuery, false)]
    [InlineData(TrainerCommands.GodMode, true)]
    [InlineData(TrainerCommands.Heal, true)]
    [InlineData(TrainerCommands.StaminaFactor, true)]
    [InlineData(TrainerCommands.InfiniteCtrlMovement, true)]
    [InlineData(TrainerCommands.MovementSpeed, true)]
    [InlineData(TrainerCommands.TimeScale, true)]
    [InlineData(TrainerCommands.RegularJumpMultiplier, true)]
    [InlineData(TrainerCommands.SpecialMovementMultiplier, true)]
    [InlineData(TrainerCommands.GravityMultiplier, true)]
    [InlineData(TrainerCommands.InventorySet, true)]
    [InlineData(TrainerCommands.InventoryAdd, true)]
    [InlineData(TrainerCommands.KingdomSet, true)]
    [InlineData(TrainerCommands.KingdomAdd, true)]
    public void ClassifiesMutationCommands(string command, bool expected)
    {
        Assert.Equal(expected, TrainerRequestValidator.IsMutation(command));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AcceptsInfiniteCtrlMovementToggle(bool enabled)
    {
        var result = TrainerRequestValidator.Validate(new PipeRequest
        {
            Command = TrainerCommands.InfiniteCtrlMovement,
            Enabled = enabled,
        });

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(TrainerCommands.TimeScale, -0.01f, false)]
    [InlineData(TrainerCommands.TimeScale, 0f, true)]
    [InlineData(TrainerCommands.TimeScale, 1000f, true)]
    [InlineData(TrainerCommands.TimeScale, 1000.01f, false)]
    [InlineData(TrainerCommands.StaminaFactor, -0.01f, false)]
    [InlineData(TrainerCommands.StaminaFactor, 0f, true)]
    [InlineData(TrainerCommands.StaminaFactor, 1000f, true)]
    [InlineData(TrainerCommands.MovementSpeed, -0.01f, false)]
    [InlineData(TrainerCommands.MovementSpeed, 0f, true)]
    [InlineData(TrainerCommands.MovementSpeed, 1000f, true)]
    [InlineData(TrainerCommands.RegularJumpMultiplier, 0f, true)]
    [InlineData(TrainerCommands.RegularJumpMultiplier, 1.25f, true)]
    [InlineData(TrainerCommands.RegularJumpMultiplier, 1000f, true)]
    [InlineData(TrainerCommands.RegularJumpMultiplier, 1000.01f, false)]
    [InlineData(TrainerCommands.SpecialMovementMultiplier, 0f, true)]
    [InlineData(TrainerCommands.SpecialMovementMultiplier, 1000f, true)]
    [InlineData(TrainerCommands.SpecialMovementMultiplier, -0.01f, false)]
    [InlineData(TrainerCommands.GravityMultiplier, 0f, true)]
    [InlineData(TrainerCommands.GravityMultiplier, 1000f, true)]
    [InlineData(TrainerCommands.GravityMultiplier, 1000.01f, false)]
    public void ValidatesNumericCommandRanges(string command, float value, bool expected)
    {
        var result = TrainerRequestValidator.Validate(new PipeRequest
        {
            Command = command,
            Value = value,
        });

        Assert.Equal(expected, result.IsValid);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(9, false)]
    [InlineData(18, true)]
    [InlineData(26, false)]
    [InlineData(38, true)]
    [InlineData(39, false)]
    public void ValidatesResourceIdentifiers(int resourceType, bool expected)
    {
        var result = TrainerRequestValidator.Validate(new PipeRequest
        {
            Command = TrainerCommands.InventorySet,
            ResourceType = resourceType,
            Amount = 100,
        });

        Assert.Equal(expected, result.IsValid);
    }

    [Fact]
    public void RejectsNegativeSetAmountButAllowsSignedAddAmount()
    {
        var setResult = TrainerRequestValidator.Validate(new PipeRequest
        {
            Command = TrainerCommands.KingdomSet,
            ResourceType = 1,
            Amount = -1,
        });
        var addResult = TrainerRequestValidator.Validate(new PipeRequest
        {
            Command = TrainerCommands.KingdomAdd,
            ResourceType = 1,
            Amount = -100,
        });

        Assert.False(setResult.IsValid);
        Assert.True(addResult.IsValid);
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    [InlineData(1000.01f)]
    public void RejectsInvalidMovementValues(float value)
    {
        var result = TrainerRequestValidator.Validate(new PipeRequest
        {
            Command = TrainerCommands.MovementSpeed,
            Value = value,
        });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void RejectsUnknownCommand()
    {
        var result = TrainerRequestValidator.Validate(new PipeRequest { Command = "unknown" });

        Assert.False(result.IsValid);
        Assert.Equal("UNKNOWN_COMMAND", result.ErrorCode);
    }

    [Theory]
    [InlineData(1_000_000_000, true)]
    [InlineData(-1_000_000_000, true)]
    public void AllowsResourceDeltaBoundaries(int amount, bool expected)
    {
        var result = TrainerRequestValidator.Validate(new PipeRequest
        {
            Command = TrainerCommands.InventoryAdd,
            ResourceType = 1,
            Amount = amount,
        });

        Assert.Equal(expected, result.IsValid);
    }
}
