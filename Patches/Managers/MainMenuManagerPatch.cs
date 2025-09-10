using BlackMagicAPI.Helpers;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace BlackMagicAPI.Patches.Managers;

[HarmonyPatch(typeof(MainMenuManager))]
internal class MainMenuManagerPatch
{
    // Static field to store the hash value.
    private static string Hash = "";

    // Static field to store the instantiated Text component.
    private static Text? text;

    [HarmonyPatch(nameof(MainMenuManager.Start))]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.First)]
    private static void Start_Postfix()
    {
        // Instantiate the Text component from the specified path and get its component.
        text = UnityEngine.Object.Instantiate(Utils.FindInactive("Canvas (1)/Main/AccentMenu/Text (Legacy) (3)/Text (Legacy) (6)")?.GetComponent<Text>());

        // If the Text component was successfully instantiated, set its properties.
        if (text != null)
        {
            // Set the parent of the Text component to the specified parent object.
            text.transform.SetParent(Utils.FindInactive("Canvas (1)/Main/AccentMenu/Text (Legacy) (3)")?.transform);

            // Set the position of the Text component.
            text.transform.position = new Vector3(1821f, 90f, 0f);

            // Set the text content of the Text component.
            text.text = Hash;
        }
    }

    // Static method to update the hash value and refresh the Text component's text content.
    internal static void UpdateHash(string hash)
    {
        // Update the hash value.
        Hash = hash;

        // If the Text component is not null, update its text content.
        if (text != null)
            text.text = Hash;
    }
}
