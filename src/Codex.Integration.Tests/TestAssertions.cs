using BuildXL.Utilities.Collections;
using Codex.Utilities.Serialization;

namespace Codex.Integration.Tests;

public class TestAssertions : ITestAssertions
{
    public static readonly TestAssertions Instance = new();

    public void ShouldBeEquivalentTo<T>(IEnumerable<T> actual, IEnumerable<T> expected)
    {
        actual.Should().BeEquivalentTo(expected);
    }

    public void ShouldBeEquivalentTo<T>(T actual, T expected)
    {
        actual.Should().BeEquivalentTo(expected);
    }

    public void ShouldBeEquivalentTo<T>(ReadOnlySpan<T> actual, ReadOnlySpan<T> expected)
    {
        actual.ShouldBeEquivalentTo(expected);
    }
}

public static class Assertions
{
    public static void ShouldBeEquivalentTo<T>(this ReadOnlySpan<T> actual, ReadOnlySpan<T> expected)
    {
        using var actualList = actual.AsScope();
        using var expectedList = expected.AsScope();

        actualList.Should().BeEquivalentTo(expectedList);
    }
}