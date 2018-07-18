using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Minitor.Engine.Internal
{
    //--------------------------------------------------------------------------
    // Queues events for a single subscription, delivers them asynchronously
    internal class Subscription : IDisposable
    {
        private Action _dispose;
        private Func<Event, Task> _observer;
        private Queue<Event> _queue;
        private bool _busy;

        //----------------------------------------------------------------------
        public Subscription(Action dispose, Func<Event, Task> observer)
        {
            _dispose = dispose;
            _observer = observer;
            _queue = new Queue<Event>();
        }

        //----------------------------------------------------------------------
        public void Dispose()
        {
            lock (this)
            {
                _dispose?.Invoke();
                _dispose = null;

                _observer = null;
                _queue.Clear();
            }
        }

        //----------------------------------------------------------------------
        public void Send(Event evnt)
        {
            lock (this)
            {
                if (_observer == null) return;

                _queue.Enqueue(evnt);
                if (!_busy)
                {
                    _busy = true;
                    Task.Factory.StartNew(Deliver);
                }
            }
        }

        //----------------------------------------------------------------------
        private async Task Deliver()
        {
            Func<Event, Task> observer;
            Event evnt;
            bool failed = false;

            while (!failed)
            {
                lock (this)
                {
                    observer = _observer;
                    if (observer == null || _queue.Count == 0)
                    {
                        _busy = false;
                        return;
                    }
                    evnt = _queue.Dequeue();
                }

                try
                {
                    await observer.Invoke(evnt);
                }
                catch(Exception e)
                {
                    Log.Debug(e);
                    failed = true;
                }
            }
            Dispose();
        }
    }
}
