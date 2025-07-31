using System.Text.Json;
using System.Text.Json.Serialization;

namespace Codex.Utilities.Serialization;

[GeneratorExclude]
public interface IJsonConvertible
{
    static virtual Type JsonFormatType { get; }
}

[GeneratorExclude]
public interface IJsonConvertible<TSelf> : IJsonConvertible
{
}

public interface IStringConvertible<TSelf> : IJsonConvertible<TSelf, string>
    where TSelf : IStringConvertible<TSelf>
{
    static Type IJsonConvertible.JsonFormatType => typeof(string);
}

[GeneratorExclude]
public interface IJsonSerializable
{
}

[GeneratorExclude]
public interface IJsonSerializable<TSelf> : IJsonSerializable
    where TSelf : IJsonSerializable<TSelf>
{
    static abstract TSelf Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options);

    static abstract void Write(Utf8JsonWriter writer, TSelf value, JsonSerializerOptions options);
}

[GeneratorExclude]
public interface IJsonConvertible<T, TJsonFormat> : IJsonConvertible<T>
    where T : IJsonConvertible<T, TJsonFormat>
{
    static Type IJsonConvertible.JsonFormatType => typeof(TJsonFormat);

    static abstract T ConvertFromJson(TJsonFormat jsonFormat);

    //static Type JsonFormatType => typeof(TJsonFormat);

    TJsonFormat ConvertToJson();

}

public static class JsonHelpers
{
    public static JsonWrap<T> JsonWrap<T>(this T value) => new(value);
}

public record struct JsonWrap<T>(T Value) : IJsonConvertible<JsonWrap<T>, T>
{
    public static JsonWrap<T> ConvertFromJson(T jsonFormat)
    {
        return new(jsonFormat);
    }

    public T ConvertToJson()
    {
        return Value;
    }
}