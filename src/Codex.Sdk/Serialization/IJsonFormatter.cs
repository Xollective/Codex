using System.Text.Json;
using System.Text.Json.Serialization;

namespace Codex.Utilities.Serialization;

public interface IJsonFormatter<T> 
{
    bool IsApplicable();

    T Read(ref Utf8JsonReader reader, JsonSerializerOptions options);

    void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options);

    JsonConverter<T> AsConverter();
}

public interface IJsonFormatterResolver
{
    IJsonFormatter<T> GetJsonFormatter<T>();
}

public interface IJsonFormatterProvider<T>
{
    IJsonFormatter<T> GetJsonFormatter();
}