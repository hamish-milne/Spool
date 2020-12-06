

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Lexico;

namespace Spool.Harlowe
{

    interface Expression
    {
        object Evaluate(Context context);
    }

    interface Mutable : Expression
    {
        void Set(Context context, object value);
        void Delete(Context context);
    }

    delegate bool Filter(object value);

    static class Expressions
    {
        // Marker interface for expressions that can be a part of operators etc.
        // This makes sure the parser doesn't try to parse `each $foo + 1` as `(each $foo) + 1` etc.
        interface SubExpression : Expression {}
        interface TopExpression : Expression {}

        [WhitespaceSeparated]
        class Each : TopExpression
        {
            [Literal("each")] Unnamed _;
            [Term] Variable variable;

            public object Evaluate(Context context)
            {
                return new Filter(value => {
                    variable.Set(context, value);
                    return true;
                });
            }
        }

        [WhitespaceSeparated]
        class Where : TopExpression
        {
            [Term] Variable variable;
            [Literal("where")] Unnamed _;
            [Term] SubExpression expression;

            public object Evaluate(Context context)
            {
                return new Filter(value => {
                    variable.Set(context, value);
                    return (bool)expression.Evaluate(context);
                });
            }
        }

        [WhitespaceSeparated]
        class To : TopExpression
        {
            [Term] SubExpression Variable;
            [Literal("to")] Unnamed _;
            [Term] SubExpression Value;

            public object Evaluate(Context context) {
                var m = Variable as Mutable;
                if (m == null) {
                    throw new Exception("Value not mutable");
                }
                var v = Value.Evaluate(context);
                return new VariableToValue {
                    Source = v,
                    Destination = m
                };
            }
        }

        [WhitespaceSeparated]
        class Into : TopExpression
        {
            [Term] SubExpression Value;
            [Literal("into")] Unnamed _;
            [Term] SubExpression Variable;

            public object Evaluate(Context context) {
                var m = Variable as Mutable;
                var v = Value.Evaluate(context);
                if (m == null) {
                    throw new Exception("Value not mutable");
                }
                return new ValueIntoVariable {
                    Source = v,
                    Destination = m,
                    ToRemove = Value as Mutable
                };
            }
        }



        // TODO: Fix this so that order of operations is respected
        class SimpleOperatorExpr : SubExpression
        {
            [Term] protected SubExpression LHS;
            [WhitespaceSurrounded] protected Operator Operator;
            [Term] protected SubExpression RHS;

            public object Evaluate(Context context)
            {
                Operator.LHS = LHS;
                Operator.RHS = RHS;
                return Operator.Evaluate(context);
            }
        }
        class SimpleUnaryExpr : SubExpression
        {
            [WhitespaceSurrounded] protected Unary Operator;
            [Term] protected SubExpression RHS;

            public object Evaluate(Context context)
            {
                Operator.RHS = RHS;
                return Operator.Evaluate(context);
            }
        }


        abstract class Operator
        {
            /*[Term]*/ public Expression LHS;
            [IndirectLiteral(nameof(Op))/*, WhitespaceSurrounded*/] protected Unnamed _;
            /*[Term]*/ public Expression RHS;
            protected abstract string Op { get; }

            private static readonly MethodInfo[] methods = typeof(OperatorImpl).GetMethods();

            public object Evaluate(Context context)
            {
                var args = new []{LHS.Evaluate(context), RHS.Evaluate(context)};
                foreach (var m in methods.Where(m => m.Name == GetType().Name))
                {
                    try {
                        return m.Invoke(null, args);
                    } catch (TargetInvocationException e) {
                        throw e.InnerException;
                    } catch {
                        continue;
                    }
                }
                throw new Exception($"No '{GetType().Name}' operator found for arguments '{args[0]}' and '{args[1]}'");
            }
        }

