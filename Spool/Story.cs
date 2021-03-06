

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace Spool
{
    public interface Story
    {
        string GetPassage(string name);
        (int, int) GetPassagePosition(string name);
        IEnumerable<string> GetTags(string name);
        IEnumerable<string> PassageNames { get; }
        Context Run(Cursor output);
        string Start { get; }

    }
    public class HtmlStory : Story
    {
        private readonly XElement story;

        public HtmlStory(TextReader reader)
        {
            using var xml = new Sgml.SgmlReader {
                DocType = "HTML",
                WhitespaceHandling = WhitespaceHandling.All,
                CaseFolding = Sgml.CaseFolding.ToLower,
                InputStream = reader
            };
            xml.MoveToContent();
            var doc = (XContainer)XNode.ReadFrom(xml);
            story = doc.Element(XName.Get("body")).Element(XName.Get("tw-storydata"));
        }

        public string GetPassage(string name)
            => story.Elements(XName.Get("tw-passagedata"))
            .First(x => x.Attribute(XName.Get("name")).Value == name)
            .Value;

        public (int,int) GetPassagePosition(string name)
        {
            var pos = story.Elements(XName.Get("tw-passagedata"))
                .First(x => x.Attribute(XName.Get("name")).Value == name)
                .Attribute(XName.Get("position")).Value.Split(',');
            return (int.Parse(pos[0]), int.Parse(pos[1]));
        }

        public IEnumerable<string> GetTags(string passage)
        {
            return story.Elements(XName.Get("tw-passagedata"))
                .First(x => x.Attribute(XName.Get("name")).Value == passage)
                .Attribute(XName.Get("tags")).Value.Split(' ');
        }

        public IEnumerable<string> PassageNames => story.Elements(XName.Get("tw-passagedata"))
            .Select(p => p.Attribute(XName.Get("name")).Value);

        private static readonly Language[] languages =
        {
            new Harlowe.Language()
        };

        public Context Run(Cursor output)
        {
            var format = story.Attribute(XName.Get("format")).Value;
            var formatVersion = story.Attribute(XName.Get("format-version")).Value;
            var l = languages.FirstOrDefault(x => x.Supports(format, formatVersion)) 
                ?? throw new Exception($"No support for {format} version {formatVersion}");
            return l.Run(this, output);
        }

        public string Start {
            get {
                var start = story.Attribute(XName.Get("startnode")).Value;
                return story.Elements(XName.Get("tw-passagedata"))
                    .First(x => x.Attribute(XName.Get("pid")).Value == start)
                    .Attribute(XName.Get("name")).Value;
            }
        }
    }
}