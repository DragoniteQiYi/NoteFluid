using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using NoteFluid.Core.ViewModels;
using NoteFluid.Core.Views;

namespace NoteFluid.Core
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly IServiceProvider _serviceProvider;

        public MainWindow(IServiceProvider serviceProvider, MainViewModel mainViewModel)
        { 
            InitializeComponent();

            _serviceProvider = serviceProvider;
            DataContext = mainViewModel;

            var mainMenu = _serviceProvider.GetRequiredService<MainMenu>();
            MainFrame.Navigate(mainMenu);
        }

        private void NavigateToMainMenu(object sender, RoutedEventArgs e)
        {
            var mainMenu = _serviceProvider.GetRequiredService<MainMenu>();
            MainFrame.Navigate(mainMenu);
        }
    }
}