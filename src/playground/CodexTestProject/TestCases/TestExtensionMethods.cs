using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CodexTestProject;

public static class TestExtensionMethods
{
    public static bool IsNull(this string s) => s == null;

    public static bool IsEmpty<T>(this T s)
        where T : class, IList<int>, new()
    {
        s.Take(10);
        return s.Count == 0;
    }

    public static bool IsEmptyMap<T>(this T s)
        where T : struct, IDictionary, IEnumerator
    {
        return s.Count == 0;
    }

}