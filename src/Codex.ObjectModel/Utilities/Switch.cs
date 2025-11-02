namespace Codex.Utilities
{
    public record struct Switch<T>(T Primary, T Secondary)
    {
        public Switch<T> GetSwapped() => new(Secondary, Primary);

        public Switch(Func<T> factory) : this(factory(), factory())
        {
        }

        public void ApplyAll(Action<T> action)
        {
            action(Primary);
            action(Secondary);
        }
    }

    public static class Switch
    {
        public static void Swap<T>(this ref Switch<T> s)
        {
            s = s.GetSwapped();
        }

        public static Switch<T> Create<T>(T primary, T secondary) => new(primary, secondary);

        public static Switch<T> Create<T>(Func<T> factory) => new(factory(), factory());
    }
}
