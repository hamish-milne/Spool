namespace Spool.Harlowe
{
    class Color : Data
    {
        public override bool Serializable => true;
        public Color(System.Drawing.Color value) => Value = value;
        public System.Drawing.Color Value { get; }
        protected override object GetObject() => Value;

        public override Data Member(Data member)
        {
            if (member is String str) {
                return str.Value switch {
                    "r" => new Number(Value.R),
                    "g" => new Number(Value.G),
                    "b" => new Number(Value.B),
                    "h" => new Number(Value.GetHue()),
                    "s" => new Number(Value.GetSaturation()),
                    "l" => new Number(Value.GetBrightness()),
                    _ => base.Member(member)
                };
            }
            return base.Member(member);
        }

        public override Data Operate(Operator op, Data rhs)
        {
            if (op == Operator.Add && rhs is Color c) {
                return new Color(System.Drawing.Color.FromArgb(
                    (Value.A + c.Value.A) / 2,
                    (Value.R + c.Value.R) / 2,
                    (Value.G + c.Value.G) / 2,
                    (Value.B + c.Value.B) / 2
                ));
            }
            return base.Operate(op, rhs);
        }
    }
}