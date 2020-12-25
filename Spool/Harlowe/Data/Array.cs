using System;
using System.Collections.Generic;
using System.Linq;

namespace Spool.Harlowe
{
    class Array : DataCollection, IList<Data>
    {
        public static DataType Type { get; } = new DataType(typeof(Array));
        public Array(IEnumerable<Data> values) : base(values) {}

        public Data this[int index]
        {
            get => GetIndex(index);
            set => throw new NotSupportedException();
        }

        protected override bool SupportsIndexing => true;
        public override bool Equals(Data other) => other is Array a && base.Equals(other);

        class Index : Mutator
        {
            private readonly Array parent;
            private readonly int idx;

            public Index(Array parent, int idx)
            {
                this.parent = parent;
                this.idx = idx;
            }

            public Data Delete() => new Array(parent.Where((_, i) => i != idx));
            public Data Set(Data value) => new Array(parent.Select((o, i) => i == idx ? value : o));
        }

        public override Mutator MutableMember(Data member)
        {
            return member switch {
                Number idx => idx.Value > Count ? throw new IndexOutOfRangeException() : new Index(this, (int)idx.Value - 1),
                _ => base.MutableMember(member)
            };
        }

        protected override Data Create(IEnumerable<Data> values) => new Array(values);
        new public int IndexOf(Data item) => base.IndexOf(item);
        void IList<Data>.Insert(int index, Data item) => throw new NotSupportedException();
        void IList<Data>.RemoveAt(int index) => throw new NotSupportedException();
    }
}