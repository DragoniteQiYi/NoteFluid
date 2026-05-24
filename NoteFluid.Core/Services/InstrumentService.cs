using NAudio.Midi;
using NoteFluid.Core.Configs;
using NoteFluid.Core.Models;
using NoteFluid.Core.Utilities;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Media;

namespace NoteFluid.Core.Services
{
    /// <summary>
    /// 可视化服务，管理MIDI乐器信息和颜色配置
    /// </summary>
    public class InstrumentService
    {
        private readonly ConfigService _configService;

        /// <summary>
        /// 当前加载的MIDI文件信息
        /// </summary>
        public MidiFileInfo? CurrentMidiFileInfo { get; private set; }

        /// <summary>
        /// 当前加载的MIDI文件
        /// </summary>
        public MidiFile? CurrentMidiFile { get; private set; }

        /// <summary>
        /// 当前MIDI文件的乐器信息列表
        /// </summary>
        public ObservableCollection<InstrumentInfo> InstrumentInfos { get; private set; } = new();

        /// <summary>
        /// 当前MIDI文件的配置
        /// </summary>
        public MidiFileConfig? CurrentConfig { get; private set; }

        /// <summary>
        /// 当前文件的唯一标识Key
        /// </summary>
        public string? CurrentFileKey { get; private set; }

        /// <summary>
        /// 乐器颜色变化事件
        /// </summary>
        public event Action<int, Color>? OnInstrumentColorChanged;

        /// <summary>
        /// 乐器可见性变化事件
        /// </summary>
        public event Action<int, bool>? OnInstrumentVisibilityChanged;

        /// <summary>
        /// 乐器静音状态变化事件
        /// </summary>
        public event Action<int, bool>? OnInstrumentMuteChanged;

        /// <summary>
        /// 乐器独奏状态变化事件
        /// </summary>
        public event Action<int, bool>? OnInstrumentSoloChanged;

        /// <summary>
        /// 乐器名称变化事件
        /// </summary>
        public event Action<int, string>? OnInstrumentNameChanged;

        /// <summary>
        /// 六色循环：红、橙、黄、绿、蓝、紫、灰
        /// </summary>
        public static readonly Color[] RainbowColors =
        [
            Color.FromRgb(239, 68, 68),    // 柔和红
            Color.FromRgb(249, 140, 53),   // 柔和橙
            Color.FromRgb(250, 204, 72),   // 柔和黄
            Color.FromRgb(74, 200, 120),   // 柔和绿
            Color.FromRgb(78, 132, 237),   // 柔和蓝
            Color.FromRgb(147, 88, 186),   // 柔和紫
            Color.FromRgb(165, 165, 165)   // 柔和灰
        ];

