using BlackMagicAPI.Managers;
using BlackMagicAPI.Modules.Spells;
using HarmonyLib;
using Unity.VisualScripting;

namespace BlackMagicAPI.Patches.Network;

[HarmonyPatch(typeof(MainMenuManagerNetworked))]
internal class MainMenuManagerNetworkedPatch
{
    [HarmonyPatch(nameof(MainMenuManagerNetworked.Start))]
    [HarmonyPrefix]
    private static void Start_Prefix(MainMenuManagerNetworked __instance)
    {
        foreach (var list in SpellManager.Spells.Values)
        {
            foreach (var spellData in list)
            {
                var sc = __instance.AddComponent<CustomSpellCommand>();
                sc.enabled = true;
                sc.SpellData = spellData;
            }
        }
    }
}
