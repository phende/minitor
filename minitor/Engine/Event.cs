namespace Minitor.Engine
{
    //--------------------------------------------------------------------------
    // A change event a distributed to listeners
    public class Event
    {
        //----------------------------------------------------------------------
        internal Event(Update type, int id, string name = null, string text = null, Status? status = null)
        {
            EventType = type;
            Id = id;
            Name = name;
            Text = text;
            Status = status;
        }

        //----------------------------------------------------------------------
        public readonly Update EventType;
        public readonly int Id;
        public readonly string Name;
        public readonly string Text;
        public readonly Status? Status;
    }
}
