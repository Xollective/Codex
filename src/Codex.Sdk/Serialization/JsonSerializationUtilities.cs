using System.Text.Json.Serialization.Metadata;
using System.Text.Json;
using System.Globalization;
using System.Reflection;
using System.Collections.Concurrent;
using Codex.ObjectModel.Attributes;
using Codex.ObjectModel.Implementation;
using System.Collections;
using System.Text.Json.Serialization;
using Codex.ObjectModel;
using Codex.Utilities.Serialization;
using System.Text.Encodings.Web;
using System;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Text;
using Codex.Sdk.Utilities;

namespace Codex.Utilities;

using static TypeSystemHelpers;

[Flags]
public enum JsonFlags
{
    None,
    Indented = 1,
    Track = 1 << 1,
    PrimitivesAsString = 1 << 2,
    IncludeFields = 1 << 3,
}

public static partial class JsonSerializationUtilities
{
    private static ConcurrentDictionary<(ObjectStage? stage, JsonFlags flags), JsonSerializerOptions> _serializerOptionsMap = new();

    public static void SerializeEntityTo<TEntity>(
        this TEntity entity,
        Stream stream,
        ObjectStage stage = ObjectStage.All,
        JsonFlags flags = default,
        Type overrideType = default)
    {
        var type = overrideType ?? ((typeof(TEntity) == typeof(object) && (entity != null)) 
            ? entity.GetType() 
            : typeof(TEntity));
        JsonSerializer.Serialize(stream, entity, type, GetOptions(stage, flags));
    }

    public static string SerializeEntity<TEntity>(this TEntity entity, ObjectStage stage = ObjectStage.All, JsonFlags flags = default)
    {
        return JsonSerializer.Serialize(entity, GetOptions(stage, flags));
    }

    public static TEntity DeserializeEntity<TEntity>(this Stream stream)
    {
        return JsonSerializer.Deserialize<TEntity>(stream, GetOptions());
    }

    public static TEntity DeserializeEntity<TEntity>(this string stream)
    {
        return DeserializeEntity<TEntity>(stream.AsSpan());
    }

    public static TEntity DeserializeEntity<TEntity>(this ReadOnlySpan<char> stream)
    {
        return JsonSerializer.Deserialize<TEntity>(stream, GetOptions());
    }

    public static TEntity DeserializeEntity<TEntity>(this ReadOnlySpan<byte> utf8Json)
    {
        return JsonSerializer.Deserialize<TEntity>(utf8Json, GetOptions());
    }

    public static JsonSerializerOptions GetOptions(ObjectStage? stage = null, bool indented = false, bool track = false)
    {
        return GetOptions(stage, 
            (indented ? JsonFlags.Indented : JsonFlags.None)
            | (track ? JsonFlags.Track : JsonFlags.None)
            );
    }

    public static JsonSerializerOptions ModifyOptions(JsonSerializerOptions options, ObjectStage? stage, JsonFlags flags = JsonFlags.None)
    {
        options.AllowTrailingCommas = true;
        options.PropertyNameCaseInsensitive = true;
        options.ReadCommentHandling = JsonCommentHandling.Skip;
        options.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault;
        options.TypeInfoResolver = new FilteringJsonTypeResolver(stage);
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.WriteIndented = flags.HasFlag(JsonFlags.Indented);
        options.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
        options.NumberHandling = JsonNumberHandling.AllowReadingFromString;
        options.Converters.Add(new JsonStringEnumConverter());
        if (flags.HasFlag(JsonFlags.IncludeFields))
        {
            options.IncludeFields = true;
        }
        if (flags.HasFlag(JsonFlags.PrimitivesAsString))
        {
            options.Converters.Add(new PrimitiveTypeAsStringConverter());
        }
        options.Converters.Add(new JsonFormatterConverterFactory((IJsonFormatterResolver)MessagePacker.GetResolver(stage ?? ObjectStage.None)));
        return options;
    }

    public static JsonSerializerOptions GetOptions(ObjectStage? stage, JsonFlags flags = JsonFlags.None)
    {
        bool indented = flags.HasFlag(JsonFlags.Indented);
        bool track = flags.HasFlag(JsonFlags.Track);
        var key = (stage, flags);
        if (!_serializerOptionsMap.TryGetValue(key, out var options))
        {
            var innerOptions = ModifyOptions(new JsonSerializerOptions(), stage, flags);

            _serializerOptionsMap[key with { flags = key.flags & ~JsonFlags.Track }] = innerOptions;
            options = new JsonSerializerOptions(innerOptions)
            {
                Converters =
                {
                    new JsonRangeTrackingConverterFactory(innerOptions)
                }
            };

            _serializerOptionsMap[key with { flags = key.flags | JsonFlags.Track }] = options;
        }

        var result = _serializerOptionsMap[key];
        return result;
    }
    public static Type MapToImplementation(Type type)
    {
        if (EntityTypes.ToImplementationMap.TryGetValue(type, out var implType))
        {
            return implType;
        }

        return type;
    }

