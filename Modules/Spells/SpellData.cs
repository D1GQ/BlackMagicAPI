using BepInEx;
using BlackMagicAPI.Enums;
using BlackMagicAPI.Helpers;
using System.Reflection;
using UnityEngine;

namespace BlackMagicAPI.Modules.Spells;

/// <summary>
/// Abstract base class representing spell data and configuration.
/// Provides core functionality for spell textures, cooldowns, and visual effects.
/// </summary>
public abstract class SpellData
{
    /// <summary>
    /// Gets the type of spell (defaults to Page type).
    /// </summary>
    public virtual SpellType SpellType => SpellType.Page;
    /// <summary>
    /// Gets the name of the spell (must be implemented by derived classes).
    /// </summary>
    public abstract string Name { get; }
    /// <summary>
    /// Gets the cooldown time in seconds for the spell (must be implemented by derived classes).
    /// </summary>
    public abstract float Cooldown { get; }
    /// <summary>
    /// Gets the glow color for the spell's visual effects (must be implemented by derived classes).
    /// </summary>
    public abstract Color GlowColor { get; }
    /// <summary>
    /// Gets the unique identifier for the spell.
    /// </summary>
    public int Id { get; internal set; }
    internal BaseUnityPlugin? Plugin { get; set; }

    internal Sprite? GetUiSprite() => Assembly.GetExecutingAssembly().LoadSpriteFromResources("BlackMagicAPI.Resources.Items.Item_Ui.png");

    /// <summary>
    /// Gets the main texture for the spell's visual appearance.
    /// </summary>
    /// <returns>The main texture, or a default if not found.</returns>
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
    /// Gets the emission texture for the spell's glowing effects.
    /// </summary>
    /// <returns>The emission texture, or a default if not found.</returns>
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
    /// Gets the spell logic prefab associated with this spell.
    /// </summary>
    /// <returns>A Task containing the SpellLogic prefab or null if not provided.</returns>
    public virtual Task<SpellLogic?> GetLogicPrefab() => Task.FromResult<SpellLogic?>(null);

    internal void SetUpPage(PageController page, SpellLogic logic)
    {
        page.ItemID = Id;
        page.CoolDown = Cooldown;
        page.spellprefab = logic.gameObject;
        // page.pickupText = $"Grasp {Name} Page";
        SetMaterial(page.pagerender.material);
    }

    internal void SetMaterial(Material material)
    {
        var main = GetMainTexture();
        if (main != null)
            material.SetTexture("_bk", main);

        var emi = GetEmissionTexture();
        if (emi != null)
            material.SetTexture("_emistexture", emi);
    }

    internal void SetLight(Light? light)
    {
        if (light == null) return;
        light.color = GlowColor;
    }
}
