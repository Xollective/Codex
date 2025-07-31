using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Codex.ObjectModel;
using Codex.ObjectModel.Attributes;

namespace Codex.Utilities.Serialization;

public static class TypeSystemHelpers
{
    public static bool Is<T, TCandidate>(T value, Action<TCandidate> action)
    {
        if (action is Action<T> valueAction)
        {
            valueAction(value);
            return true;
        }

        return false;
    }

    public static IEnumerable<TProperty> FilterProperties<TProperty>(this IEnumerable<TProperty> properties, ObjectStage stage)
        where TProperty : ICustomAttributeProvider
    {
        return properties.Where(p => !p.ShouldRemoveProperty(stage));
    }

    public static bool ShouldRemoveProperty(this ICustomAttributeProvider property, ObjectStage stage, bool isDataContract = false)
    {
        if (isDataContract && (property.GetAttribute<DataMemberAttribute>() == null))
        {
            return true;
        }

        bool shouldRemove = false;
        if (stage != ObjectStage.None)
        {
            shouldRemove = property.GetAttribute<IncludeAttribute>() is IncludeAttribute include
                                && !matches(stage, include);

            shouldRemove |= property.GetAttribute<ExcludeAttribute>() is ExcludeAttribute exclude
                && exclude.ExcludedStages.Any(s => s == stage);
        }

        shouldRemove |= property.GetAttribute<IgnoreDataMemberAttribute>() != null;

        shouldRemove |= property.GetAttribute<JsonIgnoreAttribute>()?.Condition == JsonIgnoreCondition.Always;

        return shouldRemove;

        static bool matches(ObjectStage stage, IncludeAttribute include)
        {
            if (include.AllowedStages == ObjectStage.None) return false;
            var intersection = include.AllowedStages & stage;
            return intersection == stage || intersection == include.AllowedStages;
        }
    }

    public static IEnumerable<T> GetAttributes<T>(this ICustomAttributeProvider type) where T : class
    {
        // Check for attributes on the type itself first
        object[] attributes = type.GetCustomAttributes(typeof(T), false);
        return attributes.OfType<T>();
    }

    public static T GetAttribute<T>(this ICustomAttributeProvider type) where T : class
    {
        // Check for attributes on the type itself first
        object[] attributes = type.GetCustomAttributes(typeof(T), false);
        if (attributes.Length == 1) return attributes[0] as T;

        // If that didn't work, include base types
        attributes = type.GetCustomAttributes(typeof(T), true);
        if (attributes.Length == 1) return attributes[0] as T;

        // If there were zero, return null, otherwise throw
        if (attributes.Length == 0) return null;

        throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Found {0} instances of {1} on {2}. This method can only be used if 0 or 1 instances of the attribute exist.", attributes.Length, typeof(T).Name, type));
    }

    public static T ReflectionInvoke<T>(ref T value, Expression<Action> method, Type[] typeParams, params object[] args)
    {
        return value = (T)ReflectionInvoke(method, typeParams, args);
    }

    public static T ReflectionInvoke<T>(Expression<Action> method, Type[] typeParams, params object[] args)
    {
        return (T)ReflectionInvoke(method, typeParams, args);
    }

    public static object ReflectionInvoke(Expression<Action> method, Type[] typeParams, params object[] args)
    {
        var methodInfo = ((MethodCallExpression)method.Body).Method;

        if (typeParams.Length != 0)
        {
            methodInfo = methodInfo.GetGenericMethodDefinition().MakeGenericMethod(typeParams);
        }

        if (methodInfo.IsStatic)
        {
            return methodInfo.Invoke(null, args);
        }
        else
        {
            return methodInfo.Invoke(args[0], args[1..]);
        }
    }

    public const BindingFlags FlattenPublicInstanceFlags = BindingFlags.Public
        | BindingFlags.Instance
        | BindingFlags.FlattenHierarchy;

    public const BindingFlags DeclaredPublicInstanceFlags = BindingFlags.Public
        | BindingFlags.Instance
        | BindingFlags.DeclaredOnly;

    public static IEnumerable<PropertyInfo> GetTransitiveInterfaceProperties(this Type t)
    {
        return new[] { t }.Concat(t.GetInterfaces())
            .SelectMany(i => i.GetProperties(FlattenPublicInstanceFlags));
    }
}