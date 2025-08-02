using BepInEx;
using BetterVoiceDetection;
using BlackMagicAPI.Enums;
using BlackMagicAPI.Modules.Spells;
using BlackMagicAPI.Patches.Voice;
using FishNet.Managing;
using FishNet.Object;
using System.Collections;
using UnityEngine;

namespace BlackMagicAPI.Managers;

/// <summary>
/// Manages the registration and organization of spells within the game.
/// Handles spell creation, ID assignment, and maintains mappings between spells and their components.
/// </summary>
public static class SpellManager
{
    private static int nextId = 10000;
    private static int nextBookId = 5;

    private static readonly List<Type> registered = [];
    internal static readonly Dictionary<BaseUnityPlugin, List<SpellData>> Spells = [];
    internal static readonly List<PageController> PagePrefabs = [];
    internal static readonly Dictionary<int, (BaseUnityPlugin plugin, PageController page, SpellData data)> Mapping = [];

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

        if (registered.Contains(SpellDataType))
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
        registered.Add(spellDataType);
        Spells[baseUnity] = [];
        Spells[baseUnity].Add(data);
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
                IEnumerator WaitAdd()
                {
                    while (NetworkManager.Instances.Count == 0)
                        yield return null;
                    if (prefab == null) yield break;
                    var newNetObj = prefab.gameObject.AddComponent<NetworkObject>();
                    newNetObj.NetworkBehaviours = [];
                    NetworkManager.Instances.First().SpawnablePrefabs.AddObject(newNetObj, true);
                }
                prefab.StartCoroutine(WaitAdd());

                PagePrefabs.Add(prefab);
                spellData.SetUpPage(prefab.GetComponent<PageController>(), spellLogic);
                spellData.SetLight(prefab.GetComponentInChildren<Light>(true));

                Mapping[nextId] = (baseUnity, prefab, spellData);
                spellData.Id = nextId;
                var pageComp = prefab.GetComponent<PageController>();
                pageComp.ItemID = nextId;
                PlayerInventoryPatch.AddUiSprite(spellData.GetUiSprite(), nextId);
                nextId++;
            }
        }
        /*
        else if (spellData.SpellType == SpellType.Book)
        {
            var mageBookController = Resources.FindObjectsOfTypeAll<MageBookController>().First();
            if (mageBookController != null)
            {
                var page4 = mageBookController.transform.Find("holder/magebookp4");
                if (page4 != null)
                {
                    var page = UnityEngine.Object.Instantiate(page4, mageBookController.transform.Find("holder"));
                    page.name = $"magebookp{nextBookId}";
                    var mat = page.GetComponentInChildren<SkinnedMeshRenderer>()?.material;
                    if (mat != null)
                    {
                        spellData.SetMaterial(mat);
                    }
                    spellData.SetLight(page.GetComponentInChildren<Light>());
                    var CustomSpellPage = page.AddComponent<CustomSpellPage>();
                    CustomSpellPage.pageId = nextBookId;
                    CustomSpellPage.spellLogicPrefab = spellLogic;
                }

                spellData.Id = nextBookId;
                nextBookId++;
            }
        }
        */

        BMAPlugin.Log.LogInfo($"Successfully registered {spellData.Name} Spell from {baseUnity.Info.Metadata.GUID}");
        ReassignIds();
    }

    private static void ReassignIds()
    {
        var oldMapping = Mapping.ToList();
        Mapping.Clear();
        nextId = 10000;

        foreach (var kvp in oldMapping.OrderBy(k => k.Value.plugin.Info.Metadata.GUID).ThenBy(k => k.Value.data.Name))
        {
            kvp.Value.page.ItemID = nextId;
            kvp.Value.data.Id = nextId;
            Mapping.Add(nextId, (kvp.Value.plugin, kvp.Value.page, kvp.Value.data));
            nextId++;
        }
    }
}
