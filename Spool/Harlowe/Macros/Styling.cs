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
        public Changer textStyle(string style) => style switch {
            "none" => NullChanger.Instance,
            "bold" => new StyleChanger("b", null),
            "italic" => new StyleChanger("i", null),
            "underline" => new StyleChanger("u", null),
            "strike" => new StyleChanger("s", null),
            "superscript" => new StyleChanger("sup", null),
            "subscript" => new StyleChanger("sub", null),
            "mark" => new StyleChanger("mark", null),
            _ => new StyleChanger(style, null)
        };
    }
}