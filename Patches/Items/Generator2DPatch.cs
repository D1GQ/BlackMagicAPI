using BlackMagicAPI.Helpers;
using HarmonyLib;
using System.Reflection;

namespace BlackMagicAPI.Patches.Items;

[HarmonyPatch(typeof(Generator2D))]
internal class Generator2DPatch
{
    [HarmonyPatch(nameof(Generator2D.PlaceItemIn))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    private static void PlaceItemIn_Prefix(Generator2D __instance, ref int itemid)
    {
        itemid = __instance.plt.Pages[itemid].GetComponent<PageController>()?.ItemID ?? __instance.plt.Pages[itemid].GetComponent<PipeItem>()?.GetItemID() ?? 0;
    }

    [HarmonyPatch]
    private static class ServerPlaceItemPatch
    {
        private static MethodBase TargetMethod() => Utils.PatchRpcMethod<Generator2D>("RpcLogic___ServerPlaceItem");

        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static void Prefix(Generator2D __instance, ref int itemid)
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