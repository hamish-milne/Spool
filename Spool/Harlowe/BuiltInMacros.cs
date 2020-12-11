

using System.Drawing;
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

    struct VariableToValue
    {
        public object Source;
        public Mutable Destination;
    }

    struct ValueIntoVariable
    {
        public object Source;
        public Mutable Destination;
        public Mutable ToRemove;
    }

    class BuiltInMacros
    {
        public BuiltInMacros(Context context) => Context = context;
        public Context Context { get; }

        public void Set(params VariableToValue[] setters)
        {
            foreach (var s in setters) {
                s.Destination.Set(Context, s.Source);
            }
        }
        public void Put(params ValueIntoVariable[] setters)
        {
            foreach (var s in setters) {
                s.Destination.Set(Context, s.Source);
            }
        }

        public void Move(params ValueIntoVariable[] setters)
        {
            foreach (var s in setters) {
                if (s.ToRemove == null) {
                    throw new Exception("Source must be a variable");
                }
                s.Destination.Set(Context, s.Source);
                s.ToRemove.Delete(Context);
            }
        }

        public ICollection<object> DS(params object[] values) => DataSet(values);
        public ICollection<object> DataSet(params object[] values) => new HashSet<object>(values);
        public IDictionary DataMap(params object[] pairs) {
            var map = new Dictionary<object, object>();
            for (int i = 1; i < pairs.Length; i += 2) {
                map[pairs[i - 1]] = pairs[i];
            }
            return map;
        }
        public IDictionary DM(params object[] pairs) => DataMap(pairs);
        public IList Array(params object[] values) => values.ToList();
        public IList A(params object[] values) => Array(values);


        public Command show(IEnumerable<XContainer> nodes) => new Show(nodes);
        class Show : Command
        {
            public Show(IEnumerable<XContainer> nodes) => Nodes = nodes;
            public IEnumerable<XContainer> Nodes { get; }
            public void Run(Context context)
            {
                foreach (var node in Nodes) {
                    if (context.Hidden.TryGetValue(node, out var renderable)) {
                        var state = context.Push(node, CursorPos.Before);
                        renderable.Render(context);
                        context.Hidden.Remove(node);
                        node.Remove();
                        context.Pop(state);
                    }
                }
            }
        }

        public Renderable print(object value) => new Print(value);
        class Print : Renderable
        {
            public Print(object value) => Value = value;
            public object Value { get; }

            private static void DoPrint(Context context, object value)
            {

                if (value is IList a)
                {
                    context.AddText("[");
                    for (int i = 0; i < a.Count; i++)
                    {
                        DoPrint(context, a[i]);
                        if (i+1 < a.Count) {
                            context.AddText(", ");
                        }
                    }
                    context.AddText("]");
                } else {
                    context.AddText(value?.ToString() ?? "NULL");
                }
            }

            public void Render(Context context) => DoPrint(context, Value);
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

        public Changer @for(Filter filter, params object[] values) => new Loop(filter, values);

        class Loop : Changer
        {
            private readonly Filter filter;
            private readonly object[] values;

            public Loop(Filter filter, object[] values)
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

        public object either(params object[] choices) => choices[Context.Random.Next(choices.Length)];

        public object cond(params object[] values)
        {
            for (int i = 1; i < (values.Length-1); i += 2) {
                if ((bool)values[i - 1]) {
                    return values[i];
                }
            }
            return values[values.Length - 1];
        }

        public object nth(double number, params object[] values)
        {
            var idx = ((int)number) % values.Length;
            return values[idx];
        }

        public IList range(double a, double b) => b < a ? range(b, a) :
            Enumerable.Range((int)a, ((int)b) - ((int)a) + 1).Select(x => (object)(double)x).ToList();

        public Gradient gradient(double angle, params object[] pairs)
        {
            IEnumerable<ColorStop> getStops() {
                for (int i = 0; i < pairs.Length; i += 2) {
                    if (pairs[i] is double d && pairs[i+1] is Color c) {
                        yield return new ColorStop(c, d);
                    } else {
                        throw new ArgumentException($"Invalid color stop at {i}");
                    }
                }
            }
            return new Gradient(getStops(), angle);
        }

        public Color hsl(double hue, double saturation, double lightness) => hsla(hue, saturation, lightness);
        public Color hsl(double hue, double saturation, double lightness, double alpha) => hsla(hue, saturation, lightness, alpha);
        public Color hsla(double hue, double saturation, double lightness) => hsl(hue, saturation, lightness, 1.0);
        public Color hsla(double hue, double saturation, double lightness, double alpha)
        {
            throw new NotImplementedException();
        }

        public Color rgba(double r, double g, double b, double a) => Color.FromArgb((int)a, (int)r, (int)g, (int)b);
        public Color rgba(double r, double g, double b) => rgba(r, g, b, 255);
        public Color rgb(double r, double g, double b, double a) => rgba(r, g, b, a);
        public Color rgb(double r, double g, double b) => rgba(r, g, b);


        public bool AllPass(Filter filter, params object[] values) => values.All(new Func<object, bool>(filter));
        public bool SomePass(Filter filter, params object[] values) => values.Any(new Func<object, bool>(filter));
        public bool NonePass(Filter filter, params object[] values) => values.All(x => !filter(x));

        // TODO: altered
        public double count(IList array, params object[] testValues) => array.Cast<object>().Count(x => System.Array.IndexOf(testValues, x) >= 0);
        public double count(string text, params string[] testValues) => testValues.Sum(value => {
            int count = 0, minIndex = text.IndexOf(value, 0);
            while (minIndex != -1)
            {
                minIndex = text.IndexOf(value, minIndex + value.Length);
                count++;
            }
            return count;
        });

        public IList DataPairs(IDictionary map) => DataEntries(map);
        public IList DataEntries(IDictionary map) {
            IEnumerable<object> makeEntries() {
                var e = map.GetEnumerator();
                while (e.MoveNext()) {
                    yield return new Dictionary<object, object>{{"name", e.Key}, {"value", e.Value}};
                }
            };
            return makeEntries().ToList();
        }

        // TODO: Ordering
        public IList DataNames(IDictionary map) => map.Keys.Cast<object>().ToList();
        public IList DataValues(IDictionary map) => map.Values.Cast<object>().ToList();

        public IList find(Filter filter, params object[] values) => values.Where(new Func<object, bool>(filter)).ToList();
        // TODO: Folded
        public IList interlaced(params IList[] lists) => Enumerable.Range(0, lists.Min(x => x.Count))
            .SelectMany(i => lists.Select(l => l[i])).ToList();
        public IList repeated(double count, params object[] values) =>
            Enumerable.Range(0, (int)count).SelectMany(_ => values).ToList();
        public IList reversed(params object[] values) => values.Reverse().ToList();

        static int mod(int a, int b)
        {
            var c = a % b;
            return c*b < 0 ? c+b : c;
        }

        public IList rotated(double rotation, params object[] values) =>
            Enumerable.Range(-(int)rotation, values.Length).Select(i => values[mod(i, values.Length)]).ToList();
        public IList shuffled(params object[] list)
        {
            int n = list.Length;  
            while (n > 1) {  
                n--;  
                int k = Context.Random.Next(n + 1);  
                var value = list[k];  
                list[k] = list[n];  
                list[n] = value;  
            }
            return list.ToList();
        }
        public IList sorted(params IComparable[] list)
        {
            var newList = list.ToList();
            newList.Sort();
            return newList;
        }

        public string CurrentDate() => DateTime.Now.ToString("ddd MMM dd yyyy");
        public string CurrentTime() => DateTime.Now.ToString("hh:mm tt");
        public double MonthDay() => DateTime.Now.Day;
        public string WeekDay() => DateTime.Now.DayOfWeek.ToString();
        
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

    class Gradient
    {
        public double Angle { get; }
        public IEnumerable<ColorStop> Stops { get; }

        public Gradient(IEnumerable<ColorStop> stops, double angle)
        {
            Stops = stops;
            Angle = angle;
        }
    }

    struct ColorStop
    {
        public ColorStop(Color color, double stop)
        {
            Color = color;
            Stop = stop;
        }

        public Color Color { get; }
        public double Stop { get; }
    }

    class NullChanger : Changer
    {
        public static Changer Instance { get; } = new NullChanger();
        public void Apply(ref bool? hidden, ref string name) {}
        public void Render(Context context, Action source) => source();
    }
}