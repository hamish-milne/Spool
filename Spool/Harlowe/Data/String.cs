using System;
using System.Linq;

namespace Spool.Harlowe
{
    class String : RenderableData
    {
        public static DataType Type { get; } = new DataType(typeof(String));
        public override bool Serializable => true;
        public String(string value)
        {
            Value = value;
        }

        public string Value { get; }
        protected override object GetObject() => Value;

        public override bool Equals(Data other) => other is String str && Value == str.Value;

        public override Data Member(Data member)
        {
            return member switch {
                String str => str.Value switch {
                    "last" => new String(Value[Value.Length - 1].ToString()),
                    "length" => new Number(Value.Length),
                    "all" => new Checker(Check((s, c) => s.All(x => x == c))),
                    "any" => new Checker(Check((s, c) => s.Any(x => x == c))),
                    _ => base.Member(member)
                },
                Number num => new String(Value[(int)num.Value - 1].ToString()),
                Array selector => new String(new string(
                    selector.Select(x => Value[(int)(x as Number ?? 
                        throw new NotSupportedException("Selector must only contain numbers")
                    ).Value]).ToArray())
                ),
                _ => base.Member(member)
            };
        }

        public override Data Operate(Operator op, Data rhs)
        {
            return rhs switch {
                String str => op switch {
                    Operator.Add => new String(Value + str.Value),
                    _ => base.Operate(op, rhs)
                },
                _ => base.Operate(op, rhs)
            };
        }

        public override bool Test(TestOperator op, Data rhs)
        {
            if (op == TestOperator.Contains && rhs is String str) {
                return Value.Contains(str.Value);
            }
            return base.Test(op, rhs);
        }

        private Func<Data, bool> Check(Func<string, char, bool> checker)
        {
            return rhs => {
                if (rhs is String str) {
                    if (str.Value.Length != 1) {
                        throw new NotSupportedException("Must be compared with a single character");
                    }
                    return checker(Value, str.Value[0]);
                }
                throw new NotSupportedException();
            };
        }
    }
}