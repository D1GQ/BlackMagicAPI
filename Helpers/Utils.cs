using BetterVoiceDetection;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Serializing;
using FishNet.Transporting;
using System.Reflection;
using UnityEngine;

namespace BlackMagicAPI.Helpers;

public static class Utils
{
    private static readonly Dictionary<Type, Func<PooledReader, object>> _readers = new()
    {
        { typeof(bool), reader => reader.ReadBoolean() },
        { typeof(char), reader => reader.ReadChar() },
        { typeof(short), reader => reader.ReadInt16() },
        { typeof(ushort), reader => reader.ReadUInt16() },
        { typeof(int), reader => reader.ReadInt32() },
        { typeof(uint), reader => reader.ReadUInt32() },
        { typeof(long), reader => reader.ReadInt64() },
        { typeof(ulong), reader => reader.ReadUInt64() },
        { typeof(float), reader => reader.ReadSingle() },
        { typeof(double), reader => reader.ReadDouble() },
        { typeof(decimal), reader => reader.ReadDecimal() },
        { typeof(string), reader => reader.ReadString() },
        { typeof(Vector2), reader => reader.ReadVector2() },
        { typeof(Vector3), reader => reader.ReadVector3() },
        { typeof(Vector4), reader => reader.ReadVector4() },
        { typeof(Vector2Int), reader => reader.ReadVector2Int() },
        { typeof(Vector3Int), reader => reader.ReadVector3Int() },
        { typeof(Color), reader => reader.ReadColor() },
        { typeof(Color32), reader => reader.ReadColor32() },
        { typeof(Rect), reader => reader.ReadRect() },
        { typeof(Plane), reader => reader.ReadPlane() },
        { typeof(Ray), reader => reader.ReadRay() },
        { typeof(Ray2D), reader => reader.ReadRay2D() },
        { typeof(Matrix4x4), reader => reader.ReadMatrix4x4() },
        { typeof(DateTime), reader => reader.ReadDateTime() },
        { typeof(Guid), reader => reader.ReadGuid() },
        { typeof(NetworkObject), reader => reader.ReadNetworkObject() },
        { typeof(NetworkBehaviour), reader => reader.ReadNetworkBehaviour() },
        { typeof(NetworkConnection), reader => reader.ReadNetworkConnection() },
        { typeof(Channel), reader => reader.ReadChannel() },
    };

    private static readonly Dictionary<Type, Action<PooledWriter, object>> _writers = new()
    {
        { typeof(bool), (writer, value) => writer.WriteBoolean((bool)value) },
        { typeof(char), (writer, value) => writer.WriteChar((char)value) },
        { typeof(short), (writer, value) => writer.WriteInt16((short)value) },
        { typeof(ushort), (writer, value) => writer.WriteUInt16((ushort)value) },
        { typeof(int), (writer, value) => writer.WriteInt32((int)value) },
        { typeof(uint), (writer, value) => writer.WriteUInt32((uint)value) },
        { typeof(long), (writer, value) => writer.WriteInt64((long)value) },
        { typeof(ulong), (writer, value) => writer.WriteUInt64((ulong)value) },
        { typeof(float), (writer, value) => writer.WriteSingle((float)value) },
        { typeof(double), (writer, value) => writer.WriteDouble((double)value) },
        { typeof(decimal), (writer, value) => writer.WriteDecimal((decimal)value) },
        { typeof(string), (writer, value) => writer.WriteString((string)value) },
        { typeof(Vector2), (writer, value) => writer.WriteVector2((Vector2)value) },
        { typeof(Vector3), (writer, value) => writer.WriteVector3((Vector3)value) },
        { typeof(Vector4), (writer, value) => writer.WriteVector4((Vector4)value) },
        { typeof(Vector2Int), (writer, value) => writer.WriteVector2Int((Vector2Int)value) },
        { typeof(Vector3Int), (writer, value) => writer.WriteVector3Int((Vector3Int)value) },
        { typeof(Color), (writer, value) => writer.WriteColor((Color)value) },
        { typeof(Color32), (writer, value) => writer.WriteColor32((Color32)value) },
        { typeof(Rect), (writer, value) => writer.WriteRect((Rect)value) },
        { typeof(Plane), (writer, value) => writer.WritePlane((Plane)value) },
        { typeof(Ray), (writer, value) => writer.WriteRay((Ray)value) },
        { typeof(Ray2D), (writer, value) => writer.WriteRay2D((Ray2D)value) },
        { typeof(Matrix4x4), (writer, value) => writer.WriteMatrix4x4((Matrix4x4)value) },
        { typeof(DateTime), (writer, value) => writer.WriteDateTime((DateTime)value) },
        { typeof(NetworkObject), (writer, value) => writer.WriteNetworkObject((NetworkObject)value) },
        { typeof(NetworkBehaviour), (writer, value) => writer.WriteNetworkBehaviour((NetworkBehaviour)value) },
        { typeof(NetworkConnection), (writer, value) => writer.WriteNetworkConnection((NetworkConnection)value) },
        { typeof(Channel), (writer, value) => writer.WriteChannel((Channel)value) },
    };

    public static object ReadFast(this PooledReader reader, Type type)
    {
        if (_readers.TryGetValue(type, out var readerFunc))
        {
            return readerFunc(reader);
        }
        throw new NotSupportedException($"Type {type.Name} is not supported for fast reading");
    }

    public static void WriteFast(this PooledWriter writer, Type type, object value)
    {
        if (_writers.TryGetValue(type, out var writerAction))
        {
            writerAction(writer, value);
        }
        else
        {
            throw new NotSupportedException($"Type {type.Name} is not supported for fast writing");
        }
    }

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
