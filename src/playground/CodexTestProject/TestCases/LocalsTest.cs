namespace CodexTestProject.Ns;

/// <summary>
/// Locals test of <seealso cref="T1"/> and <seealso cref="T2"/>
/// </summary>
/// <typeparam name="T1"></typeparam>
/// <typeparam name="T2"></typeparam>
public partial class LocalsTest<T1, T2>
{
    public int Test { get; set; }

    public void SetTest()
    {
        this.Test = 1;
    }

    /// <summary>
    /// <seealso cref="T1"/>
    /// <seealso cref="T2"/>
    /// </summary>
    /// <param name="p1"></param>
    /// <param name="p2"></param>
    /// <param name="t1"></param>
    /// <param name="t2"></param>
    /// <returns></returns>
    public (int a, int b) Method<T2>(int p1, int p2, T1 t1, T2 t2)
    {
        static (int a, int b) localFunc(int p1, int p2, int add)
        {
            return (p1 + add, p2 + add);
        }

        return localFunc(p1: p1, p2: 100, p2);
    }

    public (int a, int b) Method2(int p1, int p2, T1 t1, T2 t2)
    {
        static (int a, int b) localFunc(int p1, int p2, int add)
        {
            return (p1 + add, p2 + add);
        }

        return localFunc(p1: p1, p2: 100, p2);
    }
}


/// <summary>
/// Locals test of <seealso cref="T1"/> and <seealso cref="T2"/>
/// </summary>
/// <typeparam name="T1"></typeparam>
/// <typeparam name="T2"></typeparam>
public class LocalsTest2<T1, T2>
{
    /// <summary>
    /// <seealso cref="T1"/>
    /// <seealso cref="T2"/>
    /// </summary>
    /// <param name="p1"></param>
    /// <param name="p2"></param>
    /// <param name="t1"></param>
    /// <param name="t2"></param>
    /// <returns></returns>
    public (int a, int b) Method<T2>(int p1, int p2, T1 t1, T2 t2)
    {
        static (int a, int b) localFunc(int p1, int p2, int add)
        {
            return (p1 + add, p2 + add);
        }

        return localFunc(p1: p1, p2: 100, p2);
    }

    public (int a, int b) Method2(int p1, int p2, T1 t1, T2 t2)
    {
        static (int a, int b) localFunc(int p1, int p2, int add)
        {
            return (p1 + add, p2 + add);
        }

        return localFunc(p1: p1, p2: 100, p2);
    }
}