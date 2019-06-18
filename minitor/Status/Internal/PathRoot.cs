using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;

namespace Minitor.Status.Internal
{
    //--------------------------------------------------------------------------
    // The mother of all PathNodes
    internal class PathRoot : PathNode
    {
        private readonly SubscriptionManager<StatusEvent> _treeSubscriptions;

        //----------------------------------------------------------------------
        public PathRoot() : base(null, null)
        {
            _treeSubscriptions = new SubscriptionManager<StatusEvent>();
        }

        //----------------------------------------------------------------------
        public void Trim() => Trim(DateTime.UtcNow);

        //----------------------------------------------------------------------
        public PathNode GetNode(string[] path, bool create) => GetNode(path, 0, create);

        //----------------------------------------------------------------------
        private static void CollectNodes(PathNode node, List<StatusEvent> events)
        {
            events.Add(new StatusEvent(StatusEventType.ChildAdded, node.Id, node.Path, null, node.Status));
            foreach (PathNode child in node.Children) CollectNodes(child, events);
        }

        //----------------------------------------------------------------------
        public IDisposable SubscribeTree(Func<StatusEvent, Task> observer)
        {
            List<StatusEvent> events;

            events = new List<StatusEvent>();

            // Send initial state...
            events.Add(new StatusEvent(StatusEventType.BeginInitialize, 0));

            // All nodes
            CollectNodes(this, events);

            // Done
            events.Add(new StatusEvent(StatusEventType.EndInitialize, 0));
            return _treeSubscriptions.Subscribe(observer, events);
        }

        //----------------------------------------------------------------------
        internal void SendTreeEvent(StatusEventType type, PathNode node)
        {
            if (_treeSubscriptions.Count > 0)
                _treeSubscriptions.Send(new StatusEvent(type, node.Id, node.Path, null, node.Status));
        }
    }
}
