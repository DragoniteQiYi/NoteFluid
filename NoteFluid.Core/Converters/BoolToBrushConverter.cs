// Converters/BoolToBrushConverter.cs
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace NoteFluid.Core.Converters
{
    public class BoolToBrushConverter : IValueConverter
    {
        public Brush TrueBrush { get; set; } = Brushes.White;
        public Brush FalseBrush { get; set; } = Brushes.Gray;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? TrueBrush : FalseBrush;
            }
            return FalseBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.IsNullOrEmpty(value as string) ?
                Visibility.Collapsed :
                Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}