        /// <summary>
        /// GM标准乐器名称映射表（128个乐器）
        /// </summary>
        private static readonly string[] InstrumentNames =
        [
            // 钢琴 (0-7)
            "原声大钢琴", "明亮原声钢琴", "电子大钢琴", "酒吧钢琴",
            "电钢琴1", "电钢琴2", "羽管键琴", "古钢琴",
    
            // 色彩打击乐器 (8-15)
            "钢片琴", "钟琴", "八音盒", "颤音琴",
            "马林巴", "木琴", "管钟", "扬琴",
    
            // 风琴 (16-23)
            "拉杆风琴", "打击风琴", "摇滚风琴", "教堂管风琴",
            "簧片风琴", "手风琴", "口琴", "探戈手风琴",
    
            // 吉他 (24-31)
            "原声吉他（尼龙弦）", "原声吉他（钢弦）", "电吉他（爵士）", "电吉他（清音）",
            "电吉他（闷音）", "过载吉他", "失真吉他", "吉他泛音",
    
            // 贝斯 (32-39)
            "原声贝斯", "电贝斯（指弹）", "电贝斯（拨片）", "无品贝斯",
            "打弦贝斯1", "打弦贝斯2", "合成贝斯1", "合成贝斯2",
    
            // 弦乐 (40-47)
            "小提琴", "中提琴", "大提琴", "低音提琴",
            "颤音弦乐", "拨奏弦乐", "管弦乐竖琴", "定音鼓",
    
            // 合奏 (48-55)
            "弦乐合奏1", "弦乐合奏2", "合成弦乐1", "合成弦乐2",
            "合唱啊声", "人声呜声", "合成合唱", "管弦乐齐奏",
    
            // 铜管 (56-63)
            "小号", "长号", "大号", "闷音小号",
            "圆号", "铜管乐组", "合成铜管1", "合成铜管2",
    
            // 簧片乐器 (64-71)
            "高音萨克斯", "中音萨克斯", "次中音萨克斯", "上低音萨克斯",
            "双簧管", "英国管", "巴松管", "单簧管",
    
            // 吹管乐器 (72-79)
            "短笛", "长笛", "竖笛", "排箫",
            "吹瓶", "尺八", "口哨", "陶笛",
    
            // 合成主音 (80-87)
            "主音1（方波）", "主音2（锯齿波）", "主音3（汽笛风琴）", "主音4（气声）",
            "主音5（查朗）", "主音6（人声）", "主音7（五度）", "主音8（贝斯+主音）",
    
            // 合成铺底 (88-95)
            "铺底1（新世纪）", "铺底2（温暖）", "铺底3（多复音合成）", "铺底4（合唱）",
            "铺底5（弓弦）", "铺底6（金属）", "铺底7（光环）", "铺底8（扫频）",
    
            // 合成效果 (96-103)
            "效果1（雨）", "效果2（电影配乐）", "效果3（水晶）", "效果4（氛围）",
            "效果5（明亮）", "效果6（妖精）", "效果7（回声）", "效果8（科幻）",
    
            // 民族乐器 (104-111)
            "西塔琴", "班卓琴", "三味线", "日本筝",
            "卡林巴琴", "风笛", "民间提琴", "山奈",
    
            // 打击乐器 (112-119)
            "叮当铃", "阿果果", "钢鼓", "木鱼",
            "太鼓", "旋律筒鼓", "合成鼓", "反转钹",
    
            // 音效 (120-127)
            "吉他滑弦噪音", "呼吸声", "海浪", "鸟鸣",
            "电话铃声", "直升机", "掌声", "枪声"
        ];

        public InstrumentService(ConfigService configService)
        {
            _configService = configService;
        }

        /// <summary>
        /// 加载MIDI文件并初始化乐器信息
        /// </summary>
        public void LoadMidiFile(MidiFileInfo midiFileInfo, MidiFile midiFile)
        {
            CurrentMidiFileInfo = midiFileInfo;
            CurrentFileKey = GenerateFileKey(midiFileInfo);
            CurrentMidiFile = midiFile;

            if (midiFile == null) return;

            // 读取完整的乐器信息（包括音符数等）
            var instrumentList = MidiInstrumentReader.GetTrackInstruments(midiFile);

            // 更新MidiFileInfo的乐器列表
            midiFileInfo.Instruments = instrumentList;

            // 从配置中加载或创建新的配置
            CurrentConfig = GetOrCreateMidiFileConfig(midiFileInfo);

            // 构建InstrumentInfo列表
            BuildInstrumentInfos(midiFileInfo);
        }

