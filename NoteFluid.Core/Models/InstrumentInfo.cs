using MaterialDesignThemes.Wpf;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace NoteFluid.Core.Models
{
    public class InstrumentInfo : INotifyPropertyChanged
    {
        private Color _color;
        private bool _isVisible = true;
        private bool _isMuted;
        private bool _isSolo;

        public int PatchNumber { get; set; }
        public string? InstrumentName { get; set; }
        public int Channel { get; set; }
        public int NoteCount { get; set; }
        public bool IsPercussion { get; set; }

        /// <summary>
        /// 乐器唯一标识（运行时生成，不用于持久化配置）
        /// </summary>
        public int InstrumentId { get; set; }

        /// <summary>
        /// 获取配置键（用于配置匹配）
        /// </summary>
        public string GetConfigKey() => $"{Channel}_{PatchNumber}";

        /// <summary>
        /// 显示名称
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// 乐器颜色
        /// </summary>
        public Color Color
        {
            get => _color;
            set
            {
                if (_color != value)
                {
                    _color = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 是否可见
        /// </summary>
        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (_isVisible != value)
                {
                    _isVisible = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 是否静音
        /// </summary>
        public bool IsMuted
        {
            get => _isMuted;
            set
            {
                if (_isMuted != value)
                {
                    _isMuted = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 是否独奏
        /// </summary>
        public bool IsSolo
        {
            get => _isSolo;
            set
            {
                if (_isSolo != value)
                {
                    _isSolo = value;
                    OnPropertyChanged();
                }
            }
        }

        public PackIconKind VisibleIcon
        {
            get => IsVisible? PackIconKind.Eye : PackIconKind.EyeOff;
        }

        public PackIconKind IconKind 
        { 
            get
            {
                if (PatchNumber == -1) return PackIconKind.Album;
                if (PatchNumber >= 0 && PatchNumber <= 7) return PackIconKind.Piano;
                if (PatchNumber >= 8 && PatchNumber <= 15) return PackIconKind.MusicBox;
                if (PatchNumber >= 16 && PatchNumber <= 23) return PackIconKind.Piano;
                if (PatchNumber >= 24 && PatchNumber <= 31) return PackIconKind.GuitarAcoustic;
                if (PatchNumber >= 32 && PatchNumber <= 39) return PackIconKind.GuitarElectric;
                if (PatchNumber >= 40 && PatchNumber <= 47 || PatchNumber == 48) return PackIconKind.Violin;
                if (PatchNumber >= 48 && PatchNumber <= 55) return PackIconKind.PeopleGroup;
                if (PatchNumber >= 56 && PatchNumber <= 63) return PackIconKind.Trumpet;
                if (PatchNumber >= 64 && PatchNumber <= 71) return PackIconKind.Saxophone;
                if (PatchNumber >= 72 && PatchNumber <= 79) return PackIconKind.Saxophone;
                if (PatchNumber >= 80 && PatchNumber <= 87) return PackIconKind.Waveform;
                if (PatchNumber >= 88 && PatchNumber <= 95) return PackIconKind.DotsHorizontal;
                if (PatchNumber >= 96 && PatchNumber <= 103) return PackIconKind.SineWave;
                if (PatchNumber >= 104 && PatchNumber <= 111) return PackIconKind.Earth;
                if (PatchNumber >= 112 && PatchNumber <= 119) return PackIconKind.Vibrate;
                if (PatchNumber >= 120 && PatchNumber <= 127) return PackIconKind.AlarmLight;
                return PackIconKind.Note;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
