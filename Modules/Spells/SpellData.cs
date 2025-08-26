using BepInEx;
using BlackMagicAPI.Enums;
using BlackMagicAPI.Helpers;
using BlackMagicAPI.Interfaces;
using BlackMagicAPI.Managers;
using System.Reflection;
using UnityEngine;

namespace BlackMagicAPI.Modules.Spells;

/// <summary>
/// Abstract base class representing spell data and configuration.
/// Provides core functionality for spell properties, visual effects, and resource management.
/// </summary>
/// <remarks>
/// Derived classes must implement key spell properties and can override resource loading methods
/// to provide custom assets. The class handles automatic setup of spell materials and lighting.
/// </remarks>
public abstract class SpellData : ICompatibility
{
    /// <inheritdoc/>
    public Version CompatibilityVersion => CompatibilityManager.GetReferencedVersion(GetType());

    /// <summary>
    /// Gets the classification type of the spell.
    /// </summary>
    /// <value>
    /// Defaults to <see cref="SpellType.Page"/>. Override to specify different spell types.
    /// </value>
    public virtual SpellType SpellType => SpellType.Page;

    /// <summary>
    /// Gets whether the spell should be forcibly spawned in team chests for debugging purposes.
    /// </summary>
    /// <value>
    /// Defaults to false. When true, ensures the spell appears in team chests regardless of normal spawn rules.
    /// </value>
    public virtual bool DebugForceSpawn => false;

    /// <summary>
    /// Gets whether the spell can naturally spawn in team chests during normal gameplay.
    /// </summary>
    /// <value>
    /// Defaults to false. Override to true to allow natural spawning in team chests.
    /// </value>
    public virtual bool CanSpawnInTeamChest => false;

    /// <summary>
    /// Gets whether the item can naturally spawn in coloseum during normal gameplay.
    /// </summary>
    /// <value>
    /// Defaults to false. Override in derived classes to allow coloseum spawning.
    /// </value>
    public virtual bool CanSpawnInColoseum => true;

    /// <summary>
    /// Gets whether the spell page should be kept on death.
    /// </summary>
    /// <value>
    /// Defaults to false. Override in derived classes to allow keeping spell page on death.
    /// </value>
    public virtual bool KeepOnDeath => false;

    /// <summary>
    /// Gets the display name of the spell.
    /// </summary>
    /// <remarks>
    /// This abstract property must be implemented by derived classes to provide the spell's name.
    /// The name is used for display and voice detection purposes.
    /// </remarks>
    public abstract string Name { get; }

    /// <summary>
    /// Gets the a list of sub names to add onto voice detection.
    /// </summary>
    /// <remarks>
    /// The sub names is used for voice detection purposes.
    /// </remarks>
    public virtual string[] SubNames => [];

    /// <summary>
    /// Gets the cooldown duration in seconds between spell uses.
    /// </summary>
    /// <remarks>
    /// This abstract property must be implemented by derived classes to specify the spell's cooldown.
    /// The cooldown is enforced by the spell system after each activation.
    /// </remarks>
    public abstract float Cooldown { get; }

    /// <summary>
    /// Gets the primary glow color used for the spell's visual effects.
    /// </summary>
    /// <remarks>
    /// This abstract property must be implemented by derived classes to define the spell's signature color.
    /// The color is used for particle effects, lighting, and emission materials.
    /// </remarks>
    public abstract Color GlowColor { get; }

    /// <summary>
    /// Gets the unique numeric identifier for the spell type.
    /// </summary>
    /// <remarks>
    /// This value is set internally by the API and should not be modified manually.
    /// Used for spell identification and serialization.
    /// </remarks>
    public int Id { get; internal set; }

    /// <summary>
    /// The plugin that adds the spell.
    /// </summary>
    public BaseUnityPlugin? Plugin { get; internal set; }

