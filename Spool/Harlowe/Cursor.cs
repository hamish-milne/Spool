using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Spool {
    public interface Cursor
    {
        string ReadText();
        string ReadTag();
        string ReadTagValue();
        void WriteRaw(string markup);
        void WriteText(string text);
        void PushTag(string tag, string value);
        bool Step(int chars);
        bool MoveToEnd();
        bool Advance();
        bool Pop();
        IDisposable Save();
        bool DeleteAll();
        bool DeleteChars(int chars);
        void Reset();
        void SetEvent(string name, Action<Cursor> action);
    }

    enum AdvanceType
    {
        Prepend,
        Append,
        Replace
    }

    interface Selection
    {
        Selector MakeSelector();
    }

    interface Selector
    {
        bool Advance(Cursor cursor, AdvanceType type);
    }

    class HookNameSelector : Selection, Selector
    {
        public string Name { get; }
        public bool Advance(Cursor cursor, AdvanceType type)
        {
            do {
                if (!cursor.Advance()) {
                    return false;
                }
            } while (cursor.ReadTag() != "name" && cursor.ReadTagValue() != Name);
            cursor.Advance();
            switch (type) {
            case AdvanceType.Append:
                cursor.MoveToEnd();
                break;
            case AdvanceType.Replace:
                cursor.DeleteAll();
                break;
            }
            return true;
        }

        public Selector MakeSelector() => this;
    }

    class ContentSelector : Selection
    {
        public string Text { get; }

        public Selector MakeSelector() => new Inner(this);

        private class Inner : Selector
        {
            public Inner(ContentSelector parent) => this.parent = parent;
            private readonly ContentSelector parent;
            private bool atTextStart;

            public bool Advance(Cursor cursor, AdvanceType type)
            {
                if (atTextStart) {
                    cursor.Step(parent.Text.Length);
                    atTextStart = false;
                }
                int index;
                while ( (index = cursor.ReadText()?.IndexOf(parent.Text) ?? -1) < 0 ) {
                    if (!cursor.Advance()) {
                        return false;
                    }
                }
                cursor.Step(index);
                switch (type) {
                case AdvanceType.Append:
                    cursor.Step(parent.Text.Length);
                    break;
                case AdvanceType.Replace:
                    cursor.DeleteChars(parent.Text.Length);
                    break;
                case AdvanceType.Prepend:
                    atTextStart = true;
                    break;
                }
                return true;
            }
        }
    }

    class CombinedSelector : Selection
    {
        private readonly Selector[] arguments;

        public Selector MakeSelector() => new Inner(arguments);

        private class Inner : Selector
        {
            public Inner(IEnumerable<Selector> arguments) => index = arguments.GetEnumerator();
            private IEnumerator<Selector> index;

            public bool Advance(Cursor cursor, AdvanceType type)
            {
                while (!index.Current.Advance(cursor, type)) {
                    if (index.MoveNext() == false) {
                        return false;
                    }
                }
                return true;
            }
        }
    }


    public class XCursor : Cursor
    {
        public XDocument Root { get; } = new XDocument(new XElement("tw-passage"));
        private XContainer parent;
        private XNode current;
        public int charIndex;

        public string ReadText() => (current as XText)?.Value?.Substring(charIndex);

        public string ReadTag() => (current as XElement)?.Name?.LocalName;

        public string ReadTagValue() => (current as XElement)?.Attribute(XName.Get("value"))?.Value;

        public void WriteRaw(string markup)
        {
            throw new NotImplementedException();
        }

        public void WriteText(string text)
        {
            if (current is XText tnode) {
                tnode.Value = tnode.Value.Insert(charIndex, text);
                charIndex += text.Length;
            } else if ((current?.PreviousNode ?? parent.LastNode) is XText tnode1) {
                tnode1.Value += text;
            }
        }

        public void PushTag(string tag, string value)
        {
            var el = new XElement(XName.Get(tag));
            el.SetAttributeValue(XName.Get("value"), value);
            if (current != null) {
                current.AddAfterSelf(el);
            } else {
                parent.Add(el);
            }
            parent = el;
            current = null;
        }

        public bool Step(int chars)
        {
            if (current is XText tnode) {
                charIndex = Math.Max(0, Math.Min(tnode.Value.Length, charIndex + chars));
                return true;
            }
            return false;
        }

        public bool MoveToEnd()
        {
            if (current != null) {
                current = null;
                return true;
            }
            return false;
        }

        public bool Advance()
        {
            charIndex = 0;
            if (current != null) {
                current = current.NextNode;
            }
            if (current == null) {
                do {
                    current = current.Parent;
                    if (current == null) {
                        return false;
                    }
                } while (current.NextNode == null);
            }
            return true;
        }

        public bool Pop()
        {
            if (parent.Parent == null) {
                return false;
            }
            current = parent.NextNode;
            parent = parent.Parent;
            return true;
        }

        public IDisposable Save()
        {
            throw new NotImplementedException();
        }

        public bool DeleteAll()
        {
            while (current != null) {
                var x = current;
                current = x.NextNode;
                x.Remove();
            }
            return true;
        }

        public bool DeleteChars(int chars)
        {
            if (current is XText tnode) {
                chars = Math.Min(chars, tnode.Value.Length - charIndex);
                tnode.Value = tnode.Value.Remove(charIndex, chars);
                return true;
            }
            return false;
        }

        public void Reset()
        {
            parent = Root.Root;
            current = null;
            charIndex = 0;
        }

        public void SetEvent(string name, Action<Cursor> action)
        {
            throw new NotImplementedException();
        }
    }
}