using NAudio.Midi;

namespace NoteFluid.Core.Models
{
    public class MidiFileInfo
    {
        public required string FileName { get; init; }
        public required string FilePath { get; init; }
        public int TrackCount { get; init; }
        public int NoteCount { get; init; }
        public TimeSpan Duration { get; init; }
        public MidiFile? MidiData { get; init; }  // 内部持有，供播放使用
        public List<InstrumentInfo>? Instruments { get; set; }
    }
}
