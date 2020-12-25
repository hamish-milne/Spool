using System;

namespace Spool.Harlowe
{
    partial class BuiltInMacros
    {
        class StyleChanger : Changer
        {
            public StyleChanger(string Tag, string value)
            {
                this.Tag = Tag;
                Value = value;
            }
            public string Tag { get; }
            public string Value { get; }

            public override void Render(Context context, Action source)
            {
                context.Cursor.PushTag(Tag, Value);
                source();
                context.Cursor.Pop();
            }
        }

        public Changer font(string font) => new StyleChanger("font", font);
        public Changer textColour(string color) => new StyleChanger("color", color);
        public Changer textColor(string color) => new StyleChanger("color", color);
    }
}