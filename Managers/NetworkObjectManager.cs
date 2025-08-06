using BepInEx;
using BlackMagicAPI.Helpers;
using BlackMagicAPI.Patches.Voice;
using FishNet.Managing;
using FishNet.Object;
using System.Collections;
using System.Reflection;
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

    private static ushort? prefabIdStart;
    private static readonly List<(string id, NetworkObject net)> netObjs = [];
    internal static void SynchronizeNetworkObjectPrefab(MonoBehaviour mono, string id)
    {
        if (mono.TryGetComponent<NetworkObject>(out var net))
            UnityEngine.Object.DestroyImmediate(net);

        IEnumerator CoWaitForNetwork()
        {
            while (NetworkManager.Instances.Count == 0)
                yield return null;
            if (mono == null) yield break;

            var netObj = mono.gameObject.AddComponent<NetworkObject>();
            netObj.NetworkBehaviours = [];
            NetworkManager.Instances.First().SpawnablePrefabs.AddObject(netObj, true);

            prefabIdStart ??= netObj.PrefabId;
            var prefabId = prefabIdStart;
            netObjs.Add((id, netObj));
            foreach (var item in netObjs.OrderBy(i => i.id))
            {
                var prop = typeof(NetworkObject).GetProperty("PrefabId", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                prop?.SetValue(item.net, prefabId);
                prefabId++;
            }
        }
        mono.StartCoroutine(CoWaitForNetwork());
    }
}
