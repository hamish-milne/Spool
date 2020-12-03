using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Lexico;

namespace Spool
{

    public interface Renderable
    {
        XNode Render(Context context);
    }

    public static class Util
    {
    }

    public class Harlowe
    {
        public interface Expression
        {
            object Evaluate(Context context);
        }

        public interface PrefixExpression : Expression {}

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

        public abstract class ObjectExpression : Renderable, PrefixExpression
        {
            public abstract object Evaluate(Context context);

            public XNode Render(Context context)
            {
                var value = Evaluate(context);
                if (value is Changer) {
                    throw new Exception("Changers must be applied to hooks");
                }
                if (value is Command cmd) {
                    cmd.Run(context);
                    return new XText("");
                } else {
                    return new XText(value.ToString());
                }
            }
        }

        public class Variable : ObjectExpression, Mutable
        {
            [CharSet("$_")] private char variableType { set => Global = value == '$'; }
            public bool Global { get; set; }
            [CharRange("az", "AZ", "09", "__")] public string Name { get; set; }

            public override object Evaluate(Context context) => (Global ? context.Globals : context.Locals)[Name];

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
        public class Macro : ObjectExpression
        {
            [Suffix(":"), CharRange("az", "AZ", "09", "__")] public string Name { get; set; }
            [SeparatedBy(typeof(Comma))] public List<Expression> Arguments { get; } = new List<Expression>();

            [WhitespaceSurrounded]
            private class Comma {
                [Literal(",")] Unnamed _;
            }

            public override object Evaluate(Context context)
            {
                
            }
        }

        [WhitespaceSeparated]
        public class AppliedHook : Renderable
        {

            [WhitespaceSurrounded]
            private class HookSeparator
            {
                [Literal("+")] Unnamed _;
            }

            [WhitespaceSeparated]
            private class HookPrefix : Changer
            {
                [Literal("|")] Unnamed _;
                [CharRange("az", "AZ", "09", "__")] string name;
                [CharSet(">)")] char hidden;

                public void Apply(ref bool hidden, ref string name)
                {
                    if (name != null) {
                        throw new Exception("Hook already named");
                    }
                    name = this.name;
                    if (this.hidden == ')') {
                        hidden = true;
                    }
                }

                public XElement Render(Context context, XElement source) => source;
            }

            [WhitespaceSeparated]
            private class HookSuffix : Changer
            {
                [CharSet("<(")] char hidden;
                [CharRange("az", "AZ", "09", "__")] string name;
                [Literal("|")] Unnamed _;

                public void Apply(ref bool hidden, ref string name)
                {
                    if (name != null) {
                        throw new Exception("Hook already named");
                    }
                    name = this.name;
                    if (this.hidden == ')') {
                        hidden = true;
                    }
                }

                public XElement Render(Context context, XElement source) => source;
            }

            [SeparatedBy(typeof(HookSeparator)), Repeat(Min = 0)] List<PrefixExpression> changers;
            [Optional] HookPrefix prefix; 
            [Term] Hook hook;
            [Optional] HookSuffix suffix;

            public XNode Render(Context context) => Render(context, false);

            public XNode Render(Context context, bool forceShow)
            {
                var changerObjs = new List<Changer>();
                if (prefix != null) {
                    changerObjs.Add(prefix);
                }
                if (suffix != null) {
                    changerObjs.Add(suffix);
                }
                changerObjs.AddRange(changers.Select(x => (x.Evaluate(context) as Changer) ?? throw new Exception("Hook prefix must be a Changer")));
                string name = null;
                bool hidden = false;
                foreach (var c in changerObjs) {
                    c.Apply(ref hidden, ref name);
                }
                XElement content;
                if (hidden == true && !forceShow) {
                    content = new XElement(XName.Get("hidden"));
                    context.Hidden.Add(content, this);
                } else {
                    content = hook.Render(context);
                    foreach (var c in changerObjs) {

                    }
                }
                if (name != null) {
                    content.SetAttributeValue(XName.Get("name"), name);
                }
                return content;
            }
        }

