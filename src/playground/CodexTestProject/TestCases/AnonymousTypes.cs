using System.IO;
using System.Linq;

namespace CodexTestProject;

public class AnonymousTypesTest
{
    void TestAnonymousType()
    {
        var t = new
        {
            AnonMember1 = 12,
            AnonMember2 = true
        };

        var chars = Path.InvalidPathChars.Select(c => (int)c);

        if (t.AnonMember1 > 10 && t.AnonMember2)
        {

        }
    }
}