using System.Text.Json;
using VVooOverthrown.LocalizationTool.Cli;
using Xunit;

namespace VVooOverthrown.LocalizationTool.Tests;

public sealed class LocalizationCommandTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"vvoo-loc-{Guid.NewGuid():N}");

    public LocalizationCommandTests() => Directory.CreateDirectory(_root);

    [Fact]
    public void ValidateWritesCoverageForReviewedAndPendingEntries()
    {
        var sourcePath = WriteJson("source.json", new
        {
            entries = new[]
            {
                new { id = "UI/MENU_Settings", source = "Settings" },
                new { id = "UI/MENU_Count", source = "Count: {0}" },
            },
        });
        var koreanPath = WriteJson("ko.json", new
        {
            entries = new[]
            {
                new { id = "UI/MENU_Settings", korean = "설정", status = "reviewed" },
                new { id = "UI/MENU_Count", korean = "", status = "pending" },
            },
        });
        var coveragePath = Path.Combine(_root, "coverage.json");
        var output = new StringWriter();
        var error = new StringWriter();

        var exitCode = LocalizationCommand.Run(
            ["validate", sourcePath, koreanPath, coveragePath], output, error);

        Assert.Equal(0, exitCode);
        using var document = JsonDocument.Parse(File.ReadAllText(coveragePath));
        Assert.Equal(2, document.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Equal(1, document.RootElement.GetProperty("reviewedCount").GetInt32());
        Assert.Equal(0, document.RootElement.GetProperty("errorCount").GetInt32());
        Assert.Contains("검증 완료", output.ToString());
        Assert.Empty(error.ToString());
    }

    [Fact]
    public void ValidateFailsWhenReviewedTranslationDropsPlaceholder()
    {
        var sourcePath = WriteJson("source.json", new
        {
            entries = new[] { new { id = "UI/MENU_Count", source = "Count: {0}" } },
        });
        var koreanPath = WriteJson("ko.json", new
        {
            entries = new[]
            {
                new { id = "UI/MENU_Count", korean = "수량", status = "reviewed" },
            },
        });
        var coveragePath = Path.Combine(_root, "coverage.json");
        var error = new StringWriter();

        var exitCode = LocalizationCommand.Run(
            ["validate", sourcePath, koreanPath, coveragePath], TextWriter.Null, error);

        Assert.Equal(2, exitCode);
        Assert.Contains("PLACEHOLDER_MISMATCH", error.ToString());
    }

    private string WriteJson(string name, object value)
    {
        var path = Path.Combine(_root, name);
        File.WriteAllText(path, JsonSerializer.Serialize(value));
        return path;
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);
}