    public static Type MapFromImplementation(Type type)
    {
        if (EntityTypes.FromImplementationMap.TryGetValue(type, out var baseType))
        {
            return baseType;
        }

        return type;
    }

    public static void ForceSkip(this ref Utf8JsonReader reader)
    {
        if (reader.TrySkip())
        {
            return;
        }

        if (reader.TokenType == JsonTokenType.StartObject ||
            reader.TokenType == JsonTokenType.StartArray)
        {
            int depth = 0;
            do
            {
                if (reader.TokenType == JsonTokenType.StartObject ||
                    reader.TokenType == JsonTokenType.StartArray)
                {
                    depth++;
                }
                else if (reader.TokenType == JsonTokenType.EndObject ||
                         reader.TokenType == JsonTokenType.EndArray)
                {
                    depth--;
                }
            } while (depth > 0 && reader.Read());

            // Now positioned at the matching EndObject/EndArray.
            reader.Read(); // advance once more to move past it
        }
        else
        {
            // primitives: string, number, bool, null, property name
            reader.Read();
        }
    }

    private class FilteringJsonTypeResolver : DefaultJsonTypeInfoResolver
    {
        public ObjectStage? Stage { get; }

        public JsonSerializerOptions includeFieldsOptions;

        public FilteringJsonTypeResolver(ObjectStage? stage)
        {
            Stage = stage;
        }

        public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            var typeInfo = base.GetTypeInfo(type, options);

            if (Stage is ObjectStage stage)
            {
                // Serializing


                //if (typeInfo.Kind != JsonTypeInfoKind.Object
                //    && type.GetAttribute<DataContractAttribute>() != null)
                //{
                //    typeInfo = ReflectionInvoke<JsonTypeInfo>(
                //        () => CreateObjectInfo<object>(default),
                //        typeParams: new[] { type },
                //        options);
                //}

                if (typeInfo.Kind == JsonTypeInfoKind.Object)
                {
                    var isDataContract = typeInfo.Type.GetCustomAttribute<DataContractAttribute>() != null;

                    if (isDataContract)
                    {
                        foreach (var field in type.GetFields(FlattenPublicInstanceFlags))
                        {
                            if (field.GetCustomAttribute<DataMemberAttribute>() != null)
                            {
                                Contract.Check(field.GetCustomAttribute<JsonIncludeAttribute>() != null)
                                    ?.Assert($"{type.Name} has field {field.Name} without {nameof(JsonIncludeAttribute)}");
                            }
                        }
                    }

                    for (int i = typeInfo.Properties.Count - 1; i >= 0; i--)
                    {
                        var property = typeInfo.Properties[i];
                        if (property.AttributeProvider.ShouldRemoveProperty(stage, isDataContract: isDataContract))
                        {
                            typeInfo.Properties.RemoveAt(i);
                        }
                    }

                    // Sort properties by name to ensure consistent output
                    int j = 0;
                    var implType = MapToImplementation(type);
                    foreach (var prop in typeInfo.Properties.OrderBy(p => p.Name))
                    {
                        typeInfo.Properties[j++] = prop;
                    }
                }
            }
            else
            {
                // Deserializing
            }

