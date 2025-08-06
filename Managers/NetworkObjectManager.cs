using BepInEx;
using BlackMagicAPI.Helpers;
using BlackMagicAPI.Patches.Voice;
using UnityEngine;

namespace BlackMagicAPI.Managers;

internal class NetworkObjectManager
{
    private static readonly List<(BaseUnityPlugin plugin, Type type, Func<Sprite?> getSpriteCallback, Action<int> setIdCallback)> Mapping = [];
    internal static void SynchronizeItemId(BaseUnityPlugin baseUnity, Type type, Func<Sprite?> sprite, Action<int> SetIdCallback)
    {
        Mapping.Add((baseUnity, type, sprite, SetIdCallback));
        var nextId = Resources.FindObjectsOfTypeAll<PlayerInventory>().First().ItemIcons.Length + 1;
        foreach (var map in Mapping.OrderBy(m => m.plugin.GetUniqueHash()).ThenBy(m => m.type.FullName))
        {
            PlayerInventoryPatch.SetUiSprite(map.getSpriteCallback.Invoke(), nextId);
            map.setIdCallback(nextId);
            nextId++;
        }
    }
}
