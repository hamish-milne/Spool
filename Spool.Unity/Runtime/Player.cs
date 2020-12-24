using UnityEngine;
using System.IO;
using System.Xml.Linq;

namespace Spool
{
    public class Player : MonoBehaviour
    {
        public TextAsset Story;
        private Context context;
        private XCursor output = new XCursor();
        public XNode Content => output.Root;

        protected void Start()
        {
            var story = new HtmlStory(new StringReader(Story.text));
            context = story.Run(output);
            context.Start();
            SendMessage("StoryStart");
        }
    }
}