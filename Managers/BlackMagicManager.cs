using BepInEx;
using BlackMagicAPI.Helpers;
using BlackMagicAPI.Patches.Managers;
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
    /// Registers a crafting recipe by validating and locating the required item and spell prefabs.
    /// </summary>
    /// <param name="plugin">The plugin registering the recipe</param>
    /// <param name="IItemInteraction_FirstType">Type of the first item in the recipe (must implement IItemInteraction or SpellLogic/ISpell but not be the interface itself)</param>
    /// <param name="IItemInteraction_SecondType">Type of the second item in the recipe (must implement IItemInteraction or SpellLogic/ISpell  but not be the interface itself)</param>
    /// <param name="IItemInteraction_ResultType">Type of the resulting item (must implement IItemInteraction or SpellLogic/ISpell  but not be the interface itself)</param>
    public static void RegisterCraftingRecipe(BaseUnityPlugin plugin, Type IItemInteraction_FirstType, Type IItemInteraction_SecondType, Type IItemInteraction_ResultType) =>
        ItemManager.RegisterCraftingRecipe(plugin, IItemInteraction_FirstType, IItemInteraction_SecondType, IItemInteraction_ResultType);

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
