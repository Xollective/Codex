using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace CodexTestProject;

internal unsafe partial class LL
{
    public static void TestAccess(LZ4_stream_t* s, int i)
    {
        uint r = s->hashTable[i];
    }

    protected const int LZ4_HASH_SIZE_U32 = 1 << 10;

    [StructLayout(LayoutKind.Sequential)]
    public struct LZ4_stream_t
    {
        public fixed uint hashTable[LZ4_HASH_SIZE_U32];
    }
}