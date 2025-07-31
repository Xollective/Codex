using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using MessagePack;
using MessagePack.Formatters;

namespace Codex.Utilities;

/// <summary>
/// Creates json converters from delegates
/// </summary>
public class DynamicJsonObjectResolver : IFormatterResolver
{
    public IMessagePackFormatter<T> GetFormatter<T>()
    {
        throw new NotImplementedException();
    }
}

public class DynamicJsonObjectFormatter<T> : IMessagePackFormatter<T>
{
    private Dictionary<string, JsonObjectProperty> PropertiesByName { get; } = new();

    public T Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public void Serialize(ref MessagePackWriter writer, T value, MessagePackSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public record JsonObjectProperty(JsonPropertyInfo JsonProperty)
    {

    }

    public class JsonObjectProperty<TValue>
    {

    }
}
