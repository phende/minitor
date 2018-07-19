namespace Minitor.Status
{
    //--------------------------------------------------------------------------
    // All possible UpdateEvent types
    public enum StatusEventType
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
