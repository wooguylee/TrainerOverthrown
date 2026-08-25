using Il2CppStringTableList = Il2CppSystem.Collections.Generic.IList<UnityEngine.Localization.Tables.StringTable>;
using Il2CppStringTableCollection = Il2CppSystem.Collections.Generic.ICollection<UnityEngine.Localization.Tables.StringTable>;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace VVooOverthrown.Helper.Localization;

internal sealed class StringTableLocalizationBootstrap
{
    private readonly TranslationCatalog _catalog;
    private AsyncOperationHandle<Il2CppStringTableList> _tableLoad;
    private bool _tableLoadStarted;
    private bool _finished;

    public StringTableLocalizationBootstrap(TranslationCatalog catalog)
    {
        _catalog = catalog;
    }

    public bool TryAdvance(out StringTableLocalizationResult result)
    {
        result = default;
        if (_finished)
        {
            return false;
        }

        try
        {
            if (!_tableLoadStarted)
            {
                var initialization = LocalizationSettings.InitializationOperation;
                if (!initialization.IsDone)
                {
                    return false;
                }

                if (initialization.Status != AsyncOperationStatus.Succeeded)
                {
                    return FinishFailure("INITIALIZATION_FAILED", out result);
                }

                var database = LocalizationSettings.StringDatabase;
                var locale = LocalizationSettings.SelectedLocale;
                if (database is null || locale is null)
                {
                    return FinishFailure("LOCALIZATION_UNAVAILABLE", out result);
                }

                _tableLoad = database.GetAllTables(locale);
                _tableLoadStarted = true;
                return false;
            }

            if (!_tableLoad.IsDone)
            {
                return false;
            }

            if (_tableLoad.Status != AsyncOperationStatus.Succeeded || _tableLoad.Result is null)
            {
                return FinishFailure("TABLE_LOAD_FAILED", out result);
            }

            var replacements = 0;
            var alreadyLocalized = 0;
            var missing = 0;
            var matchedTables = 0;
            var tables = _tableLoad.Result;
            var tableCount = tables.Cast<Il2CppStringTableCollection>().Count;
            for (var tableIndex = 0; tableIndex < tableCount; tableIndex++)
            {
                var table = tables[tableIndex];
                if (table is null ||
                    !_catalog.TryGetTableTranslations(table.TableCollectionName, out var translations))
                {
                    continue;
                }

                matchedTables++;
                for (var translationIndex = 0; translationIndex < translations.Count; translationIndex++)
                {
                    var translation = translations[translationIndex];
                    var entry = table.GetEntry(translation.KeyId);
                    if (entry is null)
                    {
                        missing++;
                        continue;
                    }

                    if (string.Equals(entry.Value, translation.Korean, StringComparison.Ordinal))
                    {
                        alreadyLocalized++;
                        continue;
                    }

                    entry.Value = translation.Korean;
                    replacements++;
                }
            }

            _finished = true;
            result = new StringTableLocalizationResult(
                true,
                replacements,
                alreadyLocalized,
                missing,
                matchedTables,
                string.Empty);
            return true;
        }
        catch (Exception exception)
        {
            return FinishFailure(exception.GetType().Name, out result);
        }
    }

    private bool FinishFailure(string failure, out StringTableLocalizationResult result)
    {
        _finished = true;
        result = new StringTableLocalizationResult(false, 0, 0, 0, 0, failure);
        return true;
    }
}

internal readonly record struct StringTableLocalizationResult(
    bool Success,
    int Replacements,
    int AlreadyLocalized,
    int Missing,
    int MatchedTables,
    string Failure);
