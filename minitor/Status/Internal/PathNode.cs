using Minitor.Utility;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Minitor.Status.Internal
{
    //--------------------------------------------------------------------------
    // A tree node, has a parent and children nodes, holds Monitor objects
    internal class PathNode
    {
        private static int _lastid;

        private readonly int _id;
        private readonly PathNode _parent;
        private readonly string _name;
        private readonly string _path;

        private readonly Dictionary<string, PathNode> _children;
        private readonly Dictionary<string, Monitor> _monitors;
        private readonly SubscriptionManager<StatusEvent> _subscriptions;

        private StatusState _status;

        //----------------------------------------------------------------------
        public PathNode() : this(null, null) { }

        //----------------------------------------------------------------------
        private PathNode(PathNode parent, string name)
        {
            _id = Interlocked.Increment(ref _lastid);
            _parent = parent;
            _name = name;
            _status = StatusState.Normal;

            if (_parent == null)
                _path = string.Empty;
            else if (_parent._path == string.Empty)
                _path = _name;
            else
                _path = string.Concat(_parent._path, "/", _name);

            _children = new Dictionary<string, PathNode>(StringComparer.InvariantCultureIgnoreCase);
            _monitors = new Dictionary<string, Monitor>(StringComparer.InvariantCultureIgnoreCase);
            _subscriptions = new SubscriptionManager<StatusEvent>();
        }

        //----------------------------------------------------------------------
        private bool IsEmpty { get => _children.Count == 0 && _monitors.Count == 0 && _subscriptions.Count == 0; }

        //----------------------------------------------------------------------
        private PathNode GetNode(string[] path, int index, bool create)
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
                SendEvent(StatusEventType.ChildAdded, node._id, node._name, node._path, node._status);
            }
            return node.GetNode(path, index + 1, create);
        }
        public PathNode GetNode(string[] path, bool create) => GetNode(path, 0, create);

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
                SendEvent(StatusEventType.StatusChanged, _id, _name, null, _status);
                _parent?.SendEvent(StatusEventType.ChildChanged, _id, _name, _path, _status);
                _parent?.RefreshStatus();

                CascadeEvent(new StatusEvent(StatusEventType.ParentChanged, _id, _name, _path, _status));
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
                mon = new Monitor(++_lastid, this, monitor);
                _monitors.Add(monitor, mon);
                evnt = StatusEventType.MonitorAdded;
            }

            mon.Text = text;
            mon.Status = status;
            mon.ValidUntil = utc + validity;
            mon.ExpireAfter = utc + expiration;
            mon.KeepExpired = beat;

            SendEvent(evnt, mon.Id, mon.Name, mon.Text, mon.Status);
            RefreshStatus();
        }

        //----------------------------------------------------------------------
        private IEnumerable<PathNode> PathFromRoot()
        {
            List<PathNode> list;

            list = new List<PathNode>();
            for (PathNode node = _parent; node != null; node = node._parent)
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
                events.Add(new StatusEvent(StatusEventType.ParentChanged, node._id, node._name, node._path, node._status));

            // Self
            events.Add(new StatusEvent(StatusEventType.ParentChanged, _id, _name, null, _status));

            // Children nodes
            foreach (PathNode node in _children.Values)
                events.Add(new StatusEvent(StatusEventType.ChildAdded, node._id, node._name, node._path, node._status));

            // Minitors
            foreach (Monitor monitor in _monitors.Values)
                events.Add(new StatusEvent(StatusEventType.MonitorAdded, monitor.Id, monitor.Name, monitor.Text, monitor.Status));

            events.Add(new StatusEvent(StatusEventType.EndInitialize, 0));

            return _subscriptions.Subscribe(observer, events);
        }

        //----------------------------------------------------------------------
        private void Trim(DateTime utc)
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
                    _children.Remove(node._name);
                    SendEvent(StatusEventType.ChildRemoved, node._id, node._name);
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
                            SendEvent(StatusEventType.MonitorChanged, monitor.Id, monitor.Name, monitor.Text, monitor.Status);
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
                        SendEvent(StatusEventType.MonitorChanged, monitor.Id, monitor.Name, monitor.Text, monitor.Status);
                    }
                }
            }
            if (monRemoveList != null)
            {
                foreach (Monitor monitor in monRemoveList)
                {
                    _monitors.Remove(monitor.Name);
                    SendEvent(StatusEventType.MonitorRemoved, monitor.Id, monitor.Name);
                }
            }

            if (refresh) RefreshStatus();
        }
        public void Trim() => Trim(DateTime.UtcNow);

        //----------------------------------------------------------------------
        private void SendEvent(StatusEventType type, int id, string name = null, string text = null, StatusState status = StatusState.Unknown)
        {
            if (_subscriptions.Count > 0)
                _subscriptions.Send(new StatusEvent(type, id, name, text, status));
        }

        //----------------------------------------------------------------------
        private void CascadeEvent(StatusEvent evnt)
        {
            foreach (PathNode node in _children.Values)
            {
                node._subscriptions.Send(evnt);
                node.CascadeEvent(evnt);
            }
        }
    }
}
