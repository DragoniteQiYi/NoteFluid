namespace NoteFluid.Core.Models
{
    public class MusicPlaybackState
    {
        public bool IsPlaying { get; set; }
        public bool IsPaused { get; set; }
        public TimeSpan CurrentPosition { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public float Tempo { get; set; } = 120f;
    }
}
