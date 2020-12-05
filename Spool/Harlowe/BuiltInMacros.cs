

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
        void Apply(ref bool hidden, ref string name);
        XElement Render(Context context, XElement source);
    }

    public delegate void SetFunction();
    public delegate void PutFunction();

    class BuiltInMacros
    {
        public BuiltInMacros(Context context) => Context = context;
        public Context Context { get; }

        public void Set(params SetFunction[] setters)
        {
            foreach (var s in setters) {
                s();
            }
        }
        public void Put(params PutFunction[] setters)
        {
            foreach (var s in setters) {
                s();
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
        public Array Array(params object[] values) => values;
        public Array A(params object[] values) => Array(values);


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

            public void Render(Context context)
            {
                if (Value is Array a)
                {
                    context.AddText("[" + string.Join(", ", a.Cast<object>().Select(x => x?.ToString() ?? "NULL")) + "]");
                } else {
                    context.AddText(Value?.ToString() ?? "NULL");
                }
            }
        }

        public Renderable display(string passage) => Context.Passages[passage];

        public Changer hidden() => Hidden.Instance;
        class Hidden : Changer
        {
            public static Hidden Instance { get; } = new Hidden();
            public void Apply(ref bool hidden, ref string name) => hidden = true;
            public XElement Render(Context context, XElement source) => source;
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

            public void Apply(ref bool hidden, ref string name) {}

            public XElement Render(Context context, XElement source)
            {
                var el = new XElement(XName.Get("font"));
                el.SetAttributeValue(XName.Get("value"), FontName);
                source.Remove();
                context.AddNode(el);
                el.Add(source);
                return el;
            }
        }


        public Changer textColour(string color) => new TextColor(color);
        public Changer textColor(string color) => new TextColor(color);

        class TextColor : Changer
        {
            public TextColor(string color) => Color = color;
            public string Color { get; }

            public void Apply(ref bool hidden, ref string name) {}

            public XElement Render(Context context, XElement source)
            {
                var el = new XElement(XName.Get("color"));
                el.SetAttributeValue(XName.Get("value"), Color);
                source.Remove();
                context.AddNode(el);
                el.Add(source);
                return el;
            }
        }

        public Changer @if(bool condition) => condition ? Null.Instance : Hidden.Instance;

        class Null : Changer
        {
            public static Changer Instance { get; } = new Null();
            public void Apply(ref bool hidden, ref string name) {}
            public XElement Render(Context context, XElement source) => source;
        }
    }
}