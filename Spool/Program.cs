using System.Collections.Generic;
using System;
using System.Xml.Linq;

namespace Spool
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
        }
    }

    public class Context
    {
        public IDictionary<string, object> Locals { get; }
        public IDictionary<string, object> Globals { get; }
        public IDictionary<XNode, Renderable> Hidden { get; }
        public IDictionary<string, Renderable> Passages { get; }
        public HashSet<Renderable> Shown { get; }
        public XDocument Screen { get; }
        public XContainer Cursor { get; }
    }
}
