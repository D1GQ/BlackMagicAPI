using BlackMagicAPI.Managers;
using BlackMagicAPI.Modules.Spells;
using HarmonyLib;
using Unity.VisualScripting;

namespace BlackMagicAPI.Patches.Managers;

[HarmonyPatch(typeof(MainMenuManagerNetworked))]
internal class MainMenuManagerNetworkedPatch
{
    [HarmonyPatch(nameof(MainMenuManagerNetworked.Start))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    private static void Start_Prefix(MainMenuManagerNetworked __instance)
    {
        foreach (var spellData in SpellManager.Mapping.Select(value => value.data))
        {
            AddSpellCommand(__instance, spellData, spellData.Name);
            foreach (var subName in spellData.SubNames)
            {
                AddSpellCommand(__instance, spellData, subName);
            }
        }
    }

    private static void AddSpellCommand(MainMenuManagerNetworked __instance, SpellData spellData, string name)
    {
        var sc = __instance.AddComponent<CustomSpellCommand>();
        sc.enabled = true;
        sc.SpellData = spellData;
        sc.SpellName = name;
    }
}
