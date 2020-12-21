using System;
using System.Collections.Generic;

namespace Spool.Harlowe
{

    public class Passage
    {
        public Passage(string name, Renderable body)
        {
            Name = name;
            Body = body;
        }
        public string Name { get; }
        public Renderable Body { get; }
        public int Visits { get; }
    }

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
        public Passage Passage { get; set; }
        public IDictionary<string, Passage> Passages { get; } = new Dictionary<string, Passage>();
        public bool? PreviousCondition { get; set; }
        public object MacroProvider { get; }
        public Random Random { get; } = new Random();
        public Cursor Cursor { get; }

        public void AddPassage(string name, Renderable body)
        {
            Passages[name] = new Passage(name, body);
        }

        private bool isRendering;

        public void GoTo(string passage)
        {
            Passage = Passages[passage];
            if (isRendering) {
                return;
            }
            isRendering = true;
            try {
                Passage currentPassage;
                // Loop here in case we did a (goto:) or similar
                do {
                    currentPassage = Passage;
                    Cursor.Reset();
                    Cursor.DeleteAll();
                    Passage.Body.Render(this);
                } while (Passage != currentPassage);
            } finally {
                isRendering = false;
            }
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

        public Expression It { get; set; }

        public RenderFlags Flags { get; private set; }

        public RenderFlags PushFlags(RenderFlags flags)
        {
            var prev = Flags;
            Flags |= flags;
            return prev;
        }

        public void PopFlags(RenderFlags flags)
        {
            Flags = flags;
        }
    }

    public abstract class Renderable : Data
    {
        protected override object GetObject() => this;
        protected override string GetString() => $"a {GetType().Name} renderable";
        public override bool Serializable => true;
        public abstract void Render(Context context);
    }
}