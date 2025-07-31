namespace Codex.ObjectModel;

public enum AdditionalClassificationNames
{
    OutliningRegionStart,
    OutliningRegionEnd
}

public static class ClassificationNameExtensions
{
    public static bool IsOutliningClassification(this ClassificationName? name)
    {
        return name?.IsOutliningClassification() ?? false;
    }

    public static bool IsOutliningClassification(this ClassificationName name)
    {
        return name == ClassificationName.OutliningRegionStart || name == ClassificationName.OutliningRegionEnd;
    }
}