using System.Linq.Expressions;
using System.Reflection;

namespace Codex.Utilities;

public class Reflector
{
    public static Func<TObject> GetNew<TObject>()
    {
        return Expression.Lambda<Func<TObject>>(
            Expression.New(typeof(TObject))).Compile();
    }

    public static Func<TObject, TProperty> GetGetter<TObject, TProperty>(MemberInfo member)
    {
        var obj = Expression.Parameter(typeof(TObject), "obj");
        return Expression.Lambda<Func<TObject, TProperty>>(
            Expression.MakeMemberAccess(obj, member),
            obj).Compile();
    }

    public static Action<TObject, TProperty> GetSetter<TObject, TProperty>(MemberInfo member)
    {
        var obj = Expression.Parameter(typeof(TObject), "obj");
        var value = Expression.Parameter(typeof(TProperty), "value");
        return Expression.Lambda<Action<TObject, TProperty>>(
            Expression.Assign(Expression.MakeMemberAccess(obj, member), value),
            obj, value).Compile();
    }

    public static Func<TObject, TProperty> GetGetter<TObject, TProperty>(string name)
    {
        MemberInfo member = GetMember<TObject>(name);
        return GetGetter<TObject, TProperty>(member);
    }

    public static Action<TObject, TProperty> GetSetter<TObject, TProperty>(string name)
    {
        MemberInfo member = GetMember<TObject>(name);
        return GetSetter<TObject, TProperty>(member);
    }

    private static MemberInfo GetMember<TObject>(string name)
    {
        var type = typeof(TObject);
        MemberInfo member = type.GetProperty(name,
            BindingFlags.NonPublic
            | BindingFlags.Public
            | BindingFlags.Instance);

        member ??= type.GetField(name,
            BindingFlags.NonPublic
            | BindingFlags.Public
            | BindingFlags.Instance);
        return member;
    }
}