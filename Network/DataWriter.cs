using BlackMagicAPI.Helpers;
using FishNet.Serializing;

namespace BlackMagicAPI.Network;

public class DataWriter : IDisposable
{
    private readonly List<(Type type, object value)> _dataBuffer = [];

    public void Write<T>(T value)
    {
        if (value == null) return;
        _dataBuffer.Add((typeof(T), value));
    }

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
