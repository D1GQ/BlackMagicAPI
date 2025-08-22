using BlackMagicAPI.Managers;
using HarmonyLib;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace BlackMagicAPI.Patches.Soups;

[Harmony]
internal class SoupManPatch
{
    [HarmonyPatch(typeof(SoupManController))]
    internal class SoupManControllerPatch
    {
        private static readonly ConditionalWeakTable<SoupManController, List<int>> usedIds = [];

        [HarmonyPatch(nameof(SoupManController.Start))]
        [HarmonyPostfix]
        private static void Start_Postfix(SoupManController __instance)
        {
            if (!usedIds.TryGetValue(__instance, out var controllerUsedIds))
            {
                controllerUsedIds = [];
                usedIds.Add(__instance, controllerUsedIds);
            }
            else
            {
                controllerUsedIds.Clear();
            }

            var prefabs = __instance.SoupPrefabs.ToList();
            var itemsDisplays = __instance.SoupItems.ToList();
            var mats = __instance.matas.ToList();
            int startId = __instance.SoupPrefabs.Length + 1;

            prefabs.Add(null);
            itemsDisplays.Add(null);
            mats.Add(null);

            foreach (var map in SoupManager.Mapping.OrderBy(m => m.data.ItemId))
            {
                if (controllerUsedIds.Contains(map.data.RequiredItemId))
                {
                    map.data.SoupId = -1;
                    map.itemPrefab.stewid = -1;
                    continue;
                }
                controllerUsedIds.Add(map.data.RequiredItemId);
                map.data.SoupId = startId;
                map.itemPrefab.stewid = startId;
                prefabs.Add(map.itemPrefab.gameObject);
                mats.Add(map.itemPrefab.mrend.materials[1]);
                var display = CreateItemDisplay(__instance, map.render);
                itemsDisplays.Add(display);
                startId++;
            }

            __instance.SoupPrefabs = [.. prefabs];
            __instance.SoupItems = [.. itemsDisplays];
            __instance.matas = [.. mats];
        }

        private static GameObject CreateItemDisplay(SoupManController __instance, GameObject? render)
        {
            render ??= new GameObject("Render");
            var objRender = UnityEngine.Object.Instantiate(render, __instance.SoupItems.First().transform.parent);
            objRender.SetActive(false);
            return objRender;
        }
    }

    [HarmonyPatch(typeof(SoupManInteractor))]
    internal class SoupManInteractorPatch
    {
        [HarmonyPatch(nameof(SoupManInteractor.DisplayInteractUI))]
        [HarmonyPrefix]
        private static bool DisplayInteractUI_Prefix(SoupManInteractor __instance, GameObject player, ref string __result)
        {
            if (player.TryGetComponent<PlayerInventory>(out var playerInventory) && !__instance.smc.isCookingSoup)
            {
                int equippedItemID = playerInventory.GetEquippedItemID();
                var map = SoupManager.Mapping.FirstOrDefault(map => map.data.RequiredItemId == equippedItemID);
                if (map != default)
                {
                    __result = $"Make {map.data.Name}";
                    return false;
                }
            }

            return true;
        }

        [HarmonyPatch(nameof(SoupManInteractor.Interact))]
        [HarmonyPrefix]
        private static bool Interact_Prefix(SoupManInteractor __instance, GameObject player)
        {
            if (Time.time - __instance.cd < 1f) return true;

            if (player.TryGetComponent<PlayerInventory>(out var playerInventory) && !__instance.smc.isCookingSoup)
            {
                int equippedItemID = playerInventory.GetEquippedItemID();
                var map = SoupManager.Mapping.FirstOrDefault(map => map.data.RequiredItemId == equippedItemID);
                if (map != default)
                {
                    __instance.smc.CookSoup(map.data.SoupId);
                    playerInventory.destroyHandItem();
                    return false;
                }
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(GetSoupFromGuy))]
    internal class GetSoupFromGuyPatch
    {
        [HarmonyPatch(nameof(GetSoupFromGuy.DisplayInteractUI))]
        [HarmonyPrefix]
        private static bool DisplayInteractUI_Prefix(GetSoupFromGuy __instance, GameObject player, ref string __result)
        {
            var map = SoupManager.Mapping.FirstOrDefault(map => map.data.SoupId == __instance.soupid);
            if (map != default)
            {
                __result = $"Grasp {map.data.Name}";
                return false;
            }

            return true;
        }
    }
}
