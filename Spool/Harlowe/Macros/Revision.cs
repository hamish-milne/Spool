using System;

namespace Spool.Harlowe
{
    partial class BuiltInMacros
    {
        private class Revision : Changer
        {
            public Revision(HookName[] hooks, AdvanceType mode)
            {
                Target = CombinedSelector.Create(hooks);
                Mode = mode;
            }
            public Revision(string[] strings, AdvanceType mode)
            {
                Target = new ContentSelector(strings);
                Mode = mode;
            }
            public Selection Target { get; }
            public AdvanceType Mode { get; }

            public override void Render(Context context, Action source)
            {
                using (context.Cursor.Save())
                {
                    context.Cursor.Reset();
                    var selector = Target.MakeSelector();
                    while (selector.Advance(context.Cursor, Mode, _ => true)) {
                        source();
                    }
                }
            }
        }

        public Changer append(params HookName[] hooks) => new Revision(hooks, AdvanceType.Append);
        public Changer append(params string[] strings) => new Revision(strings, AdvanceType.Append);
        public Changer replace(params HookName[] hooks) => new Revision(hooks, AdvanceType.Replace);
        public Changer replace(params string[] strings) => new Revision(strings, AdvanceType.Replace);
        public Changer prepend(params HookName[] hooks) => new Revision(hooks, AdvanceType.Prepend);
        public Changer prepend(params string[] strings) => new Revision(strings, AdvanceType.Prepend);
    }
}