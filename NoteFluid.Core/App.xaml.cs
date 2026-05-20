using Microsoft.Extensions.DependencyInjection;
using NoteFluid.Core.Services;
using NoteFluid.Core.ViewModels;
using NoteFluid.Core.Views;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace NoteFluid.Core
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool FreeConsole();

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;


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

            // 创建控制台窗口
            AllocConsole();

            // 设置控制台标题
            Console.Title = "调试控制台";

            Console.WriteLine("=== WPF 应用程序启动 ===");
            Console.WriteLine($"时间: {DateTime.Now}");
            Console.WriteLine("========================\n");

            Debug.WriteLine("=== WPF 应用程序启动 ===");
            Debug.WriteLine($"时间: {DateTime.Now}");
            Debug.WriteLine("========================\n");

            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Console.WriteLine("\n应用程序正在退出...");

            // 可选：释放控制台
            FreeConsole();

            base.OnExit(e);
        }
    }
}
