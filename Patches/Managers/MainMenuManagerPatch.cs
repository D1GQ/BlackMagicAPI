using BlackMagicAPI.Helpers;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace BlackMagicAPI.Patches.Managers;

[HarmonyPatch(typeof(MainMenuManager))]
internal class MainMenuManagerPatch
{
    private static string Hash = "";
    private static Text? text;

    [HarmonyPatch(nameof(MainMenuManager.Start))]
    [HarmonyPostfix]
    private static void Start_Postfix()
    {
        text = UnityEngine.Object.Instantiate(Utils.FindInactive("Canvas (1)/Main/AccentMenu/Text (Legacy) (3)/Text (Legacy) (6)")?.GetComponent<Text>());
        if (text != null)
        {
            text.transform.SetParent(Utils.FindInactive("Canvas (1)/Main/AccentMenu/Text (Legacy) (3)")?.transform);
            text.transform.position = new Vector3(1821f, 78f, 0f);
            text.text = Hash;
        }
    }

    internal static void UpdateHash(string hash)
    {
        Hash = hash;
        if (text != null)
            text.text = Hash;
    }
}
