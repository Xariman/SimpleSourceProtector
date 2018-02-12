using System;
using System.Diagnostics;

namespace SimpleSourceProtector
{
    public class Watcher : IDisposable
    {
        private Stopwatch _stopwatch = new Stopwatch();
        private Action<TimeSpan> _callback;

        public Watcher()
        {
            _stopwatch.Start();
        }

        public Watcher(Action<TimeSpan> callback) : this()
        {
            _callback = callback;
        }

        public static Watcher Start(Action<TimeSpan> callback)
        {
            return new Watcher(callback);
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            if (_callback != null)
                _callback(Result);
        }

        public TimeSpan Result
        {
            get { return _stopwatch.Elapsed; }
        }
    }
}
