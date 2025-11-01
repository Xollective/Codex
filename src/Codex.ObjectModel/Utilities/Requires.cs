using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.ContractsLight;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Codex
{
    public static class Requires
    {
        /// <summary>
        /// Ensure the given value matches the type at compile time. Namely to help
        /// ensure correct data is populated when constructing entity types from other entity types
        /// </summary>
        public static T Expect<T>(this T value)
        {
            return value;
        }

        public static T NonNegative<T>(T value)
            where T : INumber<T>
        {
            Contract.Assert(value >= T.Zero);
            return value;
        }

        public static T Cast<T>(object value)
        {
            return (T)value;
        }

        public static T AssertOrReturn<T>(this AssertionFailure? condition, T value, string message = null)
        {
            condition?.Assert(message);
            return value;
        }

        public static Exception UnexpectedEnum<TEnum>(TEnum e)
            where TEnum : struct, Enum
        {
            throw Contract.AssertFailure($"Unexpected enum value: {typeof(TEnum).Name}.{e}");
        }

        public static void Equals<T>(
            T actual,
            T expected,
            bool includeValuesInMessage = true,
            [CallerArgumentExpression(nameof(actual))]string actualText = null,
            [CallerArgumentExpression(nameof(expected))] string expectedText = null,
            [CallerFilePath] string path = null,
            [CallerLineNumber] int  lineNumber = 0)
        {
            bool equals = EqualityComparer<T>.Default.Equals(actual, expected);
            if (!equals)
            {
                var message = includeValuesInMessage ? $"'{actual}' != '{expected}'" : null;
                Contract.Assert(false, userMessage: message, conditionText: $"{actualText} != {expectedText}", path, lineNumber);
            }
        }

        [Conditional("DEBUG")]
        public static void EqualsDebug<T>(
            T actual,
            T expected,
            bool includeValuesInMessage = true,
            [CallerArgumentExpression(nameof(actual))] string actualText = null,
            [CallerArgumentExpression(nameof(expected))] string expectedText = null,
            [CallerFilePath] string path = null,
            [CallerLineNumber] int lineNumber = 0)
        {
            bool equals = EqualityComparer<T>.Default.Equals(actual, expected);
            if (!equals)
            {
                var message = includeValuesInMessage ? $"'{actual}' != '{expected}'" : null;
                Contract.Assert(false, userMessage: message, conditionText: $"{actualText} != {expectedText}", path, lineNumber);
            }
        }
    }
}
