

using System.Collections.Generic;
using System;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Spool
{
    public abstract class RichText<TState>
    {
        protected string GetText(TState state, XNode root)
        {
            var sb = new StringBuilder();
            Display(sb, root, state);
            return sb.ToString();
        }

        protected abstract bool QuoteTagValues { get; }

        protected abstract IEnumerable<(string, string)> GetTagsForNode(XNode node, TState state);
        private void Display(StringBuilder sb, XNode node, TState state)
        {
            var tags = GetTagsForNode(node, state).ToList();
            foreach (var (tag, value) in tags) {
                sb.Append('<').Append(tag);
                if (value != null) {
                    sb.Append('=');
                    if (QuoteTagValues) {
                        sb.Append('"').Append(value.Replace("\"", "\\\"")).Append('"');
                    } else {
                        sb.Append(value);
                    }
                }
                sb.Append('>');
            }
            if (node is XText text) {
                sb.Append(text.Value);
            } else if (node is XContainer c) {
                foreach (var n in c.Nodes()) {
                    Display(sb, n, state);
                }
            }
            tags.Reverse();
            foreach (var (tag, _) in tags) {
                sb.Append("</").Append(tag).Append(">");
            } 
        }
    }

    public class TMPro : RichText<TMPro.State>
    {
        public class State
        {
            internal readonly Dictionary<string, XNode> map
                = new Dictionary<string, XNode>();
            
            public string Text { get; }

            internal long nodeId;

            public void Event<T>(string linkId) where T : Delegate
            {
                map[linkId].Annotation<T>()?.DynamicInvoke(Array.Empty<object>());
            }

            internal State(TMPro parent, XNode root) {
                Text = parent.GetText(this, root);
            }
        }

        public State Display(XNode root) => new State(this, root);

        protected override bool QuoteTagValues => true;

        protected override IEnumerable<(string, string)> GetTagsForNode(XNode node, State state)
        {
            if (node is XElement el) {
                
                var tag = el.Name.LocalName;
                var applyTag = tag switch {
                    "font" => true,
                    "color" => true,
                    "s" => true,
                    "u" => true,
                    "b" => true,
                    "i" => true,
                    "sub" => true,
                    "sup" => true,
                    "mark" => true,
                    _ => false
                };
                if (applyTag) {
                    yield return (tag, el.Attribute(XName.Get("value"))?.Value);
                }
                if (el.Annotations<Delegate>().Any()) {
                    state.nodeId++;
                    state.map[state.nodeId.ToString()] = node;
                    yield return ("link", state.nodeId.ToString());
                }
            }
        }
    }
    public class UGUI : RichText<UGUI.State>
    {
        protected override bool QuoteTagValues => false;

        protected override IEnumerable<(string, string)> GetTagsForNode(XNode node, State state)
        {
            if (node is XElement el) {
                var tag = el.Name.LocalName;
                var applyTag = tag switch {
                    "color" => true,
                    "b" => true,
                    "i" => true,
                    "size" => true,
                    _ => false
                };
                if (applyTag) {
                    yield return (tag, el.Attribute(XName.Get("value"))?.Value);
                }
            } else if (node is XText text) {
                state.flatTextNodes.Add(text);
            }
        }

        public State Display(XNode root) => new State(this, root);

        public class State
        {
            internal readonly List<XText> flatTextNodes = new List<XText>();

            public string Text { get; }

            public void Event<T>(int charIndex) where T : Delegate
            {
                foreach (var t in flatTextNodes) {
                    if (charIndex >= t.Value.Length) {
                        charIndex -= t.Value.Length;
                    } else {
                        t.Ancestors()
                            .Select(x => x.Annotation<T>())
                            .FirstOrDefault(x => x != null)
                            ?.DynamicInvoke(Array.Empty<object>());
                        return;
                    }
                }
                throw new IndexOutOfRangeException();
            }

            internal State(UGUI parent, XNode root) {
                Text = parent.GetText(this, root);
            }
        }
    }
}