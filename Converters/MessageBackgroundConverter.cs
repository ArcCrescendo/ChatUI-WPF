using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace WpfApp1.Converters
{
    public class MessageBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isUser = (bool)value;
            return isUser ? new SolidColorBrush(Color.FromRgb(220, 248, 198)) // 用户消息背景色
                        : new SolidColorBrush(Color.FromRgb(255, 255, 255)); // AI消息背景色
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 