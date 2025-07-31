using Codex.Sdk.Utilities;

namespace Codex.Utilities
{
    /// <summary>
    /// Used to specify out paramater to async method.
    /// </summary>
    public record struct AsyncRef<T>(RefFunc<T> GetRef)
    {
        /// <summary>
        /// The value
        /// </summary>
        public T Value
        {
            get => GetRef();
            set => GetRef() = value;
        }

        /// <nodoc />
        public static implicit operator T(AsyncRef<T> value)
        {
            return value.Value;
        }

        /// <nodoc />
        public static implicit operator AsyncRef<T>(RefFunc<T> getRef)
        {
            return new AsyncRef<T>(getRef);
        }

        /// <nodoc />
        public static implicit operator AsyncRef<T>(Box<T> box)
        {
            return new AsyncRef<T>(() => ref box.Value);
        }

        public void Set(T value)
        {
            Value = value;
        }
    }

    public delegate ref TRef RefFunc<TRef>();
    public delegate ref TRef RefFunc<T, TRef>(T arg0);

    /// <summary>
    /// Helper methods for <see cref="AsyncRef{T}"/>
    /// </summary>
    public static class AsyncRef
    {
        public static AsyncRef<T> Create<T>(RefFunc<T> getRef)
        {
            return new AsyncRef<T>(getRef);
        }
    }
}