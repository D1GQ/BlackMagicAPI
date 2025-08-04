using BepInEx;
using BetterVoiceDetection;
using HarmonyLib;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace BlackMagicAPI.Patches.Items;

[HarmonyPatch(typeof(CraftingForge))]
internal class CraftingForgePatch
{
    internal static Dictionary<(IItemInteraction firstItem, IItemInteraction secondItem), GameObject> Recipes = [];

    internal static void RegisterRecipe(BaseUnityPlugin baseUnity, GameObject firstItemPrefap, GameObject secondItemPrefap, GameObject resultPrefap)
    {
        var item1 = firstItemPrefap.GetComponent<IItemInteraction>();
        var item2 = secondItemPrefap.GetComponent<IItemInteraction>();
        var resultItem = resultPrefap.GetComponent<IItemInteraction>();

        if (!TryGetRecipeByIDs(item1.GetItemID(), item2.GetItemID(), out _))
        {
            Recipes[(item1, item2)] = resultPrefap;
            BMAPlugin.Log.LogInfo($"Successfully registered ({item1.GetType()}, {item2.GetType()}) => {resultItem.GetType()} recipe from {baseUnity.Info.Metadata.GUID}");
        }
        else
        {
            BMAPlugin.Log.LogError($"Failed to register item recipe from {baseUnity.Info.Metadata.Name}: You cannot register a recipe that's already been registered!");
        }
    }

    private static bool TryGetRecipeByIDs(int id1, int id2, out GameObject? result)
    {
        foreach (var pair in Recipes)
        {
            if ((pair.Key.firstItem.GetItemID() == id1 && pair.Key.secondItem.GetItemID() == id2) ||
                (pair.Key.firstItem.GetItemID() == id2 && pair.Key.secondItem.GetItemID() == id1))
            {
                result = pair.Value;
                return true;
            }
        }
        result = null;
        return false;
    }

    [HarmonyPatch(nameof(CraftingForge.OnStartClient))]
    [HarmonyPostfix]
    private static void PlaceItemIn_Postfix(CraftingForge __instance)
    {
        __instance.StartCoroutine(CoCustomCrafter(__instance));
    }

    private static IEnumerator CoCustomCrafter(CraftingForge __instance)
    {
        while (__instance.isActiveAndEnabled)
        {
            yield return null;

            if (__instance.SlotItems == null || __instance.SlotItems.Length < 2)
                continue;

            if (__instance.SlotItems[0] == null || __instance.SlotItems[1] == null || !__instance.craftingComplete)
                continue;

            var item1 = __instance.SlotItems[0]?.GetComponent<IItemInteraction>();
            var item2 = __instance.SlotItems[1]?.GetComponent<IItemInteraction>();

            if (item1 == null || item2 == null)
                continue;

            if (TryGetRecipeByIDs(item1.GetItemID(), item2.GetItemID(), out _))
            {
                __instance.craftingComplete = false;
                __instance.ServerCraft();
            }
        }
    }

    [HarmonyPatch]
    private static class ServerCraftPatch
    {
        private static MethodBase TargetMethod()
        {
            var methods = typeof(CraftingForge)
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .Where(m => m.Name.StartsWith("RpcLogic___ServerCraft"))
                .ToList();

            if (methods.Count == 0)
                throw new Exception("Could not find RpcLogic___ServerCraft method");

            return methods[0];
        }

        [HarmonyPrefix]
        private static bool Prefix(CraftingForge __instance)
        {
            if (__instance.SlotItems[0] == null || __instance.SlotItems[1] == null) return true;

            var item1 = __instance.SlotItems[0].GetComponent<IItemInteraction>();
            var item2 = __instance.SlotItems[1].GetComponent<IItemInteraction>();
            if (item1 != null && item2 != null)
            {
                if (TryGetRecipeByIDs(item1.GetItemID(), item2.GetItemID(), out var prefab))
                {
                    if (prefab != null)
                    {
                        __instance.makepoof();

                        __instance.ServerManager.Despawn(__instance.SlotItems[0], null);
                        __instance.ServerManager.Despawn(__instance.SlotItems[1], null);
                        var gameObject = UnityEngine.Object.Instantiate(prefab);
                        gameObject.transform.position = __instance.itemSpawnPoint.position;
                        __instance.ServerManager.Spawn(gameObject, null, default);

                        __instance.SlotItems[0] = null;
                        __instance.SlotItems[1] = null;
                        __instance.CraftCleanUp();
                        __instance.craftingComplete = true;

                        return false;
                    }
                }
            }

            return true;
        }
    }
}