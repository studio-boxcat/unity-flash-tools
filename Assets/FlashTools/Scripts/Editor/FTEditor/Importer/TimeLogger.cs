using System.Diagnostics;

namespace FTEditor.Importer
{
    readonly struct TimeLogger
    {
        readonly Stopwatch _sw;
        readonly string _subject;

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