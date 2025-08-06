using BepInEx;
using BlackMagicAPI.Enums;
using BlackMagicAPI.Helpers;
using BlackMagicAPI.Modules.Spells;
using UnityEngine;

namespace BlackMagicAPI.Managers;

internal static class SpellManager
{
    private static readonly List<Type> registeredTypes = [];
    internal static List<(SpellData data, PageController page)> Mapping = [];

    internal static void RegisterSpell(BaseUnityPlugin baseUnity, Type SpellDataType, Type? SpellLogicType = null)
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
                NetworkObjectManager.SynchronizeItemId(baseUnity, spellData.GetType(), spellData.GetUiSprite, (id) =>
                {
                    spellData.Id = id;
                    prefab.ItemID = id;
                });
                NetworkObjectManager.SynchronizeNetworkObjectPrefab(prefab, Utils.GenerateHash($"{baseUnity.GetUniqueHash()}|{spellData.Name}|{spellData.GetType().Name}"));
                spellData.SetUpPage(prefab.GetComponent<PageController>(), spellLogic);
                spellData.SetLight(prefab.GetComponentInChildren<Light>(true));
                Mapping.Add((spellData, prefab));
                registeredTypes.Add(spellData.GetType());
                BlackMagicManager.UpdateSyncHash();
            }
        }

        BMAPlugin.Log.LogInfo($"Successfully registered {spellData.Name} Spell from {baseUnity.Info.Metadata.GUID}");
    }
}
