

namespace Spool
{
    interface Language
    {
        bool Supports(string format, string version);
        Context Run(Story story, Cursor output);
    }

    public interface Context
    {
        void Start();
        Cursor Cursor { get; }
        string Save();
        void Load(string savestate);
    }
}