namespace Codex.Utilities;

public static class Perf
{
    public static T[]? TryAllocateIfLarge<T>(int requiredSize)
    {
        return requiredSize > 400 ? new T[requiredSize] : null;
    }
}