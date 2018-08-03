using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Minitor.Utility;

namespace Minitor.Status.Internal
{
    //--------------------------------------------------------------------------
    // Manages a set of subscriptions, dispatch events
    internal class SubscriptionManager<TEvent> where TEvent : class
    {
        private ConcurrentDictionary<int, Subscription> _subscriptions;
        private int _subnum;

        //----------------------------------------------------------------------
        public int Count { get => _subscriptions == null ? 0 : _subscriptions.Count; }

        //----------------------------------------------------------------------
        public IDisposable Subscribe(Func<TEvent, Task> observer, IEnumerable<TEvent> events = null)
        {
            int num;
            Subscription sub;

            num = Interlocked.Increment(ref _subnum);

            if (_subscriptions == null)
                _subscriptions = new ConcurrentDictionary<int, Subscription>();

            sub = new Subscription(() => _subscriptions.TryRemove(num, out var _), observer, events);
            _subscriptions.TryAdd(num, sub);
            return sub;
        }

        //----------------------------------------------------------------------
        public void Send(TEvent evnt)
        {
            if (_subscriptions != null)
                foreach (Subscription sub in _subscriptions.Values)
                    sub.Send(evnt);
        }

        //----------------------------------------------------------------------
        private class Subscription : IDisposable
        {
            private Action _dispose;
            private Func<TEvent, Task> _observer;
            private Queue<TEvent> _queue;
            private bool _busy;

            //----------------------------------------------------------------------
            public Subscription(Action dispose, Func<TEvent, Task> observer, IEnumerable<TEvent> events)
            {
                _dispose = dispose;
                _observer = observer;
                _queue = events == null ? new Queue<TEvent>() : new Queue<TEvent>(events);
                Trigger();
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
            public void Send(TEvent evnt)
            {
                lock (this)
                {
                    if (_observer == null) return;
                    _queue.Enqueue(evnt);
                    Trigger();
                }
            }

            //----------------------------------------------------------------------
            private void Trigger()
            {
                lock (this)
                {
                    if (_busy == false && _queue != null && _queue.Count > 0)
                    {
                        _busy = true;
                        Task.Factory.StartNew(Deliver);
                    }
                }
            }

            //----------------------------------------------------------------------
            private async Task Deliver()
            {
                Func<TEvent, Task> observer;
                TEvent evnt;
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
                    catch (Exception e)
                    {
                        Logger.Write(e);
                        failed = true;
                    }
                }
                Dispose();
            }
        }
    }
}
