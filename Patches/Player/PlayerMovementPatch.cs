using BlackMagicAPI.Helpers;
using BlackMagicAPI.Managers;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace BlackMagicAPI.Patches.Player;

[HarmonyPatch(typeof(PlayerMovement))]
internal class PlayerMovementPatch
{
    [HarmonyPatch]
    private static class ObsDrinkSoupPatch
    {
        private static MethodBase TargetMethod() => Utils.PatchRpcMethod<PlayerMovement>("RpcLogic___ObsDrinkSoup");

        [HarmonyPrefix]
        private static bool Prefix(PlayerMovement __instance, int stewid)
        {
            var map = SoupManager.Mapping.FirstOrDefault(map => map.data.SoupId == stewid);
            if (map != default)
            {
                Camera.main.GetComponent<PlayerInteract>().leveluptxt(map.data.ConsumeDescription);
                var effect = UnityEngine.Object.Instantiate(map.effect);
                effect.gameObject.SetActive(true);
                effect.ApplyEffect(__instance);
                return false;
            }

            return true;
        }
    }
}