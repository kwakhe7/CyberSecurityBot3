using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace ChatbotPart2
{
    public partial class TaskJsonViewer : Window
    {
        private readonly string _filePath;

        public TaskJsonViewer()
        {
            InitializeComponent();
            _filePath = TaskStorageHelper.GetDefaultFilePath();
            LoadFile();
        }

        private void LoadFile()
        {
            try
            {
                if (!File.Exists(_filePath))
                {
                    JsonTextBox.Text = "(tasks.json not found)\n" + _filePath;
                    return;
                }

                var raw = File.ReadAllText(_filePath);
                // Try to pretty print JSON, fallback to raw
                try
                {
                    using var doc = JsonDocument.Parse(raw);
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    var formatted = JsonSerializer.Serialize(doc.RootElement, options);
                    JsonTextBox.Text = formatted;
                }
                catch
                {
                    JsonTextBox.Text = raw;
                }
            }
            catch (Exception ex)
            {
                JsonTextBox.Text = $"Error loading file: {ex.Message}";
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadFile();
        }

        private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var folder = Path.GetDirectoryName(_filePath) ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                Process.Start(new ProcessStartInfo
                {
                    FileName = folder,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            catch { }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
