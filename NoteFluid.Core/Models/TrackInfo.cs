using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace NoteFluid.Core.Models
{
    public class TrackInfo : INotifyPropertyChanged
    {
        public int TrackNumber { get; init; }
        public string? TrackName { get; set; }
        public int NoteCount { get; init; }

        private Color _color = Colors.White;
        public Color Color
        {
            get => _color;
            set => SetProperty(ref _color, value);
        }

        private bool _isVisible = true;
        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        private bool _isMuted = false;
        public bool IsMuted
        {
            get => _isMuted;
            set => SetProperty(ref _isMuted, value);
        }

        // 添加 Solo 功能
        private bool _isSolo = false;
        public bool IsSolo
        {
            get => _isSolo;
            set => SetProperty(ref _isSolo, value);
        }

        // 显示名称（优先级：TrackName > "Track " + TrackNumber）
        public string DisplayName => !string.IsNullOrWhiteSpace(TrackName)
            ? TrackName
            : $"Track {TrackNumber}";

        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
