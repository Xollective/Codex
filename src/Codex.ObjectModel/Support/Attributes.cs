using System;
using Codex.Sdk.Utilities;

namespace Codex.ObjectModel.Attributes
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class SearchDescriptorInlineAttribute : Attribute
    {
        public readonly bool Inline;

        public SearchDescriptorInlineAttribute(bool inline = false)
        {
            Inline = inline;
        }
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class EntityIdAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class PlaceholderAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class SerializationInterfaceAttribute : Attribute
    {
        public readonly Type Type;

        public SerializationInterfaceAttribute(Type type)
        {
            Type = type;
        }
    }

    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Enum | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class GeneratorExcludeAttribute : Attribute
    {
        public bool IncludeProperties { get; }

        public GeneratorExcludeAttribute(bool includeProperties = false)
        {
            IncludeProperties = includeProperties;
        }
    }

    [AttributeUsage(AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    public sealed class AdapterTypeAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    public sealed class GeneratedClassNameAttribute : Attribute
    {
        public readonly string Name;

        public GeneratedClassNameAttribute(string name)
        {
            Name = name;
        }
    }

    /// <summary>
    /// Excludes a property from serialization. Mainly used for excluding properties from serialization
    /// which have an inferred value such as (ReferenceKind on DefinitionSymbol is inferred as Definition)
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    public sealed class ExcludedSerializationPropertyAttribute : Attribute
    {
        public readonly string PropertyName;

        public ExcludedSerializationPropertyAttribute(string propertyName)
        {
            PropertyName = propertyName;
        }
    }

    /// <summary>
    /// Indicates an attached property which is not intrinsic to the parent object and should be
    /// excluded when computing the <see cref="ISearchEntity.EntityContentId"/>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class AttachedAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class QueryAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class ReadOnlyListAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class UseInterfaceAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class CoerceGetAttribute : Attribute
    {
        public readonly Type CoercedSourceType;

        public CoerceGetAttribute(Type coercedSourceType = null, bool useRef = false)
        {
            CoercedSourceType = coercedSourceType;
            UseRef = useRef;
        }

        public bool UseRef { get; }
    }

    /// <summary>
    /// Indicates stages for which the given property should be included
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class IncludeAttribute : FeatureAttributeBase<ObjectStage>
    {
        public ObjectStage AllowedStages => EnabledValue;

        public IncludeAttribute(ObjectStage stages)
            : base(stages)
        {
        }
    }

    public class FeatureAttributeBase<TValue> : Attribute
    {
        private readonly TValue _enabledValue;
        protected virtual TValue DisabledValue => default(TValue);

        protected TValue EnabledValue
        {
            get
            {
                if (WhenFeature != null)
                {
                    feature ??= Features.FeaturesByName[WhenFeature];
                    return (feature.Value ^ WhenDisabled) ? _enabledValue : DisabledValue;
                }

                return _enabledValue;
            }
        }

        private IFeatureSwitch<bool> feature;

        public string WhenFeature { get; set; }

        public bool WhenDisabled { get; set; }

        public FeatureAttributeBase(TValue enabledValue)
        {
            this._enabledValue = enabledValue;
        }
    }

    /// <summary>
    /// Indicates stages for which the given property should be excluded
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class ExcludeAttribute : FeatureAttributeBase<IReadOnlyList<ObjectStage>>
    {
        public IReadOnlyList<ObjectStage> ExcludedStages => EnabledValue;

        protected override IReadOnlyList<ObjectStage> DisabledValue => Array.Empty<ObjectStage>();

        public ExcludeAttribute(params ObjectStage[] stages)
            : base(stages)
        {
        }
    }

    /// <summary>
    /// Indicates stages for which the given base interface properties should be excluded
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface, Inherited = false, AllowMultiple = true)]
    public sealed class ExcludeBaseAttribute : FeatureAttributeBase<IReadOnlyList<ObjectStage>>
    {
        public IReadOnlyList<ObjectStage> ExcludedStages => EnabledValue;

        protected override IReadOnlyList<ObjectStage> DisabledValue => Array.Empty<ObjectStage>();

        public Type BaseInterface { get; }

        public ExcludeBaseAttribute(Type baseInterface, params ObjectStage[] stages)
            : base(stages)
        {
            BaseInterface = baseInterface;
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Interface | AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class RequiredForAttribute : Attribute
    {
        public readonly ObjectStage Stages;

        public RequiredForAttribute(ObjectStage stages)
        {
            Stages = stages;
        }
    }
}
