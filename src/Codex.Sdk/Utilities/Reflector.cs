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
        MemberInfo member = GetProperty<TObject>(name);
        return GetGetter<TObject, TProperty>(member);
    }

    public static Action<TObject, TProperty> GetSetter<TObject, TProperty>(string name)
    {
        MemberInfo member = GetProperty<TObject>(name);
        return GetSetter<TObject, TProperty>(member);
    }

    private static PropertyInfo GetProperty<TObject>(string name)
    {
        var type = typeof(TObject);
        var member = type.GetProperty(name,
            BindingFlags.NonPublic
            | BindingFlags.Public
            | BindingFlags.Instance);
        return member;
    }

    public static RefOfFunc<TObject, TField> GetFieldRef<TObject, TField>(string name)
    {
        return GetFieldRef<TObject, TField>(GetField<TObject>(name));
    }

    public static RefOfFunc<TObject, TField> GetFieldRef<TObject, TField>(MemberInfo member)
    {
        var obj = Expression.Parameter(typeof(TObject), "obj");
        return Expression.Lambda<RefOfFunc<TObject, TField>>(
            Expression.Call(null, typeof(Out).GetMethod(nameof(Out.CreateRef))!.MakeGenericMethod(typeof(TField)), Expression.MakeMemberAccess(obj, member)),
            obj).Compile();
    }

    private static FieldInfo GetField<TObject>(string name)
    {
        var type = typeof(TObject);
        var member = type.GetField(name,
            BindingFlags.NonPublic
            | BindingFlags.Public
            | BindingFlags.Instance);
        return member;
    }
}