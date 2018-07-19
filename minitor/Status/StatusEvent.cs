namespace Minitor.Status
{
    //--------------------------------------------------------------------------
    // A change event a distributed to listeners
    public class StatusEvent
    {
        //----------------------------------------------------------------------
        internal StatusEvent(StatusEventType type, int id, string name = null, string text = null, StatusState? status = null)
        {
            EventType = type;
            Id = id;
            Name = name;
            Text = text;
            Status = status;
        }

        //----------------------------------------------------------------------
        public readonly StatusEventType EventType;
        public readonly int Id;
        public readonly string Name;
        public readonly string Text;
        public readonly StatusState? Status;
    }
}
