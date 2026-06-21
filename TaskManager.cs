using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

namespace ChatbotPart2
{
    /// <summary>
    /// ViewModel for the task assistant UI. Binds to TaskStorageHelper for persistence.
    /// Keep this UI-agnostic (no WPF types in method signatures) so the XAML can bind easily.
    /// </summary>
    public class TaskManagerViewModel : INotifyPropertyChanged
    {
        private readonly TaskStorageHelper _storage;

        public ObservableCollection<TaskStorageHelper.TaskItem> Tasks { get; } = new();

        private TaskStorageHelper.TaskItem? _selectedTask;
        public TaskStorageHelper.TaskItem? SelectedTask
        {
            get => _selectedTask;
            set { _selectedTask = value; OnPropertyChanged(nameof(SelectedTask)); }
        }

        public string NewTitle { get; set; } = string.Empty;
        public string NewDescription { get; set; } = string.Empty;
        public DateTime? NewReminderLocal { get; set; }

        public ICommand AddCommand { get; }
        public ICommand CompleteCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand RefreshCommand { get; }

        public TaskManagerViewModel(TaskStorageHelper? storage = null)
        {
            _storage = storage ?? new TaskStorageHelper();

            AddCommand = new RelayCommand(_ => AddTask(), _ => !string.IsNullOrWhiteSpace(NewTitle));
            CompleteCommand = new RelayCommand(_ => CompleteSelected(), _ => SelectedTask != null && !SelectedTask.IsCompleted);
            DeleteCommand = new RelayCommand(_ => DeleteSelected(), _ => SelectedTask != null);
            RefreshCommand = new RelayCommand(_ => LoadTasks());

            LoadTasks();
        }

        public void LoadTasks()
        {
            Tasks.Clear();
            var list = _storage.GetTasks(true);
            foreach (var t in list.OrderBy(t => t.IsCompleted).ThenBy(t => t.ReminderUtc ?? DateTime.MaxValue))
                Tasks.Add(t);
        }

        public void AddTask()
        {
            var created = _storage.AddTask(NewTitle, NewDescription, NewReminderLocal);
            Tasks.Add(created);

            // clear inputs
            NewTitle = string.Empty;
            NewDescription = string.Empty;
            NewReminderLocal = null;

            OnPropertyChanged(nameof(NewTitle));
            OnPropertyChanged(nameof(NewDescription));
            OnPropertyChanged(nameof(NewReminderLocal));
        }

        public void CompleteSelected()
        {
            if (SelectedTask == null) return;
            if (_storage.CompleteTask(SelectedTask.Id))
            {
                SelectedTask.IsCompleted = true;
                SelectedTask.CompletedAtUtc = DateTime.UtcNow;
                // refresh ordering
                LoadTasks();
            }
        }

        public void DeleteSelected()
        {
            if (SelectedTask == null) return;
            if (_storage.DeleteTask(SelectedTask.Id))
            {
                Tasks.Remove(SelectedTask);
                SelectedTask = null;
            }
        }

        // Simple RelayCommand (small, self-contained)
        private class RelayCommand : ICommand
        {
            private readonly Action<object?> _execute;
            private readonly Predicate<object?>? _canExecute;

            public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }

            public event EventHandler? CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
            public void Execute(object? parameter) => _execute(parameter);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}