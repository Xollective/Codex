using Codex.ObjectModel;
using Codex.Sdk.Utilities;
using Codex.Utilities.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics.ContractsLight;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Codex.Utilities
{
    [StructLayout(LayoutKind.Explicit, Size = BYTE_LENGTH)]
    public unsafe record struct ShortHash : IEquatable<ShortHash>, IComparable<ShortHash>, IBytesStruct, ISpanParsableSlim<ShortHash>
    {
        public const int BYTE_LENGTH = 12;
        public const int CHAR_LENGTH = 24;

        private static readonly char[] s_paddingChars = new[] { '=' };

        [FieldOffset(0)]
        public ulong Low;

        [FieldOffset(8)]
        public uint High;

        [FieldOffset(0)]
        private Bytes bytes;

        public byte this[int index]
        {
            get => GetByte(index);
            set => GetByte(index) = value;
        }

        public ShortHash(ulong low, uint high)
            : this()
        {
            Low = low;
            High = high;
        }

        public static ShortHash Random()
        {
            return MemoryMarshal.Cast<Guid, ShortHash>(stackalloc[] { Guid.NewGuid() })[0];
        }

        public override string ToString()
        {
            return Convert.ToHexString(this.AsBytes());
        }

        public byte[] ToByteArray()
        {
            return this.AsBytes().ToArray();
        }

        public static ShortHash Parse(ReadOnlySpan<char> chars)
        {
            return TryParse(chars) ?? throw new FormatException();
        }

        public static ShortHash? TryParse(ReadOnlySpan<char> chars)
        {
            return MurmurHash.TryParseHexHashCore<ShortHash>(chars);
        }

        public unsafe string ToBase64String(int maxCharLength = 32, Base64.Format format = Base64.Format.UrlSafe)
        {
            fixed (byte* b = bytes.bytes)
            {
                return Base64.ToBase64String(new Span<byte>(b, BYTE_LENGTH), maxCharLength, format);
            }
        }

        public uint GetInt(int i)
        {
            var span = SpanSerializationExtensions.AsReadOnlySpanUnsafe(this);
            return MemoryMarshal.Cast<ShortHash, uint>(span)[i];
        }

        public ref byte GetByte(int i)
        {
            if (unchecked((uint)i >= (uint)BYTE_LENGTH))
            {
                throw new ArgumentOutOfRangeException();
            }

            fixed (byte* bytes = this.bytes.bytes)
            {
                return ref bytes[i];
            }
        }

        public ShortHash? AsNullable()
        {
            return this == default ? default(ShortHash?) : new ShortHash();
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(High, Low);
        }

        public bool Equals(ShortHash other)
        {
            return High == other.High &&
                   Low == other.Low;
        }

        public int CompareTo(ShortHash other)
        {
            return High.ChainCompareTo(other.High) ?? Low.CompareTo(other.Low);
        }

        public static bool operator ==(ShortHash m1, ShortHash? m2)
        {
            return m2.HasValue && m1.Equals(m2.Value);
        }

        public static bool operator !=(ShortHash m1, ShortHash? m2)
        {
            return !m2.HasValue || !m1.Equals(m2.Value);
        }

        public (ulong High, ulong Low) GetParts()
        {
            return (High, Low);
        }

        public static implicit operator ShortHash(MurmurHash hash)
        {
            return new ShortHash(hash.Low, (uint)hash.High);
        }

        public static ShortHash operator ^(ShortHash hash1, ShortHash hash2)
        {
            return new ShortHash()
            {
                High = hash1.High ^ hash2.High,
                Low = hash1.Low ^ hash2.Low
            };
        }

        [StructLayout(LayoutKind.Explicit, Size = BYTE_LENGTH)]
        private struct Bytes
        {
            [FieldOffset(0)]
            public fixed byte bytes[BYTE_LENGTH];
        }
    }
}
