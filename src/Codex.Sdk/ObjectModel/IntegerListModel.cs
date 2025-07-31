using Codex.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;
using System.IO;
using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Runtime.Serialization;
using Codex.Sdk.Utilities;

namespace Codex.ObjectModel.Implementation
{
    [DataContract]
    public class IntegerListModel
    {
        [DataMember(Order = 0)]
        public int ValueByteWidth { get; set; }

        [DataMember(Order = 1)]
        public int MinValue { get; set; }

        [DataMember(Order = 2)]
        public int DecompressedLength { get; set; }

        [DataMember(Order = 3)]
        public int CompressedLength { get; set; }

        [DataMember(Order = 4)]
        public byte[] Data { get; set; }

        [JsonPropertyName("cdata")]
        [DataMember(Order = 5)]
        public string CompressedData { get; set; }

        [JsonIgnore]
        [IgnoreDataMember]
        public int Count
        {
            get
            {
                var length = Data == null ? DecompressedLength : Data.Length;
                return length / ValueByteWidth;
            }
        }

        [JsonIgnore]
        [IgnoreDataMember]
        public int DataLength
        {
            get
            {
                var length = Data == null ? DecompressedLength : Data.Length;
                return length;
            }
        }

        public IntegerListModel()
        {
        }

        public static IntegerListModel Create<T>(IReadOnlyList<T> values, Func<T, int> selector, bool nullIfAllZeros = false)
        {
            var minValue = values.Min(selector);
            var maxValue = values.Max(v => selector(v) - minValue);
            if (nullIfAllZeros && minValue ==  0 && maxValue == 0)
            {
                return null;
            }

            var byteWidth = NumberUtils.GetByteWidth(maxValue);
            var list = new IntegerListModel(byteWidth, values.Count, minValue);

            for (int i = 0; i < values.Count; i++)
            {
                list[i] = selector(values[i]);
            }

            return list;
        }

        public IntegerListModel(BitArray bitArray)
        {
            var byteArrayLength = (bitArray.Count + 7) / 8;
            Data = new byte[byteArrayLength];
            bitArray.CopyTo(Data, 0);
            ValueByteWidth = 1;
        }

        public IntegerListModel(int byteWidth, int numberOfValues, int minValue, /* This is only here to hide constructor from message pack */ None none = default)
        {
            Data = new byte[byteWidth * numberOfValues];
            ValueByteWidth = byteWidth;
            MinValue = minValue;
        }

        [JsonIgnore]
        public int this[int index]
        {
            get
            {
                var value = GetIndexDirect(index);
                return value + MinValue;
            }

            set
            {
                value -= MinValue;
                SetIndexDirect(index, value);
            }
        }

        internal int GetIndexDirect(int index)
        {
            int value = 0;
            int dataIndex = index * ValueByteWidth;
            int byteOffset = 0;
            for (int i = 0; i < ValueByteWidth; i++, byteOffset += 8)
            {
                value |= (Data[dataIndex++] << byteOffset);
            }

            return value;
        }

        internal void SetIndexDirect(int index, int value)
        {
            int dataIndex = index * ValueByteWidth;
            for (int i = 0; i < ValueByteWidth; i++)
            {
                Data[dataIndex++] = unchecked((byte)value);
                value >>= 8;
            }
        }

        internal void Optimize(OptimizationContext context)
        {
            if (CompressedData == null && Data != null)
            {
                var compressedData = context.Compress(Data);
                if (compressedData.Length < Data.Length)
                {
                    DecompressedLength = Data.Length;
                    CompressedLength = compressedData.Length;
                    CompressedData = Convert.ToBase64String(compressedData);
                }
                else
                {
                    CompressedData = Convert.ToBase64String(Data);
                }

                Data = null;
            }
        }

        internal void ExpandData(OptimizationContext context)
        {
            if (CompressedData == null)
            {
                return;
            }

            Data = Convert.FromBase64String(CompressedData);
            if (DecompressedLength != 0)
            {
                var compressedData = Data;
                Data = new byte[DecompressedLength];
                using (var compressedStream = new DeflateStream(new MemoryStream(compressedData), CompressionMode.Decompress))
                {
                    compressedStream.Read(Data, 0, DecompressedLength);
                }
            }

            CompressedData = null;
            DecompressedLength = 0;
            CompressedLength = 0;
        }
    }
}
