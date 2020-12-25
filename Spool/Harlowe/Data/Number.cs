using System;

namespace Spool.Harlowe
{
    class Number : RenderableData
    {
        public static DataType Type { get; } = new DataType(typeof(Number));
        public override bool Serializable => true;
        public double Value { get; }
        protected override object GetObject() => Value;

        public Number(double value) => Value = value;

        public override Data Operate(Operator op, Data rhs)
        {
            return rhs switch {
                Number num => op switch {
                    Operator.Add => new Number(Value + num.Value),
                    Operator.Subtract => new Number(Value - num.Value),
                    Operator.Multiply => new Number(Value * num.Value),
                    Operator.Divide => new Number(Value / num.Value),
                    Operator.Modulo => new Number(Value % num.Value),
                    _ => throw new NotSupportedException()
                },
                _ => base.Operate(op, rhs)
            };
        }

        public override bool Test(TestOperator op, Data rhs)
        {
            return rhs switch {
                Number num => op switch {
                    TestOperator.Matches => Value == num.Value,
                    TestOperator.Less => Value < num.Value,
                    TestOperator.Greater => Value > num.Value,
                    TestOperator.LessOrEqual => Value <= num.Value,
                    TestOperator.GreaterOrEqual => Value >= num.Value,
                    _ => base.Test(op, rhs)
                },
                _ => base.Test(op, rhs)
            };
        }

        public override Data Unary(UnaryOp op)
        {
            return op switch {
                UnaryOp.Minus => new Number(-Value),
                _ => base.Unary(op)
            };
        }

        public override bool Equals(Data other) => other is Number num && num.Value == Value;
    }
}