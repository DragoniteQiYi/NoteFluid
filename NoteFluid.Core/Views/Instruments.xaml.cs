using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;
using NoteFluid.Core.Models;
using NoteFluid.Core.ViewModels;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;

namespace NoteFluid.Core.Views
{
    /// <summary>
    /// Instruments.xaml 的交互逻辑
    /// </summary>
    public partial class Instruments : Page
    {
        private readonly InstrumentsViewModel _viewModel;

        public Instruments(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _viewModel = serviceProvider.GetRequiredService<InstrumentsViewModel>();
            DataContext = _viewModel;

            // 注册转换器
            RegisterConverters();
        }

        /// <summary>
        /// 注册页面级资源转换器
        /// </summary>
        private void RegisterConverters()
        {
            // 颜色到画刷转换器
            if (Resources["ColorToBrushConverter"] == null)
            {
                Resources.Add("ColorToBrushConverter", new ColorToBrushConverter());
            }

            // 布尔到可见性图标转换器
            if (Resources["BoolToVisibilityIconConverter"] == null)
            {
                Resources.Add("BoolToVisibilityIconConverter", new BoolToVisibilityIconConverter());
            }

            // 布尔到静音图标转换器
            if (Resources["BoolToMuteIconConverter"] == null)
            {
                Resources.Add("BoolToMuteIconConverter", new BoolToMuteIconConverter());
            }

            // 布尔到独奏图标转换器
            if (Resources["BoolToSoloIconConverter"] == null)
            {
                Resources.Add("BoolToSoloIconConverter", new BoolToSoloIconConverter());
            }

            // 反向布尔到可见性转换器
            if (Resources["InverseBooleanToVisibilityConverter"] == null)
            {
                Resources.Add("InverseBooleanToVisibilityConverter", new InverseBooleanToVisibilityConverter());
            }
        }

        /// <summary>
        /// 返回按钮点击
        /// </summary>
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.NavigateTo("FileList");
        }

        /// <summary>
        /// 颜色菜单项点击
        /// </summary>
        private void ColorMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem &&
                menuItem.DataContext is InstrumentInfo instrumentInfo &&
                menuItem.Tag is string colorHex)
            {
                try
                {
                    var color = (Color)ColorConverter.ConvertFromString(colorHex);
                    _viewModel.ChangeInstrumentColor(instrumentInfo.InstrumentId, color);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Instruments] 更改颜色失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 自定义颜色菜单项点击
        /// </summary>
        private void CustomColorMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.DataContext is InstrumentInfo instrumentInfo)
            {
                var colorDialog = new System.Windows.Forms.ColorDialog();
                colorDialog.Color = System.Drawing.Color.FromArgb(
                    instrumentInfo.Color.A,
                    instrumentInfo.Color.R,
                    instrumentInfo.Color.G,
                    instrumentInfo.Color.B);

                if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var color = Color.FromArgb(
                        colorDialog.Color.A,
                        colorDialog.Color.R,
                        colorDialog.Color.G,
                        colorDialog.Color.B);
                    _viewModel.ChangeInstrumentColor(instrumentInfo.InstrumentId, color);
                }
            }
        }

        /// <summary>
        /// 可见性按钮点击
        /// </summary>
        private void VisibilityButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton button && button.DataContext is InstrumentInfo instrumentInfo)
            {
                _viewModel.ToggleInstrumentVisibility(instrumentInfo.InstrumentId);
            }
        }

        /// <summary>
        /// 静音按钮点击
        /// </summary>
        private void MuteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton button && button.DataContext is InstrumentInfo instrumentInfo)
            {
                _viewModel.ToggleInstrumentMute(instrumentInfo.InstrumentId);
            }
        }

        /// <summary>
        /// 独奏按钮点击
        /// </summary>
        private void SoloButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton button && button.DataContext is InstrumentInfo instrumentInfo)
            {
                _viewModel.ToggleInstrumentSolo(instrumentInfo.InstrumentId);
            }
        }

        /// <summary>
        /// 重置颜色按钮点击
        /// </summary>
        private void ResetColorsButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ResetColorsToDefault();
        }

        private void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.NavigateTo("MidiVisualization");
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel.LoadSelectedMidiFile();
        }
    }

    #region 转换器类

    /// <summary>
    /// 颜色到画刷转换器
    /// </summary>
    public class ColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Color color)
            {
                return new SolidColorBrush(color);
            }
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 布尔到可见性图标转换器
    /// </summary>
    public class BoolToVisibilityIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isVisible)
            {
                return isVisible ? PackIconKind.Eye : PackIconKind.EyeOff;
            }
            return PackIconKind.Eye;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 布尔到静音图标转换器
    /// </summary>
    public class BoolToMuteIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isMuted)
            {
                return isMuted ? PackIconKind.VolumeOff : PackIconKind.VolumeHigh;
            }
            return PackIconKind.VolumeHigh;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 布尔到独奏图标转换器
    /// </summary>
    public class BoolToSoloIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isSolo)
            {
                return isSolo ? PackIconKind.AlphaSCircle : PackIconKind.AlphaSCircleOutline;
            }
            return PackIconKind.AlphaSCircleOutline;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 反向布尔到可见性转换器
    /// </summary>
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility != Visibility.Visible;
            }
            return false;
        }
    }

    #endregion
}
