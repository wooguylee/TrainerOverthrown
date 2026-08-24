namespace VVooOverthrown.LocalizationTool.Validation;

public sealed class TranslationValidationResult
{
    public TranslationValidationResult(
        IReadOnlyList<TranslationValidationError> errors,
        int totalCount,
        int reviewedCount,
        int pendingCount)
    {
        Errors = errors;
        TotalCount = totalCount;
        ReviewedCount = reviewedCount;
        PendingCount = pendingCount;
    }

    public IReadOnlyList<TranslationValidationError> Errors { get; }

    public int TotalCount { get; }

    public int ReviewedCount { get; }

    public int PendingCount { get; }
}

public sealed record TranslationValidationError(string Code, string EntryId, string Message);