    /// <summary>
    /// Loads the UI icon sprite for this spell from the plugin's resources.
    /// </summary>
    /// <returns>
    /// The custom UI sprite if found in the plugin's Sprites directory,
    /// otherwise the default spell UI sprite from the API resources.
    /// Returns null if no plugin is associated.
    /// </returns>
    /// <remarks>
    /// Looks for a PNG file named "{SpellName}_Ui.png" in the plugin's Sprites folder.
    /// Removes spaces from the spell name when constructing the filename.
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
    /// Loads the main texture for the spell's visual appearance.
    /// </summary>
    /// <returns>
    /// The custom main texture if found in the plugin's Sprites directory,
    /// otherwise the default spell texture from the API resources.
    /// Returns null if no plugin is associated.
    /// </returns>
    /// <remarks>
    /// Looks for a PNG file named "{SpellName}_Main.png" in the plugin's Sprites folder.
    /// This texture is applied to the spell's base material.
    /// </remarks>
    public virtual Texture2D? GetMainTexture()
    {
        if (Plugin == null) return null;

        string pluginPath = Path.GetDirectoryName(Plugin.Info.Location);
        string spritePath = Path.Combine(pluginPath, "Sprites", $"{Name.Replace(" ", "")}_Main.png");
        if (File.Exists(spritePath))
        {
            return Utils.LoadTextureFromDisk(spritePath);
        }

        return Assembly.GetExecutingAssembly().LoadTextureFromResources("BlackMagicAPI.Resources.Items.Item_Main.png");
    }

    /// <summary>
    /// Loads the emission texture for the spell's glowing effects.
    /// </summary>
    /// <returns>
    /// The custom emission texture if found in the plugin's Sprites directory,
    /// otherwise the default emission texture from the API resources.
    /// Returns null if no plugin is associated.
    /// </returns>
    /// <remarks>
    /// Looks for a PNG file named "{SpellName}_Emission.png" in the plugin's Sprites folder.
    /// This texture is combined with the glow color for emissive effects.
    /// </remarks>
    public virtual Texture2D? GetEmissionTexture()
    {
        if (Plugin == null) return null;

        string pluginPath = Path.GetDirectoryName(Plugin.Info.Location);
        string spritePath = Path.Combine(pluginPath, "Sprites", $"{Name.Replace(" ", "")}_Emission.png");
        if (File.Exists(spritePath))
        {
            return Utils.LoadTextureFromDisk(spritePath);
        }

        return Assembly.GetExecutingAssembly().LoadTextureFromResources("BlackMagicAPI.Resources.Items.Item_Emission.png");
    }

    /// <summary>
    /// Gets the spell logic prefab that implements this spell's behavior.
    /// </summary>
    /// <returns>
    /// A Task that resolves to the SpellLogic prefab, or null if no custom prefab is provided.
    /// </returns>
    /// <remarks>
    /// Override this in derived classes to provide custom spell behavior prefabs.
    /// The default implementation returns null, which will result in a non-functional spell.
    /// </remarks>
    public virtual Task<SpellLogic?> GetLogicPrefab() => Task.FromResult<SpellLogic?>(null);

    /// <summary>
    /// Configures a page controller with this spell's properties.
    /// </summary>
    /// <param name="page">The PageController to configure.</param>
    /// <param name="logic">The SpellLogic instance associated with this spell.</param>
    /// <remarks>
    /// This method is called internally by the API during spell initialization.
    /// It sets up the page's ID, cooldown, and visual references.
    /// </remarks>
    internal void SetUpPage(PageController page, SpellLogic logic)
    {
        page.ItemID = Id;
        page.CoolDown = Cooldown;
        page.PageCoolDownTimer = -Cooldown;
        page.spellprefab = logic.gameObject;
        // page.pickupText = $"Grasp {Name} Page";
        SetMaterial(page.pagerender.material);
    }

    /// <summary>
    /// Applies this spell's textures to a material.
    /// </summary>
    /// <param name="material">The material to configure.</param>
    /// <remarks>
    /// Sets both the base texture and emission texture on the material.
    /// Uses default textures if custom ones aren't provided.
    /// </remarks>
    internal void SetMaterial(Material material)
    {
        var main = GetMainTexture();
        if (main != null)
            material.SetTexture("_bk", main);

        var emi = GetEmissionTexture();
        if (emi != null)
            material.SetTexture("_emistexture", emi);
    }

    /// <summary>
    /// Configures a light component with this spell's glow color.
    /// </summary>
    /// <param name="light">The light component to configure.</param>
    /// <remarks>
    /// Sets the light's color to match the spell's GlowColor property.
    /// Does nothing if the light parameter is null.
    /// </remarks>
    internal void SetLight(Light? light)
    {
        if (light == null) return;
        light.color = GlowColor;
    }
}