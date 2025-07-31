using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Codex.ObjectModel;

namespace Codex.Utilities.Serialization;

public interface IShouldSerializeProperty<TDeclaringType>
{
    static abstract bool ShouldSerializeProperty(TDeclaringType obj, string propertyName);
}

[GeneratorExclude(includeProperties: true)]
public interface IJsonRangeTracking<TSelf> : IJsonRangeTrackingBase
{
    [IgnoreDataMember]
    Extent<TSelf>? JsonRange { get; set; }
}

[GeneratorExclude]
public interface IJsonRangeTrackingBase
{ }

