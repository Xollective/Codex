namespace Codex.Utilities;

public static class TypeData<T>
{
    public static bool IsValueType { get; } = typeof(T).IsValueType;

    public static bool IsReferenceType => !IsValueType;

    public static bool IsAssignableTo<TOther>() => Other<TOther>.IsAssignableTo;

    public static bool Is<TOther>() => Other<TOther>.Is;

    public static string Name { get; } = typeof(T).Name;

    public static class Other<TOther>
    {
        public static bool Is { get; } = typeof(T) == typeof(TOther);

        public static bool IsAssignableTo { get; } = typeof(T).IsAssignableTo(typeof(TOther));
    }
}
