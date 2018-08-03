namespace Minitor.Status
{
    //--------------------------------------------------------------------------
    // All possible status values of a Monitor, in priority order
    public enum StatusState
    {
        Normal = 0,
        Success,
        Unknown,
        Warning,
        Error,
        Critical,
    }
}
