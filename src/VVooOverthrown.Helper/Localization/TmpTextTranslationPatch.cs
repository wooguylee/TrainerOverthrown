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
            var catalog = Catalog;
            if (catalog is null || string.IsNullOrEmpty(value) ||
                !catalog.TryTranslate(value, out var korean) ||
                !TextReplacementPolicy.ShouldReplace(value, value, korean))
            {
                return;
            }

            KoreanFontProvider.EnsureFallback(__instance);
            value = korean;
        }
        catch
        {
            // A single malformed TMP component must not affect the game or trainer runtime.
        }
    }
}
