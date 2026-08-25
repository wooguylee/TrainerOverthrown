using VVooOverthrown.Helper.Features;
using Xunit;

namespace VVooOverthrown.Helper.Tests;

public sealed class ResourceMutationVerifierTests
{
    [Fact]
    public void ExactObservedAmountIsSuccessful()
    {
        var result = ResourceMutationVerifier.Verify(250, 250);

        Assert.True(result.IsExact);
        Assert.Equal(string.Empty, result.ErrorCode);
    }

    [Fact]
    public void PartialObservedAmountReturnsExplicitFailure()
    {
        var result = ResourceMutationVerifier.Verify(250, 200);

        Assert.False(result.IsExact);
        Assert.Equal("RESOURCE_PARTIAL_APPLY", result.ErrorCode);
        Assert.Contains("200", result.Message);
        Assert.Contains("250", result.Message);
    }
}
