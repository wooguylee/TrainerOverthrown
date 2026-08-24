using System.Text.RegularExpressions;
using VVooOverthrown.LocalizationTool.Models;

namespace VVooOverthrown.LocalizationTool.Validation;

public sealed partial class TranslationValidator
{
    public TranslationValidationResult Validate(IReadOnlyList<TranslationEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        var errors = new List<TranslationValidationError>();
        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        var reviewedCount = 0;

        foreach (var entry in entries)
        {
            var id = entry.Id ?? string.Empty;
            var source = entry.Source ?? string.Empty;
            var korean = entry.Korean ?? string.Empty;
            var reviewed = entry.Status.Equals("reviewed", StringComparison.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(id))
            {
                errors.Add(new TranslationValidationError("EMPTY_ID", id, "번역 ID가 비어 있습니다."));
            }
            else if (!seenIds.Add(id))
            {
                errors.Add(new TranslationValidationError("DUPLICATE_ID", id, "중복 번역 ID입니다."));
            }

            if (string.IsNullOrEmpty(source))
            {
                errors.Add(new TranslationValidationError("EMPTY_SOURCE", id, "영어 원문이 비어 있습니다."));
            }

            if (!reviewed)
            {
                continue;
            }

            reviewedCount++;
            if (string.IsNullOrWhiteSpace(korean))
            {
                errors.Add(new TranslationValidationError("EMPTY_KOREAN", id, "검수된 한국어가 비어 있습니다."));
                continue;
            }

            if (!TokenMultisetEquals(PlaceholderRegex().Matches(source), PlaceholderRegex().Matches(korean)))
            {
                errors.Add(new TranslationValidationError(
                    "PLACEHOLDER_MISMATCH",
                    id,
                    "원문과 번역의 placeholder가 다릅니다."));
            }

            if (!TokenMultisetEquals(TagRegex().Matches(source), TagRegex().Matches(korean)))
            {
                errors.Add(new TranslationValidationError(
                    "TAG_MISMATCH",
                    id,
                    "원문과 번역의 TMP rich-text 태그가 다릅니다."));
            }
        }

        return new TranslationValidationResult(
            errors,
            entries.Count,
            reviewedCount,
            entries.Count - reviewedCount);
    }

    private static bool TokenMultisetEquals(MatchCollection left, MatchCollection right) =>
        left.Select(match => match.Value)
            .OrderBy(value => value, StringComparer.Ordinal)
            .SequenceEqual(
                right.Select(match => match.Value).OrderBy(value => value, StringComparer.Ordinal),
                StringComparer.Ordinal);

    [GeneratedRegex(@"\{[^{}]+\}", RegexOptions.CultureInvariant)]
    private static partial Regex PlaceholderRegex();

    [GeneratedRegex(@"</?[^<>]+?>", RegexOptions.CultureInvariant)]
    private static partial Regex TagRegex();
}

