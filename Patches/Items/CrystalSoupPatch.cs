using BlackMagicAPI.Managers;
using HarmonyLib;

namespace BlackMagicAPI.Patches.Items;

[HarmonyPatch(typeof(CrystalSoup))]
internal class CrystalSoupPatch
{
    [HarmonyPatch(nameof(CrystalSoup.DisplayInteractUI))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    private static bool DisplayInteractUI_Prefix(CrystalSoup __instance, ref string __result)
    {
        var map = SoupManager.Mapping.FirstOrDefault(map => map.data.SoupId == __instance.stewid);
        if (map != default)
        {
            __result = $"Grasp {map.data.Name}";
            return false;
        }

        return true;
    }
}
