namespace Codex.Utilities
{
    public struct MemoryRef<T>(Memory<T> memory, int index)
    {
        private Memory<T> ptr = memory.Slice(index, 1);

        public ref T Value => ref ptr.Span[0];
    }

    public static class MemoryRef
    {
        public static MemoryRef<T> Create<T>(this Memory<T> memory, int index)
        {
            return new MemoryRef<T>(memory, index);
        }

        public static MemoryRef<T> CreateMemoryRef<T>(this T[] array, int index)
        {
            return new MemoryRef<T>(array.AsMemory(), index);
        }
    }
}
