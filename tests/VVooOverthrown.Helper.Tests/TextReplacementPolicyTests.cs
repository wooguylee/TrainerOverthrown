using VVooOverthrown.Helper.Localization;
using Xunit;

namespace VVooOverthrown.Helper.Tests;

public sealed class TextReplacementPolicyTests
{
    [Theory]
    [InlineData("Settings", "Settings", "설정", true)]
    [InlineData("Settings", "설정", "설정", false)]
    [InlineData("Settings", " Settings ", "설정", false)]
    [InlineData("", "", "", false)]
    public void ReplacesOnlyUnchangedExactSource(
        string source,
        string current,
        string korean,
        bool expected)
    {
        Assert.Equal(expected, TextReplacementPolicy.ShouldReplace(source, current, korean));
    }
}
