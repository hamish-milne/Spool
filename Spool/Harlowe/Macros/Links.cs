using System;

namespace Spool.Harlowe
{
    partial class BuiltInMacros
    {

        abstract class LinkChanger : Changer
        {
            public LinkChanger(string text)
            {
                Text = text;
            }

            public string Text { get; }

            protected abstract bool HasLinkStyle { get; }
            protected virtual bool Repeat => false;
            protected virtual bool RemoveLinkStyle => false;
            protected virtual bool RemoveContent => false;

            protected virtual void OnClick(Context context) {}

            public override void Render(Context context, Action source)
            {
                // For repeated events we need to make an enclosing tag, because
                // we need to append to the previous set of additions
                if (Repeat) {
                    context.Cursor.PushTag("span", null);
                }
                context.Cursor.PushTag("a", null);
                context.Cursor.WriteText(Text);
                context.Cursor.SetEvent("click", _ => {
                    if (RemoveContent) {
                        context.Cursor.DeleteContainer();
                    } else if (RemoveLinkStyle) {
                        var inner = context.Cursor.DeleteAll();
                        context.Cursor.DeleteContainer();
                        inner();
                    } else if (Repeat) {
                        context.Cursor.Pop();
                        context.Cursor.MoveToEnd();
                    }
                    source();
                    OnClick(context);
                }, Repeat);
                context.Cursor.Pop();
                if (Repeat) {
                    context.Cursor.Pop();
                }
            }
        }

        abstract class LinkCommand : Renderable
        {
            public LinkCommand(string text) {
                Text = text;
            }
            public string Text { get; }
            protected virtual bool RemoveLinkStyle => false;
            public override void Render(Context context)
            {
                context.Cursor.PushTag("a", null);
                context.Cursor.WriteText(Text);
                context.Cursor.SetEvent("click", _ => {
                    if (RemoveLinkStyle) {
                        var inner = context.Cursor.DeleteAll();
                        context.Cursor.DeleteContainer();
                        inner();
                    }
                    OnClick(context);
                }, false);
                context.Cursor.Pop();
            }

            protected abstract void OnClick(Context context);
        }

        public Changer link(string text) => linkReplace(text);
        public Changer linkReplace(string text) => new LinkReplace(text);
        class LinkReplace : LinkChanger
        {
            public LinkReplace(string text) : base(text) {}
            protected override bool HasLinkStyle => true;
            protected override bool RemoveContent => true;
        }

        public Changer linkReveal(string text) => new LinkReveal(text);
        class LinkReveal : LinkChanger
        {
            public LinkReveal(string text) : base(text) {}
            protected override bool HasLinkStyle => true;
            protected override bool RemoveLinkStyle => true;
        }

        public Changer linkRepeat(string text) => new LinkRepeat(text);
        class LinkRepeat : LinkChanger
        {
            public LinkRepeat(string text) : base(text) {}
            protected override bool HasLinkStyle => true;
            protected override bool Repeat => true;
        }

        public Renderable linkGoto(string text, string passage) => new LinkGoto(text, passage);
        public Renderable linkGoto(string passage) => new LinkGoto(passage, passage);
        class LinkGoto : LinkCommand
        {
            public LinkGoto(string text, string passage) : base(text) {
                Passage = passage;
            }
            public string Passage { get; }
            protected override void OnClick(Context context) => context.GoTo(Passage);
        }

        public Changer linkRevealGoto(string text, string passage) => new LinkRevealGoto(text, passage);
        public Changer linkRevealGoto(string passage) => new LinkRevealGoto(passage, passage);
        class LinkRevealGoto : LinkChanger
        {
            public LinkRevealGoto(string text, string passage) : base(text) => Passage = passage;
            public string Passage { get; }
            protected override bool HasLinkStyle => true;
            protected override void OnClick(Context context) => context.GoTo(Passage);
        }

        public Renderable linkShow(string text, params HookName[] hooks) => new LinkShow(text, hooks);
        class LinkShow : LinkCommand
        {
            protected override bool RemoveLinkStyle => true;
            private readonly HookName[] hooks;
            public LinkShow(string text, HookName[] hooks) : base(text) => this.hooks = hooks;
            protected override void OnClick(Context context) => new Show(hooks).Run(context);
        }

        public Renderable linkUndo(string text) => new LinkUndo(text);
        class LinkUndo : LinkCommand
        {
            public LinkUndo(string text) : base(text) {}
            protected override void OnClick(Context context) => context.Undo();
        }
        
        public Changer click(HookName hook) => new ClickTarget("click", hook);
        public Changer click(string text) => new ClickTarget("click", new ContentSelector(text));
        public Changer mouseover(HookName hook) => new ClickTarget("mouseover", hook);
        public Changer mouseover(string text) => new ClickTarget("mouseover", new ContentSelector(text));
        public Changer mouseout(HookName hook) => new ClickTarget("mouseout", hook);
        public Changer mouseout(string text) => new ClickTarget("mouseout", new ContentSelector(text));