        class OperatorImpl
        {
            public static IEnumerable<Changer> Add(IEnumerable<Changer> lhs, IEnumerable<Changer> rhs) => lhs.Concat(rhs).ToArray();
            public static IEnumerable<Changer> Add(IEnumerable<Changer> lhs, Changer rhs) => lhs.Append(rhs).ToArray();
            public static IEnumerable<Changer> Add(Changer lhs, IEnumerable<Changer> rhs) => new[]{lhs}.Concat(rhs).ToArray();
            public static double Add(double lhs, double rhs) => lhs + rhs;
            public static string Add(string lhs, string rhs) => lhs + rhs;

            public static double Sub(double lhs, double rhs) => lhs - rhs;
            public static double Mul(double lhs, double rhs) => lhs * rhs;
            public static double Div(double lhs, double rhs) => lhs / rhs;
            public static double Mod(double lhs, double rhs) => lhs % rhs;

            public static bool IsIn(object lhs, ICollection<object> rhs) => rhs.Contains(lhs);
            public static bool Contains(ICollection<object> lhs, object rhs) => lhs.Contains(rhs);

            public static bool Is(object lhs, object rhs) => lhs.Equals(rhs);
            public static bool IsNot(object lhs, object rhs) => !lhs.Equals(rhs);

            public static bool Less(double lhs, double rhs) => lhs < rhs;
            public static bool Greater(double lhs, double rhs) => lhs > rhs;
            public static bool LessOrEqual(double lhs, double rhs) => lhs <= rhs;
            public static bool GreaterOrEqual(double lhs, double rhs) => lhs >= rhs;

            public static bool Or(bool lhs, bool rhs) => lhs || rhs;
            public static bool And(bool lhs, bool rhs) => lhs && rhs;

            public static bool Not(bool rhs) => !rhs;
        }

        class Less : Operator
        {
            protected override string Op => "<";
        }
        class Greater : Operator
        {
            protected override string Op => ">";
        }
        class LessOrEqual : Operator
        {
            protected override string Op => "<=";
        }
        class GreaterOrEqual : Operator
        {
            protected override string Op => ">=";
        }
        class IsIn : Operator
        {
            protected override string Op => "is in";
        }
        class Contains : Operator
        {
            protected override string Op => "contains";
        }
        class Is : Operator
        {
            protected override string Op => "is";
        }
        class IsNot : Operator
        {
            protected override string Op => "is not";
        }
        class Matches : Operator
        {
            protected override string Op => "matches";
        }
        class Or : Operator
        {
            protected override string Op => "or";
        }
        class And : Operator
        {
            protected override string Op => "and";
        }
        class Add : Operator
        {
            protected override string Op => "+";
        }
        class Mul : Operator
        {
            protected override string Op => "*";
        }
        class Sub : Operator
        {
            protected override string Op => "-";
        }
        class Div : Operator
        {
            protected override string Op => "/";
        }
        class Modulo : Operator
        {
            protected override string Op => "%";
        }
        class Of : Operator
        {
            protected override string Op => "of";
        }

        abstract class Unary
        {
            [IndirectLiteral(nameof(Op))/*, WhitespaceSurrounded*/] protected Unnamed _;
            /*[Term]*/ public Expression RHS;
            protected abstract string Op { get; }

            private static readonly MethodInfo[] methods = typeof(OperatorImpl).GetMethods();

            public object Evaluate(Context context)
            {
                var args = new []{RHS.Evaluate(context)};
                foreach (var m in methods.Where(m => m.Name == GetType().Name))
                {
                    try {
                        return m.Invoke(null, args);
                    } catch (TargetInvocationException e) {
                        throw e.InnerException;
                    } catch {
                        continue;
                    }
                }
                throw new Exception($"No '{GetType().Name}' operator found for arguments '{args[1]}'");
            }
        }

        class Not : Unary
        {
            protected override string Op => "not";
        }

        [WhitespaceSeparated, SurroundBy("(", ")")]
        class Parenthesized : SubExpression
        {
            [Pass, Cut] Unnamed _;
            [Term] SubExpression inner;

