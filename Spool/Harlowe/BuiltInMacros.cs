using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;

namespace Spool.Harlowe
{

    public interface Command
    {
        void Run(Context context);
    }

    public interface Changer
    {
        void Apply(ref bool? hidden, ref string name);
        void Render(Context context, Action source);
    }

    class BuiltInMacros
    {
        public BuiltInMacros(Context context) => Context = context;
        public Context Context { get; }

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

        public DataSet DS(params Data[] values) => new DataSet(values);
        public DataSet DataSet(params Data[] values) => new DataSet(values);
        public DataMap DataMap(params Data[] pairs) {
            var map = new Dictionary<Data, Data>();
            for (int i = 1; i < pairs.Length; i += 2) {
                map[pairs[i - 1]] = pairs[i];
            }
            return new DataMap(map);
        }
        public DataMap DM(params Data[] pairs) => DataMap(pairs);
        public Array Array(params Data[] values) => new Array(values);
        public Array A(params Data[] values) => new Array(values);


        // public Command show(HookName nodes) => new Show(nodes);
        // class Show : Command
        // {
        //     public Show(HookName nodes) => Nodes = nodes;
        //     public HookName Nodes { get; }
        //     public void Run(Context context)
        //     {
        //         foreach (var node in Nodes) {
        //             if (context.Hidden.TryGetValue(node, out var renderable)) {
        //                 var state = context.Push(node, CursorPos.Before);
        //                 renderable.Render(context);
        //                 context.Hidden.Remove(node);
        //                 node.Remove();
        //                 context.Pop(state);
        //             }
        //         }
        //     }
        // }

        public Renderable print(Data value) => new Print(value);
        class Print : Renderable
        {
            public Print(Data value) => Value = value;
            public Data Value { get; }
            public void Render(Context context) => context.AddText(Value.ToString() ?? "NULL");
        }

        public Renderable display(string passage) => Context.Passages[passage].Body;

        public Changer hidden() => Hidden.Instance;
        class Hidden : Changer
        {
            public static Hidden Instance { get; } = new Hidden();
            public void Apply(ref bool? hidden, ref string name) => hidden = true;
            public void Render(Context context, Action source) => source();
        }


        class ReplaceHook : Command
        {
            public Renderable Replacement { get; }
            public IEnumerable<XContainer> Target { get; }
            public void Run(Context context)
            {
                foreach (var node in Target)
                {
                    (XContainer, CursorPos) state;
                    if (node.NextNode is XContainer nextNode) {
                        state = context.Push(nextNode, CursorPos.Before);
                    } else if (node.Parent != null) {
                        state = context.Push(node.Parent, CursorPos.Child);
                    } else {
                        throw new InvalidOperationException();
                    }
                    node.Remove();
                    Replacement.Render(context);
                    context.Pop(state);
                }
            }
        }

        class ReplaceText : Command
        {
            public Renderable Replacement { get; }

            public void Run(Context context)
            {
                throw new NotImplementedException();
            }
        }

        public Changer font(string font) => new Font(font);

        class Font : Changer
        {
            public Font(string fontName) => FontName = fontName;
            public string FontName { get; }

            public void Apply(ref bool? hidden, ref string name) {}

            public void Render(Context context, Action source)
            {
                var el = new XElement(XName.Get("font"));
                el.SetAttributeValue(XName.Get("value"), FontName);
                context.AddNode(el);
                var state = context.Push(el, CursorPos.Child);
                source();
                context.Pop(state);
            }
        }


        public Changer textColour(string color) => new TextColor(color);
        public Changer textColor(string color) => new TextColor(color);

        class TextColor : Changer
        {
            public TextColor(string color) => Color = color;
            public string Color { get; }

            public void Apply(ref bool? hidden, ref string name) {}

            public void Render(Context context, Action source)
            {
                var el = new XElement(XName.Get("color"));
                el.SetAttributeValue(XName.Get("value"), Color);
                context.AddNode(el);
                var state = context.Push(el, CursorPos.Child);
                source();
                context.Pop(state);
            }
        }

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

            public void Apply(ref bool? hidden, ref string name) {}

            public void Render(Context context, Action source)
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

        public Array range(double a, double b) => b < a ? range(b, a) :
            new Array(Enumerable.Range((int)a, ((int)b) - ((int)a) + 1).Select(x => new Number(x)));

        // public Gradient gradient(double angle, params object[] pairs)
        // {
        //     IEnumerable<ColorStop> getStops() {
        //         for (int i = 0; i < pairs.Length; i += 2) {
        //             if (pairs[i] is double d && pairs[i+1] is Color c) {
        //                 yield return new ColorStop(c, d);
        //             } else {
        //                 throw new ArgumentException($"Invalid color stop at {i}");
        //             }
        //         }
        //     }
        //     return new Gradient(getStops(), angle);
        // }

        public Color hsl(double hue, double saturation, double lightness) => hsla(hue, saturation, lightness);
        public Color hsl(double hue, double saturation, double lightness, double alpha) => hsla(hue, saturation, lightness, alpha);
        public Color hsla(double hue, double saturation, double lightness) => hsl(hue, saturation, lightness, 1.0);
        public Color hsla(double hue, double saturation, double lightness, double alpha)
        {
            throw new NotImplementedException();
        }

