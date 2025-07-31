using System.Runtime.InteropServices;
using Codex.ObjectModel;

namespace Codex.Storage.BlockLevel;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct EntityMappingKey(int StableId, byte TypeAndKind, UInt24 QualifierId = default)
{
    public EntityMappingKey(int StableId, SearchTypeId Type, AddressKind AddressKind, UInt24 QualifierId = default)
        : this(StableId, (byte)((byte)Type << 4 | (byte)AddressKind), QualifierId)
    {
    }

    public static IComparer<EntityMappingKey> GroupFirstComparer { get; } = Compare.Builder<EntityMappingKey>()
        .CompareByAfter(k => k.GetGroup())
        .CompareByAfter(k => k.StableId);

    public SearchTypeId Type => (SearchTypeId)((TypeAndKind & 0xF0) >> 4);

    public AddressKind AddressKind => (AddressKind)(TypeAndKind & 0xF);

    public (SearchTypeId Type, int QualifierId, AddressKind AddressKind) GetGroup() => (Type, QualifierId, AddressKind);
}
