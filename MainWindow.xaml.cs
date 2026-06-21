using System;
using System.Windows;
using System.Windows.Input;
using ChatbotPart2.Services;

namespace ChatbotPart2
{
    public partial class MainWindow : Window
    {
        private ChatBot bot;
        private readonly ActivityLogger _activityLogger;

        public MainWindow()
        {
            InitializeComponent();

            bot = new ChatBot("SAFENET");
            _activityLogger = new ActivityLogger();

            LoadAsciiArt();
            PlayGreeting();

            // Prompt the user for their name on first start so replies can be personalized
            bot.PromptForName();
            AppendMessage("BOT", "Hi — what's your name? (Just type your first name)");

            _activityLogger.Log("Application started and greeted the user.");
        }

        // SEND BUTTON
        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        // ENTER KEY SUPPORT
        private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendMessage();
            }
        }

        // CORE MESSAGE HANDLER
        private void SendMessage()
        {
            string input = InputTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(input))
                return;

            AppendMessage("YOU", input);
            _activityLogger.Log($"User sent message: '{input}'");

            string response = bot.GetResponse(input);

            if (string.IsNullOrWhiteSpace(response))
            {
                response = "I'm not sure I understand. Try: password, phishing, malware, privacy, or safe browsing.";
            }

            AppendMessage("BOT", response);
            _activityLogger.Log("Bot responded to user input.");

            InputTextBox.Clear();

            ScrollToBottom();
        }

        // DISPLAY MESSAGE
        private void AppendMessage(string sender, string message)
        {
            ChatTextBlock.Text += $"{sender}: {message}\n\n";
        }

        // AUTO SCROLL
        private void ScrollToBottom()
        {
            ChatScrollViewer.ScrollToEnd();
        }

        // ASCII (you already have it in XAML, so this is optional)
        private void LoadAsciiArt()
        {
            // Not needed since ASCII is inside XAML already
        }

        // AUDIO GREETING
        private void PlayGreeting()
        {
            try
            {
                string path = System.IO.Path.Combine(AppContext.BaseDirectory, "greeting.wav");
                AudioPlayer.PlaySync(path);
                _activityLogger.Log("Played greeting audio.");
            }
            catch
            {
                // ignore errors
            }
        }

        // Mode switch buttons
        private void QuizModeButton_Click(object sender, RoutedEventArgs e)
        {
            // show quiz, hide chat/task and input
            ChatBorder.Visibility = Visibility.Collapsed;
            TaskViewControl.Visibility = Visibility.Collapsed;
            QuizViewControl.Visibility = Visibility.Visible;
            InputBorder.Visibility = Visibility.Collapsed;
        }

        private void TasksModeButton_Click(object sender, RoutedEventArgs e)
        {
            // show tasks, hide chat/quiz and input
            ChatBorder.Visibility = Visibility.Collapsed;
            QuizViewControl.Visibility = Visibility.Collapsed;
            TaskViewControl.Visibility = Visibility.Visible;
            InputBorder.Visibility = Visibility.Collapsed;
        }

        private void ChatModeButton_Click(object sender, RoutedEventArgs e)
        {
            // show chat, hide quiz and tasks and show input
            QuizViewControl.Visibility = Visibility.Collapsed;
            TaskViewControl.Visibility = Visibility.Collapsed;
            ChatBorder.Visibility = Visibility.Visible;
            InputBorder.Visibility = Visibility.Visible;
        }

        // Activity logger button - shows last 10 entries
        private void ActivityLoggerButton_Click(object sender, RoutedEventArgs e)
        {
            var wnd = new ActivityLogWindow(_activityLogger)
            {
                Owner = this
            };
            wnd.ShowDialog();
        }

        private void TasksJsonButton_Click(object sender, RoutedEventArgs e)
        {
            var wnd = new TaskJsonViewer
            {
                Owner = this
            };
            wnd.ShowDialog();
        }
    }
}