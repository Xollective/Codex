using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CodexTestProject.Ns.TestProject;

public class DefinitionDisplay
{
    public void Check(IntPtr ptr)
    {

    }

    public static void Main()
    {
        // This it the entry point
    }

    public ValueTuple<int, int> Get()
    {
        var t = new
        {
            Hello = "world"
        };

        var hello = t.Hello;

        (int, string) s = default;
        var r = s;
        return default;
    }

    class Nested<V>
    {
        public int TestMethod<T>(string s, IReadOnlyList<T> list, IEnumerable<Task> tasks, params string[] args)
        {
            return 0;
        }
    }
}