using HarmonyLib;
using TMPro;

namespace VVooOverthrown.Helper.Localization;

[HarmonyPatch(typeof(LoadingScreen), nameof(LoadingScreen.SetLoadingMessage))]
internal static class LoadingScreenTranslationPatch
{
    private static void Prefix(ref string message)
    {
        LoadingMessageTranslator.TranslateArgument(ref message);
    }

    private static void Postfix(LoadingScreen __instance)
    {
        try
        {
            LoadingMessageTranslator.TranslateDisplayed(__instance?.infoMessage);
        }
        catch
        {
            // A closing loading screen must not interrupt scene loading.
        }
    }
}

[HarmonyPatch(typeof(PreloadLoadingScreen), nameof(PreloadLoadingScreen.SetLoadingMessage))]
internal static class PreloadLoadingScreenTranslationPatch
{
    private static void Prefix(ref string message)
    {
        LoadingMessageTranslator.TranslateArgument(ref message);
    }

    private static void Postfix(PreloadLoadingScreen __instance)
    {
        try
        {
            LoadingMessageTranslator.TranslateDisplayed(__instance?.loadingMessage);
        }
        catch
        {
            // A closing preload screen must not interrupt application startup.
        }
    }
}

internal static class LoadingMessageTranslator
{
    public static void TranslateArgument(ref string message)
    {
        try
        {
            TmpTextTranslationPatch.TranslateExact(ref message);
        }
        catch
        {
            // Loading must continue even if a localized message is malformed.
        }
    }

    public static void TranslateDisplayed(TMP_Text text)
    {
        try
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
                return;
            }

            var separator = original.LastIndexOf(' ');
            if (separator <= 0)
            {
                return;
            }

            var suffix = original[(separator + 1)..];
            if (suffix.Length < 2 || suffix[^1] != '%' ||
                !int.TryParse(suffix[..^1], out var progress) ||
                progress is < 0 or > 100)
            {
                return;
            }

            var stage = original[..separator];
            var translatedStage = stage;
            TmpTextTranslationPatch.TranslateExact(text, ref translatedStage);
            if (!string.Equals(stage, translatedStage, StringComparison.Ordinal))
            {
                text.text = translatedStage + original[separator..];
            }
        }
        catch
        {
            // Loading UI failures must never interrupt scene loading.
        }
    }
}
