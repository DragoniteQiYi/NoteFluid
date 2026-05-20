namespace NoteFluid.Core.Configs
{
    public class ConfigData
    {
        public ThemeConfig? Theme { get; set; }

        public VisualizationConfig? Visualization { get; set; }

        /// <summary>
        /// MIDI文件配置字典，Key为文件唯一标识
        /// </summary>
        public Dictionary<string, MidiFileConfig> MidiFileConfigs { get; set; } = new();
    }
}
