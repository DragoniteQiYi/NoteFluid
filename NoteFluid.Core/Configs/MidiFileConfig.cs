using System.Windows.Media;

namespace NoteFluid.Core.Configs
{
    /// <summary>
    /// MIDI文件配置，用于存储每个MIDI文件的轨道设置
    /// </summary>
    public class MidiFileConfig
    {
        /// <summary>
        /// 文件唯一标识（使用文件名或哈希）
        /// </summary>
        public string FileKey { get; set; } = string.Empty;

        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// 轨道配置列表
        /// </summary>
        public List<InstrumentConfig> Instruments { get; set; } = new();

        /// <summary>
        /// 最后修改时间
        /// </summary>
        public DateTime LastModified { get; set; }
    }

    /// <summary>
    /// 单个轨道配置
    /// </summary>
    public class InstrumentConfig
    {
        /// <summary>
        /// 乐器唯一标识
        /// </summary>
        // public int InstrumentId { get; set; }

        /// <summary>
        /// 轨道编号
        /// </summary>
        public int Channel { get; set; }

        ///// <summary>
        ///// 轨道名称
        ///// </summary>
        //public string TrackName { get; set; } = string.Empty;

        /// <summary>
        /// 乐器编号（MIDI Program Number）
        /// </summary>
        public int PatchNumber { get; set; }

        /// <summary>
        /// 乐器名称
        /// </summary>
        public string InstrumentName { get; set; } = string.Empty;

        /// <summary>
        /// 轨道颜色（ARGB格式存储）
        /// </summary>
        public string ColorHex { get; set; } = "#FFFFFF";

        /// <summary>
        /// 是否可见
        /// </summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>
        /// 是否静音
        /// </summary>
        public bool IsMuted { get; set; } = false;

        /// <summary>
        /// 是否独奏
        /// </summary>
        public bool IsSolo { get; set; } = false;

        /// <summary>
        /// 获取颜色对象
        /// </summary>
        public Color GetColor()
        {
            try
            {
                return (Color)ColorConverter.ConvertFromString(ColorHex);
            }
            catch
            {
                return Colors.White;
            }
        }

        /// <summary>
        /// 设置颜色
        /// </summary>
        public void SetColor(Color color)
        {
            ColorHex = color.ToString();
        }

        /// <summary>
        /// 获取唯一标识键（用于配置匹配）
        /// </summary>
        public string GetConfigKey() => $"{Channel}_{PatchNumber}";
    }
}
