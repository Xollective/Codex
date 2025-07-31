using System.Diagnostics.ContractsLight;

namespace Codex.ObjectModel.CompilerServices
{
    [GeneratorExclude]
    public interface IPropertyTarget<TSource>
    {
        static abstract IBaseDescriptor<TSource> BaseSourceDescriptor { get; }

        //void CopyFrom(TSource source, bool shallow = false);
    }

    public interface IPropertyTarget<TSource, TSourceImpl> : IPropertyTarget<TSource>
        where TSourceImpl : TSource, IEntity<TSourceImpl, TSource>
    {
        static IBaseDescriptor<TSource> IPropertyTarget<TSource>.BaseSourceDescriptor => TSourceImpl.GetDescriptor();
    }

    public static class PropertyTarget
    {
        public static List<T> Coerce<T>(ref List<T> list)
        {
            if (list == null)
            {
                list = new();
            }

            return list;
        }

        public static IReadOnlyList<T> CoerceReadOnly<T>(ref List<T> list)
        {
            return list == null ? Array.Empty<T>() : list;
        }

        public static T CreateCopy<T>(this T source, bool shallow = false)
            where T : IBaseEntity<T, T>
        {
            return source.CreateClone(source, shallow);
        }

        //public static IReadOnlyList<T> CoerceReadOnly<T>(ref IReadOnlyList<T> list)
        //{
        //    return list == null ? Array.Empty<T>() : list;
        //}

        public static TTarget Apply<TSource, TTarget>(this TTarget target, TSource source, bool shallow = false)
            where TTarget : IPropertyTarget<TSource>, IEntity<TTarget>, TSource
        {
            DescriptorBase<TTarget, TSource> d = GetCopyDescriptor<TSource, TTarget>();

            d.CopyFrom(target, source, shallow);

            return target;
        }

        public static DescriptorBase<TTarget, TSource> GetCopyDescriptor<TSource, TTarget>() 
            where TTarget : IPropertyTarget<TSource>, IEntity<TTarget>, TSource
        {
            var targetDescriptor = TTarget.Descriptor;
            ref var d = ref DescriptorRegistrar.Relation<TTarget, TSource>.Descriptor;
            if (d == null)
            {
                var sourceDescriptor = (ISingletonBaseDescriptor<TSource>)TTarget.BaseSourceDescriptor;

                var rd = sourceDescriptor.GetCopyDescriptor<TTarget>();
                Contract.Assert(d == rd);
            }

            return d;
        }

        internal static TTarget Cast<TTarget, TSource>(TSource source)
        {
            if (source is TTarget typedSource)
            {
                return typedSource;
            }
            else
            {
                return (TTarget)(object)source;
            }
        }

        //public static T GetOrCopy<T, TSource>(TSource source, bool shallow = false)
        //    where T : TSource, IEntity<T, TSource>
        //{
        //    if (!shallow)
        //    {
        //        return T.Create(source);
        //    }
        //    else
        //    {
        //        return Cast<T, TSource>(source);
        //    }
        //}

        public static T GetOrCopy<T, TSource>(TSource source, bool shallow = false)
            where T : TSource
        {
            if (!shallow && source is IBaseEntity<T, TSource> sourceTarget)
            {
                return sourceTarget.CreateClone(source);
            }
            else
            {
                return Cast<T, TSource>(source);
            }
        }

        public static IReadOnlyList<T> GetOrCopy<T, TSource>(IReadOnlyList<TSource> source, bool shallow = false)
            where T : TSource
        {
            if (source == null || source.Count == 0) return null;

            if (shallow && source is List<T> sourceList)
            {
                return sourceList;
            }

            var list = new List<T>(source.Count);
            if (source != null)
            {
                foreach (var item in source)
                {
                    list.Add(GetOrCopy<T, TSource>(item, shallow));
                }
            }
            return list;
        }
    }
}