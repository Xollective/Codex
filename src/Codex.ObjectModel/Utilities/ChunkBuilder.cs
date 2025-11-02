using System.Numerics;

namespace Codex.Utilities
{
    public record struct XorRollingHash(int WindowSize)
    {
        public ulong Hash;

        public static XorRollingHash operator +(XorRollingHash left, ulong right)
        {
            return left with { Hash = left.Hash ^ right };
        }

        public static XorRollingHash operator -(XorRollingHash left, ulong right)
        {
            return left with { Hash = left.Hash ^ right.RotateLeft(left.WindowSize) };
        }

        public static XorRollingHash operator <<(XorRollingHash left, int value)
        {
            return left with { Hash = left.Hash.RotateLeft(value) };
        }

        public XorRollingHash Reset() => this with { Hash = 0 }; 
    }

    public record ChunkBuilder(uint DesiredChunkSize, bool AutoNext = true, int ChunkWindowSize = 13)
    {
        public ulong[] ChunkWindow = new ulong[ChunkWindowSize];

        public uint MinChunkSize { get; init; } = Math.Max(1, DesiredChunkSize / 2);

        public uint OversizeChunkSize { get; init; } = (uint)Math.Max(2, DesiredChunkSize * 1.5);

        public uint Threshold = uint.MaxValue / DesiredChunkSize;
        public ulong EffectiveThreshold = uint.MaxValue / DesiredChunkSize;

        public XorRollingHash _sum = new(WindowSize: ChunkWindowSize);

        public uint CompareValue { get; private set; }

        public int ChunkSize { get; private set; }

        public int ChunkIndex = 0;

        private int _itemIndex = 0;
        private bool _isFull = false;

        private const uint EMPTY_HASH = 0x49D03A63;

        public bool Add(MurmurHash itemHash) => Add(itemHash.Low);

        public bool Add(ShortHash itemHash) => Add(itemHash.Low);

        //public bool Add(ulong itemHash) => Add(unchecked((uint)itemHash));

        public bool Add(ulong itemHash)
        {
            if (itemHash == 0) itemHash = EMPTY_HASH;

            _sum <<= 1;

            if (_isFull)
            {
                _sum -= ChunkWindow[_itemIndex];
            }

            _sum += itemHash;
            ChunkWindow[_itemIndex] = itemHash;

            _itemIndex++;
            if (_itemIndex == ChunkWindow.Length)
            {
                _isFull = true;
                _itemIndex = 0;
            }

            var compareValue = unchecked((uint)_sum.Hash);
            return AddCompareValue(compareValue);
        }

        private bool AddCompareValue(uint compareValue)
        {
            ChunkSize++;
            CompareValue = compareValue;
            if (ChunkSize >= MinChunkSize)
            {
                if (CompareValue < EffectiveThreshold || ChunkSize >= OversizeChunkSize)
                {
                    if (AutoNext)
                    {
                        NextChunk();
                    }

                    return true;
                }
            }

            return false;
        }

        public void Reset()
        {
            NextChunk();
            ChunkIndex = 0;
        }

        public void NextChunk()
        {
            EffectiveThreshold = Threshold;
            ChunkIndex++;
            _isFull = false;
            _itemIndex = 0;
            _sum = _sum.Reset();
            ChunkSize = 0;
        }
    }
}
