using NoteFluid.Core.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoteFluid.Core.ViewModels
{
    public class VisualizationViewModel : INotifyPropertyChanged
    {
        private readonly NavigateService _navigateService;

        public event PropertyChangedEventHandler? PropertyChanged;

        public VisualizationViewModel(NavigateService navigateService)
        {
            _navigateService = navigateService;
        }

        public void NavigateTo(string pageName)
        {
            _navigateService.Navigate(pageName);
        }
    }
}
