using System.Linq;
using System;
using System.Collections.Generic;

namespace Spool.Harlowe
{

    enum Operator
    {
        Add,
        Subtract,
        Multiply,
        Divide,
        Modulo,
        And,
        Or,
    }

    enum TestOperator
    {
        Is,
        Contains,
        Matches,
        Less,
        Greater,
        LessOrEqual,
        GreaterOrEqual
    }

    enum UnaryOp
    {
        Not,
        Minus,
    }

    static class DataExtensions
    {
        public static bool IsAn(this Data data, DataType type)
        {
            return data.GetType() == type.Value;
        }
    }

    interface Data : IComparable<Data>
    {
        Data Member(Data member);
        Data Clone();
        T As<T>();
        Data Operate(Operator op, Data rhs);
        Data Unary(UnaryOp op);
        bool Test(TestOperator op, Data rhs);

    }

    class Number : Data
    {
        public double Value { get; }

        public Number(double value) => Value = value;

        public T As<T>() => (T)(object)Value;

        public Data Clone() => this;

        public Data Member(Data member) => throw new NotSupportedException();

        public Data Operate(Operator op, Data rhs)
        {
            return rhs switch {
                Number num => op switch {
                    Operator.Add => new Number(Value + num.Value),
                    Operator.Subtract => new Number(Value - num.Value),
                    Operator.Multiply => new Number(Value * num.Value),
                    Operator.Divide => new Number(Value / num.Value),
                    _ => throw new NotSupportedException()
                },
                _ => throw new NotSupportedException()
            };
        }

        public bool Test(TestOperator op, Data rhs)
        {
            return rhs switch {
                Number num => op switch {
                    TestOperator.Matches => Value == num.Value,
                    TestOperator.Is => Value == num.Value,
                    TestOperator.Less => Value < num.Value,
                    TestOperator.Greater => Value > num.Value,
                    TestOperator.LessOrEqual => Value <= num.Value,
                    TestOperator.GreaterOrEqual => Value >= num.Value,
                    _ => throw new NotSupportedException()
                },
                _ => throw new NotSupportedException()
            };
        }

        public Data Unary(UnaryOp op)
        {
            return op switch {
                UnaryOp.Minus => new Number(-Value),
                _ => throw new NotSupportedException()
            };
        }

        public int CompareTo(Data other)
        {
            throw new NotImplementedException();
        }
    }

    class Boolean : Data
    {
        public static Boolean Get(bool value) => value ? True : False;
        public static Boolean True { get; } = new Boolean(true);
        public static Boolean False { get; } = new Boolean(false);

        private Boolean(bool value) => Value = value;
        public bool Value { get; }
        public T As<T>() => (T)(object)Value;

        public Data Clone() => this;
        public Data Member(Data member) => throw new NotSupportedException();

        public Data Operate(Operator op, Data rhs)
        {
            return rhs switch {
                Boolean b => op switch {
                    Operator.And => Get(Value && b.Value),
                    Operator.Or => Get(Value && b.Value),
                    _ => throw new NotSupportedException()
                },
                _ => throw new NotSupportedException()
            };
        }

        public Data Unary(UnaryOp op)
        {
            return op switch {
                UnaryOp.Not => Get(!Value),
                _ => throw new NotSupportedException()
            };
        }

        public bool Test(TestOperator op, Data rhs)
        {
            return rhs switch {
                Boolean b => op switch {
                    TestOperator.Is => Value == b.Value,
                    _ => throw new NotSupportedException()
                },
                _ => throw new NotSupportedException()
            };
        }

        public int CompareTo(Data other)
        {
            throw new NotImplementedException();
        }
    }

