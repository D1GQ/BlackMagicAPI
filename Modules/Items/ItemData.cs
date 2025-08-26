using BepInEx;
using BlackMagicAPI.Helpers;
using BlackMagicAPI.Interfaces;
using BlackMagicAPI.Managers;
using BlackMagicAPI.Modules.Items;
using System.Reflection;
using UnityEngine;

namespace BlackMagicAPI.Modules.Spells;

/// <summary>
/// Abstract base class representing item data and configuration.
/// Provides core functionality for item identification, spawning rules, and asset loading.
/// </summary>
public abstract class ItemData : ICompatibility
{
    /// <inheritdoc/>
    public Version CompatibilityVersion => CompatibilityManager.GetReferencedVersion(GetType());

    /// <summary>
    /// Gets the display name of the item.
    /// </summary>
    /// <remarks>
    /// This must be implemented by derived classes to provide the item's proper name.
    /// </remarks>
    public abstract string Name { get; }

    /// <summary>
    /// Gets whether the item should be forcibly spawned in the team chest for debugging purposes.
    /// </summary>
    /// <value>
    /// Defaults to false. Override in derived classes to enable debug spawning.
    /// </value>
    public virtual bool DebugForceSpawn => false;

    /// <summary>
    /// Gets whether the item can naturally spawn in team chests during normal gameplay.
    /// </summary>
    /// <value>
    /// Defaults to false. Override in derived classes to allow team chest spawning.
    /// </value>
    public virtual bool CanSpawnInTeamChest => false;

    /// <summary>
    /// Gets whether the item can naturally spawn in coloseum during normal gameplay.
    /// </summary>
    public virtual bool CanSpawnInColoseum => true;

    /// <summary>
    /// Gets whether the item can be obtained through duende trading.
    /// </summary>
    /// <value>
    /// Defaults to false. Override in derived classes to allow trading.
    /// </value>
    public virtual bool CanGetFromTrade => false;

    /// <summary>
    /// Gets whether the item should be kept on death.
    /// </summary>
    /// <value>
    /// Defaults to false. Override in derived classes to allow keeping item on death.
    /// </value>
    public virtual bool KeepOnDeath => false;

    /// <summary>
    /// Gets the unique numeric identifier for the item type.
    /// </summary>
    /// <remarks>
    /// This value is set internally by the API and should not be modified manually.
    /// </remarks>
    public int Id { get; internal set; }

    /// <summary>
    /// The plugin that adds the item.
    /// </summary>
    public BaseUnityPlugin? Plugin { get; internal set; }

    /// <summary>
    /// Loads the UI sprite for this item from the plugin's resources.
    /// </summary>
    /// <returns>
    /// The custom UI sprite if found in the plugin's Sprites directory,
    /// otherwise the default item UI sprite from the API resources.
    /// Returns null if no plugin is associated.
    /// </returns>
    /// <remarks>
    /// Looks for a PNG file named "{ItemName}_Ui.png" in the plugin's Sprites folder.
    /// Removes spaces from the item name when constructing the filename.
    /// </remarks>
    internal Sprite? GetUiSprite()
    {
        if (Plugin == null) return null;

        string pluginPath = Path.GetDirectoryName(Plugin.Info.Location);
        string spritePath = Path.Combine(pluginPath, "Sprites", $"{Name.Replace(" ", "")}_Ui.png");
        if (File.Exists(spritePath))
        {
            return Utils.LoadSpriteFromDisk(spritePath);
        }

        return Assembly.GetExecutingAssembly().LoadSpriteFromResources("BlackMagicAPI.Resources.Items.Item_Ui.png");
    }

    /// <summary>
    /// Loads the pickup sound effect for this item from the plugin's resources.
    /// </summary>
    /// <returns>
    /// The custom pickup sound if found in the plugin's Sounds directory,
    /// otherwise null if no sound file exists or no plugin is associated.
    /// </returns>
    /// <remarks>
    /// Looks for a WAV file named "{ItemName}_Pickup.wav" in the plugin's Sounds folder.
    /// Removes spaces from the item name when constructing the filename.
    /// </remarks>
    public virtual AudioClip? GetPickupAudio()
    {
        if (Plugin == null) return null;

        string pluginPath = Path.GetDirectoryName(Plugin.Info.Location);
        string soundPath = Path.Combine(pluginPath, "Sounds", $"{Name.Replace(" ", "")}_Pickup.wav");
        if (File.Exists(soundPath))
        {
            return Utils.LoadWavFromDisk(soundPath);
        }

        return null;
    }

    /// <summary>
    /// Loads the equip sound effect for this item from the plugin's resources.
    /// </summary>
    /// <returns>
    /// The custom equip sound if found in the plugin's Sounds directory,
    /// otherwise null if no sound file exists or no plugin is associated.
    /// </returns>
    /// <remarks>
    /// Looks for a WAV file named "{ItemName}_Equip.wav" in the plugin's Sounds folder.
    /// Removes spaces from the item name when constructing the filename.
    /// </remarks>
    public virtual AudioClip? GetEquipAudio()
    {
        if (Plugin == null) return null;

        string pluginPath = Path.GetDirectoryName(Plugin.Info.Location);
        string soundPath = Path.Combine(pluginPath, "Sounds", $"{Name.Replace(" ", "")}_Equip.wav");
        if (File.Exists(soundPath))
        {
            return Utils.LoadWavFromDisk(soundPath);
        }

        return null;
    }

    /// <summary>
    /// Gets the prefab containing the ItemBehavior for this item.
    /// </summary>
    /// <returns>
    /// A Task that resolves to the ItemBehavior prefab, or null if no custom prefab is provided.
    /// </returns>
    /// <remarks>
    /// Override this in derived classes to provide custom item behavior prefabs.
    /// The default implementation returns null, which will use a generic item prefab.
    /// </remarks>
    public virtual Task<ItemBehavior?> GetItemPrefab() => Task.FromResult<ItemBehavior?>(null);
}