using BlackMagicAPI.Managers;
using HarmonyLib;

namespace BlackMagicAPI.Patches.Generation;

[HarmonyPatch(typeof(DungeonGenerator))]
internal class DungeonGeneratorPatch
{
    [HarmonyPatch(nameof(DungeonGenerator.Start))]
    [HarmonyPrefix]
    private static void Start_Prefix(DungeonGenerator __instance)
    {
        var lt = __instance.GetComponent<PageLootTable>();
        if (lt != null)
        {
            var list = lt.Pages.ToList();
            foreach (var page in SpellManager.PagePrefabs)
            {
                list.Add(page.gameObject);
            }
            lt.Pages = [.. list];
        }
    }
}
