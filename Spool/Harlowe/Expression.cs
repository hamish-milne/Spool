using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lexico;

namespace Spool.Harlowe
{

    public interface Expression
    {
        Data Evaluate(Context context);
    }

    public interface Mutable : Expression
    {
        void Set(Context context, Data value);
        void Delete(Context context);
    }

    delegate bool Filter(Data value);

    public static class Expressions
    {
        // Marker interface for expressions that can be a part of operators etc.
        // This makes sure the parser doesn't try to parse `each $foo + 1` as `(each $foo) + 1` etc.
        interface SubExpression : Expression {}
        interface TopExpression : Expression {}

        public static Expression Parse(string input, ITrace trace = null)
            => Lexico.Lexico.Parse<TopLevelExpression>(input, trace);


        [TopLevel, CompileFlags(CompileFlags.CheckImmediateLeftRecursion | CompileFlags.AggressiveMemoizing)]   
        class TopLevelExpression : Expression
        {
            [Term] private TopExpression expression;

            public Data Evaluate(Context context)
            {
                return expression.Evaluate(context);
            }
        }

        [WhitespaceSeparated]
        class ToOrInto : TopExpression
        {
            [Term] OperatorSequence LHS;
            [Regex("to|into")] string separator;
            [Term] OperatorSequence RHS;

            public Data Evaluate(Context context) {
                var swap = separator == "into";
                var Variable = swap ? RHS : LHS;
                var Value = swap ? LHS : RHS;
                var m = Variable.Build(context) as Mutable;
                if (m == null) {
                    throw new Exception("Value not mutable");
                }
                var v = Value.Evaluate(context);
                return new VariableToValue(m, Value.Evaluate(context), swap, Value.Build(context) as Mutable);
            }
        }

        abstract class OperatorExpr
        {            
            protected abstract string OpString { get; }
            [IndirectLiteral(nameof(OpString))] protected Unnamed _;

            protected class OperatorExpression : Expression
            {
                public OperatorExpression(Func<Context, Data> eval)
                {
                    this.eval = eval;
                }
                private readonly Func<Context, Data> eval;
                public Data Evaluate(Context context) => eval(context);
            }
        }

        abstract class BinaryOperator : OperatorExpr
        {
            public abstract int Order { get; }
            protected virtual bool Swap => false;
            public abstract Expression Build(Expression lhs, Expression rhs);
        }

        abstract class NormalBinaryOperator : BinaryOperator
        {
            public abstract Operator OpCode { get; }
            public override Expression Build(Expression lhs, Expression rhs)
                => new OperatorExpression(c =>
                    (Swap ? rhs : lhs).Evaluate(c).Operate(OpCode, (Swap ? lhs : rhs).Evaluate(c)));
        }

        abstract class TestOperatorExpr : BinaryOperator
        {
            protected virtual bool Invert => false;
            public abstract TestOperator OpCode { get; }
            public override Expression Build(Expression lhs, Expression rhs)
                => new OperatorExpression(c => Boolean.Get(Invert ^
                    (Swap ? rhs : lhs).Evaluate(c).Test(OpCode, (Swap ? lhs : rhs).Evaluate(c))));
        }

        abstract class UnaryOperator : OperatorExpr
        {
            public abstract UnaryOp OpCode { get; }
            public virtual Expression Build(Expression rhs)
                => new OperatorExpression(c => rhs.Evaluate(c).Unary(OpCode));
        }

        [WhitespaceSeparated]
        class OperatorSequence : TopExpression
        {
            [WhitespaceSeparated]
            struct Item
            {
                [Term] public BinaryOperator binary;
                [Optional] public UnaryOperator unary;
                [Term] public SubExpression expression;

                public IEnumerable<object> Tokens() => new object[]{binary, unary, expression};
            }
            
            [Optional] private UnaryOperator unaryFirst;
            [Term] private SubExpression expressionFirst;
            [Repeat(Min = 0), WhitespaceSeparated]
            private readonly List<Item> tokens = new List<Item>();

