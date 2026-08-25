using VVooOverthrown.App.ViewModels;
using Xunit;

namespace VVooOverthrown.App.Tests;

public sealed class MultiplierInputViewModelTests
{
    [Theory]
    [InlineData("0", true, 0f)]
    [InlineData("1.25", true, 1.25f)]
    [InlineData("1000", true, 1000f)]
    [InlineData("-1", false, 0f)]
    [InlineData("1000.01", false, 0f)]
    [InlineData("NaN", false, 0f)]
    [InlineData("Infinity", false, 0f)]
    [InlineData("", false, 0f)]
    public void ParsesOnlySupportedFiniteMultiplierValues(string text, bool valid, float expected)
    {
        var input = new MultiplierInputViewModel("1");

        input.Text = text;

        Assert.Equal(valid, input.IsValid);
        Assert.Equal(valid, input.TryGetValue(out var actual));
        if (valid)
        {
            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void ApplyRequiresTrainerAndValidText()
    {
        var input = new MultiplierInputViewModel("1");

        input.SetTrainerEnabled(true);
        Assert.True(input.CanApply);

        input.Text = "not-a-number";
        Assert.False(input.CanApply);

        input.Text = "2";
        input.SetTrainerEnabled(false);
        Assert.False(input.CanApply);
    }
}
