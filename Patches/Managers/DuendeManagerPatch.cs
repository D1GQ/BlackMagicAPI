using BlackMagicAPI.Helpers;
using BlackMagicAPI.Managers;
using HarmonyLib;
using System.Reflection;

namespace BlackMagicAPI.Patches.Managers;

[HarmonyPatch(typeof(DuendeManager))]
internal class DuendeManagerPatch
{
    [HarmonyPatch(nameof(DuendeManager.Awake))]
    [HarmonyPrefix]
    private static void Awake_Prefix(DuendeManager __instance)
    {
        var list = __instance.DuendeTradeItems.ToList();
        foreach (var map in ItemManager.Mapping.OrderBy(m => m.data.Id))
        {
            if (!map.data.CanGetFromTrade) continue;
            list.Add(map.behavior.gameObject);
        }
        __instance.DuendeTradeItems = [.. list];
    }

    [HarmonyPatch(nameof(DuendeManager.ServerCreatePage))]
    [HarmonyPrefix]
    private static void ServerCreatePage_Prefix(DuendeManager __instance, ref int rand)
    {
        rand = __instance.plt.Pages[rand].GetComponent<PageController>()?.ItemID ?? __instance.plt.Pages[rand].GetComponent<PipeItem>()?.GetItemID() ?? 0;
    }

    [HarmonyPatch]
    private static class ServerCreatePagePatch
    {
        private static MethodBase TargetMethod() => Utils.PatchRpcMethod<DuendeManager>("RpcLogic___ServerCreatePage");

        [HarmonyPrefix]
        private static void Prefix(DuendeManager __instance, [HarmonyArgument(1)] ref int rand)
        {
            for (int i = 0; i < __instance.plt.Pages.Length; i++)
            {
                var page = __instance.plt.Pages[i];
                if (page == null) continue;

                int id = page.GetComponent<PageController>()?.ItemID ?? page.GetComponent<PipeItem>()?.GetItemID() ?? 0;
                if (id == rand)
                {
                    rand = i;
                    return;
                }
            }

            rand = 0;
        }
    }
}