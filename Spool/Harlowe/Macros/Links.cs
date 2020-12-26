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
    }
}