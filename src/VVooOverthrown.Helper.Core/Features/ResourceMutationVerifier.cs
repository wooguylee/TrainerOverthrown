namespace VVooOverthrown.Helper.Features;

public static class ResourceMutationVerifier
{
    public static ResourceMutationResult Verify(int requestedAmount, int observedAmount)
    {
        if (requestedAmount == observedAmount)
        {
            return ResourceMutationResult.Exact;
        }

        return new ResourceMutationResult(
            false,
            "RESOURCE_PARTIAL_APPLY",
            $"요청 수량 {requestedAmount:N0}을 정확히 적용하지 못했습니다. 실제 수량은 {observedAmount:N0}입니다.");
    }
}

public sealed record ResourceMutationResult(bool IsExact, string ErrorCode, string Message)
{
    public static ResourceMutationResult Exact { get; } = new(true, string.Empty, string.Empty);
}
