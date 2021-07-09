using System.Diagnostics.Tracing;

namespace WebsocketClient
{
    internal sealed class BenchmarksEventSource : EventSource
    {
        public static readonly BenchmarksEventSource Log = new BenchmarksEventSource();

        internal BenchmarksEventSource()
            : this("Benchmarks")
        {

        }

        // Used for testing
        internal BenchmarksEventSource(string eventSourceName)
            : base(eventSourceName)
        {
        }

        [Event(1, Level = EventLevel.Informational)]
        public void Measure(string name, long value)
        {
            WriteEvent(1, name, value);
        }

        public static void Measure(string name, double value)
        {
            Log.MeasureDouble(name, value);
        }

        public static void Measure(string name, string value)
        {
            Log.MeasureString(name, value);
        }

        [Event(2, Level = EventLevel.Informational)]
        public void MeasureDouble(string name, double value)
        {
            WriteEvent(2, name, value);
        }

        [Event(3, Level = EventLevel.Informational)]
        public void MeasureString(string name, string value)
        {
            WriteEvent(3, name, value);
        }

        [Event(5, Level = EventLevel.Informational)]
        public void Metadata(string name, string aggregate, string reduce, string shortDescription, string longDescription, string format)
        {
            WriteEvent(5, name, aggregate, reduce, shortDescription, longDescription, format);
        }
    }
}