            private Expression cachedExpr;

            public Expression Build(Context context)
            {
                if (cachedExpr != null) {
                    return cachedExpr;
                }
                var list = new object[]{unaryFirst, expressionFirst}
                    .Concat(tokens.SelectMany(x => x.Tokens()))
                    .Where(x => x != null)
                    .ToList();
                if (expressionFirst is Variable) {
                    context.It = expressionFirst;
                }
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] is It) {
                        var prev = list.Take(i).OfType<Variable>().LastOrDefault() ?? context.It;
                        if (prev == null) {
                            throw new Exception("'it' used as leftmost expression");
                        }
                        list[i] = prev;
                        context.It = prev;
                    }
                }
                foreach (var u in list.OfType<UnaryOperator>().Reverse().ToList()) {
                    var idx = list.IndexOf(u);
                    var operand = (idx+1) < list.Count ? (list[idx+1] as Expression) : null;
                    if (operand == null) {
                        throw new Exception($"Expected an expression after unary {u}");
                    }
                    list.RemoveAt(idx);
                    list[idx] = u.Build(operand);
                }
                foreach (var b in list.OfType<BinaryOperator>().OrderBy(x => x.Order).ToList()) {
                    var idx = list.IndexOf(b);
                    var lhs = (idx-1) >= 0 ? (list[idx-1] as Expression) : null;
                    var rhs = (idx+1) < list.Count ? (list[idx+1] as Expression) : null;
                    if (lhs == null) {
                        throw new Exception($"Expected an expression before binary {b}");
                    }
                    if (rhs == null) {
                        throw new Exception($"Expected an expression after binary {b}");
                    }
                    list.RemoveAt(idx-1);
                    list.RemoveAt(idx-1);
                    list[idx-1] = b.Build(lhs, rhs);
                }
                if (list.Count != 1) {
                    throw new Exception($"Expected an operator between {list[0]} and {list[1]}");
                }
                cachedExpr = (Expression)list[0];
                return cachedExpr;
            }

            public Data Evaluate(Context context)
            {
                return Build(context).Evaluate(context);
            }
        }

        class LessOrEqual : TestOperatorExpr
        {
            public override int Order => 60;
            public override TestOperator OpCode => TestOperator.LessOrEqual;
            protected override string OpString => "<=";
        }
        class GreaterOrEqual : TestOperatorExpr
        {
            public override int Order => 60;
            public override TestOperator OpCode => TestOperator.GreaterOrEqual;
            protected override string OpString => ">=";
        }
        class Less : TestOperatorExpr
        {
            public override int Order => 60;
            public override TestOperator OpCode => TestOperator.Less;
            protected override string OpString => "<";
        }
        class Greater : TestOperatorExpr
        {
            public override int Order => 60;
            public override TestOperator OpCode => TestOperator.Greater;
            protected override string OpString => ">";
        }
        class IsIn : TestOperatorExpr
        {
            public override int Order => 70;
            public override TestOperator OpCode => TestOperator.Contains;
            protected override bool Swap => true;
            protected override string OpString => "is in";
        }
        class Contains : TestOperatorExpr
        {
            public override int Order => 70;
            public override TestOperator OpCode => TestOperator.Contains;
            protected override string OpString => "contains";
        }
        class Is : TestOperatorExpr
        {
            public override int Order => 80;
            public override TestOperator OpCode => TestOperator.Is;
            protected override string OpString => "is";
        }
        class IsNot : TestOperatorExpr
        {
            public override int Order => 80;
            public override TestOperator OpCode => TestOperator.Is;
            protected override bool Invert => true;
            protected override string OpString => "is not";
        }
        class Matches : TestOperatorExpr
        {
            public override int Order => 80;
            public override TestOperator OpCode => TestOperator.Matches;
            protected override string OpString => "matches";
        }
        class Or : NormalBinaryOperator
        {
            public override int Order => 90;
            public override Operator OpCode => Operator.Or;
            protected override string OpString => "or";
        }
        class And : NormalBinaryOperator
        {
            public override int Order => 90;
            public override Operator OpCode => Operator.And;
            protected override string OpString => "and";
        }
        class Add : NormalBinaryOperator
        {
            public override int Order => 40;
            public override Operator OpCode => Operator.Add;
            protected override string OpString => "+";
        }
        class Mul : NormalBinaryOperator
        {
            public override int Order => 30;
            public override Operator OpCode => Operator.Multiply;
            protected override string OpString => "*";
        }
        class Sub : NormalBinaryOperator
        {
            public override int Order => 50;
            public override Operator OpCode => Operator.Subtract;
            protected override string OpString => "-";
        }
        class Div : NormalBinaryOperator
        {
            public override int Order => 20;
            public override Operator OpCode => Operator.Divide;
            protected override string OpString => "/";
        }
        class Modulo : NormalBinaryOperator
        {
            public override int Order => 20;
            public override Operator OpCode => Operator.Modulo;
            protected override string OpString => "%";
        }
        abstract class MemberAccess : BinaryOperator
        {
            class MemberAccessExpression : Mutable
            {
                public MemberAccessExpression(Mutable lhs, Expression rhs)
                {
                    this.lhs = lhs;
                    this.rhs = rhs;
                }
                private readonly Mutable lhs;
                private readonly Expression rhs;

                public void Delete(Context context) =>
                    lhs.Set(
                        context,
                        lhs.Evaluate(context).MutableMember(rhs.Evaluate(context)).Delete()
                    );

                public void Set(Context context, Data value) =>
                    lhs.Set(
                        context,
                        lhs.Evaluate(context).MutableMember(rhs.Evaluate(context)).Set(value)
                    );

                public Data Evaluate(Context context) =>
                    lhs.Evaluate(context).Member(rhs.Evaluate(context));
            }

            public override Expression Build(Expression lhs, Expression rhs)
            {
                var _lhs = Swap ? rhs : lhs;
                var _rhs = Swap ? lhs : rhs;
                var name = _rhs is Identifier str ? new OperatorExpression(_ => new String(str.Text)) : _rhs;
                if (_lhs is Mutable m) {
                    return new MemberAccessExpression(m, _rhs);
                }
                return new OperatorExpression(c => _lhs.Evaluate(c).Member(name.Evaluate(c)));
            }
        }
        // TODO: Left-associativity
        class Of : MemberAccess
        {
            public override int Order => 10;
            protected override string OpString => "of";
            protected override bool Swap => true;
        }
        class Posessive : MemberAccess
        {
            public override int Order => 0;
            protected override string OpString => "'s";
        }

        class Where : BinaryOperator
        {
            public override int Order => 100;
            protected override string OpString => "where";

            public override Expression Build(Expression lhs, Expression rhs)
            {
                if (lhs is Variable variable) {
                    return new OperatorExpression(context =>
                        new LambdaData(value => {
                            variable.Set(context, value);
                            return ((Boolean)rhs.Evaluate(context)).Value;
                        })
                    );
                } else {
                    throw new Exception("'where' must have a variable on the left");
                }
            }
        }

        // TODO: Unary operator ordering

        class Each : UnaryOperator
        {
            protected override string OpString => "each";
            public override UnaryOp OpCode => throw new NotSupportedException();
            public override Expression Build(Expression rhs)
            {
                if (rhs is Variable variable) {
                    return new OperatorExpression(context => new LambdaData(value => {
                        variable.Set(context, value);
                        return true;
                    }));
                } else {
                    throw new Exception("'each' must be applied to a Variable");
                }
            }
        }

        class Not : UnaryOperator
        {
            protected override string OpString => "not";
            public override UnaryOp OpCode => UnaryOp.Not;
        }

        class Minus : UnaryOperator
        {
            protected override string OpString => "-";
            public override UnaryOp OpCode => UnaryOp.Minus;
        }

        [WhitespaceSeparated, SurroundBy("(", ")")]
        class Parenthesized : SubExpression
        {
            [Term] TopExpression inner;

            public Data Evaluate(Context context) => inner.Evaluate(context);
        }

        public abstract class ObjectExpression : Renderable, SubExpression
        {
            public abstract Data Evaluate(Context context);

            public override void Render(Context context)
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
                    context.Cursor.WriteText(value.ToString());
                }
            }
        }

        public class Variable : ObjectExpression, Mutable
        {
            [CharSet("$_")] private char variableType { get => Global ? '$' : '_'; set => Global = value == '$'; }
            public bool Global { get; set; }
            [Regex(@"[a-zA-Z_][a-zA-Z0-9_]*")] public string Name { get; set; }

            public override Data Evaluate(Context context) => (Global ? context.Globals : context.Locals)[Name];

            public void Set(Context context, Data value) => (Global ? context.Globals : context.Locals)[Name] = value;

            public void Delete(Context context) => (Global ? context.Globals : context.Locals).Remove(Name);
        }

        class Integer : SubExpression
        {
            [CharRange("09"), Repeat] string number;
            [Regex("st|nd|rd|th")] Unnamed _;

            public Data Evaluate(Context context) => new Number(int.Parse(number));
        }

        class Float : SubExpression
        {
            [Term] double number;

            public Data Evaluate(Context context) => new Number(number);
        }

        class HookRef : SubExpression
        {
            [Literal("?")] Unnamed _;
            [CharRange("az", "AZ", "09", "__"), Repeat] public string Name;

            // TODO: Change these depending on the renderer
            // private static readonly Dictionary<string, XName> BuiltInNames = new Dictionary<string, XName>(StringComparer.OrdinalIgnoreCase)
            // {
            //     { "Page", XName.Get("tw-story") },
            //     { "Passage", XName.Get("tw-passage") },
            //     { "Sidebar", XName.Get("tw-sidebar") },
            //     { "Link", XName.Get("link") }
            // };

            public Data Evaluate(Context context)
            {
                // BuiltInNames.TryGetValue(Name, out var builtInName);
                return new SimpleHookName(Name);
            }
        }

        [WhitespaceSeparated, SurroundBy("(", ")")]
        public class Macro : ObjectExpression
        {
            struct Argument
            {
                [Optional, Literal("...")] public string spread;
                [Term] public TopExpression value;
            }

            [CharRange("az", "AZ", "09", "__", "--"), Repeat, Suffix(":"), Cut]
            private string Name;

            [SeparatedBy(typeof(Comma)), Repeat(Min = 0)]
            private readonly List<Argument> Arguments = new List<Argument>();
            
            [Optional] private Comma _; // TODO: Replace with TrailingSeparator when available

            [WhitespaceSurrounded]
            struct Comma {
                [Literal(",")] Unnamed _;
            }

            public override Data Evaluate(Context context)
            {
                var normalizedName = Name.Replace("-", "").Replace("_", "");
                IEnumerable<Data> GetArgList()
                {
                    foreach (var a in Arguments)
                    {
                        if (a.spread == null) {
                            yield return a.value.Evaluate(context);
                        } else {
                            var toSpread = a.value.Evaluate(context);
                            foreach (var e in toSpread.Spread()) {
                                yield return e;
                            }
                        }
                    }
                }
                var args = GetArgList().ToArray();
                foreach (var m in context.MacroProvider.GetType().GetMethods()
                    .Where(x => x.Name.Equals(normalizedName, StringComparison.OrdinalIgnoreCase))
                )
                {
                    try {
                        var parameters = m.GetParameters();
                        Type varargType = null;
                        if (parameters.Length > 0 && parameters.Last().GetCustomAttribute<ParamArrayAttribute>() != null)
                        {
                            varargType = parameters.Last().ParameterType.GetElementType();
                        }
                        object ConvertArg(int idx)
                        {
                            Type target;
                            if (varargType != null && idx >= parameters.Length - 1) {
                                target = varargType;
                            } else {
                                target = parameters[idx].ParameterType;
                            }
                            if (target.IsInstanceOfType(args[idx])) {
                                return args[idx];
                            }
                            if (target.IsInstanceOfType(args[idx].Object)) {
                                return args[idx].Object;
                            }
                            throw new InvalidCastException("Invalid parameter");
                        }
                        var convertedArgs = Enumerable.Range(0, args.Length).Select(ConvertArg).ToArray();
                        object[] finalArgs;
                        if (varargType != null) {
                            var vargs = System.Array.CreateInstance(varargType, args.Length - (parameters.Length - 1));
                            convertedArgs.Skip(parameters.Length - 1).ToArray().CopyTo(vargs, 0);
                            finalArgs = convertedArgs.Take(parameters.Length - 1).Concat(new []{vargs}).ToArray();
                        } else {
                            finalArgs = convertedArgs;
                        }
                        var ret = m.Invoke(context.MacroProvider, finalArgs);
                        if (ret is Data d) {
                            return d;
                        } else if (ret == null) {
                            return null; // TODO: 'instant' sentinal?
                        } else {
                            throw new TargetInvocationException(
                                new Exception($"Macro {Name} returned {ret}, not a Data subclass")
                            );
                        }
                    } catch (TargetInvocationException) {
                        throw;
                    } catch {
                        continue;
                    }
                }
                throw new Exception($"No macro '{Name}' with the given arguments");
            }
        }

        class It : SubExpression
        {
            [Literal("it")] Unnamed _;

            public Data Evaluate(Context context)
                => throw new InvalidOperationException("Tried to use 'it' outside of a sequence of operations");
        }

        class False : SubExpression
        {
            [Literal("false")] Unnamed _;

            public Data Evaluate(Context context) => Boolean.False;
        }

        class True : SubExpression
        {
            [Literal("true")] Unnamed _;

            public Data Evaluate(Context context) => Boolean.True;
        }

        [SurroundBy("\"")]
        class QuotedString : SubExpression
        {
            [Regex(@"[^""]*")] public string Text { get; set; }

            public Data Evaluate(Context context) => new String(Text);
        }

        [SurroundBy("'")]
        class SingleQuotedString : SubExpression
        {
            [Regex(@"[^']*")] public string Text { get; set; }

            public Data Evaluate(Context context) => new String(Text);
        }

        class Identifier : SubExpression
        {
            [CharRange("az", "AZ", "09", "__", "--"), Repeat] public string Text { get; set; }

            private static readonly Dictionary<string, uint> colors = new Dictionary<string, uint>
            {
                {"red", 0xffe61919},
                {"orange", 0xffe68019},
                {"yellow", 0xffe5e619},
                {"lime", 0xff80e619},
                {"green", 0xff19e619},
                {"aqua", 0xff19e5e6},
                {"cyan", 0xff19e5e6},
                {"blue", 0xff197fe6},
                {"navy", 0xff1919e6},
                {"purple", 0xff7f19e6},
                {"magenta", 0xffe619e5},
                {"fuchsia", 0xffe619e5},
                {"white", 0xffffffff},
                {"black", 0xff000000},
                {"gray", 0xff888888},
                {"grey", 0xff888888},
            };

            public Data Evaluate(Context context)
            {
                unchecked {
                    return Text switch {
                        "visit" => new Number(context.Visits(context.CurrentPassage)),
                        _ => colors.TryGetValue(Text, out var c)
                            ? new Color(System.Drawing.Color.FromArgb((int)c))
                            : throw new Exception($"{Text} not defined")
                    };
                }
            }
        }

    }
}