using System.Text;
using System.Text.Json;

namespace BlobBackup;

public class Serializer
{
    public byte[] Serialize(object value)
    {
        return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value));
    }

    public T Deserialize<T>(byte[] bytes)
    {
        return JsonSerializer.Deserialize<T>(bytes) ?? throw new Exception($"Deserialization returned null.");
    }
}
