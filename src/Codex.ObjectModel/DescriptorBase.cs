using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using Codex.ObjectModel.CompilerServices;

namespace Codex.ObjectModel
{
    [GeneratorExclude]
    public interface IDescriptor
    {
        IDescriptorProperty GetProperty(uint fieldNumber);

        int PropertyCount { get; }
        int MaxFieldNumber { get; }
    }

    [GeneratorExclude]
    public interface ISingletonDescriptor : IDescriptor
    {
        CopyDescriptor<TImplTarget, TBaseSource> GetCopyDescriptor<TImplTarget, TBaseSource, TBaseDescriptor>()
            where TBaseDescriptor : ISingletonDescriptor<TBaseDescriptor>, IBaseDescriptor<TBaseSource>
            where TImplTarget : TBaseSource;
    }

    [GeneratorExclude]
    public interface IDescriptorProperty
    {
        uint FieldNumber { get; }
        string Name { get; }
    }

    [GeneratorExclude]
    public interface IBaseDescriptor<TBase> : IDescriptor
    {
        IReadOnlyList<IBaseSourceProperty<TBase>> Properties { get; }
    }

    [GeneratorExclude]
    public interface ISingletonBaseDescriptor<TBase> : IBaseDescriptor<TBase>
    {
        CopyDescriptor<TImplTarget, TBase> GetCopyDescriptor<TImplTarget>()
            where TImplTarget : TBase;
    }

    [GeneratorExclude]
    public interface ISingletonDescriptor<TSelf> : ISingletonDescriptor
        where TSelf : ISingletonDescriptor<TSelf>
    {
        static abstract TSelf Instance { get; }
    }

    [GeneratorExclude]
    public interface IBaseSourceProperty<TBase> : IDescriptorProperty
    {
        void AddPropertyFromSource<TImpl>(CopyDescriptor<TImpl, TBase> copyDescriptor, IDescriptorProperty targetProperty)
                where TImpl : TBase;
    }

    [GeneratorExclude]
    public interface ITargetProperty<TImpl, TFieldImpl> : IDescriptorProperty
    {
        Action<TImpl, TFieldImpl> SetImplProperty { get; }
    }

    public interface IPropertyVisitor<TImpl, TBase>
        where TImpl : TBase
    {
        void Visit<TFieldBase, TFieldImpl>(DescriptorBase<TImpl, TBase>.Property<TFieldImpl, TFieldBase> property)
            where TFieldImpl : TFieldBase;

        void Visit<TFieldBase, TFieldImpl>(DescriptorBase<TImpl, TBase>.ListProperty<TFieldImpl, TFieldBase> property)
            where TFieldImpl : TFieldBase;
    }

    public class CopyDescriptor<TImplTarget, TBaseSource, TImplDescriptor, TBaseDescriptor> : CopyDescriptor<TImplTarget, TBaseSource>
        where TImplTarget : TBaseSource
        where TImplDescriptor : ISingletonDescriptor<TImplDescriptor>
        where TBaseDescriptor : ISingletonDescriptor<TBaseDescriptor>, IBaseDescriptor<TBaseSource>
    {
        public static CopyDescriptor<TImplTarget, TBaseSource, TImplDescriptor, TBaseDescriptor> Instance { get; } = new();

        protected CopyDescriptor()
            : base(TBaseDescriptor.Instance.MaxFieldNumber, TBaseDescriptor.Instance.PropertyCount)
        {
            foreach (var property in TBaseDescriptor.Instance.Properties)
            {
                var targetProperty = TImplDescriptor.Instance.GetProperty(property.FieldNumber);
                property.AddPropertyFromSource(this, targetProperty);
            }
        }
    }

    public abstract class CopyDescriptor<TImplTarget, TBaseSource> : DescriptorBase<TImplTarget, TBaseSource>
        where TImplTarget : TBaseSource
    {
        protected CopyDescriptor(int maxFieldNumber, int propertyCount)
            : base(maxFieldNumber, propertyCount)
        {
        }
    }

