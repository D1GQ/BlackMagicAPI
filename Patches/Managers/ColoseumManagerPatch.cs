using BlackMagicAPI.Managers;
using HarmonyLib;

namespace BlackMagicAPI.Patches.Managers;

[HarmonyPatch(typeof(ColoseumManager))]
internal class ColoseumManagerPatch
{
    [HarmonyPatch(nameof(ColoseumManager.OnStartClient))]
    [HarmonyPrefix]
    private static void Start_Prefix(ColoseumManager __instance)
    {
        foreach (var (data, page) in SpellManager.Mapping)
        {
            if (!data.CanSpawnInColoseum) continue;
            __instance.LootTable.Add(page.gameObject);
        }

        foreach (var (data, behavior) in ItemManager.Mapping.OrderBy(map => map.data.Id))
        {
            if (!data.CanSpawnInColoseum) continue;
            __instance.LootTable.Add(behavior.gameObject);
        }
    }
}
