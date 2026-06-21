using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace ChatbotPart2
{
    public partial class TaskView : UserControl
    {
        public TaskView()
        {
            InitializeComponent();
            this.DataContext = new TaskManagerViewModel();
        }
    }

    // Converter: show UTC DateTime as local readable string, or empty if null
    public class UtcToLocalConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dt)
            {
                // assume stored as UTC
                try
                {
                    var local = DateTime.SpecifyKind(dt, DateTimeKind.Utc).ToLocalTime();
                    return local.ToString("g"); // short date/time pattern
                }
                catch
                {
                    return dt.ToString("g");
                }
            }
            return string.Empty;
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // not required for one-way display
            return null;
        }
    }

    // Converter: bool -> "Completed"/"Pending"
    public class BoolToStatusConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b) return b ? "Completed" : "Pending";
            return "Pending";
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
    }

    // Converter: inverse bool (for IsEnabled bindings)
    public class InverseBoolConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b) return !b;
            return true;
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
    }
}