using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Lexico;

namespace Spool.Harlowe
{
    [TopLevel, CompileFlags(CompileFlags.CheckImmediateLeftRecursion | CompileFlags.AggressiveMemoizing)]
    public class Block : Renderable
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
                typeof(HorizontalRule),
                typeof(NewLine),
                typeof(CollapsedSpan),
                typeof(AppliedHook),
                typeof(Style),
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

                public void Apply(ref bool? hidden, ref string name)
                {
                    if (name != null) {
                        throw new Exception("Hook already named");
                    }
                    name = this.name;
                    if (this.hidden == ')') {
                        hidden = true;
                    }
                }

                public void Render(Context context, Action source) => source();
            }

            [WhitespaceSeparated]
            private class HookSuffix : Changer
            {
                [CharSet("<(")] char hidden;
                [CharRange("az", "AZ", "09", "__"), Repeat] string name;
                [Literal("|")] Unnamed _;

                public void Apply(ref bool? hidden, ref string name)
                {
                    if (name != null) {
                        throw new Exception("Hook already named");
                    }
                    name = this.name;
                    if (this.hidden == '(') {
                        hidden = true;
                    }
                }

                public void Render(Context context, Action source) => source();
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
                string name = null;
                bool? hidden = false;
                changerObjs.AddRange(changerExprs.Select(x => {
                    var val = x.Evaluate(context);
                    if ((val as CommandData)?.Object is Changer c) {
                        return c;
                    } else if (val is Boolean b) {
                        hidden |= !b.Value;
                        return NullChanger.Instance;
                    } else {
                        throw new Exception("Hook prefix must be a Changer or Boolean");
                    }
                }));
                foreach (var c in changerObjs) {
                    c.Apply(ref hidden, ref name);
                }
                if (hidden != null) {
                    context.PreviousCondition = hidden;
                }
                XElement content = null;
                if (hidden == true && !forceShow) {
                    if (name != null)
                    {
                        content = new XElement(XName.Get("hidden"));
                        context.AddNode(content);
                        context.Hidden.Add(content, new ShowHiddenHook(this));
                    }
                } else {
                    Action renderHookBody;

                    // If applied to a hook, render its body:
                    if (hook != null)
                    {
                        renderHookBody = () => hook.body.Render(context);
                    }
                    else
                    {
                        // Otherwise, render the content of the final macro/variable:
                        var macroResult = changers.Last().Evaluate(context);
                        if ((macroResult as CommandData)?.Object is Renderable r) {
                            renderHookBody = () => r.Render(context);
                        } else if (macroResult is String || macroResult is Number) {
                            renderHookBody = () => context.AddText(macroResult.ToString());
                        } else if ((macroResult as CommandData)?.Object is Command cmd) {
                            if (changerObjs.Count > 0) {
                                throw new Exception("Changers cannot be applied to Commands");
                            }
                            cmd.Run(context);
                            return;
                        } else if (macroResult == null) {
                            if (changerObjs.Count > 0) {
                                throw new Exception("Changers cannot be applied to Instants");
                            }
                            // 'Instant' e.g. Set, Put
                            return;
                        } else {
                            throw new Exception($"Result {macroResult} of expression {changers.Last()} is not printable");
                        }
                    }

                    // Push a new state, and add the name tag to the inner content
                    Action initialRenderFn = () => {
                        var prevCond = context.NewCondition();
                        if (name != null) {
                            content = new XElement(XName.Get("div"));
                            context.AddNode(content);
                            var state = context.Push(content, CursorPos.Child);
                            renderHookBody();
                            context.Pop(state);
                        } else {
                            renderHookBody();
                        }
                        context.PopCondition(prevCond);
                    };

                    // Apply changers in reverse order:
                    changerObjs.Reverse();
                    var finalRenderFn = changerObjs.Aggregate(initialRenderFn,
                        (sourceFn, changer) => () => changer.Render(context, sourceFn)
                    );
                    finalRenderFn();
                }
                // TODO: Name the hook before or after rendering it? This has implications on whether (replace:) affects the hook it's running in
                if (name != null) {
                    content.SetAttributeValue(XName.Get("name"), name);
                }
            }
        }

        interface ChangerTarget : Renderable {}

        abstract class LinkBase : ChangerTarget
        {
            public abstract string Link { get; }
            public abstract string Text { get; }

            public void Render(Context context)
            {
                var el = new XElement(XName.Get("link"), new XText(Text));
                el.SetAttributeValue(XName.Get("href"), Link);
                context.AddNode(el);
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

            public void Render(Context context)
            {
                foreach (var c in GetContent()) {
                    c.Render(context);
                }
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
            [Literal("["), Cut] Unnamed _;
            [CharSet("="), Repeat] string __;
            [Term] ContentList content;

            public override List<Renderable> GetContent() => content.Items;
        }

        abstract class Style : Renderable
        {
            public abstract void Render(Context context);
            public abstract string Op { get; }
        }

        abstract class Style<T> : Style where T : Style, new()
        {
            protected struct Item {

                private string Op => new T().Op;

                [Not, IndirectLiteral(nameof(Op))] public Unnamed _;

                [Alternative(
                    typeof(NewLine),
                    typeof(CollapsedSpan),
                    typeof(AppliedHook),
                    typeof(Style),
                    typeof(PlainText)
                )]
                public Renderable item;
            }

            [IndirectLiteral(nameof(Op))] protected Unnamed prefix;
            [Term] protected List<Item> inner;
            [IndirectLiteral(nameof(Op))] protected Unnamed suffix;

            protected abstract XName Tag { get; }

            public override void Render(Context context)
            {
                var el = new XElement(Tag);
                context.AddNode(el);
                var state = context.Push(el, CursorPos.Child);
                foreach (var c in inner) {
                    c.item.Render(context);
                }
                context.Pop(state);
            }
        }

        class Italics : Style<Italics>
        {
            public override string Op => "//";
            protected override XName Tag => XName.Get("i");
        }

        class Bold : Style<Bold>
        {
            public override string Op => "''";
            protected override XName Tag => XName.Get("b");
        }

        class Strikethrough : Style<Strikethrough>
        {
            public override string Op => "~~";
            protected override XName Tag => XName.Get("s");
        }

        class Emphasis : Style<Emphasis>
        {
            public override string Op => "*";
            protected override XName Tag => XName.Get("em");
        }

        class Strong : Style<Strong>
        {
            public override string Op => "**";
            protected override XName Tag => XName.Get("strong");
        }

        class Superscript : Style<Superscript>
        {
            public override string Op => "^^";
            protected override XName Tag => XName.Get("sup");
        }

        class PlainText : Renderable
        {
            [Regex(@"[^\]]((?!//)[^\|\(\[$_\]*'~\^])*")] public string Text { get; set; }

            public void Render(Context context)
            {
                context.AddText(Text);
            }
        }

        class HorizontalRule : Renderable
        {
            [Alternative(typeof(SOF), typeof(EOL))] Unnamed _;
            [WhitespaceSurrounded, CharSet("-"), Repeat(Min = 3)] string __;
            [LookAhead, EOL] Unnamed ___;

            public void Render(Context context)
            {
                context.AddNode(new XElement(XName.Get("hr")));
            }
        }

    }
}