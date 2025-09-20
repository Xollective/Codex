using System.Diagnostics.ContractsLight;

namespace Codex.Sdk;

/// <summary>
/// Helper class for debugging
/// </summary>
public static class DebugExtensions
{
    public static void DebugAssert(this AssertionFailure failure, Action onFailure = null)
    {
        onFailure();
        failure.Assert("");
    }

    public static AssertionFailure Break(this AssertionFailure failure, Action onFailure = null)
    {
        onFailure();
        return failure;
    }
}