            return typeInfo;
        }
    }

    public class JsonFormatterConverterFactory : JsonConverterFactoryBase
    {
        public JsonFormatterConverterFactory(IJsonFormatterResolver resolver)
        {
            Resolver = resolver;
        }

        public IJsonFormatterResolver Resolver { get; }

        private static JsonTypeInfo<T> CreateObjectInfo<T>(JsonSerializerOptions options)
        {
            return JsonMetadataServices.CreateObjectInfo<T>(options, new JsonObjectInfoValues<T>());
        }

        protected override bool CanConvert<T>(Type typeToConvert)
        {
            if (EntityTypes.FromAdapterImplementationMap.ContainsKey(typeToConvert) &&
                typeToConvert.GetAttribute<DataContractAttribute>() != null)
            {
                return true;
            }

            var formatter = Resolver.GetJsonFormatter<T>();
            return formatter != null;
        }

        protected override JsonConverter<T> CreateConverter<T>(Type typeToConvert, JsonSerializerOptions options)
        {
            var formatter = Resolver.GetJsonFormatter<T>();
            if (formatter == null)
            {
                return (JsonConverter<T>)CreateObjectInfo<T>(options).Converter;
            }

            return formatter.AsConverter();
        }
    }

    public class PrimitiveTypeAsStringConverter : JsonConverterFactoryBase<IConvertible>
    {
        protected override JsonConverter<T> CreateConverterCore<T, TImpl>(Type typeToConvert, JsonSerializerOptions options)
        {
            return FuncFormatter.CreateJson(Read<T>, Write, (typeToConvert, (JsonConverter<T>)JsonSerializerOptions.Default.GetConverter(typeof(T)))).AsConverter();
        }

        private void Write<T>(Utf8JsonWriter writer, T value, bool asPropertyName, JsonSerializerOptions options, (Type typeToConvert, JsonConverter<T> converter) data)
            where T : IConvertible
        {
            data.converter.Write(writer, value, options);
        }

        private T Read<T>(ref Utf8JsonReader reader, JsonSerializerOptions options, (Type typeToConvert, JsonConverter<T> converter) data)
            where T : IConvertible
        {
            if (reader.TokenType == JsonTokenType.String || reader.TokenType == JsonTokenType.PropertyName)
            {
                return (T)Convert.ChangeType(reader.GetString(), typeof(T));
            }

            return data.converter.Read(ref reader, data.typeToConvert, options);
        }

        public override bool CanConvert(Type typeToConvert)
        {
            if (!typeToConvert.IsValueType) return false;
            if (!typeToConvert.IsPrimitive) return false;

            return base.CanConvert(typeToConvert);
        }
    }

    public abstract class JsonConverterFactoryBase<TBase> : JsonConverterFactoryBase
    {
        protected override JsonConverter<T> CreateConverter<T>(Type typeToConvert, JsonSerializerOptions options)
        {
            return (JsonConverter<T>)ReflectionInvoke(
                () => this.CreateConverterCore<TBase, TBase>(default, default),
                new[] { typeToConvert, MapToImplementation(typeToConvert) },
                this, typeToConvert, options);
        }

        protected abstract JsonConverter<T> CreateConverterCore<T, TImpl>(Type typeToConvert, JsonSerializerOptions options)
            where TImpl : T, TBase
            where T : TBase;

        protected override bool CanConvert<T>(Type typeToConvert)
        {
            throw new NotImplementedException();
        }

        public override bool CanConvert(Type typeToConvert)
        {
            return MapToImplementation(typeToConvert).IsAssignableTo(typeof(TBase));
        }
    }

    public abstract class JsonConverterFactoryBase : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return (bool)ReflectionInvoke(() => this.CanConvert<object>(default),
                new[] { typeToConvert },
                this, typeToConvert);
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            return (JsonConverter)ReflectionInvoke(
                () => this.CreateConverter<object>(default, default),
                new[] { typeToConvert },
                this, typeToConvert, options);
        }

        protected abstract JsonConverter<T> CreateConverter<T>(Type typeToConvert, JsonSerializerOptions options);

        protected abstract bool CanConvert<T>(Type typeToConvert);
    }

    public class JsonRangeTrackingConverterFactory : JsonConverterFactoryBase<IJsonRangeTrackingBase>
    {
        private JsonSerializerOptions _innerOptions;

        public JsonRangeTrackingConverterFactory(JsonSerializerOptions innerOptions)
        {
            _innerOptions = innerOptions;
        }

        protected override JsonConverter<T> CreateConverterCore<T, TImpl>(Type typeToConvert, JsonSerializerOptions options)
        {
            return ReflectionInvoke<JsonConverter<T>>(
                () => CreatRangeTrackingConverter<IDefinitionSymbol, IDefinitionSymbol>(default),
                typeParams: new[] { typeToConvert, MapFromImplementation(typeToConvert) },
                _innerOptions);
        }

        public static JsonConverter<T> CreatRangeTrackingConverter<T, TBase>(JsonSerializerOptions innerOptions)
            where T : TBase, IJsonRangeTracking<TBase>
        {
            return new JsonRangeTrackingConverter<T, TBase>(innerOptions);
        }
    }

    public class JsonRangeTrackingConverter<T, TBase> : JsonConverter<T>
        where T : TBase, IJsonRangeTracking<TBase>
    {
        private JsonSerializerOptions _innerOptions;

        public JsonRangeTrackingConverter(JsonSerializerOptions innerOptions)
        {
            _innerOptions = innerOptions;
        }

        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return JsonSerializer.Deserialize<T>(ref reader, _innerOptions);
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            var start = writer.BytesCommitted + writer.BytesPending;
            JsonSerializer.Serialize(writer, value, _innerOptions);
            var end = writer.BytesCommitted + writer.BytesPending;

            value.JsonRange = new Extent<TBase>(Extent.FromBounds((int)start, (int)end));
        }
    }
}