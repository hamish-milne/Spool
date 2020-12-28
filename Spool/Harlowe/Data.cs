using System;
using System.Collections.Generic;

namespace Spool.Harlowe
{

    public enum Operator
    {
        Add,
        Subtract,
        Multiply,
        Divide,
        Modulo,
        And,
        Or,
    }

    public enum TestOperator
    {
        Is,
        Contains,
        Matches,
        Less,
        Greater,
        LessOrEqual,
        GreaterOrEqual,
        IsOfType,
    }

    public enum UnaryOp
    {
        Not,
        Minus,
    }

    public interface Mutator
    {
        Data Set(Data value);
        Data Delete();
    }

    public abstract class Data : IComparable<Data>, IEquatable<Data>
    {
        public virtual Data Member(Data member) => throw new NotSupportedException();
        public virtual Data Operate(Operator op, Data rhs) => throw new NotSupportedException();
        public virtual Data Unary(UnaryOp op) => throw new NotSupportedException();
        public virtual bool Test(TestOperator op, Data rhs)
        {
            if (rhs is Checker chk) {
                return chk.TestSwapped(op, this);
            }
            return op switch {
                TestOperator.Is => Equals(rhs),
                TestOperator.Matches => Equals(rhs) || (rhs is DataType && rhs.Test(TestOperator.IsOfType, this)),
                _ => throw new NotSupportedException()
            };
        }
        public virtual Mutator MutableMember(Data member) => throw new NotSupportedException();
        public virtual IEnumerable<Data> Spread() => throw new NotSupportedException();
        private object cachedObject;
        public object Object => cachedObject ??= GetObject();
        protected abstract object GetObject();

        private static readonly IComparer<string> comparer = new Util.AlphanumComparator();

        public int CompareTo(Data other) => comparer.Compare(ToString(), other.ToString());

        private string cachedString;
        protected virtual string GetString() => Object == this ? throw new NotImplementedException("Please override GetString") : Object.ToString();

        public virtual bool Equals(Data other) => Object.Equals(other.Object);
        public override sealed bool Equals(object obj) => obj is Data d && Equals(d);
        public override int GetHashCode() => ToString().GetHashCode();
        public override sealed string ToString() => cachedString ??= GetString();

        public abstract bool Serializable { get; }
    }

    abstract class RenderableData : Renderable
    {
        protected override string GetString() => Object.ToString();
        public override void Render(Context context) => context.Cursor.WriteText(ToString());
    }

    class DataType : Data
    {
        public DataType(Type value) => Value = value;
        public override bool Serializable => true;
        public Type Value { get; }
        protected override object GetObject() => Value;
        protected override string GetString() => $"the {Value.Name.ToString().ToLowerInvariant()} datatype";
        public override bool Test(TestOperator op, Data rhs)
        {
            return op switch {
                TestOperator.Matches => Value.IsInstanceOfType(rhs),
                TestOperator.IsOfType => Value.IsInstanceOfType(rhs),
                _ => base.Test(op, rhs)
            };
        }
    }

    class LambdaData : Data
    {
        public override bool Serializable => true;
        public LambdaData(Filter value) => Value = value;
        public object Value { get; }
        protected override object GetObject() => Value;
        protected override string GetString() => $"A lambda";
    }

    class VariableToValue : Data
    {
        public override bool Serializable => false;
        public VariableToValue(Mutable variable, Data value, bool usesIntoKeyword, Mutable toRemove)
        {
            Variable = variable;
            Value = value;
            UsesIntoKeyword = usesIntoKeyword;
            ToRemove = toRemove;
        }
        public Mutable Variable { get; }
        public Mutable ToRemove { get; }
        public Data Value { get; }
        public bool UsesIntoKeyword { get; }

        protected override object GetObject() => new object();
        protected override string GetString() => "a 'to' or 'into' expression";
    }

}
