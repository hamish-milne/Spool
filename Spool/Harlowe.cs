using System;
using System.Collections;
using System.Collections.Generic;
using Lexico;

namespace Spool
{
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

        public interface TopLevel : Expression
        {

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
        }

        public class IsIn : Operator
        {
            protected override string Op => "is in";
        }
        public class Is : Operator
        {
            protected override string Op => "is";
        }
        public class IsNot : Operator
        {
            protected override string Op => "is not";
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
        public class Contains : Operator
        {
            protected override string Op => "contains";
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

        public class Variable : TopLevel, Mutable
        {
            [CharSet("$_")] private char variableType;
            public bool Global => variableType == '$'; 
            [CharRange("az", "AZ", "09", "__")] public string Name { get; set; }

            public object Evaluate(Context context) => (Global ? context.Globals : context.Locals)[Name];

            public void Set(Context context, object value) => (Global ? context.Globals : context.Locals)[Name] = value;
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
        public class Macro : TopLevel
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
        public class Link : TopLevel
        {
            [Regex(@"(?!->)(?!<-).+")] public string First { get; set; }
            [Optional, Regex(@"(<-)|(->)")] public string? Direction { get; set; }
            [Optional, Regex(@".+")] public string? Second { get; set; }
        }

        [SurroundBy("[", "]")]
        public class Hook : TopLevel
        {
            [Term] public List<Expression> Content { get; } = new List<Expression>();
        }

        public class OpenHook : TopLevel
        {
            [Literal("[==")] Unnamed _;
        }

        [SurroundBy("\"")]
        public class QuotedString : Expression
        {
            [Regex(@"[^""]*")] public string Text { get; set; }
        }

        public class PlainText : TopLevel
        {
            [Regex(@".[^\(\[\$\_]*")] public string Text { get; set; }
        }


        public interface Command
        {
            void Run(Context context);
        }

        public interface Changer
        {
            string Apply(Context context, List<Expression> source);
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
        }
    }

}