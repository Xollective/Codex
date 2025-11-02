namespace Codex
{
    [GeneratorExclude]
    public interface IImplicitConvertable<TSelf, TOther>
        where TSelf : IImplicitConvertable<TSelf, TOther>
    {
        public static abstract implicit operator TSelf(TOther value);
    }

    public static class IImplicitConvertableExtensions
    {
        public static TSelf From<TSelf, TOther>(this TOther value)
            where TSelf : IImplicitConvertable<TSelf, TOther>
        {
            return value;
        }
    }
}
