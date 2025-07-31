namespace Codex.Utilities
{
    /// <summary>
    /// Used to specify out paramater to async method.
    /// </summary>
    public record AsyncOut<T>
    {
        /// <summary>
        /// The value
        /// </summary>
        public T Value;

        public AsyncOut()
        {
        }

        public AsyncOut(out AsyncOut<T> value)
        {
            value = this;
        }

        /// <nodoc />
        public static implicit operator T(AsyncOut<T> value)
        {
            return value.Value;
        }

        public void Set(T value)
        {
            Value = value;
        }
    }

    /// <summary>
    /// Helper methods for <see cref="AsyncOut{T}"/>
    /// </summary>
    public static class AsyncOut
    {
        public static AsyncOut<T> SetOrCreate<T>(this AsyncOut<T> box, T value = default)
        {
            box ??= new AsyncOut<T>();
            box.Set(value);
            return box;
        }

        /// <summary>
        /// Allows inline declaration of <see cref="AsyncOut{T}"/> patterns like the
        /// (out T parameter) pattern. Usage: await ExecuteAsync(out AsyncOut.Var&lt;T&gt;(out var outParam));
        /// </summary>
        public static AsyncOut<T> Var<T>(out AsyncOut<T> value)
        {
            value = new AsyncOut<T>();
            return value;
        }
    }
}