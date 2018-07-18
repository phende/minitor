using Minitor.Engine.Internal;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Minitor.Engine
{
    //--------------------------------------------------------------------------
    // Synchronizes and manages a tree of Node and Monitor objects
    public class Engine : IDisposable
    {
        private static char[] _separators = new char[] { '/' };

        private Node _root = new Node();
        private Timer _timer;

        //----------------------------------------------------------------------
        public Engine()
        {
            TimeSpan interval;

            interval = Configuration.TrimInterval;
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
        public bool Update(string path, string name, string text, Status status, TimeSpan validity, TimeSpan expiration)
        {
            string[] parts;
            Node node;

            if (status < Status.Normal || status > Status.Dead)
                return false;

            if (name == null)
                return false;

            parts = BreakPath(path);
            if (parts == null)
                return false;

            lock (_root)
            {
                node = _root.GetNode(parts);
                node.Update(name, text, status, validity, expiration);
            }
            return true;
        }

        //----------------------------------------------------------------------
        public IDisposable Subscribe(string path, Func<Event, Task> observer)
        {
            string[] parts;
            Node node;

            if (observer == null)
                return null;

            parts = BreakPath(path);
            if (parts == null)
                return null;

            lock (_root)
            {
                node = _root.GetNode(parts);
                return node.Subscribe(observer);
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
