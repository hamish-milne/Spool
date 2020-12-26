using System.Text.RegularExpressions;
using System.Linq;

namespace Spool.Harlowe
{
    partial class BuiltInMacros
    {
        private static readonly Regex alphanum = new Regex(@"[\p{L}\p{N}]");
        public String lowercase(string x) => new String(x.ToLower());
        public String lowerfirst(string x) => new String(alphanum.Replace(x, m => m.Value.ToLower(), 1, 0));
        public String uppercase(string x) => new String(x.ToUpper());
        public String upperfirst(string x) => new String(alphanum.Replace(x, m => m.Value.ToUpper(), 1, 0));
        public String strRepeated(double count, string x)
            => new String(string.Join("", Enumerable.Repeat(x, (int)count)));
        public String stringRepeated(double count, string x) => strRepeated(count, x);
        public String strReversed(string x) => new String(new string(x.Reverse().ToArray()));
        public String stringReversed(string x) => strReversed(x);
        public String str(params Data[] values) => new String(string.Join<Data>("", values));
        public String @string(params Data[] values) => str(values);
        public String text(params Data[] values) => str(values);
        public Array words(string x) => new Array(Regex.Split(x, "\\s+").Where(s => s != "").Select(s => new String(s)));
    }
}