using Codex.Utilities;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Codex.Integration.Tests;

public class CodexTestOrderer : ITestCaseOrderer
{
    public CodexTestOrderer(IMessageSink messageSink)
    {
    }

    private static IComparer<ITestCase> Comparer { get; } = new ComparerBuilder<ITestCase>()
        .CompareByAfter(t => t.TestMethod.Method.Name)
        .CompareByAfter(t => GetRank(t));

    private static int GetRank(ITestCase t)
    {
        var searchOnlyParam = t.TestMethod.Method.GetParameters()
            .WithIndices().Where(p => p.Item.Name.EqualsIgnoreCase("searchOnly"))
            .FirstOrDefault();
        if (searchOnlyParam.Item == null) return 0;

        var searchOnlyValue = (bool)t.TestMethodArguments[searchOnlyParam.Index];

        // Search only tests must come after other tests because they rely on the 
        // state established by the non-search only tests.
        return searchOnlyValue ? 1 : 0;
    }

    public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases) 
        where TTestCase : ITestCase
    {
        return testCases.OrderBy(t => t, Comparer).ToList();
    }
}