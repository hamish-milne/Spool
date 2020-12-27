using System;

namespace Spool.Harlowe
{
    partial class BuiltInMacros
    {
        public void Set(params VariableToValue[] setters)
        {
            foreach (var s in setters) {
                s.Variable.Set(Context, s.Value);
            }
        }
        public void Put(params VariableToValue[] setters)
        {
            foreach (var s in setters) {
                s.Variable.Set(Context, s.Value);
            }
        }

        public void Move(params VariableToValue[] setters)
        {
            foreach (var s in setters) {
                if (s.ToRemove == null) {
                    throw new Exception("Source must be mutable");
                }
                s.Variable.Set(Context, s.Value);
                s.ToRemove.Delete(Context);
            }
        }

        
        public Renderable print(Data value) => new Print(value);
        class Print : Renderable
        {
            public Print(Data value) => Value = value;
            public Data Value { get; }
            public override void Render(Context context)
            {
                if (Value is Renderable r) {
                    r.Render(context);
                } else {
                    context.Cursor.WriteText($"[{Value}]");
                }
            }
        }

        public Renderable display(string passage) => Context.GetPassageBody(passage);

        public Changer @if(bool condition) => condition ? NullChanger.Instance : Hidden.Instance;
        public Changer unless(bool condition) => condition ? Hidden.Instance : NullChanger.Instance;
        public Changer elseIf(bool condition) => (condition && Context.PreviousCondition == true) ? NullChanger.Instance : Hidden.Instance;
        public Changer @else() => elseIf(true);

        public Changer @for(Filter filter, params Data[] values) => new Loop(filter, values);

        class Loop : Changer
        {
            private readonly Filter filter;
            private readonly Data[] values;

            public Loop(Filter filter, Data[] values)
            {
                this.filter = filter;
                this.values = values;
            }

            public override void Render(Context context, Action source)
            {
                foreach (var v in values) {
                    if (filter(v)) {
                        source();
                    }
                }
            }
        }

        public Data either(params Data[] choices) => choices[Context.Random.Next(choices.Length)];

        public Data cond(params Data[] values)
        {
            for (int i = 1; i < (values.Length-1); i += 2) {
                if (((Boolean)values[i - 1]).Value) {
                    return values[i];
                }
            }
            return values[values.Length - 1];
        }

        public Data nth(double number, params Data[] values)
        {
            var idx = ((int)number) % values.Length;
            return values[idx];
        }

        public Command enchant(HookName hook, Changer changer) => new Enchant(hook, changer);
        public Command enchant(string text, Changer changer) => new Enchant(new ContentSelector(new []{text}), changer);

        class Enchant : Command, PostProcessor
        {
            private readonly Selection selection;
            private readonly Changer changer;
            private string id;

            public Enchant(Selection selection, Changer changer)
            {
                this.selection = selection;
                this.changer = changer;
            }

            public void PostProcess(Context context)
            {
                context.Cursor.Reset();
                var s = selection.MakeSelector();
                while (s.Advance(context.Cursor, AdvanceType.ReplaceContainer, c => !c.CheckParentTags("enchant", id))) {
                    context.Cursor.PushTag("enchant", id);
                    changer.Render(context, () => s.ReplaceSource(context.Cursor));
                    context.Cursor.Pop();
                }
            }

            public override void Run(Context context)
            {
                if (!context.PostProcessors.Contains(this)) {
                    id = context.PostProcessors.Count.ToString();
                    context.PostProcessors.Add(this);
                }
            }
        }

    }
}