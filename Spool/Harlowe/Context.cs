using System;
using System.Collections.Generic;
using System.Linq;

namespace Spool.Harlowe
{
    public class Language : Spool.Language
    {
        public Spool.Context Run(Story story, Cursor output)
            => new Context(story, output);

        public bool Supports(string format, string version)
            => format == "Harlowe" && version.CompareTo("3.1.0") <= 0;
    }

    public interface PostProcessor
    {
        void PostProcess(Context context);
    }

    public class Context : Spool.Context
    {
        public Context(Story story, Cursor output)
        {
            MacroProvider = new BuiltInMacros(this);
            Cursor = output;
            Story = story;
        }

        private readonly Dictionary<string, Renderable> passageBody = new Dictionary<string, Renderable>();
        private readonly List<string> history = new List<string>();

        public IDictionary<string, Data> Locals { get; } = new Dictionary<string, Data>();
        public IDictionary<string, Data> Globals { get; } = new Dictionary<string, Data>();
        public string CurrentPassage { get; private set; }
        public Story Story { get; }
        public Cursor Cursor { get; }
        public bool? PreviousCondition { get; set; }
        public object MacroProvider { get; }
        public Random Random { get; } = new Random();
        public List<PostProcessor> PostProcessors { get; } = new List<PostProcessor>();

        public IEnumerable<string> History => history;
        public int Visits(string passage) => history.Count(x => x == passage);

        private bool isRendering;

        public Renderable GetPassageBody(string passage)
        {
            if (!passageBody.TryGetValue(passage, out var body)) {
                body = Lexico.Lexico.Parse<Block>(Story.GetPassage(passage));
                passageBody.Add(passage, body);
            }
            return body;
        }

        public void GoTo(string passage)
        {
            // TODO: Does goto-self work?
            if (passage == CurrentPassage) {
                return;
            }
            history.Add(CurrentPassage);
            CurrentPassage = passage;
            if (isRendering) {
                return;
            }
            isRendering = true;
            try {
                string previous;
                // Loop here in case we did a (goto:) or similar
                do {
                    previous = CurrentPassage;
                    Cursor.Reset();
                    Cursor.DeleteAll();
                    GetPassageBody(CurrentPassage).Render(this);
                    Cursor.Flush();
                    foreach (var p in PostProcessors) {
                        p.PostProcess(this);
                    }
                } while (CurrentPassage != previous);
            } finally {
                isRendering = false;
            }
        }

        public void Start() => GoTo(Story.Start);

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

        public string Save()
        {
            throw new NotImplementedException();
        }

        public void Load(string savestate)
        {
            throw new NotImplementedException();
        }

        public void Undo() => throw new NotImplementedException();

    }

    public abstract class Renderable : Data
    {
        protected override object GetObject() => this;
        protected override string GetString() => $"a {GetType().Name} renderable";
        public override bool Serializable => true;
        public abstract void Render(Context context);
    }
}