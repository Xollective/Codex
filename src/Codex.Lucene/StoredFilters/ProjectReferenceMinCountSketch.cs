using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;
using Codex.Utilities;
using Codex.Utilities.Serialization;

namespace Codex.Lucene
{
    public class ProjectReferenceMinCountSketch : MinCountSketch<string, uint>, IJsonConvertible<ProjectReferenceMinCountSketch, byte[][]>
    {
        public ProjectReferenceMinCountSketch() 
            : base(rows: 10, columns: 1000)
        {
        }

        protected override ulong GetHash(string key)
        {
            key = key.ToLowerInvariant();
            Murmur3 hasher = new Murmur3();
            var byteCount = Encoding.UTF8.GetByteCount(key);
            Span<byte> bytes = Perf.TryAllocateIfLarge<byte>(byteCount) ?? stackalloc byte[byteCount];
            int written = Encoding.UTF8.GetBytes(key, bytes);
            bytes = bytes.Slice(0, written);
            var hash = hasher.ComputeHash(bytes);
            return hash.Low;
        }

        public static ProjectReferenceMinCountSketch ConvertFromJson(byte[][] jsonFormat)
        {
            var sketch = new ProjectReferenceMinCountSketch();
            var byteSpan = sketch.GetByteSpan();
            var rowByteLength = byteSpan.Length / sketch.Rows;

            for (int i = 0; i < jsonFormat.Length; i++)
            {
                jsonFormat[i].CopyTo(byteSpan.Slice(i * rowByteLength, rowByteLength));
            }

            return sketch;
        }

        public byte[][] ConvertToJson()
        {
            var byteSpan = GetByteSpan();
            byte[][] result = new byte[Rows][];
            var rowByteLength = byteSpan.Length / Rows;

            for (int i = 0; i < Rows; i++)
            {
                result[i] = byteSpan.Slice(i * rowByteLength, rowByteLength).ToArray();
            }

            return result;
        }
    }
}
