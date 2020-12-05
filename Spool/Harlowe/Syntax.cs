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

    public enum RenderFlags
    {
        None = 0,
        CollapseWhitespace = (1 << 0)
    }

    public class Context
    {
        public Context()
        {
            Cursor = new XElement(XName.Get("passage"));
            Screen.Add(Cursor);
            MacroProvider = new BuiltInMacros(this);
        }

        public IDictionary<string, object> Locals { get; } = new Dictionary<string, object>();
        public IDictionary<string, object> Globals { get; } = new Dictionary<string, object>();
        public IDictionary<XContainer, Renderable> Hidden { get; } = new Dictionary<XContainer, Renderable>();
        public IDictionary<string, Renderable> Passages { get; } = new Dictionary<string, Renderable>();
        public XDocument Screen { get; } = new XDocument();
        public XContainer Cursor { get; private set; }
        public CursorPos Position { get; private set; } = CursorPos.Child;
        public object MacroProvider { get; }

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

    [TopLevel, CompileFlags(CompileFlags.CheckImmediateLeftRecursion | CompileFlags.AggressiveMemoizing)]
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
                typeof(NewLine),
                typeof(CollapsedSpan),
                typeof(AppliedHook),
                typeof(PlainText)
            )]
            public List<Renderable> Items;
        }

        [WhitespaceSeparated, SurroundBy("{", "}")]
        class CollapsedSpan : Renderable
        {
            [Pass, Cut] Unnamed _;
            [Term] ContentList content;

            public void Render(Context context)
            {
                // TODO: Collapse whitespace flag
                foreach (var c in content.Items) {
                    c.Render(context);
                }
            }
        }

        class NewLine : Renderable
        {
            [Optional, Literal("\r")] Unnamed _;
            [Literal("\n")] Unnamed __;

            public void Render(Context context)
            {
                // TODO: Respect collapsed whitespace flag
                context.AddText("\n");
            }
        }


        [Sequence(CheckZeroLength = true)]
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
                [CharRange("az", "AZ", "09", "__"), Repeat] string name;
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
                [CharRange("az", "AZ", "09", "__"), Repeat] string name;
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

            [Alternative(
                typeof(Expressions.Macro),
                typeof(Expressions.Variable)
            ), SeparatedBy(typeof(HookSeparator)), Repeat(Min = 0)] List<Expression> changers;

            class HookSyntax
            {
                [Optional] Whitespace _;
                [Optional] public HookPrefix prefix; 
                [Term] public ChangerTarget body;
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
                } else if (hook != null) {
                    content = hook.body.Render(context);
                    foreach (var c in changerObjs) {
                        content = c.Render(context, content);
                    }
                } else if (changerObjs.Count == 0) {
                    var macroResult = changers.Last().Evaluate(context);
                    if (macroResult is Renderable r) {
                        r.Render(context);
                    } else if (macroResult is string || macroResult is double) {
                        context.AddText(macroResult.ToString());
                    } else if (macroResult is Command cmd) {
                        cmd.Run(context);
                    } else if (macroResult == null) {
                        // 'Instant' e.g. Set, Put
                    } else {
                        throw new Exception($"Result {macroResult} of expression {changers.Last()} is not printable");
                    }
                    return;
                } else {
                    throw new NotImplementedException();
                }
                // TODO: Name the hook before or after rendering it? This has implications on whether (replace:) affects the hook it's running in
                if (name != null) {
                    content.SetAttributeValue(XName.Get("name"), name);
                }
            }
        }

        abstract class ChangerTarget
        {
            public abstract XElement Render(Context context);
        }

        abstract class LinkBase : ChangerTarget
        {
            public abstract string Link { get; }
            public abstract string Text { get; }

            public override XElement Render(Context context)
            {
                var el = new XElement(XName.Get("link"), new XText(Text));
                el.SetAttributeValue(XName.Get("href"), Link);
                context.Cursor.Add(el);
                return el;
            }
        }

        [SurroundBy("[[", "]]")]
        class RightLink : LinkBase
        {
            [Regex(@"((?!<-)(?!]]).)+")] string link;
            [Literal("<-")] Unnamed _;
            [Optional, Regex(@"((?!]]).)+")] string text;

            public override string Link => link;
            public override string Text => text;
        }

        [SurroundBy("[[", "]]")]
        class LeftLink : LinkBase
        {
            [Regex(@"((?!->)(?!]]).)+")] string text;
            [Literal("->")] Unnamed _;
            [Optional, Regex(@"((?!]]).)+")] string link;

            public override string Link => link;
            public override string Text => text;
        }

        [SurroundBy("[[", "]]")]
        class PlainLink : LinkBase
        {
            [Pass, Cut] Unnamed _;
            [Regex(@"((?!]]).)+")] string textLink;

            public override string Link => textLink;
            public override string Text => textLink;
        }

        abstract class HookBase : ChangerTarget
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
            [Pass, Cut] Unnamed _;
            [Term] ContentList content;

            public override List<Renderable> GetContent() => content.Items;
        }

        class OpenHook : HookBase
        {
            [Literal("[=="), Cut] Unnamed _;
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