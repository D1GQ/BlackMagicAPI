using BepInEx;
using BlackMagicAPI.Modules.Items;
using BlackMagicAPI.Modules.Spells;
using BlackMagicAPI.Patches.Items;
using FishUtilities.Managers;
using UnityEngine;

namespace BlackMagicAPI.Managers;

internal static class ItemManager
{
    private static readonly List<Type> registeredTypes = [];
    internal static readonly List<(ItemData data, ItemBehavior behavior)> Mapping = [];
    internal static readonly Dictionary<Type, IItemInteraction> PrefabMapping = [];

    internal static void RegisterCraftingRecipe(BaseUnityPlugin baseUnity, Type IItemInteraction_FirstType, Type IItemInteraction_SecondType, Type IItemInteraction_ResultType)
    {
        if (IItemInteraction_FirstType.IsInterface)
        {
            BMAPlugin.Log.LogError($"Failed to register item recipe from {baseUnity.Info.Metadata.Name}: IItemInteraction_FirstType can not be directly IItemInteraction interface!");
            return;
        }

        if (IItemInteraction_SecondType.IsInterface)
        {
            BMAPlugin.Log.LogError($"Failed to register item recipe from {baseUnity.Info.Metadata.Name}: IItemInteraction_SecondType can not be directly IItemInteraction interface!");
            return;
        }

        if (IItemInteraction_ResultType.IsInterface)
        {
            BMAPlugin.Log.LogError($"Failed to register item recipe from {baseUnity.Info.Metadata.Name}: IItemInteraction_ResultType can not be directly IItemInteraction interface!");
            return;
        }

        if (!typeof(IItemInteraction).IsAssignableFrom(IItemInteraction_FirstType) && !typeof(ISpell).IsAssignableFrom(IItemInteraction_FirstType))
        {
            BMAPlugin.Log.LogError($"Failed to register item recipe from {baseUnity.Info.Metadata.Name}: IItemInteraction_FirstType must be inherited from IItemInteraction interface!");
            return;
        }

        if (!typeof(IItemInteraction).IsAssignableFrom(IItemInteraction_SecondType) && !typeof(ISpell).IsAssignableFrom(IItemInteraction_SecondType))
        {
            BMAPlugin.Log.LogError($"Failed to register item recipe from {baseUnity.Info.Metadata.Name}: IItemInteraction_SecondType must be inherited from IItemInteraction interface!");
            return;
        }

        if (!typeof(IItemInteraction).IsAssignableFrom(IItemInteraction_ResultType) && !typeof(ISpell).IsAssignableFrom(IItemInteraction_SecondType))
        {
            BMAPlugin.Log.LogError($"Failed to register item recipe from {baseUnity.Info.Metadata.Name}: IItemInteraction_ResultType must be inherited from IItemInteraction interface!");
            return;
        }

        MonoBehaviour? firstItemPrefab = !typeof(ISpell).IsAssignableFrom(IItemInteraction_FirstType) ? Resources.FindObjectsOfTypeAll(IItemInteraction_FirstType)?.First() as MonoBehaviour :
            GetPageFromSpellType(IItemInteraction_FirstType);
        if (firstItemPrefab != null)
        {
            MonoBehaviour? secondItemPrefab = !typeof(ISpell).IsAssignableFrom(IItemInteraction_SecondType) ? Resources.FindObjectsOfTypeAll(IItemInteraction_SecondType)?.First() as MonoBehaviour :
                GetPageFromSpellType(IItemInteraction_FirstType);
            if (secondItemPrefab != null)
            {
                MonoBehaviour? resultItemPrefab = !typeof(ISpell).IsAssignableFrom(IItemInteraction_SecondType) ? Resources.FindObjectsOfTypeAll(IItemInteraction_ResultType)?.First() as MonoBehaviour :
                    GetPageFromSpellType(IItemInteraction_FirstType);
                if (resultItemPrefab != null)
                {
                    if (CraftingForgePatch.RegisterRecipe(firstItemPrefab.gameObject, secondItemPrefab.gameObject, resultItemPrefab.gameObject))
                    {
                        BMAPlugin.Log.LogInfo($"Successfully registered ({IItemInteraction_FirstType}, {IItemInteraction_SecondType} => {IItemInteraction_ResultType} recipe from {baseUnity.Info.Metadata.GUID}");
                    }
                    else
                    {
                        BMAPlugin.Log.LogError($"Failed to register item recipe from {baseUnity.Info.Metadata.Name}: You cannot register a recipe that's already been registered!");
                    }
                }
                else
                {
                    BMAPlugin.Log.LogError($"Failed to register item recipe from {baseUnity.Info.Metadata.Name}: Unable to find item prefab for {IItemInteraction_ResultType.Name}!");
                }
            }
            else
            {
                BMAPlugin.Log.LogError($"Failed to register item recipe from {baseUnity.Info.Metadata.Name}: Unable to find item prefab for {IItemInteraction_SecondType.Name}!");
            }
        }
        else
        {
            BMAPlugin.Log.LogError($"Failed to register item recipe from {baseUnity.Info.Metadata.Name}: Unable to find item prefab for {IItemInteraction_FirstType.Name}!");
        }
    }

