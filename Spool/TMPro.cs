

using System;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Spool
{
    public class TMPro : XCursor
    {
        private long nodeId;
        public override void SetEvent(string name, Action<Cursor> action)
        {
            if (Parent.Annotation<string>() == null) {
                Parent.AddAnnotation((++nodeId).ToString());
            }
            base.SetEvent(name, action);
        }

        public string Display()
        {
            var sb = new StringBuilder();
            Display(sb, Root);
            return sb.ToString();
        }

        public T GetEvent<T>(string linkId) where T : class
        {
            var node = Root.DescendantNodes().First(x => x.Annotation<string>() == linkId);
            return node.Annotation<T>();
        }

        private void Display(StringBuilder sb, XNode node)
        {
            if (node is XText text) {
                sb.Append(text.Value);
            } else if (node is XElement el) {
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
                    _ => false
                };
                
                if (applyTag) {
                    sb.Append('<').Append(tag);
                    var value = el.Attribute(XName.Get("value"))?.Value;
                    if (value != null) {
                        sb.Append("=\"").Append(value).Append('"');
                    }
                    sb.Append('>');
                }
                var linkId = el.Annotation<string>();
                if (linkId != null) {
                    sb.Append("<link=\"").Append(linkId).Append("\">");
                }
                foreach (var n in el.Nodes()) {
                    Display(sb, n);
                }
                if (linkId != null) {
                    sb.Append("</link>");
                }
                if (applyTag) {
                    sb.Append("</").Append(tag).Append('>');
                }
            } else if (node is XContainer c) {
                foreach (var n in c.Nodes()) {
                    Display(sb, n);
                }
            } else {
                throw new NotSupportedException(node.GetType().ToString());
            }
        }
    }
}