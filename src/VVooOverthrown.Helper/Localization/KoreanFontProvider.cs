using TMPro;

namespace VVooOverthrown.Helper.Localization;

internal static class KoreanFontProvider
{
    private static TMP_FontAsset _fallback;
    private static readonly HashSet<IntPtr> ConfiguredFontPointers = new();

    public static bool ContainsKorean(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        foreach (var character in value)
        {
            if (character is >= '\u1100' and <= '\u11FF'
                or >= '\u3130' and <= '\u318F'
                or >= '\uA960' and <= '\uA97F'
                or >= '\uAC00' and <= '\uD7AF'
                or >= '\uD7B0' and <= '\uD7FF')
            {
                return true;
            }
        }

        return false;
    }

    public static void EnsureFallback(TMP_Text text)
    {
        var font = text.font;
        if (font is null)
        {
            return;
        }

        var fontPointer = font.Pointer;
        if (fontPointer != IntPtr.Zero && ConfiguredFontPointers.Contains(fontPointer))
        {
            return;
        }

        _fallback ??= TMP_FontAsset.CreateFontAsset("Malgun Gothic", "Regular", 90);
        if (_fallback is null)
        {
            return;
        }

        var fallbacks = font.fallbackFontAssetTable;
        if (fallbacks is null)
        {
            fallbacks = new Il2CppSystem.Collections.Generic.List<TMP_FontAsset>();
            font.fallbackFontAssetTable = fallbacks;
        }

        if (!fallbacks.Contains(_fallback))
        {
            fallbacks.Add(_fallback);
        }

        if (fontPointer != IntPtr.Zero)
        {
            ConfiguredFontPointers.Add(fontPointer);
        }
    }
}
