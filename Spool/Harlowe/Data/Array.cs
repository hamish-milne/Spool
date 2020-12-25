using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Spool.Harlowe
{
    class Array : RenderableData, IList<Data>
    {
        public override bool Serializable => true;
        public Array(IEnumerable<Data> value) => this.value = value.ToArray();
        protected override object GetObject() => this;

        protected override string GetString() => string.Join(",", this);

        private readonly Data[] value;

        public int Count => value.Length;

        bool ICollection<Data>.IsReadOnly => true;

        public Data this[int index]
        {
            get => value[index];
            set => throw new NotSupportedException();
        }

        public override bool Equals(Data other) => other is Array a && value.SequenceEqual(a.value);

        public IEnumerator<Data> GetEnumerator() => ((IEnumerable<Data>)value).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        class Index : Mutator
        {
            private readonly Array parent;
            private readonly int idx;

            public Index(Array parent, int idx)
            {
                this.parent = parent;
                this.idx = idx;
            }

            public Data Delete() => new Array(parent.value.Where((_, i) => i != idx));
            public Data Set(Data value) => new Array(parent.value.Select((o, i) => i == idx ? value : o));
        }

        public override Mutator MutableMember(Data member)
        {
            return member switch {
                Number idx => idx.Value > value.Length ? throw new IndexOutOfRangeException() : new Index(this, (int)idx.Value - 1),
                _ => base.MutableMember(member)
            };
        }

        public override Data Operate(Operator op, Data rhs)
        {
            return rhs switch {
                Array a => op switch {
                    Operator.Add => new Array(value.Concat(a.value)),
                    _ => base.Operate(op, rhs)
                },
                _ => base.Operate(op, rhs)
            };
        }

        public override IEnumerable<Data> Spread() => value;

        public override bool Test(TestOperator op, Data rhs)
        {
            return op switch {
                TestOperator.Contains => Contains(rhs),
                _ => base.Test(op, rhs)
            };
        }

        public int IndexOf(Data item) => ((IList<Data>)value).IndexOf(item);
        void IList<Data>.Insert(int index, Data item) => throw new NotSupportedException();
        void IList<Data>.RemoveAt(int index) => throw new NotSupportedException();
        void ICollection<Data>.Add(Data item) => throw new NotSupportedException();
        void ICollection<Data>.Clear() => throw new NotSupportedException();
        public bool Contains(Data item) => ((IList<Data>)value).Contains(item);
        public void CopyTo(Data[] array, int arrayIndex) => value.CopyTo(array, arrayIndex);
        bool ICollection<Data>.Remove(Data item) => throw new NotSupportedException();

        public override Data Member(Data member)
        {
            return member switch {
                Number idx => value[(int)idx.Value - 1],
                String str => str.Value switch {
                    "length" => new Number(value.Length),
                    "last" => value[value.Length - 1],
                    "all" => Checker.All(this),
                    "any" => Checker.Any(this),
                    _ => base.Member(member)
                },
                Array selector => new Array(
                    selector.Select(x => value[(int)(x as Number ?? 
                        throw new NotSupportedException("Selector must only contain numbers")
                    ).Value])
                ),
                _ => base.Member(member)
            };
        }
    }
}