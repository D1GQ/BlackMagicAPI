using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace BlackMagicAPI.Patches.Voice;

[HarmonyPatch(typeof(PlayerInventory))]
internal class PlayerInventoryPatch
{
    private static readonly Dictionary<int, Sprite> UiSprites = [];

    [HarmonyPatch(nameof(PlayerInventory.SwapItemImg))]
    [HarmonyPrefix]
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
}