using System.IO;
using System.Xml;
using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Spool.Harlowe
{
    public class Context
    {
        public Context()
        {
            var story = new XElement(XName.Get("tw-story"));
            var passage = new XElement(XName.Get("tw-passage"));
            story.Add(passage);
            var sidebar = new XElement(XName.Get("tw-sidebar"));
            story.Add(sidebar);
            Screen.Add(story);
            Cursor = passage;
            MacroProvider = new BuiltInMacros(this);
        }

        public IDictionary<string, object> Locals { get; } = new Dictionary<string, object>();
        public IDictionary<string, object> Globals { get; } = new Dictionary<string, object>();
        public IDictionary<XContainer, Renderable> Hidden { get; } = new Dictionary<XContainer, Renderable>();
        public IDictionary<string, Renderable> Passages { get; } = new Dictionary<string, Renderable>();
        public XDocument Screen { get; } = new XDocument();
        public XContainer Cursor { get; private set; }
        public CursorPos Position { get; private set; } = CursorPos.Child;
        public bool? PreviousCondition { get; set; }
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

        class MyXmlReader : XmlTextReader
        {
            public MyXmlReader(TextReader tr) : base(tr) {}
            public override void ResolveEntity()
            {
                return;
            }
        }

        public void AddText(string text)
        {
            // TODO: Support HTML entities
            var reader = new MyXmlReader(new StringReader($"<x>{text}</x>"))
            {
                EntityHandling = EntityHandling.ExpandCharEntities
            };
            reader.Read();
            var node = (XContainer)XNode.ReadFrom(reader);
            foreach (var readNode in node.Nodes())
            {
                if (readNode is XText tread && PreviousNode is XText tnode) {
                    tnode.Value += tread.Value;
                } else {
                    AddNode(readNode);
                }
            }
        }
    }

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

    public interface Renderable
    {
        void Render(Context context);
    }
}