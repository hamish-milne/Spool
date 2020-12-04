using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Lexico;

namespace Spool.Harlowe
{

    public enum CursorPos
    {
        Child,
        Before
    }

    public class Context
    {
        public IDictionary<string, object> Locals { get; }
        public IDictionary<string, object> Globals { get; }
        public IDictionary<XContainer, Renderable> Hidden { get; }
        public IDictionary<string, Renderable> Passages { get; }
        public XDocument Screen { get; }
        public XContainer Cursor { get; private set; }
        public CursorPos Position { get; private set; }
        public (XContainer, CursorPos) Push(XContainer cursor, CursorPos cursorPos)
        {
            var state = (Cursor, Position);
            Cursor = cursor;
            Position = cursorPos;
            return state;
        }
        public void Pop((XContainer, CursorPos) state)
        {
            Cursor = state.Item1;
            Position = state.Item2;
        }

        public XNode PreviousNode => Position switch {
            CursorPos.Child => Cursor.LastNode,
            CursorPos.Before => Cursor.PreviousNode,
            _ => throw new ArgumentOutOfRangeException()
        };

        public void AddNode(XNode node)
        {
            switch (Position) {
                case CursorPos.Before: Cursor.AddBeforeSelf(node); break;
                case CursorPos.Child: Cursor.Add(node); break;
            }
        }

        public void AddText(string text)
        {
            if (PreviousNode is XText tnode) {
                tnode.Value += text;
            } else {
                AddNode(new XText(text));
            }
        }
    }

    public interface Renderable
    {
        void Render(Context context);
    }

    [TopLevel]
    public class Passage : Renderable
    {

        [Term] private ContentList content;

        public void Render(Context context)
        {
            foreach (var c in content.Items) {
                c.Render(context);
            }
        }

        struct ContentList
        {
            [Repeat(Min = 0), Alternative(
                typeof(CollapsedSpan),
                typeof(AppliedHook),
                typeof(PlainText)
            )]
            public List<Renderable> Items;
        }

        [SurroundBy("{", "}"), WhitespaceSeparated]
        class CollapsedSpan : Renderable
        {
            
        }



        [WhitespaceSeparated]
        class AppliedHook : Renderable
        {

            [WhitespaceSurrounded]
            private class HookSeparator
            {
                [Literal("+")] Unnamed _;
            }

            [WhitespaceSeparated]
            private class HookPrefix : Changer
            {
                [Literal("|")] Unnamed _;
                [CharRange("az", "AZ", "09", "__")] string name;
                [CharSet(">)")] char hidden;

                public void Apply(ref bool hidden, ref string name)
                {
                    if (name != null) {
                        throw new Exception("Hook already named");
                    }
                    name = this.name;
                    if (this.hidden == ')') {
                        hidden = true;
                    }
                }

                public XElement Render(Context context, XElement source) => source;
            }

            [WhitespaceSeparated]
            private class HookSuffix : Changer
            {
                [CharSet("<(")] char hidden;
                [CharRange("az", "AZ", "09", "__")] string name;
                [Literal("|")] Unnamed _;

                public void Apply(ref bool hidden, ref string name)
                {
                    if (name != null) {
                        throw new Exception("Hook already named");
                    }
                    name = this.name;
                    if (this.hidden == ')') {
                        hidden = true;
                    }
                }

                public XElement Render(Context context, XElement source) => source;
            }

            [SeparatedBy(typeof(HookSeparator)), Repeat(Min = 0), Alternative(
                typeof(Expressions.Macro),
                typeof(Expressions.Variable)
            )] List<Expression> changers;

            class HookSyntax
            {
                [Optional] public HookPrefix prefix; 
                [Term] public Hook body;
                [Optional] public HookSuffix suffix;
            }

            [Optional] HookSyntax hook;

            private class ShowHiddenHook : Renderable
            {
                public ShowHiddenHook(AppliedHook parent) => this.parent = parent;
                private readonly AppliedHook parent;

                public void Render(Context context) => parent.Render(context, true);
            }

            public void Render(Context context) => Render(context, false);

            public void Render(Context context, bool forceShow)
            {
                var changerObjs = new List<Changer>();
                if (hook?.prefix != null) {
                    changerObjs.Add(hook.prefix);
                }
                if (hook?.suffix != null) {
                    changerObjs.Add(hook.suffix);
                }
                IEnumerable<Expression> changerExprs = changers;
                if (hook == null) {
                    changerExprs = changers.Take(changers.Count - 1);
                }
                changerObjs.AddRange(changerExprs.Select(x => (x.Evaluate(context) as Changer) ?? throw new Exception("Hook prefix must be a Changer")));
                string name = null;
                bool hidden = false;
                foreach (var c in changerObjs) {
                    c.Apply(ref hidden, ref name);
                }
                XElement content;
                if (hidden == true && !forceShow) {
                    content = new XElement(XName.Get("hidden"));
                    context.Hidden.Add(content, new ShowHiddenHook(this));
                } else {
                    content = hook.body.Render(context);
                    foreach (var c in changerObjs) {
                        content = c.Render(context, content);
                    }
                }
                // TODO: Name the hook before or after rendering it? This has implications on whether (replace:) affects the hook it's running in
                if (name != null) {
                    content.SetAttributeValue(XName.Get("name"), name);
                }
            }
        }

        abstract class Hook
        {
            public abstract XElement Render(Context context);
        }

        [SurroundBy("[[", "]]")]
        class RightLink : Hook
        {
            [Regex(@"(?!<-).)+")] public string Link { get; set; }
            [Literal("<-")] Unnamed _;
            [Optional, Regex(@"((?!]]).)+")] public string Text { get; set; }

            public override XElement Render(Context context)
            {
                throw new NotImplementedException();
            }
        }

        [SurroundBy("[[", "]]")]
        class LeftLink : Hook
        {
            [Regex(@"(?!->).)+")] public string Text { get; set; }
            [Literal("->")] Unnamed _;
            [Optional, Regex(@"((?!]]).)+")] public string Link { get; set; }

            public override XElement Render(Context context)
            {
                throw new NotImplementedException();
            }
        }

        [SurroundBy("[[", "]]")]
        class PlainLink : Hook
        {
            [Regex(@"((?!]]).)+")] public string TextLink { get; set; }

            public override XElement Render(Context context)
            {
                var el = new XElement(XName.Get("link"), new XText(TextLink));
                return el;
            }
        }

        abstract class HookBase : Hook
        {
            public abstract List<Renderable> GetContent();

            public override XElement Render(Context context)
            {
                var container = new XElement(XName.Get("div"));
                context.Cursor.Add(container);
                var state = context.Push(container, CursorPos.Child);
                foreach (var c in GetContent()) {
                    c.Render(context);
                }
                context.Pop(state);
                return container;
            }
        }

        [SurroundBy("[", "]")]
        class ClosedHook : HookBase
        {
            [Term] ContentList content;

            public override List<Renderable> GetContent() => content.Items;
        }

        class OpenHook : HookBase
        {
            [Literal("[==")] Unnamed _;
            [Term] ContentList content;

            public override List<Renderable> GetContent() => content.Items;
        }

        class PlainText : Renderable
        {
            [Regex(@"[^\]][^\(\[\$\_]*")] public string Text { get; set; }

            public void Render(Context context)
            {
                if (context.Cursor.LastNode is XText prevText) {
                    prevText.Value += Text;
                } else {
                    context.Cursor.Add(new XText(Text));
                }
            }
        }

    }
}