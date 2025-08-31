using BepInEx;
using BlackMagicAPI.Helpers;
using BlackMagicAPI.Patches.Managers;
using BlackMagicAPI.Patches.Player;
using System.Text;
using UnityEngine;

namespace BlackMagicAPI.Managers;

internal class SynchronizeManager
{
    private static readonly List<(BaseUnityPlugin plugin, Type type, Func<Sprite?> getSpriteCallback, Action<int> setIdCallback)> ItemMapping = [];
    internal static void SynchronizeItemId(BaseUnityPlugin baseUnity, Type type, Func<Sprite?> sprite, Action<int> SetIdCallback)
    {
        ItemMapping.Add((baseUnity, type, sprite, SetIdCallback));
        var nextId = Resources.FindObjectsOfTypeAll<PlayerInventory>().First().ItemIcons.Length + 1;
        foreach (var map in ItemMapping.OrderBy(m => m.plugin.GetUniqueHash()).ThenBy(m => m.type.FullName))
        {
            PlayerInventoryPatch.SetUiSprite(map.getSpriteCallback.Invoke(), nextId);
            map.setIdCallback(nextId);
            nextId++;
        }
    }

    internal static void UpdateSyncHash()
    {
        var checksumBuilder = new StringBuilder();

        foreach (var (data, _) in SpellManager.Mapping.OrderBy(map => map.data.Plugin?.GetUniqueHash()).ThenBy(map => map.data.GetType().FullName))
        {
            AppendMappingData(checksumBuilder,
                data?.Plugin?.GetUniqueHash(),
                data?.Id.ToString(),
                data?.GetType().FullName);
        }

        foreach (var (data, _) in ItemManager.Mapping.OrderBy(map => map.data.Plugin?.GetUniqueHash()).ThenBy(map => map.data.GetType().FullName))
        {
            AppendMappingData(checksumBuilder,
                data?.Plugin?.GetUniqueHash(),
                data?.Id.ToString(),
                data?.GetType().FullName);
        }

        foreach (var (data, _, _, _) in SoupManager.Mapping.OrderBy(map => map.data.Plugin?.GetUniqueHash()).ThenBy(map => map.data.GetType().FullName))
        {
            AppendMappingData(checksumBuilder,
                data?.Plugin?.GetUniqueHash(),
                data?.ItemId.ToString(),
                data?.RequiredItemId.ToString(),
                data?.GetType().FullName);
        }

        var sb = checksumBuilder.ToString();
        var hash = sb.Length > 0 ? Utils.Generate9DigitHash(sb) : "000 | 000 | 000";
        BMAPlugin.BlackMagicSyncHash = hash;
        MainMenuManagerPatch.UpdateHash($"(Black Magic Sync)\n{hash}");
    }

    private static void AppendMappingData(StringBuilder builder, params string?[] values)
    {
        builder.Append('[');
        foreach (var value in values)
        {
            builder.Append(value ?? "");
        }
        builder.Append(']');
    }
}
