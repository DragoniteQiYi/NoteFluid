using System.Windows.Media;

namespace NoteFluid.Core.Models
{
    /// <summary>
    /// MIDI音符事件模型 - 记录单个音符的完整生命周期
    /// </summary>
    public class MidiNoteEvent
    {
        /// <summary>MIDI音符编号 (0-127)</summary>
        public int NoteNumber { get; set; }

        /// <summary>音符开始时间（毫秒）</summary>        
        public double StartTimeMs { get; set; }

        /// <summary>音符结束时间（毫秒）</summary>
        public double EndTimeMs { get; set; }

        /// <summary>音符持续时间（毫秒）</summary>
        public double DurationMs => EndTimeMs - StartTimeMs;

        /// <summary>MIDI通道号 (0-15)</summary>
        public int Channel { get; set; }

        /// <summary>乐器编号 (PatchNumber, 0-127)</summary>
        public int PatchNumber { get; set; }

        /// <summary>所属轨道索引</summary>
        public int TrackIndex { get; set; }

        /// <summary>是否为打击乐器通道</summary>
        public bool IsPercussion => Channel == 9;

        /// <summary>关联的乐器信息ID（用于查找颜色）</summary>
        public int InstrumentId { get; set; }

        public bool IsVisible {  get; set; }

        public bool IsMuted { get; set; }

        /// <summary>音符对应的颜色</summary>
        public Color Color { get; set; }
    }
}
