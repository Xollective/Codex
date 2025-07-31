using System.Reflection;
using Meziantou.Framework.CodeDom;

namespace Codex.Framework.Generator
{
    public interface IEnt { }
    public class Ent : IEnt, IEntity<Ent, IEnt>
    {
        public static ISingletonDescriptor Descriptor => throw new NotImplementedException();

        public static Ent Create(IEnt value = null, bool shallow = false)
        {
            throw new NotImplementedException();
        }

        public static Ent Create()
        {
            throw new NotImplementedException();
        }

        public static DescriptorBase<Ent, IEnt> GetDescriptor()
        {
            throw new NotImplementedException();
        }

        public void OnDeserialized()
        {
            throw new NotImplementedException();
        }

        public void OnSerializing()
        {
            throw new NotImplementedException();
        }
    }

    public static class CodeDomHelpers
    {
        public static T Apply<T>(this T c, Action<T> apply)
            where T : CodeObject
        {
            apply(c);
            return c;
        }

        public static T ApplyIf<T>(this T c, bool shouldApply, Action<T> apply)
            where T : CodeObject
        {
            if (shouldApply)
            {
                apply(c);
            }

            return c;
        }

        public static PropertyDeclaration AddAutoGet(this PropertyDeclaration p)
        {
            return p.Apply(p => p.Getter = GetAutoAccessor());
        }

        private static PropertyAccessorDeclaration GetAutoAccessor()
        {
            return new PropertyAccessorDeclaration() { Statements = null };
        }

        public static PropertyDeclaration AddAutoSet(this PropertyDeclaration p)
        {
            return p.Apply(p => p.Setter = GetAutoAccessor());
        }

        public static MethodDeclaration CreateOverride(this MethodInfo info, Action<MethodDeclaration> modify = null, bool callBase = false)
        {
            var method = Create(info, modify, callBase);
            method.Modifiers &= ~ModifierGroups.OverrideState;

            method.Modifiers |= Modifiers.Override;
            return method;
        }

        public static MethodDeclaration Create(this MethodInfo info, Action<MethodDeclaration> modify = null, bool callBase = false)
        {
            var method = new MethodDeclaration(info.Name);
            var modifiers = default(Modifiers);

            if (info.IsPublic) modifiers |= Modifiers.Public;
            else if (info.IsPrivate) modifiers |= Modifiers.Private;
            else modifiers |= Modifiers.Internal;

            if (info.IsStatic) modifiers |= Modifiers.Static;

            if (info.IsAbstract) modifiers |= Modifiers.Abstract;
            else if (info.IsVirtual) modifiers |= Modifiers.Virtual;

            method.ReturnType = info.ReturnType;

            MethodInvokeExpression baseCall = callBase 
                ? new(new MemberReferenceExpression(new BaseExpression(), method.Name)) 
                : null;

            foreach (var parameter in info.GetParameters())
            {
                var arg = method.AddArgument(parameter.Name, parameter.ParameterType);
                baseCall?.Arguments.Add<Expression>(arg);
            }

            modify?.Invoke(method);

            if (callBase)
            {
                method.Statements ??= new();
                method.Statements.Add<Statement>(info.ReturnType == typeof(void)
                    ? baseCall
                    : new ReturnStatement(baseCall));
            }

            method.Modifiers = modifiers;

            return method;
        }
    }

    public static class ModifierGroups
    {
        public const Modifiers OverrideState
            = Modifiers.Abstract | Modifiers.Virtual | Modifiers.Override;

        public const Modifiers Visibility
            = Modifiers.Public | Modifiers.Private | Modifiers.Internal;
    }
}