using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GTools
{
    public sealed class EventAwaiter<TEventArgs> : INotifyCompletion
    {
        private ConcurrentQueue<TEventArgs> m_events = new ConcurrentQueue<TEventArgs>();
        private Action m_continuation;
        #region Members invoked by the state machine
        // The state machine will call this first to get our awaiter; we return ourself
        public EventAwaiter<TEventArgs> GetAwaiter() { return this; }
        // Tell state machine if any events have happened yet
        public Boolean IsCompleted { get { return m_events.Count > 0; } }
        // The state machine tells us what method to invoke later; we save it
        public void OnCompleted(Action continuation)
        {
            Volatile.Write(ref m_continuation, continuation);
        }
        // The state machine queries the result; this is the await operator's result
        public TEventArgs GetResult()
        {
            TEventArgs e;
            m_events.TryDequeue(out e);
            return e;
        }
        #endregion
        // Potentially invoked by multiple threads simultaneously when each raises the event
        public void EventRaised(Object sender, TEventArgs eventArgs)
        {
            m_events.Enqueue(eventArgs); // Save EventArgs to return it from GetResult/await
                                         // If there is a pending continuation, this thread takes it
            Action continuation = Interlocked.Exchange(ref m_continuation, null);
            if (continuation != null) continuation(); // Resume the state machine
        }

        private async static void ShowExceptions()
        {
            var eventAwaiter = new EventAwaiter<FirstChanceExceptionEventArgs>();
            AppDomain.CurrentDomain.FirstChanceException += eventAwaiter.EventRaised;
            while (true)
            {
                Console.WriteLine("AppDomain exception: {0}",
                (await eventAwaiter).Exception.GetType());
            }
        }
        public static void Go()
        {
            ShowExceptions();
            for (Int32 x = 0; x < 3; x++)
            {
                try
                {
                    switch (x)
                    {
                        case 0: throw new InvalidOperationException();
                        case 1: throw new ObjectDisposedException("");
                        case 2: throw new ArgumentOutOfRangeException();
                    }
                }
                catch { }
            }
        }
    }
}
