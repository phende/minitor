﻿using Minitor.Status.Internal;
using Minitor.Utility;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Minitor.Status
{
    //--------------------------------------------------------------------------
    // Synchronizes and manages a tree of path nodes and monitors
    public class StatusManager : IDisposable
    {
        private static readonly char[] _separators = new char[] { '/' };

        private PathRoot _root = new PathRoot();
        private Timer _timer;

        //----------------------------------------------------------------------
        public StatusManager()
        {
            TimeSpan interval;

            interval = Configuration.StatusTrimInterval;
            _timer = new Timer(Trim, null, interval, interval);
        }

        //----------------------------------------------------------------------
        public void Dispose()
        {
            _timer?.Dispose();
            _timer = null;
        }

        //----------------------------------------------------------------------
        private void Trim(object _)
        {
            lock (_root)
                _root.Trim();
        }

        //----------------------------------------------------------------------
        public bool UpdateMonitor(string path, string name, string text, StatusState status, TimeSpan validity, TimeSpan expiration, bool beat)
        {
            string[] parts;
            PathNode node;

            if (status < StatusState.Normal || status > StatusState.Critical)
                return false;

            if (name == null)
                return false;

            if ((parts = BreakPath(path)) == null)
                return false;

            lock (_root)
            {
                node = _root.GetNode(parts, true);
                node.UpdateMonitor(name, text, status, validity, expiration, beat);
            }
            return true;
        }

        //----------------------------------------------------------------------
        public IDisposable SubscribeTree(Func<StatusEvent, Task> observer)
        {
            if (observer == null)
                return null;

            lock (_root)
            {
                return _root.SubscribeTree(observer);
            }
        }

        //----------------------------------------------------------------------
        public IDisposable SubscribePath(string path, Func<StatusEvent, Task> observer)
        {
            string[] parts;
            PathNode node;

            if (observer == null)
                return null;

            if ((parts = BreakPath(path)) == null)
                return null;

            lock (_root)
            {
                node = _root.GetNode(parts, true);
                return node.SubscribePath(observer);
            }
        }

        //----------------------------------------------------------------------
        private static string[] BreakPath(string path)
        {
            string[] parts;

            if (string.IsNullOrWhiteSpace(path))
                return new string[0];

            parts = path.Split(_separators, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = parts[i].Trim();
                if (parts[i].Length == 0) return null;
            }
            return parts;
        }
    }
}
