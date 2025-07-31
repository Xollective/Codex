using System;
using System.Collections.Generic;
using System.Diagnostics.ContractsLight;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Codex.ObjectModel;

namespace Codex.Utilities
{
    [DataContract]
    public struct LongExtent : IComparable<LongExtent>, IComparable<long>
    {
        [DataMember(Order = 0)]
        [JsonInclude]
        public long Start;
        public long Last
        {
            get
            {
                return Start + Length - 1;
            }
        }

        public long EndExclusive => Start + Length;

        [DataMember(Order = 1)]
        [JsonInclude]
        public long Length;

        public LongExtent(long start, long length)
        {
            Start = start;
            Length = length;
        }

        public static LongExtent FromBounds(long startInclusive, long endExclusive)
        {
            return new LongExtent(startInclusive, endExclusive - startInclusive);
        }

        public static LongExtent FromEnd(long end, long length)
        {
            return new LongExtent(end - length, length);
        }

        public LongExtent Slice(long relativeStart)
        {
            Contract.Assert(relativeStart <= Length);
            return new(Start + relativeStart, Length - relativeStart);
        }

        public LongExtent? Intersect(LongExtent other)
        {
            var start = Math.Max(Start, other.Start);
            var end = Math.Min(EndExclusive, other.EndExclusive);
            return start <= end ? FromBounds(start, end) : null;
        }

        public LongExtent MakeRelative(long position)
        {
            Contract.Assert(position <= Start);
            return new LongExtent(Start - position, Length);
        }

        public LongExtent Shift(long position)
        {
            return new LongExtent(Start + position, Length);
        }

        public LongExtent MakeAbsolute(LongExtent relativeExtent)
        {
            return relativeExtent.Shift(Start);
        }

        public int CompareTo(LongExtent other)
        {
            if (Start > other.Last)
            {
                return RangeHelper.FirstGreaterThanSecond;
            }
            else if (Last < other.Start)
            {
                return RangeHelper.FirstLessThanSecond;
            }

            return RangeHelper.FirstEqualSecond;
        }

        public override string ToString()
        {
            return $"[{Start}-{EndExclusive}]";
        }

        public string Serialize()
        {
            return $"{Start}-{EndExclusive}";
        }

        public static LongExtent Parse(ReadOnlySpan<char> chars)
        {
            chars = chars.Trim("[]");
            var firstDot = chars.IndexOf('-');
            return FromBounds(
                startInclusive: long.Parse(chars[0..firstDot], null),
                endExclusive: long.Parse(chars.Slice(firstDot + 1), null));
        }

        public bool Contains(long position)
        {
            return Start <= position && position < EndExclusive;
        }

        public static implicit operator LongExtent((long startInclusive, long endExclusive) tuple)
        {
            return FromBounds(tuple.startInclusive, tuple.endExclusive);
        }

        public static implicit operator LongExtent(Extent other)
        {
            return new LongExtent(other.Start, other.Length);
        }

        public Extent ToInt32Extent() => new((int)Start, (int)Length);

        public int CompareTo(long position)
        {
            // first = Range
            // second = position
            if (position < Start)
            {
                return RangeHelper.FirstGreaterThanSecond;
            }
            else if (position >= EndExclusive)
            {
                return RangeHelper.FirstLessThanSecond;
            }
            else
            {
                return RangeHelper.FirstEqualSecond;
            }
        }
    }
}
