namespace Tenray.ZoneTree.Options;

public interface IZoneTreeMergePolicy<TKey, TValue>
{
    public bool ShouldMergeReadOnlySegments(IZoneTreeMaintenance<TKey, TValue> maintenance);
}