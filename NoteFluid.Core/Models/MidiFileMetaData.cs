namespace NoteFluid.Core.Models
{
    public class MidiFileMetadata
    {
        public Guid Id { get; init; }
        public string FileName { get; init; } = string.Empty;
        public string FilePath { get; init; } = string.Empty;
        public int TrackCount { get; init; }
        public int NoteCount { get; init; }
        public long DurationMs { get; init; }
        public DateTime LastPlayed { get; init; }
    }
}
