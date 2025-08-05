using BlackMagicAPI.Helpers;
using BlackMagicAPI.Managers;
using HarmonyLib;
using System.Reflection;

namespace BlackMagicAPI.Patches.Items;

[HarmonyPatch(typeof(ChestNetController))]
internal class ChestNetControllerPatch
{
    [HarmonyPatch(nameof(ChestNetController.Awake))]
    [HarmonyPrefix]
    private static void Awake_Prefix(ChestNetController __instance)
    {
        var list = __instance.Items.ToList();
        foreach (var map in ItemManager.Mapping.OrderBy(m => m.data.Id))
        {
            if (!map.data.CanSpawnInTeamChest) continue;
            list.Add(map.behavior.gameObject);
        }
        foreach (var map in SpellManager.Mapping.OrderBy(m => m.data.Id))
        {
            if (!map.data.CanSpawnInTeamChest) continue;
            list.Add(map.page.gameObject);
        }
        __instance.Items = [.. list];
    }

    [HarmonyPatch]
    private static class PiseverPatch
    {
        private static MethodBase TargetMethod() => Utils.PatchRpcMethod<ChestNetController>("RpcLogic___pisever");

        [HarmonyPrefix]
        private static void Prefix(ChestNetController __instance)
        {
            if (__instance.hasBeenOpened) return;

            foreach (var map in ItemManager.Mapping.OrderBy(m => m.data.Id))
            {
                if (map.data.DebugForceSpawn)
                {
                    var gameObject = UnityEngine.Object.Instantiate(map.behavior.gameObject);
                    gameObject.transform.position = __instance.ItemPoints[__instance.slotnum].transform.position;
                    __instance.ServerManager.Spawn(gameObject, null, default);

                    if (__instance.slotnum >= 4)
                        __instance.slotnum = 0;
                    else
                        __instance.slotnum++;
                }
            }

            foreach (var map in SpellManager.Mapping.OrderBy(m => m.data.Id))
            {
                if (map.data.DebugForceSpawn)
                {
                    var gameObject = UnityEngine.Object.Instantiate(map.page.gameObject);
                    gameObject.transform.position = __instance.ItemPoints[__instance.slotnum].transform.position;
                    __instance.ServerManager.Spawn(gameObject, null, default);

                    if (__instance.slotnum >= 4)
                        __instance.slotnum = 0;
                    else
                        __instance.slotnum++;
                }
            }
        }
    }
}