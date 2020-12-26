using System;
using System.Linq;

namespace Spool.Harlowe
{
    partial class BuiltInMacros
    {
        public Number abs(double x) => new Number(Math.Abs(x));
        public Number cos(double x) => new Number(Math.Cos(x));
        public Number exp(double x) => new Number(Math.Exp(x));
        public Number log(double x) => new Number(Math.Log(x));
        public Number log10(double x) => new Number(Math.Log10(x));
        public Number log2(double x) => new Number(Math.Log(x, 2));
        public Number sign(double x) => new Number(Math.Sign(x));
        public Number sin(double x) => new Number(Math.Sin(x));
        public Number sqrt(double x) => new Number(Math.Sqrt(x));
        public Number tan(double x) => new Number(Math.Tan(x));
        public Number pow(double x, double y) => new Number(Math.Pow(x, y));
        public Number min(params double[] values) => new Number(values.Min());
        public Number max(params double[] values) => new Number(values.Max());
    }
}