using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using ChatbotPart2.Services;

namespace ChatbotPart2
{
    public partial class ActivityLogWindow : Window
    {
        private readonly ActivityLogger _logger;

        public ActivityLogWindow(ActivityLogger logger)
        {
            InitializeComponent();
            _logger = logger;
            LoadEntries();
        }

        private void LoadEntries()
        {
            ActivityListBox.Items.Clear();
            var items = _logger.GetRecent(100); // show up to 100 recent entries
            foreach (var e in items.Reverse()) // newest first
            {
                ActivityListBox.Items.Add($"[{e.Timestamp:yyyy-MM-dd HH:mm:ss}] {e.Message}");
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            _logger.Clear();
            LoadEntries();
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            if (ActivityListBox.Items.Count == 0) return;

            var sb = new StringBuilder();
            if (ActivityListBox.SelectedItems.Count > 0)
            {
                foreach (var it in ActivityListBox.SelectedItems)
                {
                    sb.AppendLine(it.ToString());
                }
            }
            else
            {
                foreach (var it in ActivityListBox.Items)
                {
                    sb.AppendLine(it.ToString());
                }
            }

            Clipboard.SetText(sb.ToString());
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
