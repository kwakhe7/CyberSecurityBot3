using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChatbotPart2.Services
{
    /// <summary>
    /// Records every significant chatbot action with a timestamp and short description.
    /// Thread-safe in-memory ring buffer.
    /// </summary>
    public class ActivityLogger
    {
        private readonly object _lock = new();
        private readonly List<LogEntry> _entries = new();
        private readonly int _maxEntries;

        public ActivityLogger(int maxEntries = 500)
        {
            _maxEntries = Math.Max(1, maxEntries);
        }

        public void Log(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            var entry = new LogEntry(DateTimeOffset.UtcNow, message.Trim());

            lock (_lock)
            {
                _entries.Add(entry);
                if (_entries.Count > _maxEntries)
                {
                    // drop oldest
                    _entries.RemoveAt(0);
                }
            }
        }

        public IReadOnlyList<LogEntry> GetRecent(int count = 10)
        {
            if (count <= 0) return Array.Empty<LogEntry>();

            lock (_lock)
            {
                int take = Math.Min(count, _entries.Count);
                return _entries.Skip(Math.Max(0, _entries.Count - take)).ToList().AsReadOnly();
            }
        }

        public string GetRecentFormatted(int count = 10)
        {
            var items = GetRecent(count);
            if (items.Count == 0) return string.Empty;

            var sb = new StringBuilder();
            foreach (var e in items)
            {
                sb.AppendLine($"[{e.Timestamp:yyyy-MM-dd HH:mm:ss}] {e.Message}");
            }
            return sb.ToString().TrimEnd();
        }

        public void Clear()
        {
            lock (_lock) { _entries.Clear(); }
        }

        public sealed class LogEntry
        {
            public DateTimeOffset Timestamp { get; }
            public string Message { get; }

            public LogEntry(DateTimeOffset timestamp, string message)
            {
                Timestamp = timestamp;
                Message = message ?? string.Empty;
            }

            public override string ToString() => $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] {Message}";
        }
    }
}
