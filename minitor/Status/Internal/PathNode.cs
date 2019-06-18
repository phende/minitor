using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Minitor.Status.Internal
{
    //--------------------------------------------------------------------------
    // A tree node, has a parent and children nodes, holds Monitor objects
    internal class PathNode
    {
        private static int _lastid;

        public readonly int Id;
        public readonly PathRoot Root;
        public readonly PathNode Parent;
        public readonly string Name;
        public readonly string Path;

        private readonly Dictionary<string, PathNode> _children;
        private readonly Dictionary<string, Monitor> _monitors;
        private readonly SubscriptionManager<StatusEvent> _pathSubscriptions;
        private StatusState _status;

        //----------------------------------------------------------------------
        protected PathNode(PathNode parent, string name)
        {
            Id = Interlocked.Increment(ref _lastid);
            Parent = parent;
            Name = name;
            _status = StatusState.Normal;

            if (Parent == null)
            {
                Root = (PathRoot)this;
                Path = string.Empty;
            }
            else
            {
                Root = Parent.Root;
                if (Parent.Parent == null)
                    Path = Name;
                else
                    Path = string.Concat(Parent.Path, "/", Name);
            }

            _children = new Dictionary<string, PathNode>(StringComparer.InvariantCultureIgnoreCase);
            _monitors = new Dictionary<string, Monitor>(StringComparer.InvariantCultureIgnoreCase);
            _pathSubscriptions = new SubscriptionManager<StatusEvent>();
        }

        //----------------------------------------------------------------------
        public StatusState Status { get => _status; }
        public IEnumerable<PathNode> Children { get => _children.Values; }

        //----------------------------------------------------------------------
        private bool IsEmpty { get => _children.Count == 0 && _monitors.Count == 0 && _pathSubscriptions.Count == 0; }

        //----------------------------------------------------------------------
        protected PathNode GetNode(string[] path, int index, bool create)
        {
            PathNode node;
            string key;

            if (index >= path.Length) return this;
            key = path[index];

            if (!_children.TryGetValue(key, out node))
            {
                if (!create) return null;

                node = new PathNode(this, key);
                _children.Add(key, node);
                SendPathEvent(StatusEventType.ChildAdded, node.Id, node.Name, node.Path, node._status);
                Root.SendTreeEvent(StatusEventType.ChildAdded, this);
            }
            return node.GetNode(path, index + 1, create);
        }

        //----------------------------------------------------------------------
        private void RefreshStatus()
        {
            StatusState status;

            status = StatusState.Normal;

            foreach (PathNode node in _children.Values)
                if (node._status > status)
                    status = node._status;

            foreach (Monitor monitor in _monitors.Values)
                if (monitor.Status > status)
                    status = monitor.Status;

            if (status != _status)
            {
                _status = status;
                SendPathEvent(StatusEventType.StatusChanged, Id, Name, null, _status);
                Parent?.SendPathEvent(StatusEventType.ChildChanged, Id, Name, Path, _status);
                Parent?.RefreshStatus();
                Root.SendTreeEvent(StatusEventType.ChildChanged, this);

                CascadePathEvent(new StatusEvent(StatusEventType.ParentChanged, Id, Name, Path, _status));
            }
        }

        //----------------------------------------------------------------------
        public void UpdateMonitor(string monitor, string text, StatusState status, TimeSpan validity, TimeSpan expiration, bool beat)
        {
            StatusEventType evnt;
            DateTime utc;
            Monitor mon;

            utc = DateTime.UtcNow;

            if (_monitors.TryGetValue(monitor, out mon))
            {
                // If no change in status nor text, just update timestamps
                if (status == mon.Status && ((text == null && mon.Text == null) || (text != null && mon.Text != null && text != mon.Text)))
                {
                    mon.ValidUntil = utc + validity;
                    mon.ExpireAfter = utc + expiration;
                    mon.KeepExpired = beat;
                    return;
                }
                evnt = StatusEventType.MonitorChanged;
            }
            else
            {
                mon = new Monitor(Interlocked.Increment(ref _lastid), this, monitor);
                _monitors.Add(monitor, mon);
                evnt = StatusEventType.MonitorAdded;
            }

            mon.Text = text;
            mon.Status = status;
            mon.ValidUntil = utc + validity;
            mon.ExpireAfter = utc + expiration;
            mon.KeepExpired = beat;

            SendPathEvent(evnt, mon.Id, mon.Name, mon.Text, mon.Status);
            RefreshStatus();
        }

        //----------------------------------------------------------------------
        private IEnumerable<PathNode> PathFromRoot()
        {
            List<PathNode> list;

            list = new List<PathNode>();
            for (PathNode node = Parent; node != null; node = node.Parent)
                list.Add(node);
            for (int i = list.Count - 1; i >= 0; i--)
                yield return list[i];
        }

        //----------------------------------------------------------------------
        public IDisposable SubscribePath(Func<StatusEvent, Task> observer)
        {
            List<StatusEvent> events;

            events = new List<StatusEvent>();

            // Send initial state...
            events.Add(new StatusEvent(StatusEventType.BeginInitialize, 0));

            // Parent nodes
            foreach (PathNode node in PathFromRoot())
                events.Add(new StatusEvent(StatusEventType.ParentChanged, node.Id, node.Name, node.Path, node._status));

            // Self
            events.Add(new StatusEvent(StatusEventType.ParentChanged, Id, Name, null, _status));

            // Children nodes
            foreach (PathNode node in _children.Values)
                events.Add(new StatusEvent(StatusEventType.ChildAdded, node.Id, node.Name, node.Path, node._status));

            // Minitors
            foreach (Monitor monitor in _monitors.Values)
                events.Add(new StatusEvent(StatusEventType.MonitorAdded, monitor.Id, monitor.Name, monitor.Text, monitor.Status));

            events.Add(new StatusEvent(StatusEventType.EndInitialize, 0));

            return _pathSubscriptions.Subscribe(observer, events);
        }

        //----------------------------------------------------------------------
        protected void Trim(DateTime utc)
        {
            bool refresh;
            List<PathNode> nodesRemoveList;
            List<Monitor> monRemoveList;

            refresh = false;
            nodesRemoveList = null;
            monRemoveList = null;

            // Clear unused children
            foreach (PathNode node in _children.Values)
            {
                node.Trim(utc);
                if (node.IsEmpty)
                {
                    if (nodesRemoveList == null) nodesRemoveList = new List<PathNode>();
                    nodesRemoveList.Add(node);
                }
            }
            if (nodesRemoveList != null)
            {
                refresh = true;
                foreach (PathNode node in nodesRemoveList)
                {
                    _children.Remove(node.Name);
                    SendPathEvent(StatusEventType.ChildRemoved, node.Id, node.Name);
                    Root.SendTreeEvent(StatusEventType.ChildRemoved, this);
                }
            }

            // Clear expired monitors
            foreach (Monitor monitor in _monitors.Values)
            {
                if (utc >= monitor.ExpireAfter)
                {
                    if (monitor.KeepExpired)
                    {
                        if (monitor.Status != StatusState.Critical)
                        {
                            refresh = true;
                            monitor.Status = StatusState.Critical;
                            SendPathEvent(StatusEventType.MonitorChanged, monitor.Id, monitor.Name, monitor.Text, monitor.Status);
                        }
                    }
                    else
                    {
                        refresh = true;
                        if (monRemoveList == null) monRemoveList = new List<Monitor>();
                        monRemoveList.Add(monitor);
                    }
                }
                else if (utc >= monitor.ValidUntil)
                {
                    if (monitor.Status != StatusState.Unknown)
                    {
                        refresh = true;
                        monitor.Status = StatusState.Unknown;
                        SendPathEvent(StatusEventType.MonitorChanged, monitor.Id, monitor.Name, monitor.Text, monitor.Status);
                    }
                }
            }
            if (monRemoveList != null)
            {
                foreach (Monitor monitor in monRemoveList)
                {
                    _monitors.Remove(monitor.Name);
                    SendPathEvent(StatusEventType.MonitorRemoved, monitor.Id, monitor.Name);
                }
            }

            if (refresh) RefreshStatus();
        }

        //----------------------------------------------------------------------
        private void SendPathEvent(StatusEventType type, int id, string name = null, string text = null, StatusState status = StatusState.Unknown)
        {
            if (_pathSubscriptions.Count > 0)
                _pathSubscriptions.Send(new StatusEvent(type, id, name, text, status));
        }

        //----------------------------------------------------------------------
        private void CascadePathEvent(StatusEvent evnt)
        {
            foreach (PathNode node in _children.Values)
            {
                node._pathSubscriptions.Send(evnt);
                node.CascadePathEvent(evnt);
            }
        }

        //----------------------------------------------------------------------
        // State of one single monitor
        private class Monitor
        {
            public Monitor(int id, PathNode parent, string name)
            {
                Id = id;
                Parent = parent;
                Name = name;
            }

            public readonly int Id;
            public readonly PathNode Parent;
            public readonly string Name;

            public StatusState Status;
            public string Text;

            public DateTime ValidUntil;
            public DateTime ExpireAfter;
            public bool KeepExpired;
        }
    }
}
