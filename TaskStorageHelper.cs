using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ChatbotPart2
{
    /// <summary>
    /// Handles persistent storage of tasks in a local JSON file.
    /// Public, thread-safe, and UI-agnostic. Callers (ViewModels / UI)
    /// should catch exceptions from RunSelfTest if they want to surface errors.
    /// </summary>
    public class TaskStorageHelper
    {
        private readonly string _filePath;
        private readonly object _lock = new();
        private List<TaskItem> _tasks = new();

        public TaskStorageHelper(string? filePath = null)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ChatbotPart2");
                Directory.CreateDirectory(folder);
                _filePath = Path.Combine(folder, "tasks.json");
            }
            else
            {
                var folder = Path.GetDirectoryName(filePath) ?? Path.GetDirectoryName(Path.GetFullPath(filePath)) ?? Path.GetTempPath();
                Directory.CreateDirectory(folder);
                _filePath = filePath;
            }

            LoadTasks();
        }

        // Expose default file path used when no explicit path is provided.
        public static string GetDefaultFilePath()
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ChatbotPart2");
            try { Directory.CreateDirectory(folder); } catch { }
            return Path.Combine(folder, "tasks.json");
        }

        // Public model for tasks
        public class TaskItem
        {
            public Guid Id { get; set; } = Guid.NewGuid();
            public string Title { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            // stored in UTC
            public DateTime? ReminderUtc { get; set; }
            public bool IsCompleted { get; set; } = false;
            public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
            public DateTime? CompletedAtUtc { get; set; }
        }

        // Add a new task. reminderLocal is optional (local time) and will be converted to UTC.
        public TaskItem AddTask(string title, string description, DateTime? reminderLocal = null)
        {
            var task = new TaskItem
            {
                Title = title ?? string.Empty,
                Description = description ?? string.Empty,
                ReminderUtc = reminderLocal?.ToUniversalTime()
            };

            lock (_lock)
            {
                _tasks.Add(task);
                SaveTasks();
            }

            return task;
        }

        // Get tasks. includeCompleted controls whether completed tasks are returned.
        public IReadOnlyList<TaskItem> GetTasks(bool includeCompleted = true)
        {
            lock (_lock)
            {
                return includeCompleted ? _tasks.ToList() : _tasks.Where(t => !t.IsCompleted).ToList();
            }
        }

        public bool CompleteTask(Guid id)
        {
            lock (_lock)
            {
                var t = _tasks.FirstOrDefault(x => x.Id == id);
                if (t == null || t.IsCompleted) return false;
                t.IsCompleted = true;
                t.CompletedAtUtc = DateTime.UtcNow;
                SaveTasks();
                return true;
            }
        }

        public bool DeleteTask(Guid id)
        {
            lock (_lock)
            {
                var removed = _tasks.RemoveAll(x => x.Id == id) > 0;
                if (removed) SaveTasks();
                return removed;
            }
        }

        // Return due reminders as of asOfLocal (null => now). Compares using UTC.
        public IReadOnlyList<TaskItem> GetDueReminders(DateTime? asOfLocal = null)
        {
            var asOfUtc = (asOfLocal ?? DateTime.Now).ToUniversalTime();
            lock (_lock)
            {
                return _tasks.Where(t => t.ReminderUtc.HasValue && !t.IsCompleted && t.ReminderUtc.Value <= asOfUtc).ToList();
            }
        }

        // Load tasks from JSON file into memory (silent failure to avoid crashing UI).
        private void LoadTasks()
        {
            try
            {
                if (!File.Exists(_filePath))
                {
                    _tasks = new List<TaskItem>();
                    return;
                }

                var json = File.ReadAllText(_filePath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var loaded = JsonSerializer.Deserialize<List<TaskItem>>(json, options);
                _tasks = loaded ?? new List<TaskItem>();
            }
            catch
            {
                // don't throw from constructor; leave empty list and let callers decide how to surface errors
                _tasks = new List<TaskItem>();
            }
        }

        // Save current in-memory tasks to disk (best-effort; swallow exceptions to keep UI responsive).
        private void SaveTasks()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(_tasks, options);
                File.WriteAllText(_filePath, json);
            }
            catch
            {
                // swallow for now — consider logging or reporting to user in future
            }
        }

        // Self-test helper to verify write/read behaviour. Useful for unit-tests or quick verification.
        // Returns true when a full write/read roundtrip produced the expected task.
        public static bool RunSelfTest(out string message)
        {
            // Use a temp file to avoid touching user data
            var tmpFile = Path.Combine(Path.GetTempPath(), $"chatbot_tasks_test_{Guid.NewGuid():N}.json");
            try
            {
                var helper = new TaskStorageHelper(tmpFile);

                // Ensure clean start
                foreach (var t in helper.GetTasks(true).ToList())
                {
                    helper.DeleteTask(t.Id);
                }

                var title = "SelfTest Task";
                var desc = "Verify write/read";
                var reminder = DateTime.Now.AddMinutes(5);

                var added = helper.AddTask(title, desc, reminder);

                // Create a new instance and load from disk to verify persistence
                var helper2 = new TaskStorageHelper(tmpFile);
                var loaded = helper2.GetTasks(true).FirstOrDefault(t => t.Id == added.Id);

                if (loaded == null)
                {
                    message = "Roundtrip failed: task not present after re-load.";
                    return false;
                }

                if (loaded.Title != title || loaded.Description != desc)
                {
                    message = "Roundtrip failed: data mismatch.";
                    return false;
                }

                // cleanup
                helper2.DeleteTask(added.Id);
                if (File.Exists(tmpFile)) File.Delete(tmpFile);

                message = "Self-test succeeded.";
                return true;
            }
            catch (Exception ex)
            {
                try { if (File.Exists(tmpFile)) File.Delete(tmpFile); } catch { }
                message = $"Self-test threw exception: {ex.Message}";
                return false;
            }
        }
    }
}