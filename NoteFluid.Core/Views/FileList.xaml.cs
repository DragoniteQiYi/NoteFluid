using NoteFluid.Core.ViewModels;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace NoteFluid.Core.Views
{
    /// <summary>
    /// FileList.xaml 的交互逻辑
    /// </summary>
    public partial class FileList : Page
    {
        private readonly FileViewModel _fileViewModel;

        public FileList(FileViewModel fileViewModel)
        {
            InitializeComponent();
            _fileViewModel = fileViewModel;
            DataContext = fileViewModel;

            ScrollViewer.PreviewMouseWheel += (sender, e) =>
            {
                // 阻止事件冒泡，确保 ScrollViewer 处理滚轮事件
                e.Handled = true;

                if (sender is not ScrollViewer scrollViewer) return;

                // 根据滚轮方向调整滚动位置
                if (e.Delta > 0)
                    scrollViewer.LineUp();
                else
                    scrollViewer.LineDown();
            };
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _fileViewModel.FilterFiles(SearchTextBox.Text);
        }

        private void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            _fileViewModel.NavigateTo("Instruments");
        }

        private void FileListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedFile = FileItemsControl.SelectedItem as FileInfo;
            if (selectedFile is not null and FileInfo)
            {
                _fileViewModel.SelectedFile = selectedFile;
            }
        }

        private void BackToMenuButton_Click(object sender, RoutedEventArgs e)
        {
            _fileViewModel.NavigateTo("MainMenu");
        }

        private void Slider_DragStarted(object sender, DragStartedEventArgs e)
        {
            _fileViewModel.IsSliderDragging = true;
        }

        private async void Slider_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            await _fileViewModel.SetProgressValue(ProgressSlider.Value);
            _fileViewModel.IsSliderDragging = false;
        }

        private void ClearSearchTextBox(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = string.Empty;
        }
    }
}
