using System;

namespace Spool.Harlowe
{
    partial class BuiltInMacros
    {
        public Number ceil(double x) => new Number(Math.Ceiling(x));
        public Number floor(double x) => new Number(Math.Floor(x));
        public Number round(double x) => new Number(Math.Round(x));
        public Number num(string x) => new Number(double.Parse(x));
        public Number number(string x) => num(x);
        public Number random(double start, double end) => new Number(Context.Random.Next((int)start, (int)end));
        public Number random(double end) => random(0, end);
    }
}