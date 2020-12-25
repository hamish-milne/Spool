using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Spool.Harlowe
{

    class DataMap : RenderableData, IDictionary<Data, Data>
    {
        public override bool Serializable => true;
        public DataMap(IEnumerable<KeyValuePair<Data, Data>> pairs)
        {
            foreach (var pair in pairs) {
                map[pair.Key] = pair.Value;
            }
        }
        private readonly Dictionary<Data, Data> map = new Dictionary<Data, Data>();

        public Data this[Data key] {
            get => map[key];
            set => map[key] = value;
        }

        public ICollection<Data> Keys => map.Keys;
        public ICollection<Data> Values => map.Values;
        public int Count => map.Count;

        bool ICollection<KeyValuePair<Data, Data>>.IsReadOnly => true;
        void IDictionary<Data, Data>.Add(Data key, Data value) => throw new NotSupportedException();
        void ICollection<KeyValuePair<Data, Data>>.Add(KeyValuePair<Data, Data> item) => throw new NotSupportedException();
        void ICollection<KeyValuePair<Data, Data>>.Clear() => throw new NotSupportedException();
        bool IDictionary<Data, Data>.Remove(Data key) => throw new NotSupportedException();
        bool ICollection<KeyValuePair<Data, Data>>.Remove(KeyValuePair<Data, Data> item) => throw new NotSupportedException();

        public bool Contains(KeyValuePair<Data, Data> item) => map.Contains(item);
        public bool ContainsKey(Data key) => map.ContainsKey(key);
        public void CopyTo(KeyValuePair<Data, Data>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<Data, Data>>)map).CopyTo(array, arrayIndex);
        }

        public override bool Equals(Data other)
        {
            return other is DataMap dm && dm.Count == Count && map.All(dm.Contains);
        }

        protected override string GetString() => "\n" + string.Join("\n", Keys.OrderBy(x => x).Select(x => $"{x} {this[x]}")) + "\n";

        public IEnumerator<KeyValuePair<Data, Data>> GetEnumerator() => map.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => map.GetEnumerator();

        public override Data Operate(Operator op, Data rhs)
        {
            if (op == Operator.Add && rhs is DataMap dm) {
                return new DataMap(map.Concat(dm.map));
            }
            return base.Operate(op, rhs);
        }

        public override Data Member(Data member)
        {
            if (map.TryGetValue(member, out var value)) {
                return value;
            } else {
                throw new NotSupportedException($"Datamap does not contain a '{value}' key");
            }
        }

        public override Mutator MutableMember(Data member)
        {
            if (map.TryGetValue(member, out var value)) {
                return new Entry(this, member);
            } else {
                throw new NotSupportedException($"Datamap does not contain a '{value}' key");
            }
        }

        public override bool Test(TestOperator op, Data rhs)
        {
            return op switch {
                TestOperator.Contains => ContainsKey(rhs),
                TestOperator.Matches => rhs switch {
                    DataMap pattern => Count == pattern.Count && Keys.All(k => pattern.ContainsKey(k) && pattern[k].Test(op, this[k])),
                    _ => base.Test(op, rhs)
                },
                _ => base.Test(op, rhs)
            };
        }

        class Entry : Mutator
        {
            public Entry(DataMap parent, Data key)
            {
                this.parent = parent;
                this.key = key;
            }
            private readonly DataMap parent;
            private readonly Data key;
            public Data Delete() => new DataMap(parent.map.Where(p => !p.Key.Equals(key)));
            public Data Set(Data value) => new DataMap(parent.map.Concat(new []{ new KeyValuePair<Data, Data>(key, value) }));
        }

        public bool TryGetValue(Data key, out Data value) => map.TryGetValue(key, out value);

        protected override object GetObject() => this;
    }

}