        /// <summary>
        /// 获取或创建MIDI文件配置
        /// </summary>
        private MidiFileConfig GetOrCreateMidiFileConfig(MidiFileInfo midiFileInfo)
        {
            var configs = _configService.ConfigData.MidiFileConfigs;

            if (configs.TryGetValue(CurrentFileKey!, out var existingConfig))
            {
                // 检查乐器数量是否匹配
                if (existingConfig.Instruments.Count == midiFileInfo.Instruments.Count)
                {
                    return existingConfig;
                }
            }

            // 创建新配置
            var newConfig = new MidiFileConfig
            {
                FileKey = CurrentFileKey!,
                FileName = midiFileInfo.FileName,
                LastModified = DateTime.Now
            };

            // 为每个乐器分配默认颜色和设置
            for (int i = 0; i < midiFileInfo.Instruments.Count; i++)
            {
                var instrument = midiFileInfo.Instruments[i];

                // 确保乐器名称正确映射
                if (string.IsNullOrEmpty(instrument.InstrumentName) ||
                    instrument.InstrumentName == "Unknown")
                {
                    instrument.InstrumentName = GetInstrumentName(instrument.PatchNumber);
                    instrument.DisplayName = $"{instrument.InstrumentName} (Ch.{instrument.Channel + 1})";
                }

                // 如果是打击乐器，默认不可见且配色为灰
                var instrumentConfig = new InstrumentConfig
                {
                    Channel = instrument.Channel,
                    PatchNumber = instrument.PatchNumber,
                    ColorHex = instrument.IsPercussion? RainbowColors[^1].ToString() :
                        RainbowColors[i % (RainbowColors.Length - 1)].ToString(),
                    IsVisible = !instrument.IsPercussion,
                    IsMuted = false,
                    IsSolo = false
                };
                instrumentConfig.InstrumentName = instrument.IsPercussion? "打击乐器" : InstrumentNames[instrument.PatchNumber];
                newConfig.Instruments.Add(instrumentConfig);
            }

            // 保存到配置
            configs[CurrentFileKey!] = newConfig;
            _configService.Save();

            return newConfig;
        }

        /// <summary>
        /// 构建InstrumentInfo列表
        /// </summary>
        private void BuildInstrumentInfos(MidiFileInfo midiFileInfo)
        {
            InstrumentInfos.Clear();

            if (CurrentConfig == null || midiFileInfo.Instruments == null) return;

            foreach (var instrument in midiFileInfo.Instruments)
            {
                // 确保乐器名称完整
                if (string.IsNullOrEmpty(instrument.InstrumentName) ||
                    instrument.InstrumentName == "Unknown")
                {
                    instrument.InstrumentName = GetInstrumentName(instrument.PatchNumber);
                }

                if (string.IsNullOrEmpty(instrument.DisplayName))
                {
                    instrument.DisplayName = $"{instrument.InstrumentName} (Ch.{instrument.Channel + 1})";
                }

                // 查找对应的配置
                var instrumentConfig = CurrentConfig.Instruments
                    .FirstOrDefault(c => c.PatchNumber == instrument.PatchNumber 
                    && c.Channel == instrument.Channel);

                if (instrumentConfig != null)
                {
                    // 从配置恢复设置
                    instrument.Color = instrumentConfig.GetColor();
                    instrument.IsVisible = instrumentConfig.IsVisible;
                    instrument.IsMuted = instrumentConfig.IsMuted;
                    instrument.IsSolo = instrumentConfig.IsSolo;

                    // 如果有自定义名称，使用配置中的名称
                    if (!string.IsNullOrEmpty(instrumentConfig.InstrumentName) &&
                        instrumentConfig.InstrumentName != instrument.InstrumentName)
                    {
                        instrument.InstrumentName = instrumentConfig.InstrumentName;
                        instrument.DisplayName = $"{instrument.InstrumentName} (Ch.{instrument.Channel + 1})";
                    }
                }
                else
                {
                    // 如果没有配置，使用默认值
                    int index = InstrumentInfos.Count;
                    instrument.Color = RainbowColors[index % RainbowColors.Length];
                    instrument.IsVisible = true;
                    instrument.IsMuted = false;
                    instrument.IsSolo = false;
                }

                // 监听InstrumentInfo的属性变化
                instrument.PropertyChanged += (s, e) =>
                {
                    if (s is InstrumentInfo info)
                    {
                        switch (e.PropertyName)
                        {
                            case nameof(InstrumentInfo.Color):
                                UpdateInstrumentColor(info.PatchNumber, info.Channel, info.Color);
                                break;
                            case nameof(InstrumentInfo.IsVisible):
                                UpdateInstrumentVisibility(info.PatchNumber, info.Channel, info.IsVisible);
                                break;
                            case nameof(InstrumentInfo.IsMuted):
                                UpdateInstrumentMute(info.PatchNumber, info.Channel, info.IsMuted);
                                break;
                            case nameof(InstrumentInfo.IsSolo):
                                UpdateInstrumentSolo(info.PatchNumber, info.Channel, info.IsSolo);
                                break;
                            case nameof(InstrumentInfo.InstrumentName):
                                // 乐器名称改变时也要更新配置
                                UpdateInstrumentName(info.PatchNumber, info.Channel, info.InstrumentName);
                                break;
                        }
                    }
                };

                InstrumentInfos.Add(instrument);

                Debug.WriteLine($"乐器: {instrument.InstrumentName}, Id: {instrument.InstrumentId}, 颜色: {instrument.Color.ToString()}");
            }
        }

