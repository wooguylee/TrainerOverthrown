using TMPro;

namespace VVooOverthrown.Helper.Localization;

internal static class RadialMenuLocalizationMonitor
{
    public static int Translate(RadialMenuDynamicWheel wheel)
    {
        if (wheel is null)
        {
            return 0;
        }

        var replacements = TranslateOptions(wheel);
        replacements += TranslateDisplayedText(wheel.title) ? 1 : 0;
        replacements += TranslateDisplayedText(wheel.subtitle) ? 1 : 0;
        return replacements;
    }

    private static int TranslateOptions(RadialMenuDynamicWheel wheel)
    {
        var options = wheel.options;
        if (options is null)
        {
            return 0;
        }

        var replacements = 0;
        for (var index = 0; index < options.Count; index++)
        {
            try
            {
                replacements += TranslateOption(options[index]);
            }
            catch
            {
                // One closing or malformed option must not block the remaining menu entries.
            }
        }

        return replacements;
    }

    private static int TranslateOption(RadialMenuDynamicOption option)
    {
        if (option is null)
        {
            return 0;
        }

        var replacements = 0;
        var originalTitle = option.title;
        var translatedTitle = TranslateField(originalTitle);
        if (!string.Equals(originalTitle, translatedTitle, StringComparison.Ordinal))
        {
            option.title = translatedTitle;
            replacements++;
        }

        var originalSubtitle = option.subtitle;
        var translatedSubtitle = TranslateField(originalSubtitle);
        if (!string.Equals(originalSubtitle, translatedSubtitle, StringComparison.Ordinal))
        {
            option.subtitle = translatedSubtitle;
            replacements++;
        }

        var originalDescription = option.description;
        var translatedDescription = TranslateField(originalDescription);
        if (!string.Equals(originalDescription, translatedDescription, StringComparison.Ordinal))
        {
            option.description = translatedDescription;
            replacements++;
        }

        return replacements;
    }

    private static string TranslateField(string original)
    {
        var translated = original;
        TmpTextTranslationPatch.TranslateExact(ref translated);
        return translated;
    }

    private static bool TranslateDisplayedText(TMP_Text text)
    {
        if (text is null)
        {
            return false;
        }

        KoreanFontProvider.EnsureFallback(text);
        var original = text.text;
        var translated = original;
        TmpTextTranslationPatch.TranslateExact(ref translated);
        if (!string.Equals(original, translated, StringComparison.Ordinal))
        {
            text.text = translated;
            return true;
        }

        return false;
    }
}
