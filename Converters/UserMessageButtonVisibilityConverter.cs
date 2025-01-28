using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WpfApp1.Converters
{
    public class UserMessageButtonVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isUser = (bool)value;
            return !isUser ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 