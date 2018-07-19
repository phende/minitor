using Minitor.Utility;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Minitor.Status.Internal
{
    //--------------------------------------------------------------------------
    // Queues events for a single subscription, delivers them asynchronously
    internal class Subscription : IDisposable
    {
        private Action _dispose;
        private Func<StatusEvent, Task> _observer;
        private Queue<StatusEvent> _queue;
        private bool _busy;

        //----------------------------------------------------------------------
        public Subscription(Action dispose, Func<StatusEvent, Task> observer)
        {
            _dispose = dispose;
            _observer = observer;
            _queue = new Queue<StatusEvent>();
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
        public void Send(StatusEvent evnt)
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
            Func<StatusEvent, Task> observer;
            StatusEvent evnt;
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
                    Logger.Debug(e);
                    failed = true;
                }
            }
            Dispose();
        }
    }
}
