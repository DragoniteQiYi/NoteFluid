using MaterialDesignThemes.Wpf;
using NoteFluid.Core.Models;
using NoteFluid.Core.Services;
using NoteFluid.Core.ViewModels;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NoteFluid.Core.Views
{
    /// <summary>
    /// Settings.xaml 的交互逻辑
    /// </summary>
    public partial class Settings : Page
    {
        private readonly MainViewModel _mainViewModel;
        private readonly ThemeService _themeService;

        public Settings(MainViewModel mainViewModel,
            ThemeService themeService)
        {
            InitializeComponent();
            _mainViewModel = mainViewModel;
            _themeService = themeService;

            DataContext = mainViewModel;
            // 根据当前主题设置 ToggleButton 的初始状态
            DarkModeToggle.IsChecked = _themeService.IsDarkTheme();

            LoadAudioDevices();
        }

        // 加载音频输出设备
        private void LoadAudioDevices()
        {
            _mainViewModel.GetAudioDevices();
        }

        private void DarkModeToggle_Checked(object sender, RoutedEventArgs e)
        {
            // 切换到深色模式
            _mainViewModel.SetBaseTheme(BaseTheme.Dark);
        }

        private void DarkModeToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            // 切换到浅色模式
            _mainViewModel.SetBaseTheme(BaseTheme.Light);
        }

        private void ColorListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem is ColorItem selectedColor)
            {
                // 直接设置索引，触发ViewModel更新
                _mainViewModel.SelectedColorIndex = listBox.SelectedIndex; 
            }
        }

        // 音频设备选择变更
        private void AudioDeviceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AudioDeviceComboBox is ComboBox devicesBox)
            {
                var index = devicesBox.SelectedIndex;

                _mainViewModel.SwitchDevice(index);
                Debug.WriteLine($"Audio device changed to: {index}");

                // 在这里添加切换音频设备的逻辑
            }
        }

        // 测试声音按钮点击
        private void TestSoundButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 播放Windows系统提示音
                _mainViewModel.PlayTestAudio();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error playing sound: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 打开MIDI文件夹按钮点击
        private void OpenMidiFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 打开MIDI文件夹 - 可以根据实际需求修改路径
                string midiFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                    + @"\NoteFluid\MIDI";

                // 如果文件夹不存在则创建
                if (!Directory.Exists(midiFolderPath))
                {
                    Directory.CreateDirectory(midiFolderPath);
                }

                Process.Start("explorer.exe", midiFolderPath);
                Debug.WriteLine($"Opened MIDI folder: {midiFolderPath}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening MIDI folder: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 返回菜单按钮点击
        private void BackToMenuButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _mainViewModel.Navigate("MainMenu");
                Debug.WriteLine("Navigated back to menu");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Navigation error: {ex.Message}");
            }
        }

        private async void ColorPickerButton_Click(object sender, RoutedEventArgs e)
        {
            await DialogHost.Show(RootDialogHost.DialogContent);
        }

        private void ColorPickerHost_Cancel(object sender, RoutedEventArgs e)
        {
            DialogHost.Close("RootDialogHost");
        }


    }
}
