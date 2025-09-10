using BlackMagicAPI.Helpers;
using BlackMagicAPI.Managers;
using BlackMagicAPI.Modules.Items;
using BlackMagicAPI.Modules.Spells;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace BlackMagicAPI.Patches.Player;

[HarmonyPatch(typeof(PlayerInventory))]
internal class PlayerInventoryPatch
{
    private static readonly Dictionary<int, Sprite?> UiSprites = [];

    [HarmonyPatch(nameof(PlayerInventory.SwapItemImg))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    private static bool SwapItemImg_Prefix(PlayerInventory __instance, int slotid, int itemid)
    {
        if (itemid > __instance.ItemIcons.Length)
        {
            if (UiSprites.TryGetValue(itemid, out var sprite))
            {
                __instance.UIImages[slotid].transform.GetChild(0).GetComponent<Image>().sprite = sprite;
                return false;
            }
        }

        return true;
    }

    internal static void SetUiSprite(Sprite? sprite, int itemId)
    {
        if (sprite == null) return;
        UiSprites[itemId] = sprite;
    }

    [HarmonyPatch(typeof(PlayerInventory), nameof(PlayerInventory.PlayerDied))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    private static bool PlayerDied_Prefix(PlayerInventory __instance)
    {
        __instance.canUseItem = false;
        __instance.canSwapItem = false;

        bool hasBook = false;
        int originalEquippedIndex = __instance.equippedIndex;
        __instance.equippedIndex = 0;

        int[] protectedItems =
        [
            BlackMagicManager.GetItemPrefab<MageBookController>()?.GetItemID() ?? -1,
            BlackMagicManager.GetItemPrefab<TorchController>()?.GetItemID() ?? -1,
            BlackMagicManager.GetItemPrefab<ExcaliburController>()?.GetItemID() ?? -1
        ];

        for (int i = 0; i < __instance.equippedItems.Length; i++)
        {
            GameObject? item = __instance.equippedItems[i];
            if (item == null)
            {
                __instance.equippedIndex++;
                continue;
            }

            if (item.TryGetComponent<ItemBehavior>(out var behavior) && behavior.KeepOnDeath)
            {
                __instance.equippedIndex++;
                continue;
            }
            else if (item.TryGetComponent<PageController>(out var page) && page.spellprefab != null
                && page.spellprefab.TryGetComponent<SpellLogic>(out var spell) && spell.KeepOnDeath)
            {
                __instance.equippedIndex++;
                continue;
            }
            else if (item.TryGetComponent<IItemInteraction>(out var itemInteraction))
            {
                int itemId = itemInteraction.GetItemID();
                if (!hasBook)
                {
                    hasBook = itemId == protectedItems[0];
                }

                if (protectedItems.Contains(itemId))
                {
                    __instance.equippedIndex++;
                    continue;
                }
            }

            __instance.Drop();
            __instance.equippedIndex++;
        }

        __instance.equippedIndex = originalEquippedIndex;
        __instance.LayerMaskSwapZero();
        var equippedItem = __instance.equippedItems[__instance.equippedIndex];
        if (equippedItem != null)
        {
            __instance.HideObject(equippedItem);
        }

        if (!hasBook)
        {
            __instance.StartCoroutine(__instance.DelaySpellbookRespawn());
        }

        return false;
    }

    [HarmonyPatch]
    private static class PlaceOnCraftingTablePatch
    {
        private static MethodBase TargetMethod() => Utils.PatchRpcMethod<PlayerInventory>("RpcLogic___PlaceOnCraftingTableObserver");

        [HarmonyPostfix]
        [HarmonyPriority(Priority.First)]
        private static void Postfix(GameObject obj, GameObject CrIn)
        {
            if (obj.TryGetComponent<ItemBehavior>(out var itemBehavior))
            {
                itemBehavior.SetTransformOnCraftingForge();
            }
        }
    }
}