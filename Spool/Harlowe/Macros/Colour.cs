using System;

namespace Spool.Harlowe
{
    partial class BuiltInMacros
    {
        // public Gradient gradient(double angle, params object[] pairs)
        // {
        //     IEnumerable<ColorStop> getStops() {
        //         for (int i = 0; i < pairs.Length; i += 2) {
        //             if (pairs[i] is double d && pairs[i+1] is Color c) {
        //                 yield return new ColorStop(c, d);
        //             } else {
        //                 throw new ArgumentException($"Invalid color stop at {i}");
        //             }
        //         }
        //     }
        //     return new Gradient(getStops(), angle);
        // }

        public Color hsl(double hue, double saturation, double lightness) => hsla(hue, saturation, lightness);
        public Color hsl(double hue, double saturation, double lightness, double alpha) => hsla(hue, saturation, lightness, alpha);
        public Color hsla(double hue, double saturation, double lightness) => hsl(hue, saturation, lightness, 1.0);
        public Color hsla(double hue, double saturation, double lightness, double alpha)
        {
            throw new NotImplementedException();
        }

        public Color rgba(double r, double g, double b, double a) => new Color(System.Drawing.Color.FromArgb((int)a, (int)r, (int)g, (int)b));
        public Color rgba(double r, double g, double b) => rgba(r, g, b, 255);
        public Color rgb(double r, double g, double b, double a) => rgba(r, g, b, a);
        public Color rgb(double r, double g, double b) => rgba(r, g, b);

    }
}