            public object Evaluate(Context context) => inner.Evaluate(context);
        }

        public abstract class ObjectExpression : Renderable, SubExpression
        {
            public abstract object Evaluate(Context context);

            public void Render(Context context)
            {
                var value = Evaluate(context);
                if (value is Changer) {
                    throw new Exception("Changers must be applied to hooks");
                }
                if (value is Command cmd) {
                    cmd.Run(context);
                    return;
                } else if (value is Renderable rnd) {
                    rnd.Render(context);
                } else {
                    if (context.Cursor.LastNode is XText prevText) {
                        prevText.Value += value;
                    } else {
                        context.Cursor.Add(new XText(value.ToString()));
                    }
                }
            }
        }

        [WhitespaceSeparated]
        class MemberAccess : Mutable, SubExpression
        {
            [Suffix("'s")] public SubExpression Object { get; set; }
            [Alternative(
                typeof(Macro),
                typeof(Parenthesized),
                typeof(Variable),
                typeof(Integer),
                typeof(Float),
                typeof(QuotedString),
                typeof(SingleQuotedString),
                typeof(BareString)
            )] public SubExpression Member { get; set; }

            public object Evaluate(Context context)
            {
                var obj = Object.Evaluate(context);
                var member = Member.Evaluate(context);
                switch (obj) {
                case IDictionary map:
                    return map[member];
                case IList array:
                    switch (member) {
                        case "length":
                            return array.Count;
                        case double idx:
                            return array[(int)idx - 1];
                    }
                    break;
                }
                throw new Exception("Member not found");
            }

            public void Set(Context context, object value)
            {
                var obj = Object.Evaluate(context);
                var member = Member.Evaluate(context);
                switch (obj) {
                case IDictionary map:
                    map[member] = value;
                    return;
                case IList array:
                    switch (member) {
                        case double idx:
                            array[(int)idx - 1] = value;
                            return;
                    }
                    break;
                }
                throw new Exception("Member not found");
            }

            public void Delete(Context context)
            {
                var obj = Object.Evaluate(context);
                var member = Member.Evaluate(context);
                switch (obj) {
                case IDictionary map:
                    map.Remove(member);
                    return;
                case IList array:
                    switch (member) {
                        case double idx:
                            array.RemoveAt((int)idx - 1);
                            return;
                    }
                    break;
                }
                throw new Exception("Member not found");
            }
        }

        public class Variable : ObjectExpression, Mutable
        {
            [CharSet("$_")] private char variableType { get => Global ? '$' : '_'; set => Global = value == '$'; }
            public bool Global { get; set; }
            [Regex(@"[a-zA-Z_][a-zA-Z0-9_]*")] public string Name { get; set; }

            public override object Evaluate(Context context) => (Global ? context.Globals : context.Locals)[Name];

            public void Set(Context context, object value) => (Global ? context.Globals : context.Locals)[Name] = value;

            public void Delete(Context context) => (Global ? context.Globals : context.Locals).Remove(Name);
        }

        class Integer : SubExpression
        {
            [CharRange("09"), Repeat] string number;
            [Optional, Regex("st|nd|rd|th")] Unnamed _;

            public object Evaluate(Context context) => (double)int.Parse(number);
        }

        class Float : SubExpression
        {
            [Term] double number;

            public object Evaluate(Context context) => number;
        }

        class HookRef : SubExpression
        {
            [Literal("?")] Unnamed _;
            [CharRange("az", "AZ", "09", "__"), Repeat] public string Name;

            // TODO: Change these depending on the renderer
            private static readonly Dictionary<string, XName> BuiltInNames = new Dictionary<string, XName>(StringComparer.OrdinalIgnoreCase)
            {
                { "Page", XName.Get("tw-story") },
                { "Passage", XName.Get("tw-passage") },
                { "Sidebar", XName.Get("tw-sidebar") },
                { "Link", XName.Get("link") }
            };

