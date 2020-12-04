

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


    static class Expressions
    {

        interface Mutable : Expression
        {
            void Set(Context context, object value);
        }

        [WhitespaceSeparated]
        class VariableToValue : Expression
        {
            [Term] public Expression Variable { get; set; }
            [Literal("to")] Unnamed _;
            [Term] public Expression Value { get; set; }

            public object Evaluate(Context context) {
                var m = Variable.Evaluate(context) as Mutable;
                var v = Value.Evaluate(context);
                if (m == null) {
                    throw new Exception("Value not mutable");
                }
                return new SetFunction(() => m.Set(context, v));
            }
        }

        [WhitespaceSeparated]
        class ValueIntoVariable : Expression
        {
            [Term] public Expression Value { get; set; }
            [Literal("into")] Unnamed _;
            [Term] public Expression Variable { get; set; }

            public object Evaluate(Context context) {
                var m = Variable.Evaluate(context) as Mutable;
                var v = Value.Evaluate(context);
                if (m == null) {
                    throw new Exception("Value not mutable");
                }
                return new PutFunction(() => m.Set(context, v));
            }
        }


        [WhitespaceSeparated]
        abstract class Operator : Expression
        {
            [Term] public Expression LHS { get; set; }
            [IndirectLiteral(nameof(Op))] protected Unnamed _;
            [Term] public Expression RHS { get; set; }
            protected abstract string Op { get; }

            private static readonly MethodInfo[] methods = typeof(OperatorImpl).GetMethods();

            public object Evaluate(Context context)
            {
                var args = new []{LHS.Evaluate(context), RHS.Evaluate(context)};
                foreach (var m in methods)
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

            public bool Less(double lhs, double rhs) => lhs < rhs;
            public bool Greater(double lhs, double rhs) => lhs > rhs;
            public bool LessOrEqual(double lhs, double rhs) => lhs <= rhs;
            public bool GreaterOrEqual(double lhs, double rhs) => lhs >= rhs;
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
        [WhitespaceSeparated, SurroundBy("(", ")")]
        class Parenthesized : Expression
        {
            [Term] Expression inner;

            public object Evaluate(Context context) => inner.Evaluate(context);
        }

        public abstract class ObjectExpression : Renderable, Expression
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
        class MemberAccess : Mutable
        {
            [Suffix("'s")] public Expression Object { get; set; }
            [Term] public Expression Member { get; set; }

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
                        case int idx:
                            return array[idx];
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
                    break;
                case IList array:
                    switch (member) {
                        case int idx:
                            array[idx] = value;
                            break;
                    }
                    break;
                }
                throw new Exception("Member not found");
            }
        }

        public class Variable : ObjectExpression, Mutable
        {
            [CharSet("$_")] private char variableType { set => Global = value == '$'; }
            public bool Global { get; set; }
            [CharRange("az", "AZ", "09", "__"), Repeat] public string Name { get; set; }

            public override object Evaluate(Context context) => (Global ? context.Globals : context.Locals)[Name];

            public void Set(Context context, object value) => (Global ? context.Globals : context.Locals)[Name] = value;
        }

        class Integer : Expression
        {
            [CharRange("09"), Repeat] string number;
            [Regex("st|nd|rd|th")] Unnamed _;

            public object Evaluate(Context context) => (double)int.Parse(number);
        }

        class Float : Expression
        {
            [Term] double number;

            public object Evaluate(Context context) => number;
        }

        class HookRef : Expression
        {
            [Literal("?")] Unnamed _;
            [CharRange("az", "AZ", "09", "__"), Repeat] public string Name { get; set; }

            public object Evaluate(Context context)
            {
                var nameAttr = XName.Get("name");
                IEnumerable<XContainer> generator(XContainer root)
                {
                    var current = root.FirstNode;
                    while (current != null)
                    {
                        if (current is XContainer c && (current as XElement)?.Attribute(nameAttr)?.Value == Name) {
                            yield return c;
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
            [CharRange("az", "AZ", "09", "__", "--"), Repeat, Suffix(":")] public string Name { get; set; }
            [SeparatedBy(typeof(Comma))] public List<Expression> Arguments { get; } = new List<Expression>();

            [WhitespaceSurrounded]
            private class Comma {
                [Literal(",")] Unnamed _;
            }

            public override object Evaluate(Context context)
            {
                var normalizedName = Name.Replace("-", "").Replace("_", "");
                var args = Arguments.Select(x => x.Evaluate(context)).ToArray();
                foreach (var m in context.MacroProvider.GetType().GetMethods().Where(x => x.Name.Equals(normalizedName, StringComparison.OrdinalIgnoreCase)))
                {
                    try {
                        return m.Invoke(context.MacroProvider, args);
                    } catch (TargetInvocationException e) {
                        throw e.InnerException;
                    } catch {
                        continue;
                    }
                }
                throw new Exception($"No macro '{Name}' with the given arguments");
            }
        }

        class False : Expression
        {
            [Literal("false")] Unnamed _;

            public object Evaluate(Context context) => false;
        }

        class True : Expression
        {
            [Literal("true")] Unnamed _;

            public object Evaluate(Context context) => true;
        }

        [SurroundBy("\"")]
        class QuotedString : Expression
        {
            [Regex(@"[^""]*")] public string Text { get; set; }

            public object Evaluate(Context context) => Text;
        }

    }
}