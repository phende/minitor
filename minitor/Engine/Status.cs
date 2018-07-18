namespace Minitor.Engine
{
    //--------------------------------------------------------------------------
    // All possible status values of a Monitor, in priority order
    public enum Status
    {
        Normal = 0,
        Unknown,
        Warning,
        Error,
        Dead,
    }
}
