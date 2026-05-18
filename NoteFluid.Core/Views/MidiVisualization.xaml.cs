using NoteFluid.Core.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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
            _viewModel.DrawPiano(PianoCanvas, ActualWidth);
        }

        private void PianoKeyboard_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // 窗口大小改变时重新绘制
            _viewModel.DrawPiano(PianoCanvas, ActualWidth);
        }

        private void BackToFileListButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.NavigateTo("FileList");
        }
    }
}
