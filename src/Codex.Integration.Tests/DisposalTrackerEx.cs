
using System.Numerics;
using Xunit.Sdk;

namespace Codex;

public class DisposalTrackerEx : DisposalTracker, IAdditionOperators<DisposalTrackerEx, IDisposable, DisposalTrackerEx>
{
    public static DisposalTrackerEx operator +(DisposalTrackerEx left, IDisposable right)
    {
        left.Add(right);
        return left;
    }
}