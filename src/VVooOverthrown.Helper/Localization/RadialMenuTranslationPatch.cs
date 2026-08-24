using HarmonyLib;
using TMPro;

namespace VVooOverthrown.Helper.Localization;

[HarmonyPatch(typeof(RadialMenu), nameof(RadialMenu.AddOption))]
internal static class RadialMenuOptionTranslationPatch
{
    private static void Prefix(ref string name, ref string subtitle, ref string description)
    {
        TranslateSafely(ref name);
        TranslateSafely(ref subtitle);
        TranslateSafely(ref description);
    }

    private static void TranslateSafely(ref string value)
    {
        try
        {
            TmpTextTranslationPatch.TranslateExact(ref value);
        }
        catch
        {
            // One malformed option value must not prevent the menu from opening.
        }
    }
}

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

        KoreanFontProvider.EnsureFallback(text);
        var original = text.text;
        var translated = original;
        TmpTextTranslationPatch.TranslateExact(text, ref translated);
        if (!string.Equals(original, translated, StringComparison.Ordinal))
        {
            text.text = translated;
        }
    }
}
