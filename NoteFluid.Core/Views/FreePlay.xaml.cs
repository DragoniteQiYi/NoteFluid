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

        public FreePlay(FreePlayViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = viewModel;

            // 初始化时只生成数据，不手动绘制
            SizeChanged += PianoKeyboard_SizeChanged;
            Loaded += Page_Loaded;
        }

        private void PianoKeyboard_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ResetKeyboard();
        }

        private void BackToMainMenuButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.NavigateTo("MainMenu");
        }

        // 当页面加载完成时初始化钢琴键
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // 初始生成数据
            _viewModel.DrawPiano(PianoCanvas, ActualWidth);
            _viewModel.GeneratePianoKeys(PianoCanvas.ActualWidth > 0 ? PianoCanvas.ActualWidth : 1000);
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
    }
}
