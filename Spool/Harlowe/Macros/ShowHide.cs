using System;

namespace Spool.Harlowe
{
    partial class BuiltInMacros
    {
        public Command show(params HookName[] hooks) => new Show(hooks);
        class Show : Command
        {
            public Show(HookName[] hooks) => Hooks = CombinedSelector.Create(hooks);
            public Selection Hooks { get; }
            public override void Run(Context context)
            {
                using (context.Cursor.Save()) {
                    context.Cursor.Reset();                    
                    var s = Hooks.MakeSelector();
                    while (s.Advance(context.Cursor, AdvanceType.Append)) {
                        context.Cursor.RunEvent("show");
                    }
                }
            }
        }

        public Changer hidden() => Hidden.Instance;
        class Hidden : Changer
        {
            public static Hidden Instance { get; } = new Hidden();
            public override void Apply(ref bool? hidden, ref string name) => hidden = true;
            public override void Render(Context context, Action source) => source();
        }
    }

    class NullChanger : Changer
    {
        public static Changer Instance { get; } = new NullChanger();
        public override void Render(Context context, Action source) => source();
    }
}