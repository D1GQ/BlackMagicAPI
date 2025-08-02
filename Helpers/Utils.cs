using BetterVoiceDetection;
using System.Reflection;
using UnityEngine;

namespace BlackMagicAPI.Helpers;

public static class Utils
{
    /// <summary>
    /// Loads a sprite from a given path, optionally specifying pixels per unit.
    /// Caches the sprite for future use.
    /// </summary>
    /// <param name="path">The file path of the texture to create the sprite from.</param>
    /// <param name="pixelsPerUnit">The number of pixels per unit for the sprite.</param>
    /// <returns>A Sprite object if successful, or null if it fails to load the sprite.</returns>
    public static Sprite? LoadSpriteFromDisk(string path, float pixelsPerUnit = 1f)
    {
        try
        {
            if (CachedSprites.TryGetValue(path + pixelsPerUnit, out var sprite))
                return sprite;

            var texture = LoadTextureFromDisk(path);
            if (texture == null)
                return null;

            sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
            sprite.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontSaveInEditor;

            return CachedSprites[path + pixelsPerUnit] = sprite;
        }
        catch (Exception ex)
        {
            BMAPlugin.Log.LogError(ex);
            return null;
        }
    }

    /// <summary>
    /// Loads a texture from disk by reading a file at the specified path.
    /// </summary>
    /// <param name="path">The file path of the texture to load.</param>
    /// <returns>A Texture2D object if the texture was successfully loaded, or null if it failed.</returns>
    public static Texture2D? LoadTextureFromDisk(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                Texture2D texture = new Texture2D(2, 2, TextureFormat.ARGB32, false);
                byte[] byteTexture = File.ReadAllBytes(path);

                bool isLoaded = texture.LoadImage(byteTexture, false);

                if (isLoaded)
                    return texture;
                else
                    BMAPlugin.Log.LogError("Failed to load image data into texture.");
            }
            else
            {
                BMAPlugin.Log.LogError("File does not exist: " + path);
            }
        }
        catch (Exception ex)
        {
            BMAPlugin.Log.LogError("Exception while loading texture: " + ex);
        }
        return null;
    }

    internal static Dictionary<string, Sprite> CachedSprites = [];

    /// <summary>
    /// Loads a sprite from a given path, optionally specifying pixels per unit.
    /// Caches the sprite for future use.
    /// </summary>
    /// <param name="path">The file path of the texture to create the sprite from.</param>
    /// <param name="pixelsPerUnit">The number of pixels per unit for the sprite.</param>
    /// <returns>A Sprite object if successful, or null if it fails to load the sprite.</returns>
    public static Sprite? LoadSpriteFromResources(this Assembly assembly, string path, float pixelsPerUnit = 1f)
    {
        try
        {
            if (CachedSprites.TryGetValue(path + pixelsPerUnit, out var sprite))
                return sprite;

            var texture = LoadTextureFromResources(assembly, path);
            if (texture == null)
                return null;

            sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
            sprite.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontSaveInEditor;

            return CachedSprites[path + pixelsPerUnit] = sprite;
        }
        catch (Exception ex)
        {
            BMAPlugin.Log.LogError(ex);
            return null;
        }
    }

    /// <summary>
    /// Loads a texture from embedded resources in the application's assembly.
    /// </summary>
    /// <param name="path">The path to the texture resource.</param>
    /// <returns>A Texture2D object if the texture was loaded successfully, or null if it failed.</returns>
    public static Texture2D? LoadTextureFromResources(this Assembly assembly, string path)
    {
        try
        {
            var stream = assembly.GetManifestResourceStream(path);
            if (stream == null)
                return null;

            var texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                if (!texture.LoadImage(ms.ToArray(), false))
                    return null;
            }

            return texture;
        }
        catch (Exception ex)
        {
            BMAPlugin.Log.LogError(ex);
            return null;
        }
    }
}
