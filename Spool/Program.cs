using System.Collections.Generic;
using System;

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
    }
}
