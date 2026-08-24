using HarmonyLib;
using TMPro;

namespace VVooOverthrown.Helper.Localization;

[HarmonyPatch(typeof(TMP_Text), "set_text")]
internal static class TmpTextTranslationPatch
{
    internal static TranslationCatalog Catalog { get; set; }

    private static void Prefix(TMP_Text __instance, ref string value)
    {
        try
        {
            Translate(__instance, ref value);
        }
        catch
        {
            // A single malformed TMP component must not affect the game or trainer runtime.
        }
    }

    internal static void Translate(TMP_Text text, ref string value)
    {
        Translate(text, ref value, exactOnly: false);
    }

    internal static void TranslateExact(TMP_Text text, ref string value)
    {
        Translate(text, ref value, exactOnly: true);
    }

    private static void Translate(TMP_Text text, ref string value, bool exactOnly)
    {
        var catalog = Catalog;
        if (catalog is null || text is null || string.IsNullOrEmpty(value))
        {
            return;
        }

        string korean;
        var found = exactOnly
            ? catalog.TryTranslateExact(value, out korean)
            : catalog.TryTranslate(value, out korean);
        if (!found || !TextReplacementPolicy.ShouldReplace(value, value, korean))
        {
            return;
        }

        KoreanFontProvider.EnsureFallback(text);
        value = korean;
    }
}
