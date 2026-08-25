using System.Text.Json;
using System.Text;
using System.Text.RegularExpressions;

namespace VVooOverthrown.Helper.Localization;

public sealed class TranslationCatalog
{
    private static readonly Regex HoldMarkerPattern = new(
        @"\[HOLD\]",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly Regex FormattingGapPattern = new(
        @"^(?:\s|<[^>]+>)*$",
        RegexOptions.CultureInvariant);

    private readonly IReadOnlyDictionary<string, string> _bySource;
    private readonly IReadOnlyDictionary<string, IReadOnlyList<TableTranslation>> _byTable;
    private readonly IReadOnlyList<KeyValuePair<string, string>> _sourcesByLength;
    private readonly IReadOnlyList<TemplateTranslation> _templates;

    private TranslationCatalog(
        IReadOnlyDictionary<string, string> bySource,
        IReadOnlyList<TableTranslation> tableTranslations)
    {
        _bySource = bySource;
        _byTable = tableTranslations
            .GroupBy(entry => entry.Table, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<TableTranslation>)group.ToArray(),
                StringComparer.Ordinal);
        _sourcesByLength = bySource
            .OrderByDescending(entry => entry.Key.Length)
            .ToArray();
        _templates = bySource
            .Where(entry => entry.Key.Contains('{'))
            .Select(entry => TemplateTranslation.Create(entry.Key, entry.Value))
            .Where(template => template is not null)
            .Cast<TemplateTranslation>()
            .OrderByDescending(template => template.LiteralCharacterCount)
            .ThenBy(template => template.PlaceholderCount)
            .ToArray();
    }

    public int Count => _bySource.Count;

    public int TableTranslationCount => _byTable.Values.Sum(entries => entries.Count);

    public bool TryTranslateExact(string source, out string korean) =>
        _bySource.TryGetValue(source, out korean!);

    public bool TryGetTableTranslations(
        string table,
        out IReadOnlyList<TableTranslation> translations)
    {
        if (_byTable.TryGetValue(table, out var found))
        {
            translations = found;
            return true;
        }

        translations = Array.Empty<TableTranslation>();
        return false;
    }

    public bool TryTranslate(string source, out string korean)
    {
        if (TryTranslateExact(source, out korean))
        {
            return true;
        }

        // Once reviewed StringTables are localized at startup, most TMP writes already contain
        // Korean. Avoid running every dynamic regex against those values or numeric-only labels.
        if (!CouldContainRuntimeSource(source))
        {
            korean = string.Empty;
            return false;
        }

        foreach (var template in _templates)
        {
            if (template.TryTranslate(source, out korean))
            {
                return true;
            }
        }

        if (TryTranslateHoldPrompt(source, out korean))
        {
            return true;
        }

        korean = string.Empty;
        return false;
    }

    private static bool CouldContainRuntimeSource(string source)
    {
        var containsAsciiLetter = false;
        foreach (var character in source)
        {
            if (character is >= '\u1100' and <= '\u11FF' or
                >= '\u3130' and <= '\u318F' or
                >= '\uA960' and <= '\uA97F' or
                >= '\uAC00' and <= '\uD7AF' or
                >= '\uD7B0' and <= '\uD7FF')
            {
                return false;
            }

            containsAsciiLetter |= character is >= 'A' and <= 'Z' or >= 'a' and <= 'z';
        }

        return containsAsciiLetter;
    }

