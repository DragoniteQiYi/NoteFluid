using NoteFluid.Core.ViewModels;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace NoteFluid.Core.Views
{
    /// <summary>
    /// FreePlay.xaml 的交互逻辑
    /// </summary>
    public partial class FreePlay : Page
    {
        private FreePlayViewModel _viewModel;

        // 存储当前活动的瀑布条
        private readonly Dictionary<int, WaterfallBar> _activeWaterfalls = [];

        public FreePlay(FreePlayViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = viewModel;

            // 初始化时只生成数据，不手动绘制
            SizeChanged += PianoKeyboard_SizeChanged;
            Loaded += Page_Loaded;

            // 订阅按键事件
            _viewModel.KeyPressed += OnKeyPressed;
            _viewModel.KeyReleased += OnKeyReleased;
        }

        private void PianoKeyboard_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ResetKeyboard();
        }

        // 处理按键按下事件
        private void OnKeyPressed(int midiNote)
        {
            Dispatcher.Invoke(() =>
            {
                var key = _viewModel.PianoKeys.FirstOrDefault(k => k.MidiNote == midiNote);
                if (key != null && WaterfallCanvas.ActualHeight > 0)
                {
                    // 如果该键已有瀑布条，先移除旧的
                    if (_activeWaterfalls.ContainsKey(midiNote))
                    {
                        var oldBar = _activeWaterfalls[midiNote];
                        oldBar.Release(); // 释放旧瀑布条，让它自然消失
                        _activeWaterfalls.Remove(midiNote);
                    }

                    // 计算缩放比例
                    double scaleX = WaterfallCanvas.ActualWidth / PianoCanvas.Width;

                    // 转换坐标：从 PianoCanvas 坐标到 WaterfallCanvas 坐标
                    double adjustedX = key.X * scaleX;
                    double adjustedWidth = key.Width * scaleX;

                    var waterfallBar = new WaterfallBar
                    {
                        Width = adjustedWidth,
                        Height = WaterfallCanvas.ActualHeight
                    };

                    // 设置瀑布条位置
                    Canvas.SetLeft(waterfallBar, adjustedX);
                    Canvas.SetTop(waterfallBar, 0);

                    WaterfallCanvas.Children.Add(waterfallBar);
                    _activeWaterfalls[midiNote] = waterfallBar;

                    // 启动增长动画
                    waterfallBar.StartGrowing(WaterfallCanvas.ActualHeight);
                    // waterfallBar.StartFalling(WaterfallCanvas.ActualHeight, 100);
                }
            });
        }

        // 处理按键释放事件
        private void OnKeyReleased(int midiNote)
        {
            Dispatcher.Invoke(() =>
            {
                if (_activeWaterfalls.TryGetValue(midiNote, out var waterfallBar))
                {
                    waterfallBar.Release(); // 停止增长但继续向上移动
                    _activeWaterfalls.Remove(midiNote);
                }
            });
        }

        private void BackToMainMenuButton_Click(object sender, RoutedEventArgs e)
        {
            // 清理所有瀑布条
            foreach (var bar in _activeWaterfalls.Values)
            {
                
            }
            _activeWaterfalls.Clear();

            _viewModel.NavigateTo("MainMenu");
        }

        // 当页面加载完成时初始化钢琴键
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // 初始生成数据
            _viewModel.DrawPiano(PianoCanvas, ActualWidth);
            // _viewModel.GeneratePianoKeys(PianoCanvas.ActualWidth > 0 ? PianoCanvas.ActualWidth : 1000);
        }

        private void PianoKey_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int midiNote)
            {
                Debug.WriteLine($"钢琴键被点击 - MIDI音符: {midiNote}");
                _viewModel.OnKeyClicked(midiNote);
            }
        }

        // 点击 Menu 按钮切换右侧侧边栏
        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            MainDrawerHost.IsRightDrawerOpen = !MainDrawerHost.IsRightDrawerOpen;
        }

        private void ShowPitchNameToggle_Checked(object sender, RoutedEventArgs e)
        {
            _viewModel.ChangePitchNameDisplay(true);
            ResetKeyboard();
        }

        private void ShowPitchNameToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            _viewModel.ChangePitchNameDisplay(false);
            ResetKeyboard();
        }

        private void ShowOctaveToggle_Checked(object sender, RoutedEventArgs e)
        {
            _viewModel.ChangeOctaveDisplay(true);
            ResetKeyboard();
        }

        private void ShowOctaveToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            _viewModel.ChangeOctaveDisplay(false);
            ResetKeyboard();
        }

        private void ResetKeyboard()
        {
            // 只重新生成数据，让XAML自动绑定
            _viewModel.DrawPiano(PianoCanvas, ActualWidth);
            _viewModel.GeneratePianoKeys(PianoCanvas.ActualWidth > 0 ? PianoCanvas.ActualWidth : 1000);
        }

        // 页面卸载时清理资源
        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _viewModel.KeyPressed -= OnKeyPressed;
            _viewModel.KeyReleased -= OnKeyReleased;

            foreach (var bar in _activeWaterfalls.Values)
            {
                
            }
            _activeWaterfalls.Clear();
        }
    }
}
