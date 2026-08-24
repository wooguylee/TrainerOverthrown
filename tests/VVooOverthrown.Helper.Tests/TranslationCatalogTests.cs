using VVooOverthrown.Helper.Localization;
using Xunit;

namespace VVooOverthrown.Helper.Tests;

public sealed class TranslationCatalogTests
{
    [Fact]
    public void UsesOnlyReviewedEntriesAndExactOrdinalSource()
    {
        const string source = """
            {"entries":[
              {"id":"UI/MENU_Settings","source":"Settings"},
              {"id":"UI/MENU_Loading","source":"Loading..."}
            ]}
            """;
        const string korean = """
            {"entries":[
              {"id":"UI/MENU_Settings","korean":"설정","status":"reviewed"},
              {"id":"UI/MENU_Loading","korean":"불러오는 중...","status":"pending"}
            ]}
            """;

        var catalog = TranslationCatalog.Load(source, korean);

        Assert.True(catalog.TryTranslate("Settings", out var translated));
        Assert.Equal("설정", translated);
        Assert.False(catalog.TryTranslate("settings", out _));
        Assert.False(catalog.TryTranslate("Loading...", out _));
    }

    [Fact]
    public void ConflictingKoreanForSameSourceIsRejected()
    {
        const string source = """
            {"entries":[
              {"id":"UI/A","source":"Back"},
              {"id":"UI/B","source":"Back"}
            ]}
            """;
        const string korean = """
            {"entries":[
              {"id":"UI/A","korean":"뒤로","status":"reviewed"},
              {"id":"UI/B","korean":"돌아가기","status":"reviewed"}
            ]}
            """;

        Assert.Throws<InvalidDataException>(() => TranslationCatalog.Load(source, korean));
    }

    [Fact]
    public void InvalidJsonCanDisableTranslationWithoutThrowing()
    {
        var loaded = TranslationCatalog.TryLoad("not-json", "{}", out var catalog);

        Assert.False(loaded);
        Assert.Null(catalog);
    }
}
