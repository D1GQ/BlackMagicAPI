using BepInEx;
using BlackMagicAPI.Modules.Soups;
using BlackMagicAPI.Modules.Spells;
using FishNet.Object;
using FishUtilities.Managers;
using UnityEngine;

namespace BlackMagicAPI.Managers;

internal class SoupManager
{
    private static readonly List<Type> registeredTypes = [];
    internal static List<(SoupData data, CrystalSoup itemPrefab, GameObject? render, SoupEffect effect)> Mapping = [];

    internal static (SoupData data, CrystalSoup itemPrefab, GameObject? render, SoupEffect effect)? GetMapFromItemId(int id) => Mapping.FirstOrDefault(map => map.data.ItemId == id);

    internal static void RegisterSoup(BaseUnityPlugin baseUnity, Type IItemInteraction, Type soupDataType, Type? soupEffectType = null)
    {
        if (IItemInteraction.IsInterface)
        {
            BMAPlugin.Log.LogError($"Failed to register soup from {baseUnity.Info.Metadata.Name}: IItemInteraction can not be directly IItemInteraction interface!");
            ModSyncManager.FailedSoups.Add((baseUnity, soupDataType));
            return;
        }

        if (!soupDataType.IsSubclassOf(typeof(SoupData)))
        {
            BMAPlugin.Log.LogError($"Failed to register soup from {baseUnity.Info.Metadata.Name}: soupDataType must be inherited from SoupData!");
            ModSyncManager.FailedSoups.Add((baseUnity, soupDataType));
            return;
        }

        if (soupEffectType != null)
        {
            if (!soupEffectType.IsSubclassOf(typeof(SoupEffect)))
            {
                BMAPlugin.Log.LogError($"Failed to register soup from {baseUnity.Info.Metadata.Name}: soupEffectType must be inherited from SoupEffect!");
                ModSyncManager.FailedSoups.Add((baseUnity, soupDataType));
                return;
            }
        }

        if (registeredTypes.Contains(soupDataType))
        {
            BMAPlugin.Log.LogError($"Failed to register soup from {baseUnity.Info.Metadata.Name}: {soupDataType.Name} has already been registered!");
            ModSyncManager.FailedSoups.Add((baseUnity, soupDataType));
            return;
        }

        _ = RegisterSoupTask(baseUnity, IItemInteraction, soupDataType, soupEffectType);
    }

    private static async Task RegisterSoupTask(BaseUnityPlugin baseUnity, Type IItemInteraction, Type soupDataType, Type? soupEffectType)
    {
        if (Activator.CreateInstance(soupDataType) is not SoupData data)
        {
            ModSyncManager.FailedSoups.Add((baseUnity, soupDataType));
            throw new InvalidCastException($"Failed to create or cast {soupDataType} to SoupData");
        }

        data.Plugin = baseUnity;
        data.RequiredItemId = ItemManager.GetItemPrefab(IItemInteraction)?.GetItemID() ?? -1;
        SoupEffect? effect = await data.GetEffectPrefab();
        if (effect == null)
        {
            if (soupEffectType == null)
            {
                BMAPlugin.Log.LogError($"Failed to register soup from {baseUnity.Info.Metadata.Name}: soupEffectType cannot be null without a loadable prefab!");
                ModSyncManager.FailedSoups.Add((baseUnity, soupDataType));
                return;
            }

            effect = CreateSoupEffect(data, soupEffectType);
        }
        effect.gameObject.SetActive(false);

        CreateSoupItem(baseUnity, IItemInteraction, data, effect);
    }

    private static SoupEffect CreateSoupEffect(SoupData soupData, Type SoupEffectType)
    {
        var prefab = new GameObject($"{soupData.Name.Replace(" ", "")}Effect");
        UnityEngine.Object.DontDestroyOnLoad(prefab);
        var effect = (SoupEffect)prefab.AddComponent(SoupEffectType);
        effect.OnPrefabCreatedAutomatically(effect.gameObject);
        return effect;
    }

    private static void CreateSoupItem(BaseUnityPlugin baseUnity, Type IItemInteraction, SoupData soupData, SoupEffect soupEffect)
    {
        var itemPrefab = Resources.FindObjectsOfTypeAll<CrystalSoup>().First();
        var prefab = UnityEngine.Object.Instantiate(itemPrefab);
        UnityEngine.Object.DontDestroyOnLoad(prefab);
        prefab.name = $"{soupData.Name.Replace(" ", "")}BowlItem";
        prefab.stewid = -1;
        prefab.mrend.materials[1].color = soupData.SoupColor - Color.cyan;
        if (prefab.TryGetComponent<NetworkObject>(out NetworkObject? networkObject) && networkObject != null)
        {
            UnityEngine.Object.DestroyImmediate(networkObject);
        }
        UnityEngine.Object.Destroy(prefab.mrend.transform.GetChild(0).gameObject);
        var render = UnityEngine.Object.Instantiate((ItemManager.GetItemPrefab(IItemInteraction) as MonoBehaviour)?.transform.GetChild(0).gameObject);
        if (render != null)
        {
            soupData.SetObjectVisualTransform(render);
        }
        render?.transform.SetParent(prefab.mrend.transform, false);

        SynchronizeManager.SynchronizeItemId(baseUnity, soupData.GetType(), soupData.GetUiSprite, (id) =>
        {
            soupData.ItemId = id;
            soupEffect.Id = id;
            prefab.itemid = id;
        });
        FishManager.RegisterNetworkObjectPrefab(BMAPlugin.Instance, prefab, $"{soupData.GetType().FullName}");
        Mapping.Add((soupData, prefab, render, soupEffect));
        Mapping = [.. Mapping.OrderBy(map => map.data.ItemId)];
        SynchronizeManager.UpdateSyncHash();

        BMAPlugin.Log.LogInfo($"Successfully registered {soupData.Name} Soup from {baseUnity.Info.Metadata.GUID}");

        var dupes = Mapping.ToLookup(map => map.data.RequiredItemId);

        foreach (var group in dupes.Where(g => g.Count() > 1))
        {
            var data = group.OrderBy(map => map.data.ItemId).FirstOrDefault().data;
            string firstSoup = $"{data.GetType().FullName}";
            BMAPlugin.Log.LogWarning($"Warning multiple soups have been registered to the same item, " +
                $"{firstSoup} will override ({string.Join(", ", group.OrderBy(map => map.data.ItemId).Skip(1).Select(map => map.data.GetType().FullName))}) soups.");
        }
    }
}
