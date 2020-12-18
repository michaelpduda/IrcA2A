/* This file is part of the IrcA2A project, which is released under MIT License.
 * See LICENSE.md or visit:
 * https://github.com/michaelpduda/irca2a/blob/main/LICENSE.md
 */
using System;
using System.Threading;

namespace IrcA2A.Communication
{
    internal class EventWaiter : IDisposable
    {
        private readonly ManualResetEvent _manualResetEvent = new ManualResetEvent(false);
        private readonly EventHandler _handleEvent;
        private readonly Action<EventHandler> _unsubscriber;

        public EventWaiter(Action<EventHandler> subscriber, Action<EventHandler> unsubscriber)
        {
            _ = subscriber ?? throw new ArgumentNullException(nameof(subscriber));
            _unsubscriber = unsubscriber ?? throw new ArgumentNullException(nameof(unsubscriber));
            _handleEvent = new EventHandler((o, e) => _manualResetEvent.Set());
            subscriber(_handleEvent);
        }

        public void Dispose()
        {
            _manualResetEvent.WaitOne();
            _unsubscriber(_handleEvent);
            _manualResetEvent.Dispose();
        }
    }

    internal class EventWaiter<TEventArgs> : IDisposable
        where TEventArgs : EventArgs
    {
        private readonly ManualResetEvent _manualResetEvent = new ManualResetEvent(false);
        private readonly EventHandler<TEventArgs> _handleEvent;
        private readonly Action<EventHandler<TEventArgs>> _unsubscriber;

        public EventWaiter(Action<EventHandler<TEventArgs>> subscriber, Action<EventHandler<TEventArgs>> unsubscriber)
        {
            _ = subscriber ?? throw new ArgumentNullException(nameof(subscriber));
            _unsubscriber = unsubscriber ?? throw new ArgumentNullException(nameof(unsubscriber));
            _handleEvent = new EventHandler<TEventArgs>((o, e) => _manualResetEvent.Set());
            subscriber(_handleEvent);
        }

        public void Dispose()
        {
            _manualResetEvent.WaitOne();
            _unsubscriber(_handleEvent);
            _manualResetEvent.Dispose();
        }
    }
}
