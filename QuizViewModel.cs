using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Threading;

namespace ChatbotPart2
{
    public class QuizViewModel : INotifyPropertyChanged
    {
        private readonly QuizManager _manager;
        private int _selectedOptionIndex = -1;
        private string _currentQuestionText = string.Empty;
        private string _feedback = string.Empty;
        private string _finalSummary = string.Empty;
        private bool _isQuizActive;
        private int _timePerQuestionSeconds = 30;
        private int _timeRemainingSeconds = 30;
        private readonly DispatcherTimer _timer;

        public ObservableCollection<string> Options { get; } = new();
        public ICommand StartCommand { get; }
        public ICommand SubmitCommand { get; }

        public int SelectedOptionIndex
        {
            get => _selectedOptionIndex;
            set { _selectedOptionIndex = value; OnPropertyChanged(nameof(SelectedOptionIndex)); ((RelayCommand)SubmitCommand).RaiseCanExecuteChanged(); }
        }

        public string CurrentQuestionText
        {
            get => _currentQuestionText;
            private set { _currentQuestionText = value; OnPropertyChanged(nameof(CurrentQuestionText)); }
        }

        public string Feedback
        {
            get => _feedback;
            private set { _feedback = value; OnPropertyChanged(nameof(Feedback)); }
        }

        public string FinalSummary
        {
            get => _finalSummary;
            private set { _finalSummary = value; OnPropertyChanged(nameof(FinalSummary)); }
        }

        public bool IsQuizActive
        {
            get => _isQuizActive;
            private set { _isQuizActive = value; OnPropertyChanged(nameof(IsQuizActive)); ((RelayCommand)StartCommand).RaiseCanExecuteChanged(); ((RelayCommand)SubmitCommand).RaiseCanExecuteChanged(); }
        }

        // Timer-related (bind these into the view)
        public int TimePerQuestionSeconds
        {
            get => _timePerQuestionSeconds;
            set { _timePerQuestionSeconds = Math.Max(5, value); OnPropertyChanged(nameof(TimePerQuestionSeconds)); }
        }

        public int TimeRemainingSeconds
        {
            get => _timeRemainingSeconds;
            private set { _timeRemainingSeconds = Math.Max(0, value); OnPropertyChanged(nameof(TimeRemainingSeconds)); OnPropertyChanged(nameof(TimeRemainingText)); OnPropertyChanged(nameof(TimeProgress)); }
        }

        public string TimeRemainingText => TimeSpan.FromSeconds(TimeRemainingSeconds).ToString(@"mm\:ss");

        // Progress value (0..1) for styling progress bars
        public double TimeProgress => TimePerQuestionSeconds == 0 ? 0.0 : (double)TimeRemainingSeconds / TimePerQuestionSeconds;

        // Question counter text (e.g. "2 / 10")
        private string _questionCounter = string.Empty;
        public string QuestionCounter
        {
            get => _questionCounter;
            private set { _questionCounter = value; OnPropertyChanged(nameof(QuestionCounter)); }
        }

        public QuizViewModel()
        {
            _manager = new QuizManager();
            StartCommand = new RelayCommand(_ => StartQuiz(), _ => !IsQuizActive);
            SubmitCommand = new RelayCommand(_ => SubmitAnswer(), _ => IsQuizActive && SelectedOptionIndex >= 0);

            _timer = new DispatcherTimer(DispatcherPriority.Normal)
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (!IsQuizActive) { _timer.Stop(); return; }

            TimeRemainingSeconds--;
            if (TimeRemainingSeconds <= 0)
            {
                // auto-submit as timeout using -1 (treated as incorrect)
                SubmitAnswer(autoTimeout: true);
            }
        }

        public void StartQuiz(int? questionCount = 10)
        {
            _manager.StartQuiz(questionCount, shuffle: true);
            IsQuizActive = _manager.IsActive;
            Feedback = string.Empty;
            FinalSummary = string.Empty;
            SelectedOptionIndex = -1;

            LoadCurrentQuestion();
        }

        private void LoadCurrentQuestion()
        {
            var q = _manager.GetCurrentQuestion();
            Options.Clear();
            SelectedOptionIndex = -1;

            if (q == null)
            {
                CurrentQuestionText = string.Empty;
                IsQuizActive = false;
                FinalSummary = _manager.GetFinalSummary() ?? string.Empty;
                StopTimer();
                QuestionCounter = string.Empty;
                return;
            }

            CurrentQuestionText = q.Question;
            foreach (var o in q.Options) Options.Add(o);

            // update question counter
            var idx = _manager.CurrentQuestionIndex; // zero-based
            var total = _manager.TotalQuestions;
            QuestionCounter = $"{Math.Min(idx + 1, Math.Max(1, total))} / {total}";

            // reset and start timer
            TimeRemainingSeconds = TimePerQuestionSeconds;
            StartTimer();
            ((RelayCommand)SubmitCommand).RaiseCanExecuteChanged();
        }

        private void SubmitAnswer(bool autoTimeout = false)
        {
            // If not active, ignore
            if (!IsQuizActive) return;

            int submitIndex = SelectedOptionIndex;
            if (autoTimeout) submitIndex = -1;

            var (isCorrect, feedback, finished) = _manager.AnswerCurrentQuestion(submitIndex);
            Feedback = feedback;
            IsQuizActive = _manager.IsActive;

            // stop timer for the just-answered question so UI can show feedback
            StopTimer();

            // small pause could be added by UI; for simplicity we load next immediately
            if (finished)
            {
                FinalSummary = _manager.GetFinalSummary() ?? string.Empty;
                CurrentQuestionText = string.Empty;
                Options.Clear();
                QuestionCounter = string.Empty;
                SelectedOptionIndex = -1;
            }
            else
            {
                LoadCurrentQuestion();
            }
        }

        private void StartTimer()
        {
            _timer.Stop();
            TimeRemainingSeconds = TimePerQuestionSeconds;
            _timer.Start();
        }

        private void StopTimer()
        {
            _timer.Stop();
        }

        // Minimal RelayCommand for this VM (with Raise)
        private class RelayCommand : ICommand
        {
            private readonly Action<object?> _execute;
            private readonly Predicate<object?>? _canExecute;
            public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }
            public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
            public void Execute(object? parameter) => _execute(parameter);
            public event EventHandler? CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }
            public void RaiseCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}