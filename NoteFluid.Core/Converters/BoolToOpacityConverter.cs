using System.Globalization;
using System.Windows.Data;

namespace NoteFluid.Core.Converters
{
    public class BoolToOpacityConverter : IValueConverter
    {
        /// <summary>
        /// True 时的透明度，默认 1.0（完全不透明）
        /// </summary>
        public double TrueOpacity { get; set; } = 1.0;

        /// <summary>
        /// False 时的透明度，默认 0.5（半透明）
        /// </summary>
        public double FalseOpacity { get; set; } = 0.5;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? TrueOpacity : FalseOpacity;
            }
            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double opacity)
            {
                return Math.Abs(opacity - TrueOpacity) < 0.01;
            }
            return Binding.DoNothing;
        }
    }
}