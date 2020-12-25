using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Spool.Harlowe
{
    abstract class Collection : RenderableData, IEnumerable<Data>
    {
        public abstract int Count { get; }
        public IEnumerator<Data> GetEnumerator() {
            for (int i = 0; i < Count; i++) {
                yield return GetIndex(i);
            }
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        protected abstract bool SupportsIndexing { get; }
        protected abstract Data GetIndex(int index);

        private Data CheckIndexing(Data obj) => SupportsIndexing ? obj : throw new NotSupportedException(); 

        protected abstract Data Create(IEnumerable<Data> values);

        public override Data Member(Data member)
        {
            return member switch {
                Number idx => CheckIndexing(GetIndex((int)idx.Value - 1)),
                String str => str.Value switch {
                    "length" => new Number(Count),
                    "last" => CheckIndexing(GetIndex(Count - 1)),
                    "all" => new Checker(this, x => !x.Contains(false)),
                    "any" => new Checker(this, x => x.Contains(true)),
                    _ => base.Member(member)
                },
                Array selector => SupportsIndexing ? Create(
                    selector.Select(x => GetIndex((int)(x as Number ?? 
                        throw new NotSupportedException("Selector must only contain numbers")
                    ).Value - 1))
                ) : throw new NotSupportedException(),
                _ => base.Member(member)
            };
        }
    }

    class Checker : Data
    {
        private readonly Collection testValues;
        private readonly Func<IEnumerable<bool>, bool> aggregator;

        public override bool Serializable => throw new NotImplementedException();

        public Checker(Collection testValues, Func<IEnumerable<bool>, bool> aggregator)
        {
            this.testValues = testValues;
            this.aggregator = aggregator;
        }

        public bool TestSwapped(TestOperator op, Data lhs) =>
            aggregator(testValues.Select(x => lhs.Test(op, x)));

        public override bool Test(TestOperator op, Data rhs) =>
            aggregator(testValues.Select(x => x.Test(op, rhs)));

        protected override string GetString() => "an 'any' or 'all' expression";
        protected override object GetObject() => this;
        public override bool Equals(Data other) => false;
    }

    abstract class DataCollection : Collection, ICollection<Data>
    {
        public override bool Serializable => true;
        public DataCollection(IEnumerable<Data> value) => this.value = value.ToArray();
        protected override object GetObject() => this;
        protected override string GetString() => string.Join(",", this);

        private readonly Data[] value;

        public override int Count => value.Length;
        protected override Data GetIndex(int index) => value[index];
        protected int IndexOf(Data item) => ((IList<Data>)value).IndexOf(item);

        bool ICollection<Data>.IsReadOnly => true;

        public override int GetHashCode() => this.Aggregate(
            GetType().GetHashCode(),
            (x, data) => (x * 51) + data.GetHashCode()
        );

        public override bool Equals(Data other) => other is DataCollection a && value.SequenceEqual(a.value);

        public override Data Operate(Operator op, Data rhs)
        {
            return rhs switch {
                DataCollection a => op switch {
                    Operator.Add => Create(value.Concat(a.value)),
                    _ => base.Operate(op, rhs)
                },
                _ => base.Operate(op, rhs)
            };
        }

        public override IEnumerable<Data> Spread() => value;

        public override bool Test(TestOperator op, Data rhs)
        {
            if (rhs is Checker) {
                return base.Test(op, rhs);
            }
            return op switch {
                TestOperator.Contains => Contains(rhs),
                TestOperator.Matches => rhs is DataCollection other
                    && other.Count == Count
                    && this.Zip(other, (a,b) => a.Test(op, b)).All(x => x),
                _ => base.Test(op, rhs)
            };
        }

        void ICollection<Data>.Add(Data item) => throw new NotSupportedException();
        void ICollection<Data>.Clear() => throw new NotSupportedException();
        public bool Contains(Data item) => ((IList<Data>)value).Contains(item);
        public void CopyTo(Data[] array, int arrayIndex) => value.CopyTo(array, arrayIndex);
        bool ICollection<Data>.Remove(Data item) => throw new NotSupportedException();
    }
}