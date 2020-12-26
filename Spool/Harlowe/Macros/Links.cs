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
                }, Repeat);
                context.Cursor.Pop();
                if (Repeat) {
                    context.Cursor.Pop();
                }
            }
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
    }
}