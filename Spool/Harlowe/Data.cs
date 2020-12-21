using System.Linq;
using System;
using System.Collections.Generic;
using System.Collections;

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
        GreaterOrEqual
    }

    public enum UnaryOp
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
            return op switch {
                TestOperator.Is => Equals(rhs),
                TestOperator.Matches => Equals(rhs) || (rhs is DataType dt && this.IsAn(dt)),
                _ => throw new NotSupportedException()
            };
        }
        public virtual Mutator MutableMember(Data member) => throw new NotSupportedException();
        public virtual IEnumerable<Data> Spread() => throw new NotSupportedException();
        private object cachedObject;
        public object Object => cachedObject ??= GetObject();
        protected abstract object GetObject();

        private static readonly IComparer comparer = new AlphanumComparator.AlphanumComparator();

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

    class Number : RenderableData
    {
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

    class Boolean : RenderableData
    {
        public override bool Serializable => true;
        public static Boolean Get(bool value) => value ? True : False;
        public static Boolean True { get; } = new Boolean(true);
        public static Boolean False { get; } = new Boolean(false);

        private Boolean(bool value) => Value = value;
        public bool Value { get; }
        protected override object GetObject() => Value;
        public override Data Operate(Operator op, Data rhs)
        {
            return rhs switch {
                Boolean b => op switch {
                    Operator.And => Get(Value && b.Value),
                    Operator.Or => Get(Value && b.Value),
                    _ => base.Operate(op, rhs)
                },
                _ => base.Operate(op, rhs)
            };
        }

        public override Data Unary(UnaryOp op)
        {
            return op switch {
                UnaryOp.Not => Get(!Value),
                _ => base.Unary(op)
            };
        }

        public override bool Equals(Data other) => other is Boolean b && b.Value == Value;
    }

    class String : RenderableData
    {
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

    class Checker : Data
    {
        public override bool Serializable => false;
        public Checker(Func<Data, bool> checker) => this.checker = checker;
        private readonly Func<Data, bool> checker;

        public override bool Test(TestOperator op, Data rhs)
        {
            if (op == TestOperator.Is) {
                return checker(rhs);
            } else {
                throw new NotSupportedException();
            }
        }

        protected override object GetObject() => checker;

        private static Func<Data, bool> Check(IEnumerable<Data> list, Func<IEnumerable<Data>, Data, bool> checker)
        {
            return rhs => checker(list, rhs);
        }

        public static Checker All(IEnumerable<Data> list) => new Checker(rhs => list.All(x => x == rhs));
        public static Checker Any(ICollection<Data> list) => new Checker(rhs => list.Contains(rhs));
    }

    class DataType : Data
    {
        public override bool Serializable => true;
        public Type Value { get; }
        protected override object GetObject() => Value;
        protected override string GetString() => $"[the {Value.Name.ToString().ToLowerInvariant()} datatype]";
    }

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

    class LambdaData : Data
    {
        public override bool Serializable => true;
        public LambdaData(Filter value) => Value = value;
        public object Value { get; }
        protected override object GetObject() => Value;
        protected override string GetString() => $"[A lambda]";
    }

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

    class Color : Data
    {
        public override bool Serializable => true;
        public Color(System.Drawing.Color value) => Value = value;
        public System.Drawing.Color Value { get; }
        protected override object GetObject() => Value;

        public override Data Member(Data member)
        {
            if (member is String str) {
                return str.Value switch {
                    "r" => new Number(Value.R),
                    "g" => new Number(Value.G),
                    "b" => new Number(Value.B),
                    "h" => new Number(Value.GetHue()),
                    "s" => new Number(Value.GetSaturation()),
                    "l" => new Number(Value.GetBrightness()),
                    _ => base.Member(member)
                };
            }
            return base.Member(member);
        }

        public override Data Operate(Operator op, Data rhs)
        {
            if (op == Operator.Add && rhs is Color c) {
                return new Color(System.Drawing.Color.FromArgb(
                    (Value.A + c.Value.A) / 2,
                    (Value.R + c.Value.R) / 2,
                    (Value.G + c.Value.G) / 2,
                    (Value.B + c.Value.B) / 2
                ));
            }
            return base.Operate(op, rhs);
        }
    }

    abstract class HookName : Data, Selection
    {
        public override bool Serializable => false;
        protected override object GetObject() => this;

        // public override bool Equals(Data other) => other is HookName hn && this.SequenceEqual(hn);

        public override Data Member(Data member)
        {
            return member switch {
                String str => str.Value switch {
                    // "chars" => new HookName(spans.SelectMany(x => x.Chars)),
                    // "links" => new HookName(spans.SelectMany(x => x.FindByAttribute("link"))),
                    // "lines" => new HookName(spans.SelectMany(x => x.Lines)),
                    _ => base.Member(member)
                },
                // Number num => new HookName(spans.Skip((int)num.Value - 1).Take(1)),
                _ => base.Member(member)
            };
        }

        protected override string GetString()
        {
            return "a complex hook name";
        }

        public abstract Selector MakeSelector();
    }

    class SimpleHookName : HookName
    {
        public SimpleHookName(string name) => Name = name;
        public string Name { get; }
        private readonly Context context;

        public override Selector MakeSelector() => new HookNameSelector();

        protected override string GetString() {
            return $"?{Name}, (a hook name)";
        }
    }
}
