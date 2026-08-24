using System.Text.Json;
using VVooOverthrown.LocalizationTool.Models;
using VVooOverthrown.LocalizationTool.Validation;

namespace VVooOverthrown.LocalizationTool.Cli;

public static class LocalizationCommand
{
    private static readonly JsonSerializerOptions OutputJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length != 4 || !args[0].Equals("validate", StringComparison.OrdinalIgnoreCase))
        {
            error.WriteLine("사용법: validate <source.en.json> <ko.json> <coverage.json>");
            return 1;
        }

        try
        {
            var source = ReadSource(args[1]);
            var korean = ReadKorean(args[2]);
            var entries = source.Select(item =>
            {
                korean.TryGetValue(item.Id, out var target);
                return new TranslationEntry(
                    item.Id,
                    item.Source,
                    target?.Korean ?? string.Empty,
                    target?.Status ?? "pending");
            }).ToArray();

            var result = new TranslationValidator().Validate(entries);
            var report = new CoverageReport(
                1,
                result.TotalCount,
                result.ReviewedCount,
                result.PendingCount,
                result.Errors.Count,
                result.Errors);
            File.WriteAllText(
                args[3],
                JsonSerializer.Serialize(report, OutputJsonOptions) + Environment.NewLine);

            if (result.Errors.Count > 0)
            {
                foreach (var validationError in result.Errors)
                {
                    error.WriteLine($"{validationError.Code} [{validationError.EntryId}] {validationError.Message}");
                }

                return 2;
            }

            output.WriteLine(
                $"검증 완료: 전체 {result.TotalCount}, 검수 {result.ReviewedCount}, 대기 {result.PendingCount}, 오류 0");
            return 0;
        }
        catch (Exception exception)
        {
            error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static SourceItem[] ReadSource(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.GetProperty("entries")
            .EnumerateArray()
            .Select(element => new SourceItem(
                element.GetProperty("id").GetString() ?? string.Empty,
                element.GetProperty("source").GetString() ?? string.Empty))
            .ToArray();
    }

    private static Dictionary<string, KoreanItem> ReadKorean(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.GetProperty("entries")
            .EnumerateArray()
            .Select(element => new KoreanItem(
                element.GetProperty("id").GetString() ?? string.Empty,
                element.GetProperty("korean").GetString() ?? string.Empty,
                element.GetProperty("status").GetString() ?? "pending"))
            .ToDictionary(item => item.Id, StringComparer.Ordinal);
    }

    private sealed record SourceItem(string Id, string Source);

    private sealed record KoreanItem(string Id, string Korean, string Status);

    private sealed record CoverageReport(
        int SchemaVersion,
        int TotalCount,
        int ReviewedCount,
        int PendingCount,
        int ErrorCount,
        IReadOnlyList<TranslationValidationError> Errors);
}
