using VVooOverthrown.LocalizationTool.Models;
using VVooOverthrown.LocalizationTool.Validation;
using Xunit;

namespace VVooOverthrown.LocalizationTool.Tests;

public sealed class TranslationValidatorTests
{
    private readonly TranslationValidator _validator = new();

    [Fact]
    public void RejectsDroppedFormatPlaceholder()
    {
        var result = _validator.Validate([
            new TranslationEntry("ui.count", "Count: {0}", "수량", "reviewed")
        ]);

        Assert.Contains(result.Errors, error => error.Code == "PLACEHOLDER_MISMATCH");
    }

    [Fact]
    public void RejectsChangedRichTextTags()
    {
        var result = _validator.Validate([
            new TranslationEntry("ui.warning", "<b>Warning</b>", "경고", "reviewed")
        ]);

        Assert.Contains(result.Errors, error => error.Code == "TAG_MISMATCH");
    }

    [Fact]
    public void RejectsDuplicateIds()
    {
        var result = _validator.Validate([
            new TranslationEntry("menu.settings", "Settings", "설정", "reviewed"),
            new TranslationEntry("menu.settings", "Options", "옵션", "reviewed")
        ]);

        Assert.Contains(result.Errors, error => error.Code == "DUPLICATE_ID");
    }

    [Fact]
    public void CountsValidReviewedAndPendingEntries()
    {
        var result = _validator.Validate([
            new TranslationEntry("menu.settings", "Settings", "설정", "reviewed"),
            new TranslationEntry("menu.exit", "Exit", "", "pending")
        ]);

        Assert.Empty(result.Errors);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(1, result.ReviewedCount);
        Assert.Equal(1, result.PendingCount);
    }
}

