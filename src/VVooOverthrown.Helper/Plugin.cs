using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using VVooOverthrown.Helper.Localization;
using VVooOverthrown.Helper.Runtime;
using VVooOverthrown.Helper.Safety;

namespace VVooOverthrown.Helper;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BasePlugin
{
    public const string PluginGuid = "local.vvoooverthrown.helper";
    public const string PluginName = "VVooOverthrown Helper";
    public const string PluginVersion = "0.1.0";

    private Harmony _harmony;
    private RuntimeHost _runtimeHost;

    public override void Load()
    {
        if (!RuntimeBuildGuard.Current.IsSupported(Paths.GameRootPath, out var buildReason))
        {
            Log.LogError($"Unsupported Overthrown build; Helper disabled ({buildReason}).");
            return;
        }

        var assemblyRoot = Path.GetDirectoryName(typeof(Plugin).Assembly.Location)
                           ?? throw new InvalidOperationException("Helper 경로를 확인할 수 없습니다.");
        try
        {
            var sourceJson = File.ReadAllText(Path.Combine(assemblyRoot, "translation", "source.en.json"));
            var koreanJson = File.ReadAllText(Path.Combine(assemblyRoot, "translation", "ko.json"));
            if (!TranslationCatalog.TryLoad(sourceJson, koreanJson, out var catalog))
            {
                Log.LogWarning("Korean translation catalog is invalid; localization disabled.");
            }
            TmpTextTranslationPatch.Catalog = catalog;
        }
        catch (Exception exception)
        {
            TmpTextTranslationPatch.Catalog = null;
            Log.LogWarning($"Korean localization disabled: {exception.GetType().Name}");
        }

        _harmony = new Harmony(PluginGuid);
        _harmony.PatchAll(typeof(Plugin).Assembly);
        _runtimeHost = AddComponent<RuntimeHost>();
        _runtimeHost.Initialize(Log, TmpTextTranslationPatch.Catalog);
        Log.LogInfo(
            $"VVooOverthrown helper loaded; " +
            $"translations={TmpTextTranslationPatch.Catalog?.Count ?? 0}, " +
            $"tableEntries={TmpTextTranslationPatch.Catalog?.TableTranslationCount ?? 0}");
    }

    public override bool Unload()
    {
        _harmony?.UnpatchSelf();
        if (_runtimeHost != null)
        {
            UnityEngine.Object.Destroy(_runtimeHost);
        }
        TmpTextTranslationPatch.Catalog = null;
        return true;
    }
}
