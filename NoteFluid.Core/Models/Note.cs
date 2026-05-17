namespace NoteFluid.Core.Models
{
    public class Note
    {
        public int TrackNumber { get; init; }
        public int NoteNumber { get; init; }     // MIDI音符号 0~127
        public long AbsoluteTime { get; init; }  // 微秒
        public long Duration { get; init; }      // 微秒

        // 添加便利属性
        public string NoteName => GetNoteName(NoteNumber);
        public int Octave => (NoteNumber / 12) - 1;
        public double AbsoluteTimeInSeconds => AbsoluteTime / 1_000_000.0;
        public double DurationInSeconds => Duration / 1_000_000.0;
        public int Velocity { get; init; } = 100;  // 按键力度

        private static readonly string[] NoteNames =
            { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

        public static string GetNoteName(int noteNumber)
        {
            if (noteNumber < 0 || noteNumber > 127)
                return "Unknown";
            return $"{NoteNames[noteNumber % 12]}";
        }
    }
}
