using Minitor.Utility;
using System;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Minitor.Status.Internal
{
    //--------------------------------------------------------------------------
    // A tree node, has a parent and children nodes, holds Monitor objects
    internal class Node
    {
        private static int _lastid;

        private readonly int _id;
        private readonly Node _parent;
        private readonly string _name;
        private readonly string _path;

        private readonly Dictionary<string, Node> _children;
        private readonly Dictionary<string, Monitor> _monitors;

        private readonly ConcurrentDictionary<int, Subscription> _subscriptions;

        private int _subnum;
        private StatusState _status;

        //----------------------------------------------------------------------
        public Node() : this(null, null) { }

        //----------------------------------------------------------------------
        private Node(Node parent, string name)
        {
            _id = ++_lastid;
            _parent = parent;
            _name = name;

            if (_parent == null)
                _path = string.Empty;
            else if (_parent._path == string.Empty)
                _path = _name;
            else
                _path = _parent._path + "/" + _name;

            _children = new Dictionary<string, Node>(StringComparer.InvariantCultureIgnoreCase);
            _monitors = new Dictionary<string, Monitor>(StringComparer.InvariantCultureIgnoreCase);
            _subscriptions = new ConcurrentDictionary<int, Subscription>();
        }

        //----------------------------------------------------------------------
        private bool IsEmpty { get => _children.Count == 0 && _monitors.Count == 0 && _subscriptions.Count == 0; }

        //----------------------------------------------------------------------
        private Node GetNode(string[] path, int index, bool create)
        {
            Node node;
            string key;

            if (index >= path.Length) return this;
            key = path[index];

            if (!_children.TryGetValue(key, out node))
            {
                if (!create) return null;

                node = new Node(this, key);
                _children.Add(key, node);
                SendEvent(StatusEventType.ChildAdded, node._id, node._name, node._path, StatusState.Unknown);
            }
            return node.GetNode(path, index + 1, create);
        }
        public Node GetNode(string[] path, bool create) => GetNode(path, 0, create);

        //----------------------------------------------------------------------
        private void RefreshStatus()
        {
            StatusEvent evnt;
            StatusState status;

            status = StatusState.Normal;

            foreach (Node node in _children.Values)
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

                evnt = new StatusEvent(StatusEventType.ParentChanged, _id, _name, _path, _status);
                CascadeEvent(evnt);
            }
        }

        //----------------------------------------------------------------------
        public void UpdateMonitor(string monitor, string text, StatusState status, TimeSpan validity, TimeSpan expiration)
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
                    mon.ExpireAfter = utc + expiration.Duration();
                    mon.KeepExpired = expiration.Ticks < 0;
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
            mon.ExpireAfter = utc + expiration.Duration();
            mon.KeepExpired = expiration.Ticks < 0;

            SendEvent(evnt, mon.Id, mon.Name, mon.Text, mon.Status);
            RefreshStatus();
        }

        //----------------------------------------------------------------------
        private IEnumerable<Node> PathFromRoot()
        {
            List<Node> list;

            list = new List<Node>();
            for (Node node = _parent; node != null; node = node._parent)
                list.Add(node);
            for (int i = list.Count - 1; i >= 0; i--)
                yield return list[i];
        }

        //----------------------------------------------------------------------
        public IDisposable Subscribe(Func<StatusEvent, Task> observer)
        {
            int num;
            Subscription sub;

            num = ++_subnum;
            sub = new Subscription(() => _subscriptions.TryRemove(num, out var _), observer);
            _subscriptions.TryAdd(num, sub);

            try
            {
                // Send initial state...
                sub.Send(new StatusEvent(StatusEventType.BeginInitialize, 0));

                // Parent nodes
                foreach (Node node in PathFromRoot())
                    sub.Send(new StatusEvent(StatusEventType.ParentChanged, node._id, node._name, node._path, node._status));

                // Self
                sub.Send(new StatusEvent(StatusEventType.ParentChanged, _id, _name, null, _status));

                // Children nodes
                foreach (Node node in _children.Values)
                    sub.Send(new StatusEvent(StatusEventType.ChildAdded, node._id, node._name, node._path, node._status));

                // Mnitors
                foreach (Monitor monitor in _monitors.Values)
                    sub.Send(new StatusEvent(StatusEventType.MonitorAdded, monitor.Id, monitor.Name, monitor.Text, monitor.Status));

                sub.Send(new StatusEvent(StatusEventType.EndInitialize, 0));
            }
            catch (Exception e)
            {
                Logger.Debug(e);
                sub.Dispose();
                return null;
            }
            return sub;
        }

        //----------------------------------------------------------------------
        private void Trim(DateTime utc)
        {
            bool refresh;
            List<Node> nodesRemoveList;
            List<Monitor> monRemoveList;

            refresh = false;
            nodesRemoveList = null;
            monRemoveList = null;

            // Clear unused children
            foreach (Node node in _children.Values)
            {
                node.Trim(utc);
                if (node.IsEmpty)
                {
                    if (nodesRemoveList == null) nodesRemoveList = new List<Node>();
                    nodesRemoveList.Add(node);
                }
            }
            if (nodesRemoveList != null)
            {
                refresh = true;
                foreach (Node node in nodesRemoveList)
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
                        if (monitor.Status != StatusState.Dead)
                        {
                            refresh = true;
                            monitor.Status = StatusState.Dead;
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
                    if (monitor.Status != StatusState.Unknown && monitor.Status != StatusState.Completed)
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
                SendEvent(new StatusEvent(type, id, name, text, status));
        }

        //----------------------------------------------------------------------
        private void SendEvent(StatusEvent evnt)
        {
            foreach (Subscription sub in _subscriptions.Values)
                sub.Send(evnt);
        }

        //----------------------------------------------------------------------
        private void CascadeEvent(StatusEvent evnt)
        {
            foreach (Node node in _children.Values)
            {
                node.SendEvent(evnt);
                node.CascadeEvent(evnt);
            }
        }
    }
}