        public Color rgba(double r, double g, double b, double a) => new Color(System.Drawing.Color.FromArgb((int)a, (int)r, (int)g, (int)b));
        public Color rgba(double r, double g, double b) => rgba(r, g, b, 255);
        public Color rgb(double r, double g, double b, double a) => rgba(r, g, b, a);
        public Color rgb(double r, double g, double b) => rgba(r, g, b);


        public Boolean AllPass(Filter filter, params Data[] values) => Boolean.Get(values.All(new Func<Data, bool>(filter)));
        public Boolean SomePass(Filter filter, params Data[] values) => Boolean.Get(values.Any(new Func<Data, bool>(filter)));
        public Boolean NonePass(Filter filter, params Data[] values) => Boolean.Get(values.All(x => !filter(x)));

        // TODO: altered
        public Number count(Array array, params Data[] testValues) => new Number(array.Count(x => System.Array.IndexOf(testValues, x) >= 0));
        public Number count(string text, params string[] testValues) => new Number(testValues.Sum(value => {
            int count = 0, minIndex = text.IndexOf(value, 0);
            while (minIndex != -1)
            {
                minIndex = text.IndexOf(value, minIndex + value.Length);
                count++;
            }
            return count;
        }));

        public Array DataPairs(DataMap map) => DataEntries(map);
        public Array DataEntries(DataMap map) {
            return new Array(map.OrderBy(p => p.Key).Select(p =>
                new DataMap(new Dictionary<Data, Data>{
                    {new String("name"), p.Key},
                    {new String("value"), p.Value}
                })
            ));
        }

        // TODO: Ordering
        public Array DataNames(DataMap map) => new Array(map.Keys.OrderBy(x => x));
        public Array DataValues(DataMap map) => new Array(map.OrderBy(p => p.Key).Select(p => p.Value));

        public Array find(Filter filter, params Data[] values) => new Array(values.Where(new Func<Data, bool>(filter)));
        // TODO: Folded
        public Array interlaced(params Array[] lists) => new Array(
            Enumerable.Range(0, lists.Min(x => x.Count))
            .SelectMany(i => lists.Select(l => l[i]))
        );
        public Array repeated(double count, params Data[] values) =>
            new Array(Enumerable.Range(0, (int)count).SelectMany(_ => values));
        public Array reversed(params Data[] values) => new Array(values.Reverse());

        static int mod(int a, int b)
        {
            var c = a % b;
            return c*b < 0 ? c+b : c;
        }

        public Array rotated(double rotation, params Data[] values) =>
            new Array(Enumerable.Range(-(int)rotation, values.Length).Select(i => values[mod(i, values.Length)]));
        public Array shuffled(params Data[] list)
        {
            int n = list.Length;  
            while (n > 1) {  
                n--;  
                int k = Context.Random.Next(n + 1);  
                var value = list[k];  
                list[k] = list[n];  
                list[n] = value;  
            }
            return new Array(list);
        }
        public Array sorted(params Data[] list)
        {
            System.Array.Sort(list);
            return new Array(list);
        }

        public String CurrentDate() => new String(DateTime.Now.ToString("ddd MMM dd yyyy"));
        public String CurrentTime() => new String(DateTime.Now.ToString("hh:mm tt"));
        public Number MonthDay() => new Number(DateTime.Now.Day);
        public String WeekDay() => new String(DateTime.Now.DayOfWeek.ToString());
        
        // TODO: History
        // TODO: Passage
        // TODO: Value semantics!! AAAAARGGH!

        // TODO: Bind, cycling-link
        // TODO: Bind, dropdown

        public Changer link(string text) => linkReplace(text);
        public Changer linkReplace(string text) => new LinkReplace(text);

        class LinkReplace : Changer
        {
            public LinkReplace(string text)
            {
                Text = text;
            }

            public string Text { get; }

            public void Apply(ref bool? hidden, ref string name) => hidden = true;

            public void Render(Context context, Action source)
            {
                var el = new XElement(XName.Get("link"));
                el.SetAttributeValue(XName.Get("href"), "something here"); // TODO: Clicks
                context.AddNode(el);
                var state = context.Push(el, CursorPos.Child);
                source();
                context.Pop(state);
            }
        }

        public Changer linkReveal(string text) => new LinkReveal(text);

        class LinkReveal : Changer
        {
            public LinkReveal(string text)
            {
                Text = text;
            }

            public string Text { get; }

            public void Apply(ref bool? hidden, ref string name) => hidden = true;

            public void Render(Context context, Action source)
            {
                var el = new XElement(XName.Get("link"));
                el.SetAttributeValue(XName.Get("href"), "something here"); // TODO: Clicks
                context.AddNode(el);
                var state = context.Push(el, CursorPos.Child);
                source();
                context.Pop(state);
            }
        }
        
    }

    class NullChanger : Changer
    {
        public static Changer Instance { get; } = new NullChanger();
        public void Apply(ref bool? hidden, ref string name) {}
        public void Render(Context context, Action source) => source();
    }
}