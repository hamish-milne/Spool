using System.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace Spool
{
    public enum RenderFlags
    {
        None = 0,
        CollapseWhitespace = (1 << 0)
    }
    public interface Cursor
    {
        string ReadText();
        string ReadTag();
        string ReadTagValue();
        void Flush();
        void WriteRaw(string markup);
        void WriteText(string text);
        void PushTag(string tag, string value);
        bool CheckParentTags(string tag, string value);
        bool Step(int chars);
        bool MoveToEnd();
        bool Advance();
        bool Pop();
        IDisposable Save();
        Action DeleteContainer();
        Action DeleteAll();
        bool DeleteChars(int chars);
        void Reset();
        void SetEvent(string name, Action<Cursor> action, bool repeat);
        void RunEvent(string name);
    }

    enum AdvanceType
    {
        Prepend,
        Append,
        Replace,
        ReplaceContainer
    }

    interface Selection
    {
        Selector MakeSelector();
    }

    interface Selector
    {
        bool Advance(Cursor cursor, AdvanceType type, Func<Cursor, bool> condition);
        void ReplaceSource(Cursor cursor);
    }

    class HookNameSelector : Selection, Selector
    {
        private Action replaceSource;
        public HookNameSelector(string name) => Name = name;
        public string Name { get; }
        public bool Advance(Cursor cursor, AdvanceType type, Func<Cursor, bool> condition)
        {
            while (cursor.ReadTag() != "name" && cursor.ReadTagValue() != Name && condition?.Invoke(cursor) != false) {
                if (!cursor.Advance()) {
                    return false;
                }
            }
            cursor.Advance();
            switch (type) {
            case AdvanceType.Append:
                cursor.MoveToEnd();
                break;
            case AdvanceType.ReplaceContainer:
                replaceSource = cursor.DeleteContainer();
                break;
            case AdvanceType.Replace:
                replaceSource = cursor.DeleteAll();
                break;
            }
            return true;
        }

        public Selector MakeSelector() => this;

        public void ReplaceSource(Cursor cursor) => replaceSource();
    }

    class ContentSelector : Selection
    {
        public ContentSelector(string text) => this.text = new []{text};
        public ContentSelector(IEnumerable<string> text) => this.text = text.ToArray();
        private string[] text;

        public Selector MakeSelector() => new Inner(this);

        private class Inner : Selector
        {
            public Inner(ContentSelector parent) => this.parent = parent;
            private readonly ContentSelector parent;
            private string atTextStart;
            private string textToReplace;

            public bool Advance(Cursor cursor, AdvanceType type, Func<Cursor, bool> condition)
            {
                if (atTextStart != null) {
                    cursor.Step(atTextStart.Length);
                    atTextStart = null;
                }
                int index = 0;
                string foundText = null;
                while (foundText == null) {
                    foreach (var text in parent.text) {
                        index = cursor.ReadText()?.IndexOf(text) ?? -1;
                        if (index >= 0 && condition?.Invoke(cursor) != false) {
                            foundText = text;
                            break;
                        }
                    }
                    if (foundText == null && !cursor.Advance()) {
                        return false;
                    }
                }
                cursor.Step(index);
                switch (type) {
                case AdvanceType.Append:
                    cursor.Step(foundText.Length);
                    break;
                case AdvanceType.ReplaceContainer:
                case AdvanceType.Replace:
                    cursor.DeleteChars(foundText.Length);
                    textToReplace = foundText;
                    break;
                case AdvanceType.Prepend:
                    atTextStart = foundText;
                    break;
                }
                return true;
            }

            public void ReplaceSource(Cursor cursor) => cursor.WriteText(textToReplace);
        }
    }

    class CombinedSelector : Selection
    {
        public static Selection Create(IEnumerable<Selection> arguments)
        {
            var argArray = arguments.ToArray();
            if (argArray.Length == 1) {
                return argArray[0];
            }
            return new CombinedSelector(argArray);
        }
        private CombinedSelector(Selection[] arguments) {
            this.arguments = arguments;
        }
        private readonly Selection[] arguments;

        public Selector MakeSelector() => new Inner(arguments.Select(x => x.MakeSelector()));

        private class Inner : Selector
        {
            public Inner(IEnumerable<Selector> arguments) => index = arguments.GetEnumerator();
            private IEnumerator<Selector> index;

            public bool Advance(Cursor cursor, AdvanceType type, Func<Cursor, bool> condition)
            {
                if (index.Current == null) {
                    index.MoveNext();
                }
                while (!index.Current.Advance(cursor, type, condition)) {
                    if (index.MoveNext() == false) {
                        return false;
                    }
                }
                return true;
            }

            public void ReplaceSource(Cursor cursor) => index.Current.ReplaceSource(cursor);
        }
    }


    public class XCursor : Cursor
    {
        public delegate void ClickEvent();
        public delegate void ShowEvent();
        public delegate void MouseInEvent();
        public XDocument Root { get; } = new XDocument(new XElement("tw-passage"));
        private XContainer parent;
        private XNode current;
        private int charIndex;

        public XNode Current => current;
        public XContainer Parent => parent;

        public string ReadText() => (current as XText)?.Value?.Substring(charIndex);

        public string ReadTag() => (current as XElement)?.Name?.LocalName;

        public string ReadTagValue() => (current as XElement)?.Attribute(XName.Get("value"))?.Value;

        private void InsertNode(XNode node)
        {
            if (current is XText text) {
                // Normalize cursor position - prevents creating empty text nodes
                if (charIndex >= text.Value.Length) {
                    current = current.NextNode;
                    charIndex = 0;
                }
                if (node is XText text2) {
                    WriteTextInternal(text2.Value);
                    return;
                } else if (charIndex > 0) {
                    // Split text into two and put the new node in the middle
                    var splitText = new XText(text.Value.Substring(charIndex));
                    text.Value = text.Value.Substring(0, charIndex);
                    text.AddAfterSelf(splitText);
                    current = splitText;
                    charIndex = 0;
                }
            }
            if (current != null) {
                current.AddBeforeSelf(node);
            } else {
                parent.Add(node);
            }
        }

        private readonly StringBuilder markupBuffer = new StringBuilder();

        public void WriteRaw(string markup)
        {
            markupBuffer.Append(markup);
        }

        public void Flush()
        {
            var markup = markupBuffer.ToString();
            markupBuffer.Clear();
            // Markup consisting solely of whitespace is ignored by XmlReader
            if (string.IsNullOrWhiteSpace(markup)) {
                WriteTextInternal(markup);
                return;
            }
            // TODO: Support HTML entities
            var xr = XmlReader.Create(new StringReader(markup), new XmlReaderSettings {
                ConformanceLevel = ConformanceLevel.Fragment
            });
            xr.MoveToContent();
            XNode node;
            while (!xr.EOF && (node = XNode.ReadFrom(xr)) != null)
            {
                InsertNode(node);
            }
        }

        public void WriteText(string text)
        {
            markupBuffer.Append(new XText(text).ToString(SaveOptions.DisableFormatting));
        }

        private void WriteTextInternal(string text)
        {
            if (string.IsNullOrEmpty(text)) {
                return;
            }
            if (current is XText tnode) {
                tnode.Value = tnode.Value.Insert(charIndex, text);
                charIndex += text.Length;
            } else if ((current?.PreviousNode ?? parent.LastNode) is XText tnode1) {
                tnode1.Value += text;
            } else {
                InsertNode(new XText(text));
            }
        }

        public void PushTag(string tag, string value)
        {
            Flush();
            var el = new XElement(XName.Get(tag));
            el.SetAttributeValue(XName.Get("value"), value);
            InsertNode(el);
            parent = el;
            current = null;
        }

        public bool CheckParentTags(string tag, string value)
        {
            Flush();
            return new []{parent}.Concat(parent.Ancestors()).OfType<XElement>()
                .Any(x => x.Name == XName.Get(tag) && x.Attribute(XName.Get("value"))?.Value == value);
        }

        public bool Step(int chars)
        {
            Flush();
            if (current is XText tnode) {
                charIndex = Math.Max(0, Math.Min(tnode.Value.Length, charIndex + chars));
                return true;
            }
            return false;
        }

        public bool MoveToEnd()
        {
            Flush();
            if (current != null) {
                current = null;
                return true;
            }
            return false;
        }

        public bool Advance()
        {
            Flush();
            charIndex = 0;
            if (current is XContainer container) {
                parent = container;
                current = parent.FirstNode;
            } else if (current != null) {
                current = current.NextNode;
            } else if (parent.Parent == null) {
                return false;
            } else {
                current = parent.NextNode;
                parent = parent.Parent;
            }
            return true;
        }

        public bool Pop()
        {
            Flush();
            if (parent.Parent == null) {
                return false;
            }
            current = parent.NextNode;
            parent = parent.Parent;
            return true;
        }

        class SavePoint : IDisposable
        {
            public SavePoint(XCursor parent) {
                this.parent = parent;
                state = (parent.parent, parent.current, parent.charIndex);
            }
            XCursor parent;
            (XContainer, XNode, int) state;

            public void Dispose()
            {
                (parent.parent, parent.current, parent.charIndex) = state;
            }
        }

        public IDisposable Save() {
            Flush();
            return new SavePoint(this);
        }

        public Action DeleteAll()
        {
            Flush();
            var items = new Queue<XNode>();
            while (current != null) {
                var x = current;
                current = x.NextNode;
                items.Enqueue(x);
                x.Remove();
            }
            return () => {
                Flush();
                while (items.Count > 0) {
                    InsertNode(items.Dequeue());
                }
            };
        }

        public Action DeleteContainer()
        {
            Flush();
            var toRemove = parent;
            if (Pop()) {
                toRemove.Remove();
                return () => {
                    Flush();
                    InsertNode(toRemove);
                };
            }
            throw new InvalidOperationException("At root");
        }

        public bool DeleteChars(int chars)
        {
            Flush();
            if (current is XText tnode) {
                chars = Math.Min(chars, tnode.Value.Length - charIndex);
                tnode.Value = tnode.Value.Remove(charIndex, chars);
                return true;
            }
            return false;
        }

        public void Reset()
        {
            Flush();
            parent = Root.Root;
            current = parent.FirstNode;
            charIndex = 0;
        }

        public virtual void SetEvent(string name, Action<Cursor> action, bool repeat)
        {
            Flush();
            var cParent = parent;
            switch (name) {
                case "click":
                // TODO: Make better use of RunEvent
                    parent.AddAnnotation(new ClickEvent(() => {
                        using (Save()) {
                            parent = cParent;
                            current = cParent.FirstNode;
                            charIndex = 0;
                            action(this);
                            if (!repeat) {
                                cParent.RemoveAnnotations<ClickEvent>();
                            }
                            Flush();
                        }
                    }));
                    break;
                case "show":
                    parent.AddAnnotation(new ShowEvent(() => {
                        using (Save()) {
                            parent = cParent;
                            current = cParent.FirstNode;
                            charIndex = 0;
                            action(this);
                            cParent.RemoveAnnotations<ShowEvent>();
                            Flush();
                        }
                    }));
                    break;
            }
        }

        public void RunEvent(string name)
        {
            Flush();
            switch (name) {
                case "click":
                    parent.Annotation<ClickEvent>()?.Invoke();
                    break;
                case "show":
                    parent.Annotation<ShowEvent>()?.Invoke();
                    break;
            }
        }
    }
}