using MaterialDesignThemes.Wpf;
using NoteFluid.Core.Models;
using NoteFluid.Core.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace NoteFluid.Core.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly NavigateService _navigateService;
        private readonly ThemeService _themeService;
        private readonly AudioService _audioService;
        private readonly ConfigService _configService;

        private List<AudioDeviceInfo>? _outputDevices;
        private AudioDeviceInfo? _selectedDevice;
        private AudioDeviceInfo? _defaultDevice;
        private int _currentDeviceIndex;
        private bool _isLoading;
        private string? _statusMessage;
        private ObservableCollection<ColorItem> _colors;
        private int _selectedColorIndex;
        private ColorItem _selectedColor;

        public event PropertyChangedEventHandler? PropertyChanged;

        #region 属性

        /// <summary>
        /// 音频输出设备列表
        /// </summary>
        public List<AudioDeviceInfo>? OutputDevices
        {
            get => _outputDevices;
            set
            {
                _outputDevices = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 当前选中的设备
        /// </summary>
        public AudioDeviceInfo? SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                if (_selectedDevice != value)
                {
                    _selectedDevice = value;
                    OnPropertyChanged();

                    // 当选择改变时自动切换设备
                    if (_selectedDevice != null)
                    {
                        SwitchDevice(_currentDeviceIndex);
                    }
                }
            }
        }

        /// <summary>
        /// 默认设备
        /// </summary>
        public AudioDeviceInfo? DefaultDevice
        {
            get => _defaultDevice;
            set
            {
                _defaultDevice = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 加载状态
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsNotLoading));
            }
        }

        /// <summary>
        /// 非加载状态（用于UI绑定）
        /// </summary>
        public bool IsNotLoading => !IsLoading;

        /// <summary>
        /// 状态消息
        /// </summary>
        public string? StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<ColorItem> Colors
        {
            get => _colors;
            set { _colors = value; OnPropertyChanged(); }
        }

        public int SelectedColorIndex
        {
            get => _selectedColorIndex;
            set
            {
                _selectedColorIndex = value;
                OnPropertyChanged();
                // 通过索引获取颜色
                if (value >= 0 && value < Colors?.Count)
                {
                    _selectedColor = Colors[value];
                    OnPropertyChanged(nameof(SelectedColor));
                    ChangeColor(Colors[value].Color, value);
                }
            }
        }

        public ColorItem SelectedColor
        {
            get => _selectedColor;
            set
            {
                _selectedColor = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 是否有选中的设备
        /// </summary>
        public bool HasSelectedDevice => SelectedDevice != null;

        #endregion

        public MainViewModel(NavigateService navigateService, 
            ThemeService themeService, AudioService audioService,
            ConfigService configService)
        {
            _navigateService = navigateService;
            _themeService = themeService;
            _audioService = audioService;
            _configService = configService;

            Colors =
            [
                new() { Name = "暖桃红", ColorHex = "#FF6B6B" },
                new() { Name = "蜜橙", ColorHex = "#FFA94D" },
                new() { Name = "翠绿", ColorHex = "#2F9E44" },
                new() { Name = "天空蓝", ColorHex = "#4DABF7" },
                new() { Name = "薰衣草紫", ColorHex = "#9775FA" }
            ];

            BaseTheme baseTheme;
            int colorIndex;
            if (_configService.ConfigData.Theme != null)
            {
                baseTheme = _configService.ConfigData.Theme.IsDarkTheme ?
                    BaseTheme.Dark : BaseTheme.Light;
                colorIndex = _configService.ConfigData.Theme.ColorIndex;

                SelectedColorIndex = colorIndex;
                _themeService.SetBaseTheme(baseTheme);
                // ChangeColor(Colors[colorIndex].Color, colorIndex);
            }
        }

        public void Navigate(string pageName)
        {
            _navigateService.Navigate(pageName);
        }

        public void GetAudioDevices()
        {

            _defaultDevice = _audioService.GetDefaultDevice();
            _outputDevices = _audioService.GetOutputDevices();
            if (_selectedDevice == null)
            {
                _selectedDevice = _outputDevices.FirstOrDefault(
                d => d.DeviceId == _defaultDevice.DeviceId
                ) ?? _outputDevices.FirstOrDefault();
            }
            else
            {
                _selectedDevice = _outputDevices[_currentDeviceIndex];
            }


            foreach (var outputDevice in _outputDevices)
            {
                Debug.WriteLine(outputDevice.DeviceFriendlyName);
            }
        }

        /// <summary>
        /// 切换音频设备
        /// </summary>
        public void SwitchDevice(int index)
        {
            try
            {
                var device = _outputDevices?[index];       
                StatusMessage = $"正在切换到设备: {device?.DeviceName}";


                bool success = _audioService.SetOutputDevice(index);

                if (success)
                {
                    StatusMessage = $"已切换到设备: {device?.DeviceName}";
                    _currentDeviceIndex = index;
                }
                else
                {
                    StatusMessage = $"切换设备失败: {device?.DeviceName}";
                }

            }
            catch (Exception ex)
            {
                StatusMessage = $"切换设备异常: {ex.Message}";
                Debug.WriteLine($"切换设备异常: {ex.Message}");
            }
        }

        public void PlayTestAudio()
        {
            _audioService.TestOutputDevice();
        }

        private void ChangeColor(Color selectedColor, int colorIndex)
        {
            _themeService.ChangePrimaryColor(selectedColor);
            if (_configService.ConfigData.Theme != null)
            {
                _configService.ConfigData.Theme.ColorIndex = colorIndex;
                _configService.Save();
            }
        }

        public void SetBaseTheme(BaseTheme baseTheme)
        {
            _themeService.SetBaseTheme(baseTheme);
            if (_configService.ConfigData.Theme != null)
            {
                _configService.ConfigData.Theme.IsDarkTheme =
                    baseTheme == BaseTheme.Dark ? true : false;
                _configService.Save();
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