        class ClickTarget : Changer
        {
            private readonly string eventName;
            private readonly Selection selection;
            public ClickTarget(string eventName, Selection selection) {
                this.eventName = eventName;
                this.selection = selection;
            }
            public override void Apply(ref bool? hidden, ref string name) => hidden = true;
            public override void RememberHidden(Context context, IDisposable cursorPosition)
            {
                new Enchant(selection, new ClickLinkShow(eventName, cursorPosition)).Run(context);
            }
            public override void Render(Context context, Action source) => source();
        }
        class ClickLinkShow : Changer
        {
            public ClickLinkShow(string eventName, IDisposable toShow) {
                this.eventName = eventName;
                this.toShow = toShow;
            }
            private readonly string eventName;
            private readonly IDisposable toShow;
            public override void Render(Context context, Action source)
            {
                context.Cursor.PushTag("a", null);
                context.Cursor.SetEvent(eventName, _ => {
                    using (context.Cursor.Save()) {
                        toShow.Dispose();
                        context.Cursor.RunEvent("show");
                    }
                }, false);
                source();
                context.Cursor.Pop();
            }
        }

        public Changer clickAppend(HookName hook) => new ClickRevision("click", hook, AdvanceType.Append);
        public Changer clickAppend(string text) => new ClickRevision("click", new ContentSelector(text), AdvanceType.Append);
        public Changer clickPrepend(HookName hook) => new ClickRevision("click", hook, AdvanceType.Prepend);
        public Changer clickPrepend(string text) => new ClickRevision("click", new ContentSelector(text), AdvanceType.Prepend);
        public Changer clickReplace(HookName hook) => new ClickRevision("click", hook, AdvanceType.Replace);
        public Changer clickReplace(string text) => new ClickRevision("click", new ContentSelector(text), AdvanceType.Replace);
        public Changer mouseoverAppend(HookName hook) => new ClickRevision("mouseover", hook, AdvanceType.Append);
        public Changer mouseoverAppend(string text) => new ClickRevision("mouseover", new ContentSelector(text), AdvanceType.Append);
        public Changer mouseoverPrepend(HookName hook) => new ClickRevision("mouseover", hook, AdvanceType.Prepend);
        public Changer mouseoverPrepend(string text) => new ClickRevision("mouseover", new ContentSelector(text), AdvanceType.Prepend);
        public Changer mouseoverReplace(HookName hook) => new ClickRevision("mouseover", hook, AdvanceType.Replace);
        public Changer mouseoverReplace(string text) => new ClickRevision("mouseover", new ContentSelector(text), AdvanceType.Replace);
        public Changer mouseoutAppend(HookName hook) => new ClickRevision("mouseout", hook, AdvanceType.Append);
        public Changer mouseoutAppend(string text) => new ClickRevision("mouseout", new ContentSelector(text), AdvanceType.Append);
        public Changer mouseoutPrepend(HookName hook) => new ClickRevision("mouseout", hook, AdvanceType.Prepend);
        public Changer mouseoutPrepend(string text) => new ClickRevision("mouseout", new ContentSelector(text), AdvanceType.Prepend);
        public Changer mouseoutReplace(HookName hook) => new ClickRevision("mouseout", hook, AdvanceType.Replace);
        public Changer mouseoutReplace(string text) => new ClickRevision("mouseout", new ContentSelector(text), AdvanceType.Replace);

        class ClickRevision : Changer
        {
            private readonly string eventName;
            private readonly Selection selection;
            private readonly AdvanceType type;
            public ClickRevision(string eventName, Selection selection, AdvanceType type) {
                this.eventName = eventName;
                this.selection = selection;
                this.type = type;
            }
            public override void Render(Context context, Action source) {
                new Enchant(selection, new ClickLinkRevision(eventName, source, type)).Run(context);
            }
        }

        class ClickLinkRevision : Changer
        {
            public ClickLinkRevision(string eventName, Action source, AdvanceType type) {
                this.eventName = eventName;
                this.source = source;
                this.type = type;
            }
            private readonly string eventName;
            private readonly Action source;
            private readonly AdvanceType type;
            public override void Render(Context context, Action source)
            {
                context.Cursor.PushTag("a", null);
                context.Cursor.SetEvent(eventName, cursor => {
                    switch (type) {
                    case AdvanceType.Append:
                        cursor.Pop();
                        break;
                    case AdvanceType.Replace:
                        cursor.DeleteAll();
                        break;
                    case AdvanceType.ReplaceContainer:
                        cursor.DeleteContainer();
                        break;
                    }
                    source();
                }, false);
                source();
                context.Cursor.Pop();
            }
        }

        public Command clickGoto(HookName hook, string target) => new Enchant(hook, new ClickLinkGoto("click", target));
        public Command clickGoto(string text, string target) => new Enchant(new ContentSelector(text), new ClickLinkGoto("click", target));
        public Command mouseoverGoto(HookName hook, string target) => new Enchant(hook, new ClickLinkGoto("mouseover", target));
        public Command mouseoverGoto(string text, string target) => new Enchant(new ContentSelector(text), new ClickLinkGoto("mouseover", target));
        public Command mouseoutGoto(HookName hook, string target) => new Enchant(hook, new ClickLinkGoto("mouseout", target));
        public Command mouseoutGoto(string text, string target) => new Enchant(new ContentSelector(text), new ClickLinkGoto("mouseout", target));

        class ClickLinkGoto : Changer
        {
            public ClickLinkGoto(string eventName, string target) {
                this.eventName = eventName;
                this.target = target;
            }
            private readonly string eventName;
            private readonly string target;
            public override void Render(Context context, Action source)
            {
                context.Cursor.PushTag("a", null);
                context.Cursor.SetEvent(eventName, _ => context.GoTo(target), false);
                source();
                context.Cursor.Pop();
            }
        }

    }
}