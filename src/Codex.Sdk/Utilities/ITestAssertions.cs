namespace BuildXL.Utilities.Collections;

public interface ITestAssertions
{
    void ShouldBeEquivalentTo<T>(IEnumerable<T> actual, IEnumerable<T> expected);

    void ShouldBeEquivalentTo<T>(T actual, T expected);

    void ShouldBeEquivalentTo<T>(ReadOnlySpan<T> actual, ReadOnlySpan<T> expected);
}