        /// <summary>
        /// 根据PatchNumber和Channel查找InstrumentConfig的辅助方法
        /// </summary>
        private InstrumentConfig? FindInstrumentConfig(int patchNumber, int channel)
        {
            return CurrentConfig?.Instruments
                .FirstOrDefault(c => c.PatchNumber == patchNumber && c.Channel == channel);
        }

        /// <summary>
        /// 更新乐器名称
        /// </summary>
        public void UpdateInstrumentName(int patchNumber, int channel, string newName)
        {
            var instrumentConfig = FindInstrumentConfig(patchNumber, channel);
            if (instrumentConfig != null)
            {
                instrumentConfig.InstrumentName = newName;
                _configService.Save();

                var instrumentInfo = InstrumentInfos
                    .FirstOrDefault(i => i.PatchNumber == patchNumber && i.Channel == channel);
                if (instrumentInfo != null)
                {
                    OnInstrumentNameChanged?.Invoke(instrumentInfo.InstrumentId, newName);
                }
            }
        }

        /// <summary>
        /// 更新乐器颜色
        /// </summary>
        public void UpdateInstrumentColor(int patchNumber, int channel, Color color)
        {
            var instrumentConfig = FindInstrumentConfig(patchNumber, channel);
            if (instrumentConfig != null)
            {
                instrumentConfig.SetColor(color);
                _configService.Save();

                // 查找对应的InstrumentInfo以获取InstrumentId用于事件通知
                var instrumentInfo = InstrumentInfos
                    .FirstOrDefault(i => i.PatchNumber == patchNumber && i.Channel == channel);
                if (instrumentInfo != null)
                {
                    OnInstrumentColorChanged?.Invoke(instrumentInfo.InstrumentId, color);
                }
            }
        }

        /// <summary>
        /// 更新乐器可见性
        /// </summary>
        public void UpdateInstrumentVisibility(int patchNumber, int channel, bool isVisible)
        {
            var instrumentConfig = FindInstrumentConfig(patchNumber, channel);
            if (instrumentConfig != null)
            {
                instrumentConfig.IsVisible = isVisible;
                _configService.Save();

                var instrumentInfo = InstrumentInfos
                    .FirstOrDefault(i => i.PatchNumber == patchNumber && i.Channel == channel);
                if (instrumentInfo != null)
                {
                    OnInstrumentVisibilityChanged?.Invoke(instrumentInfo.InstrumentId, isVisible);
                }
            }
        }

        /// <summary>
        /// 更新乐器静音状态
        /// </summary>
        public void UpdateInstrumentMute(int patchNumber, int channel, bool isMuted)
        {
            var instrumentConfig = FindInstrumentConfig(patchNumber, channel);
            if (instrumentConfig != null)
            {
                instrumentConfig.IsMuted = isMuted;
                _configService.Save();

                var instrumentInfo = InstrumentInfos
                    .FirstOrDefault(i => i.PatchNumber == patchNumber && i.Channel == channel);
                if (instrumentInfo != null)
                {
                    OnInstrumentMuteChanged?.Invoke(instrumentInfo.InstrumentId, isMuted);
                }
            }
        }

