using System;

namespace Spool.Harlowe
{
    abstract class Changer : Data
    {
        public static DataType Type { get; } = new DataType(typeof(Changer));
        public override bool Serializable => true;
        protected override object GetObject() => this;
        protected override string GetString() => $"a ({GetType().Name}:) changer";
        public virtual void Apply(ref bool? hidden, ref string name) {}
        public abstract void Render(Context context, Action source);
        public virtual void RememberHidden(Context context, IDisposable cursorPosition) {}
    }
}