            public object Evaluate(Context context)
            {
                BuiltInNames.TryGetValue(Name, out var builtInName);
                var nameAttr = XName.Get("name");
                IEnumerable<XContainer> generator(XContainer root)
                {
                    var current = root.FirstNode;
                    while (current != null)
                    {
                        if (current is XElement el && (
                            el.Attribute(nameAttr)?.Value?.Equals(Name, StringComparison.OrdinalIgnoreCase) == true
                            || el.Name == builtInName)) {
                            yield return el;
                        }
                        var next = current.NextNode;
                        while (next == null && current != null) {
                            current = current.Parent;
                            next = current.NextNode;
                        }
                        current = next;
                    }
                }
                return generator(context.Screen);
            }
        }

        [WhitespaceSeparated, SurroundBy("(", ")")]
        public class Macro : ObjectExpression
        {
            public struct Argument
            {
                [Optional, Literal("...")] public string spread;
                [Alternative(typeof(TopExpression), typeof(SubExpression))] public Expression value;
            }

            [CharRange("az", "AZ", "09", "__", "--"), Repeat, Suffix(":"), Cut] public string Name { get; set; }
            [SeparatedBy(typeof(Comma)), Repeat(Min = 0)] public List<Argument> Arguments { get; } = new List<Argument>();
            [Optional] protected Comma _ { get; set; } // TODO: Replace with TrailingSeparator when available

            [WhitespaceSurrounded]
            protected struct Comma {
                [Literal(",")] Unnamed _;
            }

            public override object Evaluate(Context context)
            {
                var normalizedName = Name.Replace("-", "").Replace("_", "");
                IEnumerable<object> GetArgList()
                {
                    foreach (var a in Arguments)
                    {
                        if (a.spread == null) {
                            yield return a.value.Evaluate(context);
                        } else {
                            var toSpread = a.value.Evaluate(context);
                            if (toSpread is IEnumerable enumerable) {
                                foreach (var e in enumerable) {
                                    yield return e;
                                }
                            } else {
                                throw new Exception($"Tried to use spread operator on {toSpread}");
                            }
                        }
                    }
                }
                var args = GetArgList().ToArray();
                foreach (var m in context.MacroProvider.GetType().GetMethods().Where(x => x.Name.Equals(normalizedName, StringComparison.OrdinalIgnoreCase)))
                {
                    try {
                        var mArgs = args;
                        var parameters = m.GetParameters();
                        if (parameters.Length > 0 && parameters.Last().GetCustomAttribute<ParamArrayAttribute>() != null)
                        {
                            var inArray = args.Skip(parameters.Length - 1).ToArray();
                            var pArray = Array.CreateInstance(parameters.Last().ParameterType.GetElementType(), inArray.Length);
                            inArray.CopyTo(pArray, 0);
                            mArgs = args.Take(parameters.Length - 1).Append(pArray).ToArray();
                        }
                        return m.Invoke(context.MacroProvider, mArgs);
                    } catch (TargetInvocationException e) {
                        throw e.InnerException;
                    } catch {
                        continue;
                    }
                }
                throw new Exception($"No macro '{Name}' with the given arguments");
            }
        }

        class False : SubExpression
        {
            [Literal("false")] Unnamed _;

            public object Evaluate(Context context) => false;
        }

        class True : SubExpression
        {
            [Literal("true")] Unnamed _;

            public object Evaluate(Context context) => true;
        }

        [SurroundBy("\"")]
        class QuotedString : SubExpression
        {
            [Regex(@"[^""]*")] public string Text { get; set; }

            public object Evaluate(Context context) => Text;
        }

        [SurroundBy("'")]
        class SingleQuotedString : SubExpression
        {
            [Regex(@"[^']*")] public string Text { get; set; }

            public object Evaluate(Context context) => Text;
        }

        class BareString : SubExpression
        {
            [CharRange("az", "AZ", "09", "__", "--"), Repeat] public string Text { get; set; }

            public object Evaluate(Context context) => Text;
        }

    }
}