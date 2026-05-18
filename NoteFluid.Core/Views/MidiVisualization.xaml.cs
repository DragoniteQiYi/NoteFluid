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
            // 只重新生成数据，让XAML自动绑定
            _viewModel.DrawPiano(PianoCanvas, ActualWidth);
            _viewModel.GeneratePianoKeys(PianoCanvas.ActualWidth > 0 ? PianoCanvas.ActualWidth : 1000);
        }

        private void BackToFileListButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.NavigateTo("FileList");
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
    }
}
