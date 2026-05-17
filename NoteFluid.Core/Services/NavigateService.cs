using Microsoft.Extensions.DependencyInjection;
using NoteFluid.Core.Views;
using System.Windows.Controls;

namespace NoteFluid.Core.Services
{
    public class NavigateService(IServiceProvider serviceProvider)
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private Frame? _frame;

        public event Action<Page>? Navigated;

        public void SetFrame(Frame frame)
        {
            _frame = frame;
        }

        public void Navigate(string pageName)
        {
            Page page = pageName switch
            {
                "MainMenu" => _serviceProvider.GetRequiredService<MainMenu>(),
                "Settings" => _serviceProvider.GetRequiredService<Settings>(),
                "FileList" => _serviceProvider.GetRequiredService<FileList>(),
                _ => throw new ArgumentException($"未知页面: {pageName}")
            };

            _frame?.Navigate(page);
            Navigated?.Invoke(page);
        }
    }
}
