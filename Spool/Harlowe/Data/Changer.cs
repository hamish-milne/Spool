using System;

namespace Spool.Harlowe
{
    abstract class Changer : Data
    {
        public override bool Serializable => true;
        protected override object GetObject() => this;
        protected override string GetString() => $"a ({GetType().Name}:) changer";
        public virtual void Apply(ref bool? hidden, ref string name) {}
        public abstract void Render(Context context, Action source);
    }
}