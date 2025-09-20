using CommunityToolkit.HighPerformance;

namespace Codex.Utilities
{
    public ref struct ValueScope<T>(T value, Ref<T> valueRef) : IDisposable
    {
        public Ref<T> ValueRef = valueRef;

        public T OriginalValue = Exchange(ref valueRef.Value, value);

        private static T Exchange(ref T location, T value)
        {
            var result = location;
            location = value;
            return result;
        }

        public void Dispose()
        {
            ValueRef.Value = OriginalValue;
        }
    }
}
