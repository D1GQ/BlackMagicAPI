using BepInEx;
using BetterVoiceDetection;
using BlackMagicAPI.Enums;
using BlackMagicAPI.Modules.Spells;
using BlackMagicAPI.Patches.Voice;
using FishNet.Managing;
using FishNet.Object;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace BlackMagicAPI.Managers;

/// <summary>
/// Manages the registration and organization of spells within the game.
/// Handles spell creation, ID assignment, and maintains mappings between spells and their components.
/// </summary>
public static class SpellManager
{
    private static readonly List<Type> registeredTypes = [];
    internal static readonly List<(BaseUnityPlugin plugin, SpellData data, SpellLogic logic)> SpellMapping = [];
    internal static readonly Dictionary<SpellData, PageController> PageMapping = [];

    /// <summary>
    /// Registers a new spell with the spell management system.
    /// </summary>
    /// <param name="baseUnity">The plugin registering the spell.</param>
    /// <param name="SpellDataType">The type of the spell data (must inherit from SpellData).</param>
    /// <param name="SpellLogicType">The type of the spell logic (must inherit from SpellLogic, optional if prefab is provided).</param>
    /// <exception cref="InvalidCastException">Thrown if spell data cannot be created or cast to SpellData.</exception>
    public static void RegisterSpell(BaseUnityPlugin baseUnity, Type SpellDataType, Type? SpellLogicType = null)
    {
        if (SpellDataType.IsAbstract)
        {
            BMAPlugin.Log.LogError($"Failed to register spell from {baseUnity.Info.Metadata.Name}: SpellDataType can not be abstract!");
            return;
        }

        if (!SpellDataType.IsSubclassOf(typeof(SpellData)))
        {
            BMAPlugin.Log.LogError($"Failed to register spell from {baseUnity.Info.Metadata.Name}: SpellDataType must be inherited from SpellData!");
            return;
        }

        if (SpellLogicType != null)
        {
            if (SpellLogicType.IsAbstract)
            {
                BMAPlugin.Log.LogError($"Failed to register spell from {baseUnity.Info.Metadata.Name}: SpellLogicType can not be abstract!");
                return;
            }

            if (!SpellLogicType.IsSubclassOf(typeof(SpellLogic)))
            {
                BMAPlugin.Log.LogError($"Failed to register spell from {baseUnity.Info.Metadata.Name}: SpellDataType must be inherited from SpellLogic!");
                return;
            }
        }

        if (registeredTypes.Contains(SpellDataType))
        {
            BMAPlugin.Log.LogError($"Failed to register spell from {baseUnity.Info.Metadata.Name}: {SpellDataType.Name} has already been registered!");
            return;
        }

        _ = RegisterSpellTask(baseUnity, SpellDataType, SpellLogicType);
    }

    private static async Task RegisterSpellTask(BaseUnityPlugin baseUnity, Type spellDataType, Type? spellLogicType)
    {
        if (Activator.CreateInstance(spellDataType) is not SpellData data)
        {
            throw new InvalidCastException($"Failed to create or cast {spellDataType} to SpellData");
        }

        data.Plugin = baseUnity;
        SpellLogic? logic = await data.GetLogicPrefab();
        if (logic == null)
        {
            if (spellLogicType == null)
            {
                BMAPlugin.Log.LogError($"Failed to register spell from {baseUnity.Info.Metadata.Name}: spellLogicType cannot be null without a loadable prefab!");
                return;
            }

            logic = CreateSpellLogic(data, spellLogicType);
        }

        CreateSpell(baseUnity, data, logic);
    }

    private static SpellLogic CreateSpellLogic(SpellData spellData, Type spellLogicType)
    {
        var prefab = new GameObject($"{spellData.Name.Trim()}Spell")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        UnityEngine.Object.DontDestroyOnLoad(prefab);
        var logic = (SpellLogic)prefab.AddComponent(spellLogicType);
        logic.OnPrefabCreatedAutomatically(logic.gameObject);
        return logic;
    }

    private static void CreateSpell(BaseUnityPlugin baseUnity, SpellData spellData, SpellLogic spellLogic)
    {
        if (spellData.SpellType == SpellType.Page)
        {
            var pageController = Resources.FindObjectsOfTypeAll<PageController>().First();
            if (pageController != null)
            {
                var prefab = UnityEngine.Object.Instantiate(pageController);
                prefab.hideFlags = HideFlags.HideAndDontSave;
                UnityEngine.Object.DontDestroyOnLoad(prefab);
                prefab.name = $"Page{spellData.Name}";
                UnityEngine.Object.DestroyImmediate(prefab.GetComponent<NetworkObject>());
                IEnumerator CoWaitForNetwork()
                {
                    while (NetworkManager.Instances.Count == 0)
                        yield return null;
                    if (prefab == null) yield break;
                    var newNetObj = prefab.gameObject.AddComponent<NetworkObject>();
                    newNetObj.NetworkBehaviours = [];
                    NetworkManager.Instances.First().SpawnablePrefabs.AddObject(newNetObj, true);
                    SynchronizeNetworkObjectPrefab(newNetObj, $"{baseUnity.Info.Metadata.GUID}|{baseUnity.Info.Metadata.Name}|{baseUnity.Info.Metadata.Version}|{spellData.Name}|{spellData.GetType().Name}");
                }
                prefab.StartCoroutine(CoWaitForNetwork());
                spellData.SetUpPage(prefab.GetComponent<PageController>(), spellLogic);
                spellData.SetLight(prefab.GetComponentInChildren<Light>(true));
                SynchronizeSpellData(baseUnity, spellData, spellLogic, prefab);
            }
        }

        BMAPlugin.Log.LogInfo($"Successfully registered {spellData.Name} Spell from {baseUnity.Info.Metadata.GUID}");
    }

    private static void SynchronizeSpellData(BaseUnityPlugin baseUnity, SpellData spellData, SpellLogic spellLogic, PageController page)
    {
        PageMapping[spellData] = page;
        SpellMapping.Add((baseUnity, spellData, spellLogic));
        registeredTypes.Add(spellData.GetType());
        var nextId = Resources.FindObjectsOfTypeAll<PlayerInventory>().First().ItemIcons.Length + 1;
        foreach (var value in SpellMapping.OrderBy(k => k.plugin.Info.Metadata.GUID).ThenBy(k => k.plugin.Info.Metadata.Name).ThenBy(k => k.plugin.Info.Metadata.Version).ThenBy(k => k.data.Name))
        {
            PlayerInventoryPatch.SetUiSprite(spellData.GetUiSprite(), nextId);
            value.data.Id = nextId;
            PageMapping[value.data].ItemID = nextId;
            nextId++;
        }
    }

    private static ushort? prefabIdStart;
    private static readonly List<(string id, NetworkObject net)> netObjs = [];
    private static void SynchronizeNetworkObjectPrefab(NetworkObject netObj, string id)
    {
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
}
