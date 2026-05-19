using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using NoteFluid.Core.Services;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace NoteFluid.Core.ViewModels
{
    public class FileViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly NavigateService _navigateService;
        private readonly FileService _fileService;
        private readonly MidiService _midiService;

        public List<FileInfo>? FileInfos
        {
            get => _fileInfos;
            set
            {
                _fileInfos = value;
                OnPropertyChanged();
            }
        }

        public List<FileInfo>? FilteredFiles { get; set; }

        public FileInfo? SelectedFile
        {
            get => _selectedFile;
            set
            {
                _selectedFile = value;
                _progressValue = 0;

                OnPropertyChanged();
                HandleSelectedFileChanged();
            }
        }

        public bool CanPlay
        {
            get => _selectedFile != null;
        }

        public bool IsPlaying
        {
            get => _isPlaying;
            set
            {
                _isPlaying = value;
                OnPropertyChanged();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        public bool IsPausing
        {
            get => _isPausing;
            set
            {
                _isPausing = value;
                OnPropertyChanged();
            }
        }

        public bool CanPlayOrStop
        {
            get => _canPlayOrStop;
            set
            {
                _canPlayOrStop = value;
                OnPropertyChanged();
            }
        }

        public double ProgressValue
        {
            get => _progressValue;
            set
            {
                _progressValue = value;
                OnPropertyChanged();
            }
        }

        public bool IsSliderDragging
        {
            get => _isSliderDragging;
            set
            {
                _isSliderDragging = value;
                OnPropertyChanged();
            }
        }

        public PackIconKind PlayIconKind
        {
            get => _playIconKind;
        }

        private List<FileInfo>? _fileInfos;
        private FileInfo? _selectedFile;
        private bool _isPlaying;
        private bool _isLoading;
        private bool _isPausing;
        private bool _isSliderDragging;
        private bool _canPlayOrStop;
        private double _progressValue;
        private PackIconKind _playIconKind = PackIconKind.Play;

        public ICommand PlayStopCommand { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public FileViewModel(NavigateService navigateService,
            FileService fileService,
            MidiService midiService)
        {
            _midiService = midiService;
            _navigateService = navigateService;
            _fileService = fileService;

            _midiService.OnMidiFilePlaying += HandlePlayStateChanged;
            _midiService.OnProgressChanged += HandleProgressValueChanged;
            _midiService.OnMidiFileCompleted += HandlePlayComleted;

            PlayStopCommand = new AsyncRelayCommand(PlayStopAsync);
            LoadFiles();
        }

        public void NavigateTo(string pagePath)
        {
            _midiService.StopMidiFile();
            CanPlayOrStop = false;
            IsPlaying = false;
            _playIconKind = PackIconKind.Play;
            HandlePlayStateChanged(false);
            OnPropertyChanged(nameof(CanPlay));
            OnPropertyChanged(nameof(ProgressValue));
            _navigateService.Navigate(pagePath);
            Dispose();
        }

        private async Task PlayStopAsync()
        {
            if (IsLoading || _selectedFile == null) return;

            if (!IsPlaying)
            {
                IsLoading = true;
                CanPlayOrStop = false;
               
                await _midiService.PlayMidiFile(_selectedFile);

                IsLoading = false;
                CanPlayOrStop = true;
                IsPlaying = true;
                IsPausing = false;
                _playIconKind = PackIconKind.Pause;
                OnPropertyChanged(nameof(PlayIconKind));
                return;
            }
            if (_isPausing)
            {
                IsPausing = false;
                _midiService.ResumeMidiFile();
                _playIconKind = PackIconKind.Pause;
                OnPropertyChanged(nameof(PlayIconKind));
            }
            else
            {
                IsPausing = true;
                _midiService.PauseMidiFile();
                _playIconKind = PackIconKind.Play;
                OnPropertyChanged(nameof(PlayIconKind));
            }
        }

        public async Task SetProgressValue(double value)
        {
            var normalizedValue = value / 100;
            await _midiService.SetProgress(normalizedValue);
        }

        public void FilterFiles(string regexText)
        {
            if (_fileInfos == null || _fileInfos.Count == 0)
            {
                FilteredFiles = new List<FileInfo>();
                OnPropertyChanged(nameof(FilteredFiles));
                return;
            }

            // 如果正则表达式为空或只有空白字符，显示所有文件
            if (string.IsNullOrWhiteSpace(regexText))
            {
                FilteredFiles = [.. _fileInfos];
                OnPropertyChanged(nameof(FilteredFiles));
                return;
            }

            try
            {
                var regex = new Regex(regexText, RegexOptions.IgnoreCase);
                FilteredFiles = [.. _fileInfos.Where(file => regex.IsMatch(file.Name))];
            }
            catch (ArgumentException)
            {
                // 正则表达式无效时，返回空列表或显示所有文件
                FilteredFiles = [];
            }

            OnPropertyChanged(nameof(FilteredFiles));
        }

        private void LoadFiles()
        { 
            _fileInfos = _fileService.GetAllMidiFiles();
            FilteredFiles = _fileInfos;
            OnPropertyChanged(nameof(FilteredFiles));
        }

        private void HandlePlayStateChanged(bool isPlaying)
        {
            if (isPlaying)
            {
                _isPlaying = true;
                _isLoading = false;
                OnPropertyChanged(nameof(PlayIconKind));
                OnPropertyChanged(nameof(IsPlaying));
                OnPropertyChanged(nameof(IsLoading));
            }
            else
            {
                OnPropertyChanged(nameof(PlayIconKind));
                OnPropertyChanged(nameof(IsPlaying));
            }
        }

        private void HandleProgressValueChanged(TimeSpan currentTime, TimeSpan totalTime)
        {
            if (_isSliderDragging)
            {
                return;
            }

            _progressValue = currentTime / totalTime * 100;
            OnPropertyChanged(nameof(ProgressValue));
        }

        private void HandleSelectedFileChanged()
        {
            _midiService?.StopMidiFile();
            if (SelectedFile != null)
            {
                _fileService.SelectedFile = SelectedFile;
            } 
            CanPlayOrStop = false;
            IsPlaying = false;
            _playIconKind = PackIconKind.Play;
            HandlePlayStateChanged(false);
            OnPropertyChanged(nameof(CanPlay));
            OnPropertyChanged(nameof(ProgressValue));
        }

        private void HandlePlayComleted()
        {
            IsPlaying = false;
            _playIconKind = PackIconKind.Play;
            _progressValue = 0;
            HandlePlayStateChanged(false);
            OnPropertyChanged(nameof(CanPlay));
            OnPropertyChanged(nameof(ProgressValue));
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            _midiService.OnMidiFilePlaying -= HandlePlayStateChanged;
            _midiService.OnProgressChanged -= HandleProgressValueChanged;
            _midiService.OnMidiFileCompleted -= HandlePlayComleted;
        }
    }
}
