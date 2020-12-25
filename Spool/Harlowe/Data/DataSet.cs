using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Spool.Harlowe
{

    class DataSet : RenderableData, ICollection<Data>
    {
        public override bool Serializable => true;
        public DataSet(IEnumerable<Data> value)
        {
            this.value = new HashSet<Data>(value);
        }

        private readonly HashSet<Data> value;

        public int Count => throw new NotImplementedException();

        protected override object GetObject() => this;

        public override Data Member(Data member)
        {
            return member switch {
                String str => str.Value switch {
                    "length" => new Number(value.Count),
                    "all" => Checker.All(this),
                    "any" => Checker.Any(this),
                    _ => base.Member(member)
                },
                _ => base.Member(member)
            };
        }

        public override Data Operate(Operator op, Data rhs)
        {
            return rhs switch {
                DataSet ds => op switch {
                    Operator.Add => new DataSet(value.Concat(ds.value)),
                    Operator.Subtract => new DataSet(value.Where(x => !ds.value.Contains(x))),
                    _ => base.Operate(op, rhs)
                },
                _ => base.Operate(op, rhs)
            };
        }

        public override bool Test(TestOperator op, Data rhs)
        {
            return op switch {
                TestOperator.Contains => value.Contains(rhs),
                _ => base.Test(op, rhs)
            };
        }

        public override IEnumerable<Data> Spread() => value.OrderBy(x => x);

        public override bool Equals(Data other) => other is DataSet ds
            && value.Count == ds.value.Count
            && value.All(ds.value.Contains);

        public override int GetHashCode() => value.OrderBy(x => x)
            .Aggregate(typeof(DataSet).GetHashCode(), (x, data) => (x * 51) + data.GetHashCode());

        protected override string GetString() => string.Join(",", value.OrderBy(x => x));

        bool ICollection<Data>.IsReadOnly => true;
        void ICollection<Data>.Add(Data item) => throw new NotSupportedException();
        bool ICollection<Data>.Remove(Data item) => throw new NotSupportedException();
        void ICollection<Data>.Clear() => throw new NotSupportedException();

        public bool Contains(Data item) => value.Contains(item);

        public void CopyTo(Data[] array, int arrayIndex)
        {
            value.CopyTo(array, arrayIndex);
        }

        public IEnumerator<Data> GetEnumerator() => value.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => value.GetEnumerator();
    }

}