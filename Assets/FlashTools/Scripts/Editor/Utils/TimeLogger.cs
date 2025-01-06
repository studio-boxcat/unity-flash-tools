using System.Diagnostics;
using FTRuntime;

namespace FTEditor
{
    internal readonly struct TimeLogger
    {
        private readonly Stopwatch _sw;
        private readonly string _subject;

        public TimeLogger(string subject)
        {
            _sw = Stopwatch.StartNew();
            _subject = subject;
        }

        public void Dispose()
        {
            _sw.Stop();
            L.I($"{_subject}: {_sw.ElapsedMilliseconds}ms");
        }
    }
}