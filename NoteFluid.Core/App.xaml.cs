using Microsoft.Extensions.DependencyInjection;
using NoteFluid.Core.Services;
using NoteFluid.Core.ViewModels;
using NoteFluid.Core.Views;
using System.Diagnostics;
using System.Windows;

namespace NoteFluid.Core
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public IServiceProvider ServiceProvider { get; set; }

        public App()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            // 注册服务
            services.AddSingleton<ThemeService>();
            services.AddSingleton<NavigateService>();
            services.AddSingleton<AudioService>();
            services.AddSingleton<FileService>();
            services.AddSingleton<MidiService>();
            services.AddSingleton<ConfigService>();
            services.AddSingleton<VisualizationService>();
            services.AddSingleton<WaterfallService>();

            // 注册ViewModel
            services.AddSingleton<MainViewModel>();
            services.AddTransient<FileViewModel>();
            services.AddTransient<VisualizationViewModel>();
            services.AddTransient<FreePlayViewModel>();
            services.AddTransient<InstrumentsViewModel>();
            
            // 注册窗口/页面
            services.AddTransient<MainWindow>();
            services.AddTransient<MainMenu>();
            services.AddTransient<Settings>();
            services.AddTransient<FileList>();
            services.AddTransient<MidiVisualization>();
            services.AddTransient<FreePlay>();
            services.AddTransient<Instruments>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            var navigateService = ServiceProvider.GetRequiredService<NavigateService>();
            navigateService?.SetFrame(mainWindow.MainFrame);

#if DEBUG
            Debug.WriteLine("=== WPF 应用程序启动 ===");
            Debug.WriteLine($"时间: {DateTime.Now}");
            Debug.WriteLine("========================\n");
#endif

            mainWindow.Show();
        }
    }
}