    private static MonoBehaviour? GetPageFromSpellType(Type spellType)
    {
        var pages = Resources.FindObjectsOfTypeAll<PageController>();
        foreach (var page in pages)
        {
            if (page?.spellprefab?.GetComponent<ISpell>()?.GetType() == spellType)
            {
                return page;
            }
        }
        return null;
    }

    internal static T? GetItemPrefab<T>() where T : IItemInteraction
    {
        if (PrefabMapping.TryGetValue(typeof(T), out var behavior))
        {
            return (T)behavior;
        }

        var customItem = Mapping.Select(map => (IItemInteraction)map.behavior)?.FirstOrDefault(behavior => behavior.GetType() == typeof(T));
        if (customItem != null)
        {
            PrefabMapping[typeof(T)] = customItem;
            return (T)customItem;
        }

        T? item = (T)(IItemInteraction)Resources.FindObjectsOfTypeAll(typeof(T)).FirstOrDefault();
        if (item != null)
        {
            PrefabMapping[typeof(T)] = item;
            return item;
        }

        throw new NullReferenceException("Item prefab could not be found!");
    }

    internal static void RegisterItem(BaseUnityPlugin baseUnity, Type ItemDataType, Type? ItemBehaviorType = null)
    {
        if (ItemDataType.IsAbstract)
        {
            BMAPlugin.Log.LogError($"Failed to register item from {baseUnity.Info.Metadata.Name}: ItemDataType can not be abstract!");
            return;
        }

        if (!ItemDataType.IsSubclassOf(typeof(ItemData)))
        {
            BMAPlugin.Log.LogError($"Failed to register item from {baseUnity.Info.Metadata.Name}: ItemDataType must be inherited from SpellData!");
            return;
        }

        if (ItemBehaviorType != null)
        {
            if (ItemBehaviorType.IsAbstract)
            {
                BMAPlugin.Log.LogError($"Failed to register item from {baseUnity.Info.Metadata.Name}: ItemBehaviorType can not be abstract!");
                return;
            }

            if (!ItemBehaviorType.IsSubclassOf(typeof(ItemBehavior)))
            {
                BMAPlugin.Log.LogError($"Failed to register item from {baseUnity.Info.Metadata.Name}: ItemBehaviorType must be inherited from SpellLogic!");
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
                BMAPlugin.Log.LogError($"Failed to register item from {baseUnity.Info.Metadata.Name}: spellLogicType cannot be null without a loadable prefab!");
                return;
            }

            behavior = CreateItemBehavior(data, itemBehaviorType);
        }

        CreateItem(baseUnity, data, behavior);
    }

    private static ItemBehavior CreateItemBehavior(ItemData itemData, Type itemBehaviorType)
    {
        var prefab = new GameObject($"{itemData.Name.Replace(" ", "")}Item")
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
        NetworkObjectManager.SynchronizeItemId(baseUnity, itemData.GetType(), itemData.GetUiSprite, (id) =>
        {
            itemData.Id = id;
            itemBehavior.Id = id;
        });
        FishManager.RegisterNetworkObjectPrefab(BMAPlugin.Instance, itemBehavior, itemData.GetType().FullName);
        Mapping.Add((itemData, itemBehavior));
        registeredTypes.Add(itemData.GetType());
        BlackMagicManager.UpdateSyncHash();

        BMAPlugin.Log.LogInfo($"Successfully registered {itemData.Name} Item from {baseUnity.Info.Metadata.GUID}");
    }
}
