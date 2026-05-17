using NoteFluid.Core.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace NoteFluid.Core.Views
{
    /// <summary>
    /// MainMenu.xaml 的交互逻辑
    /// </summary>
    public partial class MainMenu : Page
    {
        private readonly IServiceProvider _serviceProvider;

        private readonly MainViewModel _mainViewModel;

        public MainMenu(IServiceProvider serviceProvider, MainViewModel mainViewModel)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;
            _mainViewModel = mainViewModel;
            DataContext = mainViewModel;
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            _mainViewModel.Navigate("Settings");
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            _mainViewModel.Navigate("FileList");
        }

        private void ExitButton_Click(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
