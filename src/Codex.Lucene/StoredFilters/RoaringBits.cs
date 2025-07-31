using System.Collections;
using Codex.Utilities.Serialization;

public class RoaringBits
{
    public class Writer
    {
        public CodexArrayBufferWriter<byte> bufferWriter;


    }

    public class SetBase
    { 

    }

    public class ShortSet : SetBase
    {
        private int _nextIndex = 0;
        public ushort[] Values { get; } = new ushort[4096];

    }

    public class BitArraySet : SetBase
    {
        public BitArray BitArray { get; } = new BitArray(1 << 16);
    }

}