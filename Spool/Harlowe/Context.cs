using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Spool.Harlowe
{

    public class Passage
    {
        public string Name { get; }
        public Renderable Body { get; }
        public int Visits { get; }
    }

    // public class Cursor
    // {
    //     public XContainer Parent { get; }
    //     public int Index { get; }
    // }

    public class Context
    {
        public Context()
        {
            // var story = new XElement(XName.Get("tw-story"));
            // var passage = new XElement(XName.Get("tw-passage"));
            // story.Add(passage);
            // var sidebar = new XElement(XName.Get("tw-sidebar"));
            // story.Add(sidebar);
            // Screen.Add(story);
            // Cursor = passage;
            MacroProvider = new BuiltInMacros(this);
            Cursor = new XCursor();
            Cursor.Reset();
        }

        public IDictionary<string, Data> Locals { get; } = new Dictionary<string, Data>();
        public IDictionary<string, Data> Globals { get; } = new Dictionary<string, Data>();
        // public IDictionary<XContainer, Renderable> Hidden { get; } = new Dictionary<XContainer, Renderable>();
        public Passage Passage { get; } = new Passage();
        public IDictionary<string, Passage> Passages { get; } = new Dictionary<string, Passage>();
        // public XDocument Screen { get; } = new XDocument();
        // private XContainer Cursor { get; set; }
        // public CursorPos Position { get; private set; } = CursorPos.Child;
        public bool? PreviousCondition { get; set; }
        public object MacroProvider { get; }
        public Random Random { get; } = new Random();
        public Cursor Cursor { get; }

        // public (XContainer, CursorPos) Push(XContainer cursor, CursorPos cursorPos)
        // {
        //     var state = (Cursor, Position);
        //     Cursor = cursor;
        //     Position = cursorPos;
        //     return state;
        // }
        // public void Pop((XContainer, CursorPos) state)
        // {
        //     Cursor = state.Item1;
        //     Position = state.Item2;
        // }

        // TODO: Clean this up a bit
        public bool? NewCondition()
        {
            var state = PreviousCondition;
            PreviousCondition = null;
            return state;
        }
        public void PopCondition(bool? state)
        {
            PreviousCondition = state;
        }

        // private XNode PreviousNode => Position switch {
        //     CursorPos.Child => Cursor.LastNode,
        //     CursorPos.Before => Cursor.PreviousNode,
        //     _ => throw new ArgumentOutOfRangeException()
        // };

        // public void AddNode(XNode node)
        // {
        //     switch (Position) {
        //         case CursorPos.Before: Cursor.AddBeforeSelf(node); break;
        //         case CursorPos.Child: Cursor.Add(node); break;
        //     }
        // }

        // class MyXmlReader : XmlTextReader
        // {
        //     public MyXmlReader(TextReader tr) : base(tr) {}
        //     public override void ResolveEntity()
        //     {
        //         return;
        //     }
        // }

        // public void AddText(string text)
        // {
        //     // TODO: Support HTML entities
        //     var reader = new MyXmlReader(new StringReader($"<x>{text}</x>"))
        //     {
        //         EntityHandling = EntityHandling.ExpandCharEntities
        //     };
        //     reader.Read();
        //     var node = (XContainer)XNode.ReadFrom(reader);
        //     foreach (var readNode in node.Nodes())
        //     {
        //         if (readNode is XText tread && PreviousNode is XText tnode) {
        //             tnode.Value += tread.Value;
        //         } else {
        //             AddNode(readNode);
        //         }
        //     }
        // }

        public Expression It { get; set; }
    }

    // public enum CursorPos
    // {
    //     Child,
    //     Before
    // }

    public enum RenderFlags
    {
        None = 0,
        CollapseWhitespace = (1 << 0)
    }

    public abstract class Renderable : Data
    {
        protected override object GetObject() => this;
        protected override string GetString() => $"a {GetType().Name} renderable";
        public override bool Serializable => true;
        public abstract void Render(Context context);
    }
}