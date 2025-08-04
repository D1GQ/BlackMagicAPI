using BlackMagicAPI.Helpers;
using FishNet.Serializing;

namespace BlackMagicAPI.Network;

/// <summary>
/// A disposable buffer for serializing and deserializing game data.
/// </summary>
public class DataWriter : IDisposable
{
    private readonly List<(Type type, object value)> _dataBuffer = [];

    /// <summary>
    /// Writes a strongly-typed value to the buffer.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to write (ignored if null).</param>
    public void Write<T>(T value)
    {
        if (value == null) return;
        _dataBuffer.Add((typeof(T), value));
    }

    /// <summary>
    /// Writes an object to the buffer using its runtime type.
    /// </summary>
    /// <param name="value">The value to write (throws if null).</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public void Write(object value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));
        _dataBuffer.Add((value.GetType(), value));
    }

    internal void WriteFromBuffer(PooledWriter writer)
    {
        writer.Write((byte)_dataBuffer.Count);
        foreach (var (type, value) in _dataBuffer)
        {
            writer.Write(type.AssemblyQualifiedName);
            writer.WriteFast(type, value);
        }
    }

    internal void ReadToBuffer(PooledReader reader)
    {
        ClearBuffer();
        byte count = reader.Read<byte>();
        for (int i = 0; i < count; i++)
        {
            string typeName = reader.ReadString();
            Type type = Type.GetType(typeName) ??
                throw new InvalidOperationException($"Type not found: {typeName}");
            var value = reader.ReadFast(type);
            _dataBuffer.Add((value.GetType(), value));
        }
    }

    internal object[] GetObjectBuffer()
    {
        var arr = new object[_dataBuffer.Count];
        for (int i = 0; i < _dataBuffer.Count; i++)
            arr[i] = _dataBuffer[i].value;
        return arr;
    }

    internal void ClearBuffer()
    {
        _dataBuffer.Clear();
    }

    public void Dispose()
    {
        ClearBuffer();
        GC.SuppressFinalize(this);
    }
}
