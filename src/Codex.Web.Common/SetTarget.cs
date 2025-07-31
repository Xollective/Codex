using System.Threading.Tasks;

namespace Codex.Uno.Shared
{
    public class SetTarget<T>
    {
        private TaskCompletionSource<T> _tcs = new TaskCompletionSource<T>();
        private T _value;

        public T Value
        {
            get => _value;
            set
            {
                if (_tcs.TrySetResult(value))
                {
                    _value = value;
                }
            }
        }

        public Task<T> Task => _tcs.Task;
    }
}