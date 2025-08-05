using BlackMagicAPI.Helpers;
using HarmonyLib;
using System.Reflection;

namespace BlackMagicAPI.Patches.Items;

[HarmonyPatch(typeof(ChestNetController1))]
internal class ChestNetController1Patch
{
    [HarmonyPatch(nameof(ChestNetController1.PlaceItemIn))]
    [HarmonyPrefix]
    private static void PlaceItemIn_Prefix(ChestNetController1 __instance, ref int itemid)
    {
        itemid = __instance.plt.Pages[itemid].GetComponent<PageController>()?.ItemID ?? __instance.plt.Pages[itemid].GetComponent<PipeItem>()?.GetItemID() ?? 0;
    }

    [HarmonyPatch]
    private static class ServerPlaceItemPatch
    {
        private static MethodBase TargetMethod() => Utils.PatchRpcMethod<ChestNetController1>("RpcLogic___ServerPlaceItem");

        [HarmonyPrefix]
        private static void Prefix(ChestNetController1 __instance, ref int itemid)
        {
            for (int i = 0; i < __instance.plt.Pages.Length; i++)
            {
                var page = __instance.plt.Pages[i];
                if (page == null) continue;

                int id = page.GetComponent<PageController>()?.ItemID ?? page.GetComponent<PipeItem>()?.GetItemID() ?? 0;
                if (id == itemid)
                {
                    itemid = i;
                    return;
                }
            }

            itemid = 0;
        }
    }
}