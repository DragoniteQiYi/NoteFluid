using NoteFluid.Core.ViewModels;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace NoteFluid.Core.Views
{
    /// <summary>
    /// MidiVisualization.xaml 的交互逻辑
    /// </summary>
    public partial class MidiVisualization : Page
    {
        private readonly VisualizationViewModel _viewModel;

        public MidiVisualization(VisualizationViewModel viewModel)
        {
            InitializeComponent();
            SizeChanged += PianoKeyboard_SizeChanged;
            DataContext = viewModel;
            _viewModel = viewModel;

            // 初始化时只生成数据，不手动绘制
            SizeChanged += PianoKeyboard_SizeChanged;
            Loaded += Page_Loaded;
        }

        private void PianoKeyboard_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ResetKeyboard();
        }

        private void BackToFileListButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.NavigateTo("FileList");
        }

        // 当页面加载完成时初始化钢琴键
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ResetKeyboard();
            _viewModel.PlayMidiFile(WaterfallCanvas, PianoCanvas);
        }

        private void PianoKey_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int midiNote)
            {
                Debug.WriteLine($"钢琴键被点击 - MIDI音符: {midiNote}");
                _viewModel.OnKeyClicked(midiNote);
            }
        }

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
            // _viewModel.GeneratePianoKeys(PianoCanvas.ActualWidth > 0 ? PianoCanvas.ActualWidth : 1000);
        }
    }
}
