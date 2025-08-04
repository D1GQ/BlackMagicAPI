using BepInEx;
using BetterVoiceDetection;
using BlackMagicAPI.Helpers;
using BlackMagicAPI.Modules.Items;
using BlackMagicAPI.Modules.Spells;
using UnityEngine;

namespace BlackMagicAPI.Managers;

/// <summary>
/// Manages the registration and organization of spells within the game.
/// Handles spell creation, ID assignment, and maintains mappings between spells and their components.
/// </summary>
public static class ItemManager
{
    private static readonly List<Type> registeredTypes = [];
    internal static readonly List<(ItemData data, ItemBehavior behavior)> Mapping = [];

    /// <summary>
    /// Registers a new item with the item management system.
    /// </summary>
    /// <param name="baseUnity">The plugin registering the spell.</param>
    /// <param name="ItemDataType">The type of the item data (must inherit from ItemData).</param>
    /// <param name="ItemBehaviorType">The type of the item behavior (must inherit from ItemBehavior, optional if prefab is provided).</param>
    /// <exception cref="InvalidCastException">Thrown if item data cannot be created or cast to ItemData.</exception>
    public static void RegisterItem(BaseUnityPlugin baseUnity, Type ItemDataType, Type? ItemBehaviorType = null)
    {
        if (ItemDataType.IsAbstract)
        {
            BMAPlugin.Log.LogError($"Failed to register spell from {baseUnity.Info.Metadata.Name}: ItemDataType can not be abstract!");
            return;
        }

        if (!ItemDataType.IsSubclassOf(typeof(ItemData)))
        {
            BMAPlugin.Log.LogError($"Failed to register spell from {baseUnity.Info.Metadata.Name}: ItemDataType must be inherited from SpellData!");
            return;
        }

        if (ItemBehaviorType != null)
        {
            if (ItemBehaviorType.IsAbstract)
            {
                BMAPlugin.Log.LogError($"Failed to register spell from {baseUnity.Info.Metadata.Name}: ItemBehaviorType can not be abstract!");
                return;
            }

            if (!ItemBehaviorType.IsSubclassOf(typeof(ItemBehavior)))
            {
                BMAPlugin.Log.LogError($"Failed to register spell from {baseUnity.Info.Metadata.Name}: ItemBehaviorType must be inherited from SpellLogic!");
                return;
            }
        }

        if (registeredTypes.Contains(ItemDataType))
        {
            BMAPlugin.Log.LogError($"Failed to register item from {baseUnity.Info.Metadata.Name}: {ItemDataType.Name} has already been registered!");
            return;
        }

        _ = RegisterItemTask(baseUnity, ItemDataType, ItemBehaviorType);
    }

    private static async Task RegisterItemTask(BaseUnityPlugin baseUnity, Type spellItemType, Type? itemBehaviorType)
    {
        if (Activator.CreateInstance(spellItemType) is not ItemData data)
        {
            throw new InvalidCastException($"Failed to create or cast {spellItemType} to SpellData");
        }

        data.Plugin = baseUnity;
        ItemBehavior? behavior = await data.GetItemPrefab();
        if (behavior == null)
        {
            if (itemBehaviorType == null)
            {
                BMAPlugin.Log.LogError($"Failed to register spell from {baseUnity.Info.Metadata.Name}: spellLogicType cannot be null without a loadable prefab!");
                return;
            }

            behavior = CreateItemBehavior(data, itemBehaviorType);
        }

        CreateItem(baseUnity, data, behavior);
    }

    private static ItemBehavior CreateItemBehavior(ItemData itemData, Type itemBehaviorType)
    {
        var prefab = new GameObject($"{itemData.Name.Trim()}Item")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        UnityEngine.Object.DontDestroyOnLoad(prefab);
        prefab.AddComponent<BoxCollider>();
        prefab.AddComponent<AudioSource>();
        var render = new GameObject("ItemRender");
        render.transform.SetParent(prefab.transform);
        var behavior = (ItemBehavior)prefab.AddComponent(itemBehaviorType);
        behavior.Name = itemData.Name;
        behavior.ItemRender = render;
        behavior.EquipSound = itemData.GetPickupAudio();
        behavior.DropSound = itemData.GetEquipAudio();
        behavior.OnPrefabCreatedAutomatically(behavior.gameObject);
        return behavior;
    }

    private static void CreateItem(BaseUnityPlugin baseUnity, ItemData itemData, ItemBehavior itemBehavior)
    {
        NetworkObjectManager.SynchronizeNetworkObjectPrefab(itemBehavior, $"{baseUnity.GetUniqueHash()}|{itemData.Name}|{itemData.GetType().Name}");
        NetworkObjectManager.SynchronizeItemId(baseUnity, itemData.Name, itemData.GetUiSprite, (id) =>
        {
            itemData.Id = id;
            itemBehavior.Id = id;
        });
        Mapping.Add((itemData, itemBehavior));
        registeredTypes.Add(itemData.GetType());

        BMAPlugin.Log.LogInfo($"Successfully registered {itemData.Name} Item from {baseUnity.Info.Metadata.GUID}");
    }
}
