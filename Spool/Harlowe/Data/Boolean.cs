namespace Spool.Harlowe
{
    class Boolean : RenderableData
    {
        public static DataType Type { get; } = new DataType(typeof(Boolean));
        public override bool Serializable => true;
        public static Boolean Get(bool value) => value ? True : False;
        public static Boolean True { get; } = new Boolean(true);
        public static Boolean False { get; } = new Boolean(false);

        private Boolean(bool value) => Value = value;
        public bool Value { get; }
        protected override object GetObject() => Value;
        public override Data Operate(Operator op, Data rhs)
        {
            return rhs switch {
                Boolean b => op switch {
                    Operator.And => Get(Value && b.Value),
                    Operator.Or => Get(Value && b.Value),
                    _ => base.Operate(op, rhs)
                },
                _ => base.Operate(op, rhs)
            };
        }

        public override Data Unary(UnaryOp op)
        {
            return op switch {
                UnaryOp.Not => Get(!Value),
                _ => base.Unary(op)
            };
        }

        public override bool Equals(Data other) => other is Boolean b && b.Value == Value;
    }
}