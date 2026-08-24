using System.Text.Json;

namespace VVooOverthrown.Helper.Localization;

public sealed class TranslationCatalog
{
    private readonly IReadOnlyDictionary<string, string> _bySource;

    private TranslationCatalog(IReadOnlyDictionary<string, string> bySource) =>
        _bySource = bySource;

    public int Count => _bySource.Count;

    public bool TryTranslate(string source, out string korean) =>
        _bySource.TryGetValue(source, out korean!);

    public static bool TryLoad(
        string sourceJson,
        string koreanJson,
        out TranslationCatalog? catalog)
    {
        try
        {
            catalog = Load(sourceJson, koreanJson);
            return true;
        }
        catch
        {
            catalog = null;
            return false;
        }
    }

    public static TranslationCatalog Load(string sourceJson, string koreanJson)
    {
        using var sourceDocument = JsonDocument.Parse(sourceJson);
        using var koreanDocument = JsonDocument.Parse(koreanJson);

        var sourceById = sourceDocument.RootElement.GetProperty("entries")
            .EnumerateArray()
            .ToDictionary(
                entry => entry.GetProperty("id").GetString() ?? string.Empty,
                entry => entry.GetProperty("source").GetString() ?? string.Empty,
                StringComparer.Ordinal);

        var bySource = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var target in koreanDocument.RootElement.GetProperty("entries").EnumerateArray())
        {
            if (!string.Equals(
                    target.GetProperty("status").GetString(),
                    "reviewed",
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var id = target.GetProperty("id").GetString() ?? string.Empty;
            var korean = target.GetProperty("korean").GetString() ?? string.Empty;
            if (!sourceById.TryGetValue(id, out var source) ||
                string.IsNullOrEmpty(source) ||
                string.IsNullOrEmpty(korean))
            {
                continue;
            }

            if (bySource.TryGetValue(source, out var previous) &&
                !previous.Equals(korean, StringComparison.Ordinal))
            {
                throw new InvalidDataException($"동일 원문에 충돌하는 번역이 있습니다: {source}");
            }

            bySource[source] = korean;
        }

        return new TranslationCatalog(bySource);
    }
}
