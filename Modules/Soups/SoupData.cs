using BepInEx;
using BlackMagicAPI.Helpers;
using BlackMagicAPI.Modules.Soups;
using System.Reflection;
using UnityEngine;

namespace BlackMagicAPI.Modules.Spells;

/// <summary>
/// Abstract base class representing soup data and configuration.
/// Provides core functionality for soup identification, visual properties, and effect loading.
/// </summary>
public abstract class SoupData
{
    /// <summary>
    /// Gets the display name of the soup.
    /// </summary>
    /// <remarks>
    /// This must be implemented by derived classes to provide the soup's proper name.
    /// </remarks>
    public abstract string Name { get; }

    /// <summary>
    /// Gets the description text shown when consuming the soup.
    /// </summary>
    /// <remarks>
    /// This must be implemented by derived classes to provide the consumption description.
    /// </remarks>
    public abstract string ConsumeDescription { get; }

    /// <summary>
    /// Gets the color of the soup liquid.
    /// </summary>
    /// <remarks>
    /// This must be implemented by derived classes to define the visual appearance of the soup.
    /// </remarks>
    public abstract Color SoupColor { get; }

    /// <summary>
    /// Gets the unique numeric identifier for the associated item type.
    /// </summary>
    /// <remarks>
    /// This value is set internally by the API and should not be modified manually.
    /// </remarks>
    public int ItemId { get; internal set; }

    /// <summary>
    /// Gets the unique numeric identifier for the soup type.
    /// </summary>
    /// <remarks>
    /// This value is set internally by the API and should not be modified manually.
    /// </remarks>
    public int SoupId { get; internal set; }

    /// <summary>
    /// The plugin that adds the soup.
    /// </summary>
    public BaseUnityPlugin? Plugin { get; internal set; }

    internal int RequiredItemId { get; set; } = -1;

    /// <summary>
    /// Loads the UI sprite for this soup from the plugin's resources.
    /// </summary>
    /// <returns>
    /// The custom UI sprite if found in the plugin's Sprites directory,
    /// otherwise the default empty UI sprite from the API resources.
    /// Returns null if no plugin is associated.
    /// </returns>
    /// <remarks>
    /// Looks for a PNG file named "{SoupName}_Ui.png" in the plugin's Sprites folder.
    /// Removes spaces from the soup name when constructing the filename.
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

        return Assembly.GetExecutingAssembly().LoadSpriteFromResources("BlackMagicAPI.Resources.Items.Empty_Ui.png");
    }

    /// <summary>
    /// Gets the prefab containing the SoupEffect for this soup.
    /// </summary>
    /// <returns>
    /// A Task that resolves to the SoupEffect prefab, or null if no custom prefab is provided.
    /// </returns>
    /// <remarks>
    /// Override this in derived classes to provide custom soup effect prefabs.
    /// The default implementation returns null, which may use a generic effect.
    /// </remarks>
    public virtual Task<SoupEffect?> GetEffectPrefab() => Task.FromResult<SoupEffect?>(null);

    /// <summary>
    /// Configures the visual transform properties for the soup render object.
    /// </summary>
    /// <param name="render">The GameObject representing the soup visual representation.</param>
    /// <remarks>
    /// Override this method to customize the position, rotation, and scale of the soup visual.
    /// The default implementation provides standard positioning for soup objects.
    /// </remarks>
    public virtual void SetObjectVisualTransform(GameObject render)
    {
        render.transform.localPosition = new Vector3(-0.0003f, 0.004f, 0.004f);
        render.transform.rotation = Quaternion.Euler(-3f, 0f, 0f);
        render.transform.localScale = render.transform.localScale * 0.03f;
    }
}