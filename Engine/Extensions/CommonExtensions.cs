using SM4C.Model;
using Coeus;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;

namespace SM4C.Engine.Extensions
{
    internal static class CommonExtensions
    {
        public static void CheckArgNull(this string argument, string name)
        {
            if (string.IsNullOrWhiteSpace(argument))
            {
                throw new ArgumentNullException(name);
            }
        }

        public static void CheckArgNull(this object argument, string name)
        {
            if (argument == null)
            {
                throw new ArgumentNullException(name);
            }
        }

        public static bool IsEqualTo(this string x, string y, bool ignoreCase = true)
        {
            if (x == null || y == null)
            {
                return false;
            }

            return string.Compare(x, y, ignoreCase, CultureInfo.InvariantCulture) == 0;
        }

        public static bool IsJQExpression(this string s)
        {
            return s.StartsWith("${ ") && s.EndsWith(" }");
        }

        private static string TrimJQExpression(this string jsonPath)
        {
            return jsonPath.Trim('$', '{', '}').Trim();
        }

        public static bool EvalPredicateExpr(this string? expr, JToken json, StateMachineContext context)
        {
            var predicateResult = expr.EvalExpr(json, context);

            if (predicateResult == null)
            {
                return false;
            }

            return predicateResult.Type == JTokenType.Boolean && predicateResult.Value<bool>();
        }

        public static JToken? EvalExpr(this string? expr, JToken json, StateMachineContext context)
        {
            json.CheckArgNull(nameof(json));
            context.CheckArgNull(nameof(context));

            if (string.IsNullOrWhiteSpace(expr))
            {
                return json;
            }
            else if (!expr.IsJQExpression())
            {
                throw new InvalidOperationException("Invalid JQ expression: " + expr);
            }

            expr = expr.TrimJQExpression();

            if (expr.StartsWith("fn:"))
            {
                var functionName = expr[3..];

                Debug.Assert(!string.IsNullOrWhiteSpace(functionName));

                var function = context.Workflow.Functions?.SingleOrDefault(func => func.Name.IsEqualTo(functionName));

                if (function == null || function.Type != FunctionsType.Expression)
                {
                    throw new InvalidOperationException("Unable to resolve function referenced in fn: expression: " + functionName);
                }

                Debug.Assert(!string.IsNullOrWhiteSpace(function.Operation));

                return function.Operation.EvalExpr(json, context);
            }
            else
            {
                expr = expr?.Replace("$input", $"({context.Input.ToString(Newtonsoft.Json.Formatting.None)})");

                expr = expr?.Replace("$output", $"({context.Output.ToString(Newtonsoft.Json.Formatting.None)})");

                return expr.EvalToToken(json);
            }
        }

        public static JToken? Merge(this JToken value, JToken lhs, string? jqExpr, StateMachineContext context)
        {
            value.CheckArgNull(nameof(value));
            lhs.CheckArgNull(nameof(lhs));
            context.CheckArgNull(nameof(context));

            jqExpr = jqExpr?.Replace("$value", $"({value.ToString(Newtonsoft.Json.Formatting.None)})");

            return jqExpr.EvalExpr(lhs, context);
        }
    }
}
