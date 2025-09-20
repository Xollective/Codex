using System.Numerics;

namespace Codex.Utilities
{
    public record struct ByteCount(long Value, Units units = Units.MB) : IAdditionOperators<ByteCount, long, ByteCount>
    {
        public long Value = Value;

        public static ByteCount operator +(ByteCount left, long right)
        {
            return new ByteCount(left.Value + right);
        }

        public static ByteCount operator *(ByteCount left, long right)
        {
            return new ByteCount(left.Value * right);
        }

        public static ByteCount operator *(long left, ByteCount right)
        {
            return new ByteCount(right.Value * left);
        }

        public override string ToString()
        {
            var scaledValue = Value / ((long)units * 1.0);

            return $"{scaledValue:F2} {units}";
        }

        public static implicit operator ByteCount(long value) => new(value);

        public static implicit operator ByteCount(Units units) => new(1, units);

    }

    public static class ByteCountExtensions
    {
        public static void InterlockedAdd(this ref ByteCount count, long bytes)
        {
            Interlocked.Add(ref count.Value, bytes);
        }
    }

    [GeneratorExclude]
    public enum Units : long
    {
        bytes = 1,
        KB = 1 << 10,
        MB = 1 << 20,
        GB = 1 << 30,
    }

    public static class Bytes
    {
        public static ByteCount bytes = Units.bytes;
        public static ByteCount KB = Units.KB;
        public static ByteCount MB = Units.MB;
        public static ByteCount GB = Units.GB;
    }
}