        public class False : Expression
        {
            [Literal("false")] Unnamed _;

            public object Evaluate(Context context) => false;
        }

        public class True : Expression
        {
            [Literal("true")] Unnamed _;

            public object Evaluate(Context context) => true;
        }

        public abstract class Hook : Renderable
        {
            public abstract XElement Render(Context context);

            XNode Renderable.Render(Context context) => Render(context);
        }

        [SurroundBy("[[", "]]")]
        public class RightLink : Hook
        {
            [Regex(@"(?!<-).)+")] public string Link { get; set; }
            [Literal("<-")] Unnamed _;
            [Optional, Regex(@"((?!]]).)+")] public string Text { get; set; }

            public override XElement Render(Context context)
            {
                throw new NotImplementedException();
            }
        }

        [SurroundBy("[[", "]]")]
        public class LeftLink : Hook
        {
            [Regex(@"(?!->).)+")] public string Text { get; set; }
            [Literal("->")] Unnamed _;
            [Optional, Regex(@"((?!]]).)+")] public string Link { get; set; }

            public override XElement Render(Context context)
            {
                throw new NotImplementedException();
            }
        }

        [SurroundBy("[[", "]]")]
        public class PlainLink : Hook
        {
            [Regex(@"((?!]]).)+")] public string TextLink { get; set; }

            public override XElement Render(Context context)
            {
                var el = new XElement(XName.Get("link"), new XText(TextLink));
                return el;
            }
        }

        [SurroundBy("[", "]")]
        public class ClosedHook : Hook
        {
            [Term] public List<Renderable> Content { get; } = new List<Renderable>();

            public override XElement Render(Context context)
            {
                return new XElement(XName.Get("div"), (object[])Content.Select(x => x.Render(context)).ToArray());
            }
        }

        public class OpenHook : Hook
        {
            [Literal("[==")] Unnamed _;
            [Term] public List<Renderable> Content { get; } = new List<Renderable>();

            public override XElement Render(Context context)
            {
                return new XElement(XName.Get("div"), (object[])Content.Select(x => x.Render(context)).ToArray());
            }
        }

        [SurroundBy("\"")]
        public class QuotedString : Expression
        {
            [Regex(@"[^""]*")] public string Text { get; set; }

            public object Evaluate(Context context) => Text;
        }

        public class PlainText : Renderable
        {
            [Regex(@".[^\(\[\$\_]*")] public string Text { get; set; }

            public XNode Render(Context context) => new XText(Text);
        }

        public interface Command
        {
            void Run(Context context);
        }

        public interface Changer
        {
            void Apply(ref bool hidden, ref string name);
            XElement Render(Context context, XElement source);
        }

        public class Hidden : Changer
        {
            public void Apply(ref bool hidden, ref string name) => hidden = true;
            public XElement Render(Context context, XElement source) => source;
        }

        public class Named : Changer
        {
            public string Name { get; }

            public void Apply(ref bool hidden, ref string name)
            {
                if (name == null) {
                    throw new Exception("Hook is already named");
                }
                name = Name;
            }
            public XElement Render(Context context, XElement source) => source;
        }

        public class Show : Command
        {
            public IEnumerable<XNode> Nodes { get; }
            public void Run(Context context)
            {
                foreach (var node in Nodes) {
                    if (context.Hidden.TryGetValue(node, out var renderable)) {
                        // TODO: Avoid casting here
                        node.AddAfterSelf(((AppliedHook)renderable).Render(context, true));
                        context.Hidden.Remove(node);
                        node.Remove();
                    }
                }
            }
        }

        public class Print : Command
        {
            public object Value { get; }

            public void Run(Context context) => context.Cursor.Add(new XText(Value.ToString()));
        }

        public class Display : Command
        {
            public string Passage { get; }

            public void Run(Context context) => context.Cursor.Add(context.Passages[Passage].Render(context));
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
            public Command Show(IEnumerable<XNode> nodes) => new Show(nodes);
        }
    }

}