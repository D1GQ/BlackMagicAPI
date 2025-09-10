using BepInEx;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace BlackMagicAPI.Helpers;

/// <summary>
/// Basic utilities for modding Mage Arena.
/// </summary>
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
    /// <param name="assembly">The assembly to load from.</param>
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
    /// <param name="assembly">The assembly to load from.</param>
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

    /// <summary>
    /// Loads an audio clip from a WAV file on disk.
    /// </summary>
    /// <param name="filePath">Full path to the WAV file to load.</param>
    /// <returns>
    /// The loaded AudioClip if successful, or null if:
    /// - The file doesn't exist
    /// - The file isn't a .wav file
    /// - An error occurs during loading
    /// </returns>
    /// <remarks>
    /// <para>This method performs the following checks:</para>
    /// <list type="number">
    /// <item><description>Verifies the file exists at the specified path</description></item>
    /// <item><description>Validates the file has a .wav extension</description></item>
    /// <item><description>Attempts to load and convert the WAV data to a Unity AudioClip</description></item>
    /// </list>
    /// <para>Errors are logged to the Unity console with detailed messages.</para>
    /// <example>
    /// Basic usage:
    /// <code>
    /// var clip = Utils.LoadWavFromDisk(@"C:\Sounds\effect.wav");
    /// if (clip != null) audioSource.PlayOneShot(clip);
    /// </code>
    /// </example>
    /// </remarks>
    public static AudioClip? LoadWavFromDisk(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError($"File not found: {filePath}");
            return null;
        }

        if (Path.GetExtension(filePath).ToLower() != ".wav")
        {
            Debug.LogError("Only .wav files are supported.");
            return null;
        }

        try
        {
            byte[] wavBytes = File.ReadAllBytes(filePath);
            return WavUtility.ToAudioClip(wavBytes);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to load WAV: {ex}");
            return null;
        }
    }

    /// <summary>
    /// Loads an audio clip from an embedded WAV resource in an assembly.
    /// </summary>
    /// <param name="assembly">The assembly containing the embedded resource.</param>
    /// <param name="resourcePath">
    /// The resource path using dot notation (e.g., "MyNamespace.Resources.sound.wav").
    /// Resource names are case-sensitive.
    /// </param>
    /// <returns>
    /// The loaded AudioClip if successful, or null if:
    /// - The resource isn't found
    /// - An error occurs during loading
    /// </returns>
    /// <remarks>
    /// <para>This extension method performs the following operations:</para>
    /// <list type="number">
    /// <item><description>Locates the embedded resource in the assembly</description></item>
    /// <item><description>Copies the resource data to a memory stream</description></item>
    /// <item><description>Converts the WAV data to a Unity AudioClip</description></item>
    /// </list>
    /// <para>Important notes about embedded resources:</para>
    /// <list type="bullet">
    /// <item><description>The resource must be marked as "Embedded Resource" in the project</description></item>
    /// <item><description>The resource path uses dot notation and doesn't include the assembly name</description></item>
    /// <item><description>Resource paths are case-sensitive</description></item>
    /// </list>
    /// <example>
    /// Basic usage:
    /// <code>
    /// var clip = Assembly.GetExecutingAssembly().LoadWavFromResources("MyMod.Sounds.effect.wav");
    /// if (clip != null) audioSource.PlayOneShot(clip);
    /// </code>
    /// </example>
    /// </remarks>
    public static AudioClip? LoadWavFromResources(this Assembly assembly, string resourcePath)
    {
        try
        {
            using (Stream stream = assembly.GetManifestResourceStream(resourcePath))
            {
                if (stream == null)
                {
                    Debug.LogError($"Resource not found: {resourcePath}");
                    return null;
                }

                using (MemoryStream ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
                    byte[] wavBytes = ms.ToArray();
                    return WavUtility.ToAudioClip(wavBytes);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to load WAV from resources: {ex}");
            return null;
        }
    }

    /// <summary>
    /// Loads an AssetBundle from a file path synchronously.
    /// </summary>
    /// <param name="path">The file path of the AssetBundle.</param>
    /// <returns>The loaded AssetBundle or null if failed.</returns>
    public static AssetBundle? LoadAssetBundleFromDisk(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                Debug.LogError($"AssetBundle file not found at: {path}");
                return null;
            }

            AssetBundle bundle = AssetBundle.LoadFromFile(path);
            if (bundle == null)
            {
                Debug.LogError($"Failed to load AssetBundle from: {path}");
            }
            return bundle;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception loading AssetBundle: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Loads an AssetBundle from embedded resources in the application's assembly.
    /// </summary>
    /// <param name="assembly">The assembly containing the embedded AssetBundle.</param>
    /// <param name="resourcePath">The path to the embedded resource.</param>
    /// <returns>The loaded AssetBundle or null if failed.</returns>
    public static AssetBundle? LoadAssetBundleFromResources(this Assembly assembly, string resourcePath)
    {
        try
        {
            using Stream stream = assembly.GetManifestResourceStream(resourcePath);
            if (stream == null)
            {
                Debug.LogError($"Resource not found: {resourcePath}");
                return null;
            }

            using MemoryStream ms = new();
            stream.CopyTo(ms);
            AssetBundle bundle = AssetBundle.LoadFromMemory(ms.ToArray());
            if (bundle == null)
            {
                Debug.LogError($"Failed to load AssetBundle from embedded resource: {resourcePath}");
            }
            return bundle;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception loading AssetBundle from resources: {ex.Message}");
            return null;
        }
    }

    internal static string Generate9DigitHash(string input)
    {
        using SHA256 sha256 = SHA256.Create();
        byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        uint hashValue = BitConverter.ToUInt32(hashBytes, 0);
        long nineDigitNumber = hashValue % 1000000000L;
        return nineDigitNumber.ToString("000 | 000 | 000");
    }

    internal static string GenerateHash(string input)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(hashBytes).Trim();
        }
    }

    internal static string GetUniqueHash(this BaseUnityPlugin baseUnity)
    {
        string metadataString = $"{baseUnity.Info.Metadata.GUID}|{baseUnity.Info.Metadata.Name}";
        return GenerateHash(metadataString);
    }

    internal static MethodBase PatchRpcMethod<T>(string rpcNamePrefix)
    {
        var methods = typeof(T)
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Where(m => m.Name.StartsWith(rpcNamePrefix))
            .ToList();

        if (methods.Count == 0)
            throw new Exception($"Could not find method that starts with {rpcNamePrefix} in {typeof(T)}");

        return methods[0];
    }

    internal static GameObject? FindInactive(string path, string? sceneName = null)
    {
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("[FindInactive] Path is null or empty");
            return null;
        }

        string[] parts = path.Split('/');
        if (parts.Length == 0)
        {
            Debug.LogError("[FindInactive] Path has no valid segments");
            return null;
        }

        // Search in specific scene if provided, otherwise check all scenes
        bool searchAllScenes = string.IsNullOrEmpty(sceneName);
        Transform? parent = null;

        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);

            // Skip if we're looking for a specific scene and this isn't it
            if (!searchAllScenes && !scene.name.Equals(sceneName))
                continue;

            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name.Equals(parts[0]))
                {
                    parent = root.transform;
                    Debug.Log($"[FindInactive] Found root '{parts[0]}' in scene '{scene.name}'");
                    break;
                }
            }

            if (parent != null) break; // Found our starting point
        }

        if (parent == null)
        {
            Debug.LogError($"[FindInactive] Root object '{parts[0]}' not found {(searchAllScenes ? "in any scene" : $"in scene '{sceneName}'")}");
            return null;
        }

        // Traverse the remaining path
        for (int i = 1; i < parts.Length; i++)
        {
            parent = parent.Find(parts[i]);
            if (parent == null)
            {
                Debug.LogError($"[FindInactive] Child '{parts[i]}' not found under '{parts[i - 1]}'");
                return null;
            }
        }

        Debug.Log($"[FindInactive] Successfully found '{path}' {(searchAllScenes ? "" : $"in scene '{sceneName}'")}");
        return parent.gameObject;
    }
}
