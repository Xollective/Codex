using MessagePack.Formatters;
using Codex.ObjectModel;
using MessagePack;
using Codex.Sdk.Utilities;
using System.Text.Json;
using System.Reflection.PortableExecutable;
using System.Text.Json.Serialization;
using System.Buffers;

namespace Codex.Utilities.Serialization;

using static Codex.Sdk.Utilities.Base64;
using static TypeSystemHelpers;

public static partial class MessagePacker
{
    private static IMessagePackFormatter<T> Wrap<T>(IMessagePackFormatter<T> formatter)
    {
        var type = typeof(T);
        if (type.IsAssignableTo(typeof(IJsonRangeTrackingBase)))
        {
            var property = JsonSerializationUtilities.MapToImplementation(type).GetProperty(nameof(IJsonRangeTracking<Standin>.JsonRange));
            var rangeType = property.PropertyType.GenericTypeArguments[0].GenericTypeArguments[0];

            return ReflectionInvoke<IMessagePackFormatter<T>>(
                () => WrapRangeTracking<Standin, Standin>(default),
                typeParams: new[] { type, rangeType },
                formatter);
        }

        return formatter;
    }

    private static IMessagePackFormatter<T> GetExtensionFormatter<T>()
    {
        var type = typeof(T);
        if (type.IsAssignableTo(typeof(IJsonConvertible<T>)))
        {
            var jsonFormatType = ReflectionInvoke<Type>(
                () => GetJsonFormat<Standin>(),
                typeParams: new[] { type });
            return ReflectionInvoke<IMessagePackFormatter<T>>(
                () => GetConvertibleFormatter<Standin, int>(),
                typeParams: new[] { type, jsonFormatType });
        }

        if (type.IsAssignableTo(typeof(IStringEnum)))
        {
            return ReflectionInvoke<IMessagePackFormatter<T>>(
                () => GetStringEnumFormatter<StringEnum<SymbolKinds>>(),
                typeParams: new[] { type });
        }

        if (type.IsAssignableTo(typeof(IBytesStruct)))
        {
            return ReflectionInvoke<IMessagePackFormatter<T>>(
                () => GetBytesStructFormatter<ShortHash>(),
                typeParams: new[] { type });
        }

        if (type.IsAssignableTo(typeof(IBinaryItem)))
        {
            return ReflectionInvoke<IMessagePackFormatter<T>>(
                () => GetBinaryItemFormatter<BinaryItem.MemoryBinaryItem>(),
                typeParams: new[] { type });
        }

        if (EntityTypes.ToAdapterImplementationMap.TryGetValue(type, out var adapterImplType)
            && type != adapterImplType)
        {
            Contract.Assert(adapterImplType.IsAssignableTo(type));
            return ReflectionInvoke<IMessagePackFormatter<T>>(
                () => GetDerivedFormatter<IPropertyMap, PropertyMap>(),
                typeParams: new[] { type, adapterImplType });
        }

        if (type.IsValueType 
            && type.IsConstructedGenericType
            && type.IsAssignableTo(typeof(System.Collections.IEnumerable))
            && type.GetGenericTypeDefinition() == typeof(ArraySegment<>))
        {
            
        }

        return null;
    }

    public static IMessagePackFormatter<T> GetBinaryItemFormatter<T>()
        where T : struct, IBinaryItem<T>
    {
        T read(ref MessagePackReader reader, MessagePackSerializerOptions options, None data)
        {
            throw new NotImplementedException();
        }

        void write(ref MessagePackWriter writer, T value, MessagePackSerializerOptions options, None data)
        {
            using var scope = value.GetSpan().AsScope();
            writer.Write(scope.Span);
        }

        return FuncFormatter.Create(read, write, None.Value);
    }

    public static IMessagePackFormatter<T> GetBytesStructFormatter<T>()
        where T : unmanaged
    {
        T read(ref MessagePackReader reader, MessagePackSerializerOptions options, None data)
        {
            T result = default;
            reader.ReadBytes()?.CopyTo(result.AsWritableBytes());
            return result;
        }

        void write(ref MessagePackWriter writer, T value, MessagePackSerializerOptions options, None data)
        {
            using var scope = value.AsBytes().AsScope();
            writer.Write(scope.Span);
        }

        return FuncFormatter.Create(read, write, None.Value);
    }

    public static IMessagePackFormatter<T> CreateConversionFormatter<T, TFormat>(Func<T, TFormat> toFormat, Func<TFormat, T> fromFormat, IJsonFormatter<T> jsonFormatter = null)
    {
        T read(ref MessagePackReader reader, MessagePackSerializerOptions options, None data)
        {
            var formatter = options.Resolver.GetFormatterWithVerify<TFormat>();
            var value = formatter.Deserialize(ref reader, options);
            return fromFormat(value);
        }

        void write(ref MessagePackWriter writer, T value, MessagePackSerializerOptions options, None data)
        {
            var formatter = options.Resolver.GetFormatterWithVerify<TFormat>();
            var formatValue = toFormat(value);
            formatter.Serialize(ref writer, formatValue, options);
        }

        return FuncFormatter.Create(read, write, None.Value, jsonFormatter ?? CreateConversionJsonFormatter(toFormat, fromFormat));
    }

