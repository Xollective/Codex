using System.Numerics;

namespace Codex.Utilities
{
    public record struct ByteCount(long Bytes, Units units = Units.MB) : IAdditionOperators<ByteCount, long, ByteCount>
    {
        public long Bytes = Bytes;

        public static ByteCount operator +(ByteCount left, long right)
        {
            return new ByteCount(left.Bytes + right, left.units);
        }

        public static ByteCount operator *(ByteCount left, long right)
        {
            return new ByteCount(left.Bytes * right, left.units);
        }

        public static ByteCount operator *(long left, ByteCount right)
        {
            return new ByteCount(right.Bytes * left, right.units);
        }

        public ByteCount Multiply(double value)
        {
            return new ByteCount((long)(Bytes * value), units);
        }

        public override string ToString()
        {
            var scaledValue = Bytes / ((long)units * 1.0);

            return $"{scaledValue:F2} {units}";
        }

        public static implicit operator ByteCount(long value) => new(value);

        public static implicit operator ByteCount(Units units) => new((long)units, units);

        public int IntegerBytes => checked((int)Bytes);
    }

    public static class ByteCountExtensions
    {
        public static void InterlockedAdd(this ref ByteCount count, long bytes)
        {
            Interlocked.Add(ref count.Bytes, bytes);
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
