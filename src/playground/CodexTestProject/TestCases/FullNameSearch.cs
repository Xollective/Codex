namespace CodexTestProject.FullNameSearch
{
    public class GenericType<T1, TNext, T3Arg>
    {
        public void GenericMethod<T2, T4>()
        {
        }
    }
}

namespace CodexTestProject.FullNameSearch.Subns
{
    public class TestType<A, B, C>
    {
        public struct Nested<T>
        {
            public void Method<T2, T4>()
            {
            }
        }
    }
}