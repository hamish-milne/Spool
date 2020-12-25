namespace Spool.Harlowe
{
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

        public override Selector MakeSelector() => new HookNameSelector(Name);

        protected override string GetString() {
            return $"?{Name}, (a hook name)";
        }
    }
}