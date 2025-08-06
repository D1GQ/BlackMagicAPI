using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace BlackMagicAPI.Patches.Managers;

[HarmonyPatch(typeof(PlayerRespawnManager))]
internal class PlayerRespawnManagerPatch
{
    private static List<(string reason, Texture2D icon)> DeathIcons = [];

    [HarmonyPatch(nameof(PlayerRespawnManager.OnStartClient))]
    [HarmonyPostfix]
    private static void OnStartClient_Postfix(PlayerRespawnManager __instance)
    {
        foreach (var data in DeathIcons)
        {
            if (data.icon == null) continue;
            __instance.dethicons.Add(data.reason, data.icon);
        }
    }

    internal static bool AddDeathIcon(BaseUnityPlugin baseUnity, string deathReason, Texture2D deathIcon)
    {
        if (DeathIcons.Any(data => data.reason == deathReason))
        {
            BMAPlugin.Log.LogError($"Failed to register {deathReason} Death Icon from {baseUnity.Info.Metadata.Name}: Unable to register the same DeathReason twice!");
            return false;
        }

        DeathIcons.Add((deathReason, deathIcon));
        return true;
    }
}
