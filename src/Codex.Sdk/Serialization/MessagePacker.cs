using System.Text.Json;
using System.Text.Json.Serialization;
using MessagePack.Formatters;
using MessagePack;
using Codex.ObjectModel;
using Codex.ObjectModel.CompilerServices;
using MessagePack.Resolvers;
using Codex.Sdk.Utilities;
using Codex.ObjectModel.Internal;
using Codex.Sdk;
using System.Collections.Concurrent;
using System.Text.Encodings.Web;
using CommunityToolkit.HighPerformance;
using System.Text.Json.Serialization.Metadata;

namespace Codex.Utilities.Serialization;

using static TypeSystemHelpers;

[Flags]
public enum PackFlags
{
    None,
    Compressed = 1
}

public static partial class MessagePacker
{
    private static ConcurrentDictionary<(ObjectStage, PackFlags), MessagePackSerializerOptions> _serializerOptionsMap = new();

    public static void PackSerializeEntityTo<TEntity>(this TEntity entity, Stream stream, ObjectStage stage = ObjectStage.All, PackFlags flags = PackFlags.None)
    {
        MessagePackSerializer.Serialize(stream, entity, GetOptions(stage, flags));
    }

    public static byte[] PackSerializeEntity<TEntity>(this TEntity entity, ObjectStage stage = ObjectStage.All, PackFlags flags = PackFlags.None)
    {
        return MessagePackSerializer.Serialize(entity, GetOptions(stage, flags));
    }

    public static TEntity PackDeserializeEntity<TEntity>(this Stream stream, PackFlags flags = PackFlags.None)
    {
        return MessagePackSerializer.Deserialize<TEntity>(stream, GetOptions(flags: flags));
    }

    public static TEntity PackDeserializeEntity<TEntity>(this ReadOnlyMemory<byte> stream, Out<int> consumed = default, ObjectStage stage = ObjectStage.All, PackFlags flags = PackFlags.None)
    {
        var result = MessagePackSerializer.Deserialize<TEntity>(stream, GetOptions(stage, flags), out var bytesRead);
        consumed.Set(bytesRead);
        return result;
    }

    public static MessagePackSerializerOptions GetOptions(ObjectStage stage = ObjectStage.All, PackFlags flags = PackFlags.None)
    {
        var key = (stage, flags);
        if (!_serializerOptionsMap.TryGetValue(key, out var options))
        {
            options = new MessagePackSerializerOptions(GetResolver(stage));
            if (flags.HasFlag(PackFlags.Compressed))
            {
                options = options.WithCompression(MessagePackCompression.Lz4BlockArray);
            }

            _serializerOptionsMap[key] = options;
        }

        return options;
    }


    public static IFormatterResolver GetResolver(ObjectStage stage)
    {
        return ObjectStages.GetBox(stage).Invoke(ResolverFactory.Instance, None.Value);
    }

    private class ResolverFactory : IDerivedFunc<None, IObjectStage, IFormatterResolver>
    {
        public static readonly ResolverFactory Instance = new ResolverFactory();

        public IFormatterResolver Invoke<TArg>(None data, TArg arg) where TArg : IObjectStage
        {
            return FormatterResolver<TArg>.Instance;
        }
    }

    private class EntityJsonTypeInfoResolver : DefaultJsonTypeInfoResolver
    {
        public static EntityJsonTypeInfoResolver Instance { get; } = new();

        public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            if (EntityTypes.Map.ContainsKey(type))
            {
                return base.GetTypeInfo(type, options);
            }
            else
            {
                return GetTypeInfo(typeof(object), options);
            }
        }
    }

    public partial class FormatterResolver<TObjectStage> : IFormatterResolver, IJsonFormatterResolver
        where TObjectStage : IObjectStage
    {
        public static readonly IFormatterResolver Instance = new FormatterResolver<TObjectStage>();

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerOptions.Default)
        {
            TypeInfoResolver = EntityJsonTypeInfoResolver.Instance
        };

        // configure your custom resolvers.
        private static readonly IFormatterResolver[] Resolvers = new IFormatterResolver[]
        {
            StandardResolverAllowPrivate.Instance
        };

        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            return Cache<T>.Formatter;
        }

        IJsonFormatter<T> IJsonFormatterResolver.GetJsonFormatter<T>()
        {
            return Cache<T>.JsonFormatter;
        }

        private static class Cache<T>
        {
            public static IMessagePackFormatter<T> InnerFormatter;

            public static IMessagePackFormatter<T> Formatter;

            public static IJsonFormatter<T> JsonFormatter;

            public static IMessagePackFormatter<TImpl> GetImplFormatter<TImpl, TBase>()
                where TImpl : TBase, IEntity<TImpl, TBase>
            {
                return new DescriptorImplFormatter<TImpl, TBase>()
                    .Apply(f => f.Initialize(TObjectStage.GetValue(), JsonOptions.GetTypeInfo(typeof(TBase))));
            }

            public static IMessagePackFormatter<TBase> GetBaseFormatter<TImpl, TBase>()
                where TImpl : TBase, IEntity<TImpl, TBase>
            {
                return new DescriptorBaseFormatter<TImpl, TBase>()
                    .Apply(f => f.Initialize(TObjectStage.GetValue(), JsonOptions.GetTypeInfo(typeof(TBase))));
            }

            static IMessagePackFormatter<T> GetFormatter()
            {
                var type = typeof(T);
                Type baseType = null;
                if (EntityTypes.ToImplementationMap.TryGetValue(type, out var implType)
                    || EntityTypes.FromImplementationMap.TryGetValue(type, out baseType))
                {
                    implType ??= type;
                    baseType ??= type;

                    bool isImpl = type == implType;

                    var formatter = isImpl
                        ? ReflectionInvoke(() => GetImplFormatter<Span, ISpan>(), new[] { implType, baseType })
                        : ReflectionInvoke(() => GetBaseFormatter<Span, ISpan>(), new[] { implType, baseType });

                    return (IMessagePackFormatter<T>)formatter;
                }

                // configure your custom formatters.
                //if (typeof(T) == typeof(XXX))
                //{
                //    Formatter = new ICustomFormatter();
                //    return;
                //}

                foreach (var resolver in Resolvers)
                {
                    var f = resolver.GetFormatter<T>();
                    if (f != null)
                    {
                        return f;
                    }
                }

                return null;
            }

            static Cache()
            {
                var innerFormatter = InnerFormatter = (PredefinedFormatter<T>.Formatter
                    ?? GetExtensionFormatter<T>()
                    ?? GetFormatter());

                var jsonFormatter = JsonFormatter = (PredefinedFormatter<T>.JsonFormatter
                    ?? InnerFormatter as IJsonFormatter<T>
                    ?? (InnerFormatter as IJsonFormatterProvider<T>)?.GetJsonFormatter()
                    ?? (InnerFormatter as IJsonFormatterResolver)?.GetJsonFormatter<T>());

                Formatter = Wrap(
                   InnerFormatter);
            }
        }
    }

}

