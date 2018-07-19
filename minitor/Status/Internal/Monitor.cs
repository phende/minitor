using System;

namespace Minitor.Status.Internal
{
    //--------------------------------------------------------------------------
    // State of one single monitor
    internal class Monitor
    {
        public Monitor(int id, Node parent, string name)
        {
            Id = id;
            Parent = parent;
            Name = name;
        }

        public readonly int Id;
        public readonly Node Parent;
        public readonly string Name;

        public StatusState Status;
        public string Text;

        public DateTime ValidUntil;
        public DateTime ExpireAfter;
        public bool KeepExpired;
    }
}
