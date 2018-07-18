namespace Minitor.Engine
{
    //--------------------------------------------------------------------------
    // All possible UpdateEvent types
    public enum Update
    {
        ParentChanged = 1,
        StatusChanged,

        ChildAdded,
        ChildChanged,
        ChildRemoved,

        MonitorAdded,
        MonitorChanged,
        MonitorRemoved,

        BeginInitialize,
        EndInitialize,
    }
}
