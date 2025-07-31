using System.Collections;
using System.IO;

namespace CodexTestProject.Ns;

public class LocalsTestBase
{
    public int Value;

    protected virtual void SetValue(int b)
    {
        object c = null;
        switch (c)
        {
            case Stream s:
                b = (int)s.Length;
                break;
            case ICollection s:
                b = s.Count;
                break;
            default:
                break;
        }


        void localFunc(int b)
        {
            b += 1;

            localFunc(b);
            void localFunc(int a)
            {

            }

            localFunc(b);
        }

        foreach (var i in new[] { 1, 2 })
        {

        }
        for (int i = 0; i < 10; i++)
        {

        }

        this.Set(out Value, 1);
        this.Set(out var name, 1);
        this.Value = 1;
        base.GetHashCode();
        {
            var s = 1;
        }
        {
            var s = 1;
        }
    }

    protected void Set(out int target, int value)
    {
        target = value;
    }
}

public class LocalsTestImpl : LocalsTestBase
{
    protected override void SetValue(int b)
    {
        base.SetValue(b);
    }
}

public partial class LocalsTest<T1, T2>
{
}
