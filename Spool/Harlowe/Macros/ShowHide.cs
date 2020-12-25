using System;

namespace Spool.Harlowe
{
    partial class BuiltInMacros
    {
        // public Command show(HookName nodes) => new Show(nodes);
        // class Show : Command
        // {
        //     public Show(HookName nodes) => Nodes = nodes;
        //     public HookName Nodes { get; }
        //     public void Run(Context context)
        //     {
        //         foreach (var node in Nodes) {
        //             if (context.Hidden.TryGetValue(node, out var renderable)) {
        //                 var state = context.Push(node, CursorPos.Before);
        //                 renderable.Render(context);
        //                 context.Hidden.Remove(node);
        //                 node.Remove();
        //                 context.Pop(state);
        //             }
        //         }
        //     }
        // }

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