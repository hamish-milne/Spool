using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lexico;

namespace Spool
{

    namespace Layout
    {
        public interface Span
        {
        }

        public class TextSpan : Span
        {
            public string Text { get; }
            public TextSpan(string text) => Text = text;
        }

        public class NamedSpan : Span
        {
            public string Name { get; }
            public Span Inner { get; }
        }

        public class Sequence : Span
        {
            public IEnumerable<Span> Children { get; }
        }
        
        public class Hidden : Span
        {
            public Renderable Renderer { get; }
        }
    }

    public static class Util
    {
        public static Layout.Span Replace(this Layout.Span span, string name, Layout.Span)
    }

    public interface Renderable
    {
        Layout.Span Render(Context context);
    }

    public class Harlowe
    {
        public interface Expression
        {
            object Evaluate(Context context);
        }

        public interface Mutable : Expression
        {
            void Set(Context context, object value);
        }

        public delegate void SetFunction();
        public delegate void PutFunction();

        [WhitespaceSeparated]
        public abstract class Operator : Expression
        {
            [Term] public Expression LHS { get; set; }
            [IndirectLiteral(nameof(Op))] Unnamed _;
            [Term] public Expression RHS { get; set; }
            protected abstract string Op { get; }

            public abstract object Evaluate(Context context);
        }

        public class IsIn : Operator
        {
            protected override string Op => "is in";
            public override object Evaluate(Context context) => (RHS.Evaluate(context) as ICollection<object>).Contains(LHS.Evaluate(context));
        }
        public class Contains : Operator
        {
            protected override string Op => "contains";
            public override object Evaluate(Context context) => (LHS.Evaluate(context) as ICollection<object>).Contains(RHS.Evaluate(context));
        }
        public class Is : Operator
        {
            protected override string Op => "is";
            public override object Evaluate(Context context) => LHS.Evaluate(context).Equals(RHS.Evaluate(context));
        }
        public class IsNot : Operator
        {
            protected override string Op => "is not";
            public override object Evaluate(Context context) => !LHS.Evaluate(context).Equals(RHS.Evaluate(context));
        }
        public class Add : Operator
        {
            protected override string Op => "+";
        }
        public class Mul : Operator
        {
            protected override string Op => "*";
        }
        public class Sub : Operator
        {
            protected override string Op => "-";
        }
        public class Div : Operator
        {
            protected override string Op => "/";
        }
        public class Modulo : Operator
        {
            protected override string Op => "%";
        }
        public class Less : Operator
        {
            protected override string Op => "<";
        }
        public class Greater : Operator
        {
            protected override string Op => ">";
        }
        public class LessOrEqual : Operator
        {
            protected override string Op => "<=";
        }
        public class GreaterOrEqual : Operator
        {
            protected override string Op => ">=";
        }
        public class Matches : Operator
        {
            protected override string Op => "matches";
        }
        public class Of : Operator
        {
            protected override string Op => "of";
        }

        [WhitespaceSeparated]
        public class VariableToValue : Expression
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
        public class ValueIntoVariable : Expression
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

        public class Variable : Renderable, Mutable
        {
            [CharSet("$_")] private char variableType { set => Global = value == '$'; }
            public bool Global { get; set; }
            [CharRange("az", "AZ", "09", "__")] public string Name { get; set; }

            public object Evaluate(Context context) => (Global ? context.Globals : context.Locals)[Name];

            public void Set(Context context, object value) => (Global ? context.Globals : context.Locals)[Name] = value;

            public Layout.Span Render(Context context) => new Layout.TextSpan(Evaluate(context).ToString());
        }

        [WhitespaceSeparated]
        public class MemberAccess : Mutable
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

        [SurroundBy("(", ")"), WhitespaceSeparated]
        public class Macro : Renderable, Expression
        {
            [Suffix(":"), CharRange("az", "AZ", "09", "__")] public string Name { get; set; }
            [SeparatedBy(typeof(Comma))] public List<Expression> Arguments { get; } = new List<Expression>();

            [WhitespaceSurrounded]
            private class Comma {
                [Literal(",")] Unnamed _;
            }

            public object Evaluate(Context context)
            {
                
            }
        }

        [SurroundBy("[[", "]]")]
        public class Link : Renderable
        {
            [Regex(@"(?!->)(?!<-).+")] public string First { get; set; }
            [Optional, Regex(@"(<-)|(->)")] public string Direction { get; set; }
            [Optional, Regex(@".+")] public string Second { get; set; }
        }

        [SurroundBy("[", "]")]
        public class Hook : Renderable
        {
            [Term] public List<Renderable> Content { get; } = new List<Renderable>();

            public Layout.Span Render(Context context)
            {
                return Layout.Sequence(Content.Select(x => x.Render(context)).ToArray());
            }
        }

        public class OpenHook : Renderable
        {
            [Literal("[==")] Unnamed _;

            public Layout.Span Render(Context context) => new Layout.TextSpan("");
        }

        [SurroundBy("\"")]
        public class QuotedString : Expression
        {
            [Regex(@"[^""]*")] public string Text { get; set; }
        }

        public class PlainText : Renderable
        {
            [Regex(@".[^\(\[\$\_]*")] public string Text { get; set; }

            public Layout.Span Render(Context context) => new Layout.TextSpan(Text);
        }


        public class ChangerChain : Renderable
        {
            public Changer Changer { get; }
            public Renderable Source { get; }

            public Layout.Span Render(Context context) => Changer.Apply(context, Source);
        }


        public interface Command
        {
            void Run(Context context);
        }

        public interface Changer
        {
            Layout.Span Apply(Context context, Renderable source);
        }

        public class Hidden : Changer
        {
            public Layout.Span Apply(Context context, Renderable source)
            {
                return new Layout.Hidden(source);
            }
        }

        public class Show : Command
        {
            public void Run(Context context)
            {

            }
        }

        public class BuiltInMacros
        {
            public void Set(SetFunction setter) => setter();
            public void Put(PutFunction setter) => setter();
            public ICollection<object> DS(params object[] values) => DataSet(values);
            public ICollection<object> DataSet(params object[] values) => new HashSet<object>(values);
            public IDictionary DataMap(params object[] pairs) {
                var map = new Dictionary<object, object>();
                for (int i = 1; i < pairs.Length; i += 2) {
                    map[pairs[i - 1]] = pairs[i];
                }
                return map;
            }
            public IDictionary DM(params object[] pairs) => DataMap(pairs);
            public Array Array(params object[] values) => values;
            public Array A(params object[] values) => Array(values);
            public Changer Hidden() => new Hidden();
            public Command Show() => new Show();
        }
    }

}