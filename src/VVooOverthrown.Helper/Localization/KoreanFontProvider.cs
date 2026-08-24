using TMPro;

namespace VVooOverthrown.Helper.Localization;

internal static class KoreanFontProvider
{
    private static TMP_FontAsset _fallback;

    public static void EnsureFallback(TMP_Text text)
    {
        if (text.font is null)
        {
            return;
        }

        _fallback ??= TMP_FontAsset.CreateFontAsset("Malgun Gothic", "Regular", 90);
        if (_fallback is null)
        {
            return;
        }

        var fallbacks = text.font.fallbackFontAssetTable;
        if (fallbacks is null)
        {
            fallbacks = new Il2CppSystem.Collections.Generic.List<TMP_FontAsset>();
            text.font.fallbackFontAssetTable = fallbacks;
        }

        if (!fallbacks.Contains(_fallback))
        {
            fallbacks.Add(_fallback);
        }
    }
}