    internal class DescriptorRegistrar
    {
        public struct PerType<T>
        {
            public static ISingletonDescriptor Descriptor { get; set; }
            public static ISingletonBaseDescriptor<T> BaseDescriptor { get; set; }
        }

        public struct Relation<TImpl, TBase>
            where TImpl : TBase
        {
            public static DescriptorBase<TImpl, TBase> Descriptor;
        }
    }

    public abstract class SingletonDescriptorBase<TImpl, TBase, TSelf> : DescriptorBase<TImpl, TBase>, ISingletonDescriptor<TSelf>, ISingletonBaseDescriptor<TBase>
        where TImpl : TBase
        where TSelf : SingletonDescriptorBase<TImpl, TBase, TSelf>, ISingletonDescriptor<TSelf>, ICreate<TSelf>
    {
        public static TSelf Instance { get; } = TSelf.Create();

        protected SingletonDescriptorBase(int maxFieldNumber, int propertyCount)
            : base(maxFieldNumber, propertyCount)
        {

        }

        protected override void OnComplete()
        {
            DescriptorRegistrar.PerType<TImpl>.Descriptor = this;
            DescriptorRegistrar.PerType<TBase>.Descriptor = this;
            DescriptorRegistrar.PerType<TBase>.BaseDescriptor = this;
            base.OnComplete();
        }

        public CopyDescriptor<TImplTarget, TBase> GetCopyDescriptor<TImplTarget>()
            where TImplTarget : TBase
        {
            var implDescriptor = DescriptorRegistrar.PerType<TImplTarget>.Descriptor;
            return implDescriptor.GetCopyDescriptor<TImplTarget, TBase, TSelf>();
        }

        public CopyDescriptor<TImplTarget, TBaseSource> GetCopyDescriptor<TImplTarget, TBaseSource, TBaseDescriptor>()
            where TImplTarget : TBaseSource
            where TBaseDescriptor : ISingletonDescriptor<TBaseDescriptor>, IBaseDescriptor<TBaseSource>
        {
            var descriptor = CopyDescriptor<TImplTarget, TBaseSource, TSelf, TBaseDescriptor>.Instance;
            return descriptor;
        }
    }

