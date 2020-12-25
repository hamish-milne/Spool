using System.Collections.Generic;

namespace Spool.Harlowe
{
    class String : Collection
    {
        public static DataType Type { get; } = new DataType(typeof(String));
        public override bool Serializable => true;
        public String(string value) => Value = value;
        public string Value { get; }
        public override int Count => Value.Length;
        protected override bool SupportsIndexing => true;
        protected override object GetObject() => Value;
        public override bool Equals(Data other) => other is String str && Value == str.Value;
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
        protected override Data GetIndex(int index) => new String(Value[index].ToString());
        protected override Data Create(IEnumerable<Data> values) => new String(string.Join("", values));
    }
}