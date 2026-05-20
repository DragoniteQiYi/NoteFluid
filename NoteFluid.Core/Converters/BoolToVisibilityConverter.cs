using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace NoteFluid.Core.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// 是否反转逻辑（False 时显示，True 时隐藏）
        /// </summary>
        public bool IsInverted { get; set; } = false;

        /// <summary>
        /// False 时使用 Hidden 还是 Collapsed，默认 Collapsed
        /// </summary>
        public bool UseHidden { get; set; } = false;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                if (IsInverted)
                    boolValue = !boolValue;

                return boolValue ? Visibility.Visible
                                 : (UseHidden ? Visibility.Hidden : Visibility.Collapsed);
            }
            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                bool result = visibility == Visibility.Visible;
                return IsInverted ? !result : result;
            }
            return Binding.DoNothing;
        }
    }
}