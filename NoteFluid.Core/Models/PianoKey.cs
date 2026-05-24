using System.ComponentModel;
using System.Windows.Input;

namespace NoteFluid.Core.Models
{
    public class PianoKey : INotifyPropertyChanged
    {
        public int MidiNote { get; set; }
        public string? NoteName { get; set; }
        public int Octave { get; set; }
        public bool IsBlackKey { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public int ZIndex { get; set; }
        public bool IsPressed { get; set; }
        public string? DisplayText { get; set; }

        // 添加点击命令
        public ICommand? KeyClickCommand { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
