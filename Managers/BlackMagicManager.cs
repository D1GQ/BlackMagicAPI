using BepInEx;
using BlackMagicAPI.Helpers;
using BlackMagicAPI.Patches.Managers;
using FishNet.Managing;
using System.Text;
using UnityEngine;

namespace BlackMagicAPI.Managers;

/// <summary>
/// 
/// </summary>
public class BlackMagicManager
{
    internal static void UpdateSyncHash()
    {
        var checksumBuilder = new StringBuilder();

        foreach (var map in SpellManager.Mapping.OrderBy(map => map.data.Plugin?.GetUniqueHash()).ThenBy(map => map.data.GetType().FullName))
        {
            AppendMappingData(checksumBuilder,
                map.data?.Plugin?.GetUniqueHash(),
                map.data?.Id.ToString(),
                map.data?.GetType().FullName);
        }

        foreach (var map in ItemManager.Mapping.OrderBy(map => map.data.Plugin?.GetUniqueHash()).ThenBy(map => map.data.GetType().FullName))
        {
            AppendMappingData(checksumBuilder,
                map.data?.Plugin?.GetUniqueHash(),
                map.data?.Id.ToString(),
                map.data?.GetType().FullName);
        }

        var sb = checksumBuilder.ToString();
        var hash = sb.Length > 0 ? Utils.Generate9DigitHash(sb) : "000 | 000 | 000";
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

    /// <summary>
    /// Spawns a networked instance of an item of type T in the game world at an optional position and rotation.
    /// Requires an active NetworkManager and valid item prefab registration.
    /// </summary>
    /// <typeparam name="T">Type of item to spawn, must implement IItemInteraction</typeparam>
    /// <param name="position">Optional world position for the spawned item. If null, uses prefab's default position.</param>
    /// <param name="rotation">Optional rotation for the spawned item. If null, uses prefab's default rotation.</param>
    /// <returns>The spawned item instance implementing IItemInteraction</returns>
    /// <exception cref="InvalidOperationException">Thrown when no NetworkManager instance is available</exception>
    /// <exception cref="NullReferenceException">Thrown when the item prefab cannot be found</exception>
    public static T? SpawnItem<T>(Vector3? position = null, Quaternion? rotation = null) where T : IItemInteraction
    {
        var network = NetworkManager.Instances.FirstOrDefault() ?? throw new InvalidOperationException("Can not spawn iten when the NetworkManager is null!");
        if (!network.IsHostStarted)
        {
            BMAPlugin.Log.LogError($"Failed to spawn item {typeof(T).Name}: Items can only be spawned as host.");
            return default;
        }

        var prefab = GetItemPrefab<T>();
        if (prefab is MonoBehaviour mono)
        {
            var item = UnityEngine.Object.Instantiate(mono);
            if (position != null)
                item.transform.position = (Vector3)position;
            if (rotation != null)
                item.transform.rotation = (Quaternion)rotation;
            network.ServerManager.Spawn(item.gameObject);

            BMAPlugin.Log.LogInfo($"Successfully spawned item {typeof(T).Name}.");

            return (T)(IItemInteraction)item;
        }

        BMAPlugin.Log.LogError($"Failed to spawn item {typeof(T).Name}: Item prefab returned null.");
        return default;
    }

    /// <summary>
    /// Spawns a networked instance of a spell page containing spell of type T in the game world at an optional position and rotation.
    /// Requires an active NetworkManager and valid spell prefab registration.
    /// </summary>
    /// <typeparam name="T">Type of spell to spawn, must implement ISpell</typeparam>
    /// <param name="position">Optional world position for the spawned spell page. If null, uses prefab's default position.</param>
    /// <param name="rotation">Optional rotation for the spawned spell page. If null, uses prefab's default rotation.</param>
    /// <returns>The ISpell component of the spawned spell prefab</returns>
    /// <exception cref="InvalidOperationException">Thrown when no NetworkManager instance is available</exception>
    /// <exception cref="NullReferenceException">Thrown when the spell prefab cannot be found</exception>
    public static PageController? SpawnSpell<T>(Vector3? position = null, Quaternion? rotation = null) where T : ISpell
    {
        var network = NetworkManager.Instances.FirstOrDefault() ?? throw new InvalidOperationException("Can not spawn iten when the NetworkManager is null!");
        if (!network.IsHostStarted)
        {
            BMAPlugin.Log.LogError($"Failed to spawn spell page {typeof(T).Name}: Spells can only be spawned as host.");
            return default;
        }

        var prefab = GetSpellPagePrefab<T>();
        if (prefab is PageController page)
        {
            var spell = UnityEngine.Object.Instantiate(page);
            if (position != null)
                spell.transform.position = (Vector3)position;
            if (rotation != null)
                spell.transform.rotation = (Quaternion)rotation;
            network.ServerManager.Spawn(spell.gameObject);

            BMAPlugin.Log.LogInfo($"Successfully spawned spell page {typeof(T).Name}.");

            return spell;
        }

        BMAPlugin.Log.LogError($"Failed to spawn spell page {typeof(T).Name}: Spells prefab returned null.");
        return default;
    }

    /// <summary>
    /// Registers a crafting recipe by validating and locating the required item and spell prefabs.
    /// </summary>
    /// <param name="plugin">The plugin registering the recipe</param>
    /// <param name="IItemInteraction_FirstType">Type of the first item in the recipe (must implement IItemInteraction or SpellLogic/ISpell but not be the interface itself)</param>
    /// <param name="IItemInteraction_SecondType">Type of the second item in the recipe (must implement IItemInteraction or SpellLogic/ISpell  but not be the interface itself)</param>
    /// <param name="IItemInteraction_ResultType">Type of the resulting item (must implement IItemInteraction or SpellLogic/ISpell  but not be the interface itself)</param>
    public static void RegisterCraftingRecipe(BaseUnityPlugin plugin, Type IItemInteraction_FirstType, Type IItemInteraction_SecondType, Type IItemInteraction_ResultType) =>
        ItemManager.RegisterCraftingRecipe(plugin, IItemInteraction_FirstType, IItemInteraction_SecondType, IItemInteraction_ResultType);

    /// <summary>
    /// Retrieves the prefab of type T that implements IItemInteraction.
    /// First checks the prefab cache, then custom item mappings, and finally searches through all loaded resources.
    /// Found prefabs are cached for future lookups.
    /// </summary>
    /// <typeparam name="T">The type of item prefab to retrieve, must implement IItemInteraction</typeparam>
    /// <returns>The cached or newly found item prefab of type T if found, null if not found</returns>
    /// <exception cref="NullReferenceException">Thrown when the item prefab cannot be found in cache, custom mappings, or resources</exception>
    public static T? GetItemPrefab<T>() where T : IItemInteraction => ItemManager.GetItemPrefab<T>();

    /// <summary>
    /// Registers a new item with the item management system.
    /// </summary>
    /// <param name="plugin">The plugin registering the spell.</param>
    /// <param name="ItemDataType">The type of the item data (must inherit from ItemData).</param>
    /// <param name="ItemBehaviorType">The type of the item behavior (must inherit from ItemBehavior, optional if prefab is provided).</param>
    /// <exception cref="InvalidCastException">Thrown if item data cannot be created or cast to ItemData.</exception>
    public static void RegisterItem(BaseUnityPlugin plugin, Type ItemDataType, Type? ItemBehaviorType = null) =>
        ItemManager.RegisterItem(plugin, ItemDataType, ItemBehaviorType);

    /// <summary>
    /// Retrieves the spell page prefab containing a spell of type T that implements ISpell.
    /// First checks the prefab cache, then custom spell mappings, and finally searches through all loaded resources.
    /// Found prefabs are cached for future lookups.
    /// </summary>
    /// <typeparam name="T">The type of spell to search for in page prefabs, must implement ISpell</typeparam>
    /// <returns>The cached or newly found PageController prefab containing the specified spell type</returns>
    /// <exception cref="NullReferenceException">Thrown when the page prefab cannot be found in cache, custom mappings, or resources</exception>
    public static PageController GetSpellPagePrefab<T>() where T : ISpell => SpellManager.GetSpellPagePrefab<T>();

    /// <summary>
    /// Registers a new spell with the spell management system.
    /// </summary>
    /// <param name="plugin">The plugin registering the spell.</param>
    /// <param name="SpellDataType">The type of the spell data (must inherit from SpellData).</param>
    /// <param name="SpellLogicType">The type of the spell logic (must inherit from SpellLogic, optional if prefab is provided).</param>
    /// <exception cref="InvalidCastException">Thrown if spell data cannot be created or cast to SpellData.</exception>
    public static void RegisterSpell(BaseUnityPlugin plugin, Type SpellDataType, Type? SpellLogicType = null) =>
        SpellManager.RegisterSpell(plugin, SpellDataType, SpellLogicType);

    /// <summary>
    /// Provides methods to register custom death icons that will be displayed when a player dies with a specific death reason.
    /// </summary>
    /// <param name="plugin">The plugin instance registering the death icon (used for identification and logging).</param>
    /// <param name="deathReason">The unique death reason string that will trigger this icon (case-sensitive).</param>
    /// <param name="spriteName">The filename of the sprite (without extension) located in the plugin's "Sprites" folder.</param>
    /// <remarks>
    /// The sprite should be placed in a "Sprites" subfolder of your plugin's directory.
    /// Supported formats include PNG. The method automatically handles .png extension.
    /// </remarks>
    public static void RegisterDeathIcon(BaseUnityPlugin plugin, string deathReason, string spriteName)
    {
        string pluginPath = Path.GetDirectoryName(plugin.Info.Location);
        string spritePath = Path.Combine(pluginPath, "Sprites", $"{spriteName.Replace(".png", "")}.png");
        if (File.Exists(spritePath))
        {
            var icon = Utils.LoadTextureFromDisk(spritePath);
            if (icon == null)
            {
                return;
            }

            if (PlayerRespawnManagerPatch.AddDeathIcon(plugin, deathReason, icon))
            {
                BMAPlugin.Log.LogInfo($"Successfully registered {deathReason} Death Icon from {plugin.Info.Metadata.GUID}");
            }
        }
    }

    /// <summary>
    /// Provides methods to register custom death icons that will be displayed when a player dies with a specific death reason.
    /// </summary>
    /// <param name="plugin">The plugin instance registering the death icon.</param>
    /// <param name="deathReason">The unique death reason string that will trigger this icon.</param>
    /// <param name="icon">The Texture2D object containing the death icon image.</param>
    /// <remarks>
    /// Use this overload if you need to programmatically generate or modify the texture before registration.
    /// </remarks>
    public static void RegisterDeathIcon(BaseUnityPlugin plugin, string deathReason, Texture2D icon)
    {
        if (icon == null)
        {
            return;
        }
        if (PlayerRespawnManagerPatch.AddDeathIcon(plugin, deathReason, icon))
        {
            BMAPlugin.Log.LogInfo($"Successfully registered {deathReason} Death Icon from {plugin.Info.Metadata.GUID}");
        }
    }
}
