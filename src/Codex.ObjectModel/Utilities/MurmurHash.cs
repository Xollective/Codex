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
    public unsafe struct MurmurHash : IEquatable<MurmurHash>, IComparable<MurmurHash>, IBytesStruct
    {
        public const int BYTE_LENGTH = 16;
        public const int HEX_CHAR_LENGTH = BYTE_LENGTH * 2;
        public const int BASE64_CHAR_LENGTH = 22;
        public const int BASE64_CHAR_LENGTH_WITH_PADDING = 24;

        private static readonly char[] s_paddingChars = new[] { '=' };

        public static readonly MurmurHash EmptyBytesHash = Murmur3.Shared.ComputeHash(Array.Empty<byte>().AsSpan());
        public static readonly MurmurHash Zero = default;

        [FieldOffset(8)]
        public ulong High;

        [FieldOffset(0)]
        public ulong Low;

        [FieldOffset(0)]
        private uint int_0;

        [FieldOffset(4)]
        private uint int_1;

        [FieldOffset(8)]
        private uint int_2;

        [FieldOffset(12)]
        private uint int_3;

        [FieldOffset(0)]
        private Guid guid;

        [FieldOffset(0)]
        private fixed byte bytes[16];

        public byte this[int index]
        {
            get => GetByte(index);
            set => GetByte(index) = value;
        }

        public MurmurHash(ulong low, ulong high)
            : this()
        {
            Low = low;
            High = high;
        }

        public MurmurHash(uint int0, uint int1, uint int2, uint int3)
            : this()
        {
            int_0 = int0;
            int_1 = int1;
            int_2 = int2;
            int_3 = int3;
        }

        public MurmurHash(Guid guid)
            : this()
        {
            this.guid = guid;
        }

        public Guid AsGuid()
        {
            return guid;
        }

        public static MurmurHash Random()
        {
            return new MurmurHash(Guid.NewGuid());
        }

        public override string ToString()
        {
            return Convert.ToHexString(this.AsBytes());
        }

        public byte[] ToByteArray()
        {
            return MemoryMarshal.AsBytes(stackalloc[] { this }).ToArray();
        }

        public static MurmurHash? TryParse(string value)
        {
            if (string.IsNullOrEmpty(value)) return null;

            return TryParse(value.AsSpan());
        }

        public static MurmurHash Parse(ReadOnlySpan<char> chars)
        {
            return TryParse(chars) ?? throw new FormatException();
        }

        internal static T? TryParseHexHashCore<T>(ReadOnlySpan<char> chars)
            where T : unmanaged
        {
            int size = Unsafe.SizeOf<T>();
            if (chars.Length == (size *2) == UInt128.TryParse(chars, NumberStyles.HexNumber, null, out var value))
            {
                var bytes = value.AsWritableBytes().Slice(0, size);
                bytes.Reverse();

                var hash = Unsafe.As<UInt128, T>(ref value);
                return hash;
            }

            return null;
        }

        public static MurmurHash? TryParse(ReadOnlySpan<char> chars)
        {
            if (chars.Length > BASE64_CHAR_LENGTH_WITH_PADDING)
            {
                return TryParseHexHashCore<MurmurHash>(chars);
            }

            Span<char> charBuffer = stackalloc char[BASE64_CHAR_LENGTH_WITH_PADDING];
            chars.CopyTo(charBuffer);

            for (int i = BASE64_CHAR_LENGTH; i < BASE64_CHAR_LENGTH_WITH_PADDING; i++)
            {
                charBuffer[i] = s_paddingChars[0];
            }

            Base64.ReplaceBase64CharsForConvert(charBuffer);

            {
                Span<byte> bytes = stackalloc byte[BYTE_LENGTH];
                if (!Convert.TryFromBase64Chars(charBuffer, bytes, out var bytesWritten))
                {
                    return null;
                }

                return MemoryMarshal.Read<MurmurHash>(bytes);
            }
        }

        public unsafe string ToBase64String(int maxCharLength = 32, Base64.Format format = Base64.Format.UrlSafe)
        {
            fixed (byte* b = bytes)
            {
                return Base64.ToBase64String(new Span<byte>(b, BYTE_LENGTH), maxCharLength, format);
            }
        }

        public uint GetInt(int i)
        {
            if (unchecked((uint)i >= (uint)4))
            {
                throw new ArgumentOutOfRangeException();
            }

            fixed (byte* bytes = this.bytes)
            {
                return ((uint*)bytes)[i];
            }
        }

        public ref byte GetByte(int i)
        {
            if (unchecked((uint)i >= (uint)BYTE_LENGTH))
            {
                throw new ArgumentOutOfRangeException();
            }

            fixed (byte* bytes = this.bytes)
            {
                return ref bytes[i];
            }
        }

        public MurmurHash? AsNullable()
        {
            return this == default ? default(MurmurHash?) : new MurmurHash();
        }

        public override bool Equals(object obj)
        {
            return obj is MurmurHash other &&
                   High == other.High &&
                   Low == other.Low;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(High, Low);
        }

        public bool Equals(MurmurHash other)
        {
            return High == other.High &&
                   Low == other.Low;
        }

        public int CompareTo(MurmurHash other)
        {
            return High.ChainCompareTo(other.High) ?? Low.CompareTo(other.Low);
        }

        public static bool operator ==(MurmurHash m1, MurmurHash? m2)
        {
            return m1.Equals(m2.GetValueOrDefault());
        }

        public static bool operator !=(MurmurHash m1, MurmurHash? m2)
        {
            return !m1.Equals(m2.GetValueOrDefault());
        }

        public (ulong High, ulong Low) GetParts()
        {
            return (High, Low);
        }

        public static MurmurHash operator ^(MurmurHash hash1, MurmurHash hash2)
        {
            return new MurmurHash()
            {
                High = hash1.High ^ hash2.High,
                Low = hash1.Low ^ hash2.Low
            };
        }

        public static MurmurHash Combine(ReadOnlySpan<MurmurHash> hashes)
        {
            return Murmur3.ComputeBytesHash(hashes);
        }
    }
}
