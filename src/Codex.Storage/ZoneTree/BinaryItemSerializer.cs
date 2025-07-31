using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Codex.Utilities;
using Codex.Utilities.Serialization;
using CommunityToolkit.HighPerformance;
using Tenray.ZoneTree.Comparers;
using Tenray.ZoneTree.Serializers;

namespace Codex.Storage;

public class BinaryItemSerializer<TArray> : ISpanSerializer<TArray>
    where TArray : unmanaged, IBinarySpanItem<TArray>
{
    public bool IsFixedSize => false;

    public TArray Deserialize(ReadOnlySpan<byte> bytes)
    {
        return TArray.FromSpan(bytes);
    }

    public ReadOnlySpan<byte> Serialize(in TArray entry)
    {
        return TArray.GetSpan(entry);
    }
}
