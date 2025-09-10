using HarmonyLib;

namespace BlackMagicAPI.Patches.Generation;

[HarmonyPatch(typeof(PaperInteract))]
internal class PaperInteractPatch
{
    [HarmonyPatch(nameof(PaperInteract.Start))]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.First)]
    private static void Start_Postfix(PaperInteract __instance)
    {
    }
}
