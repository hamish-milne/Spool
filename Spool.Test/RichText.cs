using System.IO;
using System;
using Xunit;
using System.Xml.Linq;
using Xunit.Abstractions;
using System.Collections.Generic;
using System.Collections;

namespace Spool.Test
{
    public class RichTextTest
    {
        [Fact]
        public void TMPro()
        {
            var cursor = new XCursor();
            var context = new Harlowe.Context(new ListStory {
                {"Passage 1", "[[Text->Passage 2]]"},
                {"Passage 2", "More text"}
            }, cursor);
            context.GoTo("Passage 1");
            var formatter = new TMPro();
            var state = formatter.Display(cursor.Root);
            Assert.Equal("<link=\"1\">Text</link>", state.Text);
            state.Event<XCursor.ClickEvent>("1");
            var state2 = formatter.Display(cursor.Root);
            Assert.Equal("More text", state2.Text);
        }

        [Fact]
        public void UGUI()
        {
            var cursor = new XCursor();
            var context = new Harlowe.Context(new ListStory {
                {"Passage 1", "[[Text->Passage 2]] - Not a link"},
                {"Passage 2", "More text"}
            }, cursor);
            context.GoTo("Passage 1");
            var formatter = new UGUI();
            var state = formatter.Display(cursor.Root);
            Assert.Equal("Text - Not a link", state.Text);
            state.Event<XCursor.ClickEvent>(10);
            state.Event<XCursor.ClickEvent>(1);
            var state2 = formatter.Display(cursor.Root);
            Assert.Equal("More text", state2.Text);
        }
    }
}