        /// <summary>
        /// 更新乐器独奏状态
        /// </summary>
        public void UpdateInstrumentSolo(int patchNumber, int channel, bool isSolo)
        {
            var instrumentConfig = FindInstrumentConfig(patchNumber, channel);
            if (instrumentConfig != null)
            {
                instrumentConfig.IsSolo = isSolo;
                _configService.Save();

                var instrumentInfo = InstrumentInfos
                    .FirstOrDefault(i => i.PatchNumber == patchNumber && i.Channel == channel);
                if (instrumentInfo != null)
                {
                    OnInstrumentSoloChanged?.Invoke(instrumentInfo.InstrumentId, isSolo);
                }
            }
        }

        /// <summary>
        /// 获取乐器颜色（通过InstrumentId，保持向后兼容）
        /// </summary>
        public Color GetInstrumentColor(int instrumentId)
        {
            var instrumentInfo = InstrumentInfos.FirstOrDefault(i => i.InstrumentId == instrumentId);
            if (instrumentInfo != null)
            {
                var instrumentConfig = FindInstrumentConfig(instrumentInfo.PatchNumber, instrumentInfo.Channel);
                return instrumentConfig?.GetColor() ?? instrumentInfo.Color;
            }
            return Colors.White;
        }

        /// <summary>
        /// 获取乐器名称（根据GM标准Program Number）
        /// </summary>
        public static string GetInstrumentName(int programNumber)
        {
            if (programNumber >= 0 && programNumber < InstrumentNames.Length)
            {
                return InstrumentNames[programNumber];
            }
            return "Unknown";
        }

        /// <summary>
        /// 获取所有可用的乐器名称列表
        /// </summary>
        public static string[] GetAllInstrumentNames()
        {
            return (string[])InstrumentNames.Clone();
        }

        /// <summary>
        /// 根据名称查找乐器编号
        /// </summary>
        public static int GetInstrumentNumberByName(string name)
        {
            return Array.IndexOf(InstrumentNames, name);
        }

        /// <summary>
        /// 获取乐器分类
        /// </summary>
        public static string GetInstrumentCategory(int programNumber)
        {
            if (programNumber >= 0 && programNumber <= 7) return "Piano";
            if (programNumber >= 8 && programNumber <= 15) return "Chromatic Percussion";
            if (programNumber >= 16 && programNumber <= 23) return "Organ";
            if (programNumber >= 24 && programNumber <= 31) return "Guitar";
            if (programNumber >= 32 && programNumber <= 39) return "Bass";
            if (programNumber >= 40 && programNumber <= 47) return "Strings";
            if (programNumber >= 48 && programNumber <= 55) return "Ensemble";
            if (programNumber >= 56 && programNumber <= 63) return "Brass";
            if (programNumber >= 64 && programNumber <= 71) return "Reed";
            if (programNumber >= 72 && programNumber <= 79) return "Pipe";
            if (programNumber >= 80 && programNumber <= 87) return "Synth Lead";
            if (programNumber >= 88 && programNumber <= 95) return "Synth Pad";
            if (programNumber >= 96 && programNumber <= 103) return "Synth Effects";
            if (programNumber >= 104 && programNumber <= 111) return "Ethnic";
            if (programNumber >= 112 && programNumber <= 119) return "Percussive";
            if (programNumber >= 120 && programNumber <= 127) return "Sound Effects";
            return "Unknown";
        }

        /// <summary>
        /// 获取所有可见乐器的颜色字典
        /// </summary>
        public Dictionary<int, Color> GetVisibleInstrumentColors()
        {
            var result = new Dictionary<int, Color>();

            foreach (var instrument in InstrumentInfos)
            {
                if (instrument.IsVisible)
                {
                    result[instrument.InstrumentId] = instrument.Color;
                }
            }

            return result;
        }

        /// <summary>
        /// 生成文件唯一标识Key
        /// </summary>
        private string GenerateFileKey(MidiFileInfo midiFileInfo)
        {
            return midiFileInfo.FileName;
        }

        /// <summary>
        /// 清除当前加载的文件
        /// </summary>
        public void Clear()
        {
            CurrentMidiFileInfo = null;
            CurrentConfig = null;
            CurrentFileKey = null;
            InstrumentInfos.Clear();
        }
    }
}