    private bool TryTranslateHoldPrompt(string source, out string korean)
    {
        var marker = HoldMarkerPattern.Match(source);
        if (!marker.Success)
        {
            korean = string.Empty;
            return false;
        }

        foreach (var entry in _sourcesByLength)
        {
            if (!source.StartsWith(entry.Key, StringComparison.Ordinal) ||
                marker.Index < entry.Key.Length ||
                !FormattingGapPattern.IsMatch(source[entry.Key.Length..marker.Index]) ||
                !FormattingGapPattern.IsMatch(source[(marker.Index + marker.Length)..]))
            {
                continue;
            }

            korean = entry.Value +
                     source[entry.Key.Length..marker.Index] +
                     "[길게 누르기]" +
                     source[(marker.Index + marker.Length)..];
            return true;
        }

        korean = string.Empty;
        return false;
    }

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
                entry => new SourceEntry(
                    entry.GetProperty("source").GetString() ?? string.Empty,
                    entry.TryGetProperty("table", out var table)
                        ? table.GetString() ?? string.Empty
                        : string.Empty,
                    entry.TryGetProperty("keyId", out var keyId) && keyId.TryGetInt64(out var parsedKeyId)
                        ? parsedKeyId
                        : null),
                StringComparer.Ordinal);

        var bySource = new Dictionary<string, string>(StringComparer.Ordinal);
        var tableTranslations = new List<TableTranslation>();
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
            if (!sourceById.TryGetValue(id, out var sourceEntry) ||
                string.IsNullOrEmpty(sourceEntry.Source) ||
                string.IsNullOrEmpty(korean))
            {
                continue;
            }

            if (bySource.TryGetValue(sourceEntry.Source, out var previous) &&
                !previous.Equals(korean, StringComparison.Ordinal))
            {
                throw new InvalidDataException($"동일 원문에 충돌하는 번역이 있습니다: {sourceEntry.Source}");
            }

            bySource[sourceEntry.Source] = korean;
            if (!string.IsNullOrEmpty(sourceEntry.Table) && sourceEntry.KeyId.HasValue)
            {
                tableTranslations.Add(new TableTranslation(
                    sourceEntry.Table,
                    sourceEntry.KeyId.Value,
                    sourceEntry.Source,
                    korean));
            }
        }

        return new TranslationCatalog(bySource, tableTranslations);
    }

    private sealed record SourceEntry(string Source, string Table, long? KeyId);

    private sealed class TemplateTranslation
    {
        private static readonly Regex PlaceholderPattern = new(
            @"\{[^{}]+\}",
            RegexOptions.CultureInvariant);

        private readonly Regex _sourcePattern;
        private readonly string _koreanTemplate;
        private readonly IReadOnlyDictionary<string, string> _groupByToken;

        public int LiteralCharacterCount { get; }

        public int PlaceholderCount { get; }

        private TemplateTranslation(
            Regex sourcePattern,
            string koreanTemplate,
            IReadOnlyDictionary<string, string> groupByToken,
            int literalCharacterCount,
            int placeholderCount)
        {
            _sourcePattern = sourcePattern;
            _koreanTemplate = koreanTemplate;
            _groupByToken = groupByToken;
            LiteralCharacterCount = literalCharacterCount;
            PlaceholderCount = placeholderCount;
        }

        public static TemplateTranslation? Create(string source, string korean)
        {
            var matches = PlaceholderPattern.Matches(source);
            if (matches.Count == 0)
            {
                return null;
            }

            var pattern = new StringBuilder("^");
            var groupByToken = new Dictionary<string, string>(StringComparer.Ordinal);
            var cursor = 0;
            for (var index = 0; index < matches.Count; index++)
            {
                var placeholder = matches[index];
                pattern.Append(Regex.Escape(source[cursor..placeholder.Index]));
                var groupName = $"value{index}";
                pattern.Append($"(?<{groupName}>{GetCapturePattern(placeholder.Value)})");
                groupByToken.TryAdd(placeholder.Value, groupName);
                cursor = placeholder.Index + placeholder.Length;
            }
            pattern.Append(Regex.Escape(source[cursor..]));
            pattern.Append('$');

            return new TemplateTranslation(
                new Regex(pattern.ToString(), RegexOptions.CultureInvariant),
                korean,
                groupByToken,
                source.Length - matches.Sum(match => match.Length),
                matches.Count);
        }

        public bool TryTranslate(string source, out string korean)
        {
            var match = _sourcePattern.Match(source);
            if (!match.Success)
            {
                korean = string.Empty;
                return false;
            }

            korean = PlaceholderPattern.Replace(
                _koreanTemplate,
                placeholder => _groupByToken.TryGetValue(placeholder.Value, out var groupName)
                    ? LocalizeCapturedToken(placeholder.Value, match.Groups[groupName].Value)
                    : placeholder.Value);
            return true;
        }

        private static string GetCapturePattern(string token)
        {
            if (token.Contains(":plural:hour|hours", StringComparison.Ordinal))
            {
                return "(?:hours|hour)";
            }
            if (token.Contains(":plural:day|days", StringComparison.Ordinal))
            {
                return "(?:days|day)";
            }
            if (token.Contains(":plural:second|seconds", StringComparison.Ordinal))
            {
                return "(?:seconds|second)";
            }
            return ".+?";
        }

        private static string LocalizeCapturedToken(string token, string capturedValue)
        {
            if (token.Contains(":plural:hour|hours", StringComparison.Ordinal))
            {
                return "시간";
            }
            if (token.Contains(":plural:day|days", StringComparison.Ordinal))
            {
                return "일";
            }
            if (token.Contains(":plural:second|seconds", StringComparison.Ordinal))
            {
                return "초";
            }
            return capturedValue;
        }
    }
}

public sealed record TableTranslation(string Table, long KeyId, string Source, string Korean);