    public abstract class DescriptorBase<TImpl, TBase> : IDescriptor, IBaseDescriptor<TBase>
        where TImpl : TBase
    {
        private Dictionary<uint, IProperty> _propertyMap = new();
        protected bool isAccessed = false;

        public DescriptorBase(int maxFieldNumber, int propertyCount)
        {
            MaxFieldNumber = maxFieldNumber;
            PropertyCount = propertyCount;
            _properties = _writableProperties = new();
        }

        private readonly List<IProperty> _properties;
        private List<IProperty> _writableProperties;

        public IReadOnlyList<IBaseSourceProperty<TBase>> Properties => _properties;

        public int MaxFieldNumber { get; }
        public int PropertyCount { get; }

        public void Add(IProperty property)
        {
            _writableProperties.Add(property);
            _propertyMap[property.FieldNumber] = property;

            if (_writableProperties.Count == PropertyCount)
            {
                OnComplete();
            }
        }

        protected virtual void OnComplete()
        {
            Interlocked.CompareExchange(ref _writableProperties, null, _properties);
            DescriptorRegistrar.Relation<TImpl, TBase>.Descriptor = this;
        }

        public void VisitProperties(IPropertyVisitor<TImpl, TBase> visitor)
        {
            foreach (var property in _properties)
            {
                property.Visit(visitor);
            }
        }

        public void CopyFrom(TImpl target, TBase source, bool shallow)
        {
            foreach (var property in _properties)
            {
                property.Copy(target, source, shallow);
            }
        }

        public IDescriptorProperty GetProperty(uint fieldNumber)
        {
            return _propertyMap[fieldNumber];
        }

        public interface IProperty : IDescriptorProperty, IBaseSourceProperty<TBase>
        {
            void Copy(TImpl target, TBase source, bool shallow);

            void Visit(IPropertyVisitor<TImpl, TBase> visitor);
        }

        public interface IProperty<TFieldImpl, TFieldBase> : IProperty, ITargetProperty<TImpl, TFieldImpl>
        {
            Func<TBase, TFieldBase> GetBaseProperty { get; }
        }

        public record Property<TFieldImpl, TFieldBase>(
            uint FieldNumber,
            string Name,
            Func<TBase, TFieldBase> GetBaseProperty,
            Action<TImpl, TFieldImpl> SetImplProperty)
            : IProperty<TFieldImpl, TFieldBase>
            where TFieldImpl : TFieldBase
        {
            public void Copy(TImpl target, TBase source, bool shallow)
            {
                var newValue = PropertyTarget.GetOrCopy<TFieldImpl, TFieldBase>(GetBaseProperty(source), shallow);
                SetImplProperty(target, newValue);
            }

            public void AddPropertyFromSource<TImplTarget>(CopyDescriptor<TImplTarget, TBase> copyDescriptor, IDescriptorProperty targetProperty)
                where TImplTarget : TBase
            {
                var typedTargetProperty = (ITargetProperty<TImplTarget, TFieldImpl>)targetProperty;
                copyDescriptor.Add(new DescriptorBase<TImplTarget, TBase>.Property<TFieldImpl, TFieldBase>(FieldNumber, Name, GetBaseProperty, typedTargetProperty.SetImplProperty));
            }

            public void Visit(IPropertyVisitor<TImpl, TBase> visitor)
            {
                visitor.Visit(this);
            }
        }

        public record ListProperty<TFieldImpl, TFieldBase>(
            uint FieldNumber,
            string Name,
            Func<TBase, IReadOnlyList<TFieldBase>> GetBaseProperty,
            Action<TImpl, List<TFieldImpl>> SetImplProperty,
            Action<TImpl, IReadOnlyList<TFieldImpl>> SetReadOnlyImplProperty = null)
            : IProperty<List<TFieldImpl>, IReadOnlyList<TFieldBase>>,
              ITargetProperty<TImpl, IReadOnlyList<TFieldImpl>>
            where TFieldImpl : TFieldBase
        {
            public ListProperty(
                uint FieldNumber,
                string Name,
                Func<TBase, IReadOnlyList<TFieldBase>> GetBaseProperty,
                Action<TImpl, IReadOnlyList<TFieldImpl>> SetImplProperty)
                : this(FieldNumber, Name, GetBaseProperty, SetImplProperty, SetImplProperty)
            {
            }

            Action<TImpl, IReadOnlyList<TFieldImpl>> ITargetProperty<TImpl, IReadOnlyList<TFieldImpl>>.SetImplProperty => SetReadOnlyImplProperty;

            public void AddPropertyFromSource<TImplTarget>(CopyDescriptor<TImplTarget, TBase> copyDescriptor, IDescriptorProperty targetProperty) where TImplTarget : TBase
            {
                var typedTargetProperty = (ITargetProperty<TImplTarget, List<TFieldImpl>>)targetProperty;
                var typedTargetReadOnlyProperty = (ITargetProperty<TImplTarget, IReadOnlyList<TFieldImpl>>)targetProperty;
                copyDescriptor.Add(new DescriptorBase<TImplTarget, TBase>.ListProperty<TFieldImpl, TFieldBase>(
                    FieldNumber,
                    Name,
                    GetBaseProperty,
                    typedTargetProperty.SetImplProperty,
                    typedTargetReadOnlyProperty.SetImplProperty));
            }

            public void Copy(TImpl target, TBase source, bool shallow)
            {
                var newValue = PropertyTarget.GetOrCopy<TFieldImpl, TFieldBase>(GetBaseProperty(source), shallow);
                if (SetReadOnlyImplProperty != null)
                {
                    SetReadOnlyImplProperty(target, newValue ?? Array.Empty<TFieldImpl>());
                }
                else
                {
                    SetImplProperty(target, (List<TFieldImpl>)newValue);
                }
            }

            public void Visit(IPropertyVisitor<TImpl, TBase> visitor)
            {
                visitor.Visit(this);
            }
        }
    }
}