    class String : Data
    {
        public String(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public T As<T>() => (T)(object)Value;

        public Data Clone() => this;

        public int CompareTo(Data other)
        {
            throw new NotImplementedException();
        }

        public Data Member(Data member)
        {
            return member switch {
                String str => str.Value switch {
                    "last" => new String(Value[Value.Length - 1].ToString()),
                    "length" => new Number(Value.Length),
                    "all" => new AllOf(this),
                    "any" => new AnyOf(this),
                    _ => throw new NotSupportedException()
                },
                Number num => new String(Value[(int)num.Value].ToString()),
                Array arr => null,
                _ => throw new NotSupportedException()
            };
        }

        public Data Operate(Operator op, Data rhs)
        {
            return rhs switch {
                String str => op switch {
                    Operator.Add => new String(Value + str.Value),
                    _ => throw new NotSupportedException()
                },
                _ => throw new NotSupportedException()
            };
        }

        public bool Test(TestOperator op, Data rhs)
        {
            return rhs switch {
                String str => op switch {
                    TestOperator.Is => Value == str.Value,
                    _ => throw new NotSupportedException()
                },
                _ => throw new NotSupportedException()
            };
        }

        public Data Unary(UnaryOp op) => throw new NotSupportedException();

        private abstract class Checker : Data
        {
            protected Checker(String parent) => this.parent = parent;
            private readonly String parent;
            public T As<T>() => throw new NotSupportedException();
            public Data Clone() => this;
            public Data Member(Data member) => throw new NotSupportedException();
            public Data Operate(Operator op, Data rhs) => throw new NotSupportedException();
            public Data Unary(UnaryOp op) => throw new NotSupportedException();

            public bool Test(TestOperator op, Data rhs)
            {
                if (rhs is String str && op == TestOperator.Is) {
                    if (str.Value.Length != 1) {
                        throw new NotSupportedException("Must be compared with a single character");
                    }
                    return Check(parent.Value, str.Value[0]);
                }
                throw new NotSupportedException();
            }

            protected abstract bool Check(string value, char c);
        }

        private class AllOf : Checker
        {
            public AllOf(String parent) : base(parent) {}

            protected override bool Check(string value, char c) => value.All(x => x == c);
        }

        private class AnyOf : Checker
        {
            public AnyOf(String parent) : base(parent) {}

            protected override bool Check(string value, char c) => value.Contains(c.ToString());
        }
    }

    class DataType : Data
    {
        public Type Value { get; }
        public T As<T>() => (T)(object)Value;

        public Data Clone() => this;

        public int CompareTo(Data other)
        {
            throw new NotImplementedException();
        }

        public Data Member(Data member) => throw new NotSupportedException();

        public Data Operate(Operator op, Data rhs) => throw new NotSupportedException();

        public bool Test(TestOperator op, Data rhs)
        {
            return rhs switch {
                DataType b => op switch {
                    TestOperator.Is => Value == b.Value,
                    _ => throw new NotSupportedException()
                },
                _ => throw new NotSupportedException()
            };
        }

        public Data Unary(UnaryOp op) => throw new NotSupportedException();
    }

    class DataSet : Data
    {
        public DataSet(HashSet<object> value)
        {
            Value = value;
        }

        public HashSet<object> Value { get; }

        public T As<T>() => (T)(object)Value;

        public Data Clone() => new DataSet(new HashSet<object>(Value));

        public int CompareTo(Data other)
        {
            throw new NotImplementedException();
        }

        public Data Member(Data member)
        {
            return member switch {
                String str => str.Value switch {
                    "length" => new Number(Value.Count),
                    _ => throw new NotSupportedException()
                },
                Number num => new String(Value[(int)num.Value].ToString()),
                Array arr => null,
                _ => throw new NotSupportedException()
            };
        }

        public Data Operate(Operator op, Data rhs)
        {
            throw new NotImplementedException();
        }

        public bool Test(TestOperator op, Data rhs)
        {
            throw new NotImplementedException();
        }

        public Data Unary(UnaryOp op)
        {
            throw new NotImplementedException();
        }
    }


    class DataMap : Data
    {
        public T As<T>()
        {
            throw new NotImplementedException();
        }

        public Data Clone()
        {
            throw new NotImplementedException();
        }

        public int CompareTo(Data other)
        {
            throw new NotImplementedException();
        }

        public Data Index(int index)
        {
            throw new NotImplementedException();
        }

        public Data Member(string name)
        {
            throw new NotImplementedException();
        }

        public Data Member(Data member)
        {
            throw new NotImplementedException();
        }

        public Data Operate(Operator op, Data rhs)
        {
            throw new NotImplementedException();
        }

        public bool Test(TestOperator op, Data rhs)
        {
            throw new NotImplementedException();
        }

        public Data Unary(UnaryOp op)
        {
            throw new NotImplementedException();
        }
    }
}
