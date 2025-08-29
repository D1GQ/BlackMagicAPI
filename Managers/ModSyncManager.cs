using BepInEx;
using System.Collections;
using UnityEngine;
using static MageArena_StealthSpells.ModSyncUI;

namespace BlackMagicAPI.Managers;

internal class ModSyncManager
{
    internal static List<(BaseUnityPlugin plugin, Type type)> FailedSpells = [];
    internal static List<(BaseUnityPlugin plugin, Type type)> FailedItems = [];
    internal static List<(BaseUnityPlugin plugin, Type type, Type type2, Type type3)> FailedRecipes = [];
    internal static List<(BaseUnityPlugin plugin, Type type)> FailedSoups = [];

    internal static void LogAll()
    {
        BMAPlugin.Instance.StartCoroutine(CoLogAll());
    }

    private static IEnumerator CoLogAll()
    {
        float time;
        foreach (var (plugin, type) in FailedSpells)
        {
            time = 0.1f;

            ShowMessage($"BlackMagicAPI: {type.Name} spell from {plugin.Info.Metadata.Name} failed to load!", MessageType.Error);

            while (time > 0f)
            {
                time -= Time.deltaTime;
                yield return null;
            }
        }
        foreach (var (plugin, type) in FailedItems)
        {
            time = 0.1f;

            ShowMessage($"BlackMagicAPI: {type.Name} item from {plugin.Info.Metadata.Name} failed to load!", MessageType.Error);

            while (time > 0f)
            {
                time -= Time.deltaTime;
                yield return null;
            }
        }
        foreach (var (plugin, type, type2, type3) in FailedRecipes)
        {
            time = 0.1f;

            ShowMessage($"BlackMagicAPI: Recipe ({type.Name}, {type2.Name} -> {type3.Name}) from {plugin.Info.Metadata.Name} failed to load!", MessageType.Error);

            while (time > 0f)
            {
                time -= Time.deltaTime;
                yield return null;
            }
        }
        foreach (var (plugin, type) in FailedSoups)
        {
            time = 0.1f;

            ShowMessage($"BlackMagicAPI: {type.Name} soup from {plugin.Info.Metadata.Name} failed to load!", MessageType.Error);

            while (time > 0f)
            {
                time -= Time.deltaTime;
                yield return null;
            }
        }
    }
}