    public static IJsonFormatter<T> CreateConversionJsonFormatter<T, TFormat>(Func<T, TFormat> toFormat, Func<TFormat, T> fromFormat)
    {
        JsonConverter<TFormat> _converter = null;
        var typeToConvert = typeof(TFormat);
        JsonConverter<TFormat> getConverter(JsonSerializerOptions options)
        {
            return _converter ?? (JsonConverter<TFormat>)options.GetConverter(typeof(TFormat));
        }

        T read(ref Utf8JsonReader reader, JsonSerializerOptions options, None data)
        {
            var converter = getConverter(options);
            var value = converter.Read(ref reader, typeToConvert, options);
            return fromFormat(value);
        }

        void write(Utf8JsonWriter writer, T value, bool asPropertyName, JsonSerializerOptions options, None data)
        {
            var formatValue = toFormat(value);
            if (asPropertyName && formatValue is string stringValue)
            {
                writer.WritePropertyName(stringValue);
            }
            else
            {
                var converter = getConverter(options);
                converter.Write(writer, formatValue, options);
            }
        }

        return FuncFormatter.CreateJson(read, write, None.Value);
    }

    private static IMessagePackFormatter<T> GetStringEnumFormatter<T>()
        where T : IStringEnum<T>
    {
        T read(ref MessagePackReader reader, MessagePackSerializerOptions options, None data)
        {
            if (reader.NextMessagePackType == MessagePackType.Integer)
            {
                return T.FromInteger(reader.ReadInt32());
            }
            else
            {
                Contract.Check(reader.NextMessagePackType == MessagePackType.String)?
                    .Assert($"Unexpected type: {reader.NextMessagePackType}. Expected {nameof(MessagePackType.String)}.");
                return T.FromString(reader.ReadString());
            }
        }

        void write(ref MessagePackWriter writer, T value, MessagePackSerializerOptions options, None data)
        {
            if (value.IntegralValue != null)
            {
                writer.Write(value.IntegralValue.Value);
            }
            else
            {
                writer.Write(value.StringValue);
            }
        }

        return FuncFormatter.Create(read, write, None.Value, CreateConversionJsonFormatter<T, string>(
            s => s.ToString(), 
            T.FromString));
    }

    private static Type GetJsonFormat<T>()
        where T : IJsonConvertible
    {
        var jsonFormatType = T.JsonFormatType;
        Contract.Assert(typeof(T) != jsonFormatType);
        return jsonFormatType;
    }

    private static IMessagePackFormatter<TBase> GetDerivedFormatter<TBase, TDerived>()
        where TDerived : TBase
    {
        return CreateConversionFormatter<TBase, TDerived>(baseValue => (TDerived)baseValue, derivedValue => derivedValue);
    }

    private static IMessagePackFormatter<T> GetConvertibleFormatter<T, TJsonFormat>()
        where T : IJsonConvertible<T, TJsonFormat>
    {
        Contract.Assert(typeof(T) != typeof(TJsonFormat));
        return CreateConversionFormatter<T, TJsonFormat>(value => value.ConvertToJson(), T.ConvertFromJson);
    }

    private static IMessagePackFormatter<T> WrapRangeTracking<T, TBase>(IMessagePackFormatter<T> formatter)
        where T : TBase, IJsonRangeTracking<TBase>
    {
        T read(ref MessagePackReader reader, MessagePackSerializerOptions options, None data)
        {
            var start = (int)reader.Consumed;
            var value = formatter.Deserialize(ref reader, options);
            var end = (int)reader.Consumed;

            if (Features.TrackRangesOnRead)
            {
                value.JsonRange = new Extent<TBase>(Extent.FromBounds(start, end));
            }

            return value;
        }

        void write(ref MessagePackWriter writer, T value, MessagePackSerializerOptions options, None data)
        {
            var start = (int)writer.WrittenBytes;
            formatter.Serialize(ref writer, value, options);
            var end = (int)writer.WrittenBytes;
            value.JsonRange = new Extent<TBase>(Extent.FromBounds(start, end));
        }

        return FuncFormatter.Create(read, write, None.Value);
    }

    private class Standin : IJsonConvertible<Standin, int>, IJsonRangeTracking<Standin>, IShouldSerializeProperty<int>
    {
        public Extent<Standin>? JsonRange { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public static Standin ConvertFromJson(int jsonFormat)
        {
            throw new NotImplementedException();
        }

        public static bool ShouldSerializeProperty(int obj, string propertyName)
        {
            throw new NotImplementedException();
        }

        public int ConvertToJson()
        {
            throw new NotImplementedException();
        }
    }
}

