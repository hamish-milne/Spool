using System.IO;
using System;
using System.Xml;


namespace Spool
{

    enum TextElement
    {
        StartTag,
        EndTag,
        Text,
        EOF
    }

    interface Renderer
    {
        void Push(string id);
        void Pop();
        void Write(string text);
        void Skip();
        void Delete();
        TextElement Peek();
        string PeekText();
        string PeekTag();
        event Action<string> OnClick;
    }

    class RichTextRenderer : Renderer
    {
        public virtual string LinkElement => "link";

        public event Action<string> OnClick;

        readonly MemoryStream stream;
        readonly XmlReader reader;
        readonly XmlWriter writer;

        public void Delete()
        {
            var start = (int)stream.Position;
            var toDelete = reader.MoveToContent() switch
            {
                XmlNodeType.Text => reader.ReadContentAsString().Length,
                XmlNodeType.Element => reader.ReadOuterXml().Length,
                _ => 0
            };
            if (toDelete <= 0) {
                return;
            }
            var buf = stream.GetBuffer();
            buf[(start + toDelete)..].CopyTo((Memory<byte>)buf[start..]);
            stream.SetLength(stream.Length - toDelete);

        }

        public TextElement Peek() => reader.MoveToContent() switch {
            XmlNodeType.Element => TextElement.StartTag,
            XmlNodeType.EndElement => TextElement.EndTag,
            XmlNodeType.Text => TextElement.Text,
            XmlNodeType.None => TextElement.EOF,
            _ => throw new InvalidOperationException("Invalid XML")
        };

        public string PeekTag() => reader.GetAttribute("id");

        public string PeekText() => reader.Value;

        public void Pop() => writer.WriteEndElement();

        public void Push(string id) => writer.WriteStartElement(LinkElement);

        public void Skip() => reader.Read();

        public void Write(string text) => writer.WriteString(text);
    }
}