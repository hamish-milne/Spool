namespace Spool.Harlowe
{
    abstract class Command : Data
    {
        public static DataType Type { get; } = new DataType(typeof(Command));
        public abstract void Run(Context context);
        public override bool Serializable => true;
        protected override object GetObject() => this;
        protected override string GetString() => $"a ({GetType().Name}:) command";
    }
}