using HarmonyLib;
using TMPro;

namespace VVooOverthrown.Helper.Localization;

[HarmonyPatch(typeof(RadialMenuDynamicWheel), nameof(RadialMenuDynamicWheel.RefreshInformation))]
internal static class RadialMenuTranslationPatch
{
    private static void Postfix(RadialMenuDynamicWheel __instance)
    {
        try
        {
            TranslateDisplayedText(__instance?.title);
        }
        catch
        {
            // A malformed title must not prevent the description from being translated.
        }

        try
        {
            TranslateDisplayedText(__instance?.subtitle);
        }
        catch
        {
            // A malformed description or closing radial menu must not affect the game runtime.
        }
    }

    private static void TranslateDisplayedText(TMP_Text text)
    {
        if (text is null)
        {
            return;
        }

        var original = text.text;
        var translated = original;
        TmpTextTranslationPatch.TranslateExact(text, ref translated);
        if (!string.Equals(original, translated, StringComparison.Ordinal))
        {
            text.text = translated;
        }
    }
}
