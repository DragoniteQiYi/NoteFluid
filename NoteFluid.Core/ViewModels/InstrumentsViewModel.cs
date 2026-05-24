using NAudio.Midi;
using NoteFluid.Core.Models;
using NoteFluid.Core.Services;
using NoteFluid.Core.Utilities;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Media;

namespace NoteFluid.Core.ViewModels
{
    /// <summary>
    /// 乐器配置视图模型 - 基于Instrument建模
    /// </summary>
    public class InstrumentsViewModel : INotifyPropertyChanged
    {
        private readonly NavigateService _navigateService;
        private readonly FileService _fileService;
        private readonly VisualizationService _visualizationService;
        private readonly MidiService _midiService;

        /// <summary>
        /// 当前选中的MIDI文件
        /// </summary>
        public FileInfo? SelectedMidiFile => _fileService.SelectedFile;

        /// <summary>
        /// MIDI文件信息
        /// </summary>
        public MidiFileInfo? MidiFileInfo { get; private set; }

        /// <summary>
        /// 乐器信息列表（从VisualizationService获取）
        /// </summary>
        public ObservableCollection<InstrumentInfo> InstrumentInfos => _visualizationService.InstrumentInfos;

        /// <summary>
        /// 是否已加载文件
        /// </summary>
        public bool IsFileLoaded => MidiFileInfo != null;

        /// <summary>
        /// 文件名显示
        /// </summary>
        public string FileNameDisplay => MidiFileInfo?.FileName ?? "未选择文件";

        /// <summary>
        /// 乐器数量
        /// </summary>
        public int InstrumentCount => InstrumentInfos.Count;

        public event PropertyChangedEventHandler? PropertyChanged;

        public InstrumentsViewModel(
            NavigateService navigateService,
            FileService fileService,
            VisualizationService visualizationService,
            MidiService midiService)
        {
            _navigateService = navigateService;
            _fileService = fileService;
            _visualizationService = visualizationService;
            _midiService = midiService;
        }

        /// <summary>
        /// 加载选中的MIDI文件
        /// </summary>
        public async Task LoadSelectedMidiFile()
        {
            var file = _fileService.SelectedFile;
            if (file == null)
            {
                Debug.WriteLine("[InstrumentsViewModel] 没有选中的文件");
                return;
            }

            try
            {
                Debug.WriteLine($"[InstrumentsViewModel] 加载文件: {file.FullName}");

                // 使用NAudio读取MIDI文件信息
                var midiFile = await _midiService.LoadMidiFile(file);
                //var midiFile = new MidiFile(file.FullName, false);
                if (midiFile == null)
                {
                    Debug.WriteLine("加载MIDI文件失败");
                    return;
                }

                // 创建MidiFileInfo
                MidiFileInfo = new MidiFileInfo
                {
                    FileName = file.Name,
                    FilePath = file.FullName,
                    TrackCount = midiFile.Tracks,
                    NoteCount = CountAllNotes(midiFile),
                    Duration = CalculateDuration(midiFile),
                    MidiData = midiFile,
                };

                // 委托VisualizationService加载和处理所有数据
                _visualizationService.LoadMidiFile(MidiFileInfo, midiFile);
                _midiService.SetInstruments(InstrumentInfos);

                MidiFileInfo = _visualizationService.CurrentMidiFileInfo;

                OnPropertyChanged(nameof(MidiFileInfo));
                OnPropertyChanged(nameof(IsFileLoaded));
                OnPropertyChanged(nameof(FileNameDisplay));
                OnPropertyChanged(nameof(InstrumentCount));

                Debug.WriteLine($"[InstrumentsViewModel] 加载完成，共 {InstrumentCount} 个乐器");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[InstrumentsViewModel] 加载文件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 统计所有音符数量
        /// </summary>
        private int CountAllNotes(MidiFile midiFile)
        {
            int count = 0;
            foreach (var track in midiFile.Events)
            {
                foreach (var midiEvent in track)
                {
                    if (midiEvent is NoteOnEvent noteOn && noteOn.Velocity > 0)
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        /// <summary>
        /// 计算MIDI文件时长
        /// </summary>
        private TimeSpan CalculateDuration(MidiFile midiFile)
        {
            long maxTicks = 0;
            foreach (var track in midiFile.Events)
            {
                if (track.Count > 0)
                {
                    var lastEvent = track[track.Count - 1];
                    maxTicks = Math.Max(maxTicks, lastEvent.AbsoluteTime);
                }
            }

            double seconds = maxTicks * 0.5 / midiFile.DeltaTicksPerQuarterNote;
            return TimeSpan.FromSeconds(seconds);
        }

        /// <summary>
        /// 更改乐器颜色
        /// </summary>
        public void ChangeInstrumentColor(int instrumentId, Color newColor)
        {
            var instrument = InstrumentInfos.FirstOrDefault(i => i.InstrumentId == instrumentId);
            if (instrument != null)
            {
                instrument.Color = newColor;
                // VisualizationService会通过PropertyChanged自动处理
            }
        }

        /// <summary>
        /// 重置所有乐器颜色为默认彩虹色
        /// </summary>
        public void ResetColorsToDefault()
        {
            var rainbowColors = VisualizationService.RainbowColors;
            for (int i = 0; i < InstrumentInfos.Count; i++)
            {
                if (InstrumentInfos[i].IsPercussion)
                {
                    var color = rainbowColors[^1];
                    InstrumentInfos[i].Color = color;
                    break;
                }
                var defaultColor = rainbowColors[i % (rainbowColors.Length - 1)];
                InstrumentInfos[i].Color = defaultColor;
            }
        }

        /// <summary>
        /// 切换乐器可见性
        /// </summary>
        public void ToggleInstrumentVisibility(int instrumentId)
        {
            var instrument = InstrumentInfos.FirstOrDefault(i => i.InstrumentId == instrumentId);
            if (instrument != null)
            {
                instrument.IsVisible = !instrument.IsVisible;
            }
        }

        /// <summary>
        /// 切换乐器静音
        /// </summary>
        public void ToggleInstrumentMute(int instrumentId)
        {
            var instrument = InstrumentInfos.FirstOrDefault(i => i.InstrumentId == instrumentId);
            if (instrument != null)
            {
                instrument.IsMuted = !instrument.IsMuted;
            }
        }

        /// <summary>
        /// 切换乐器独奏
        /// </summary>
        public void ToggleInstrumentSolo(int instrumentId)
        {
            var instrument = InstrumentInfos.FirstOrDefault(i => i.InstrumentId == instrumentId);
            if (instrument != null)
            {
                instrument.IsSolo = !instrument.IsSolo;
                if (instrument.IsMuted)
                {
                    instrument.IsMuted = !instrument.IsMuted;
                }

                // 如果有任何乐器开启独奏，其他乐器静音
                bool anySoloActive = InstrumentInfos.Any(i => i.IsSolo);

                if (anySoloActive)
                {
                    // 至少有一个独奏激活，静音所有非独奏乐器
                    foreach (var otherInstrument in InstrumentInfos)
                    {
                        if (!otherInstrument.IsSolo)
                        {
                            otherInstrument.IsMuted = true;
                        }
                    }
                }
                else
                {
                    // 没有独奏激活，取消所有静音
                    foreach (var otherInstrument in InstrumentInfos)
                    {
                        otherInstrument.IsMuted = false;
                    }
                }
            }
        }

        /// <summary>
        /// 获取指定乐器的所有信息
        /// </summary>
        public InstrumentInfo? GetInstrumentInfo(int instrumentId)
        {
            return InstrumentInfos.FirstOrDefault(i => i.InstrumentId == instrumentId);
        }

        /// <summary>
        /// 获取指定通道的所有乐器
        /// </summary>
        public IEnumerable<InstrumentInfo> GetInstrumentsByChannel(int channel)
        {
            return InstrumentInfos.Where(i => i.Channel == channel);
        }

        /// <summary>
        /// 获取所有打击乐器
        /// </summary>
        public IEnumerable<InstrumentInfo> GetPercussionInstruments()
        {
            return InstrumentInfos.Where(i => i.IsPercussion);
        }

        /// <summary>
        /// 获取所有非打击乐器
        /// </summary>
        public IEnumerable<InstrumentInfo> GetMelodicInstruments()
        {
            return InstrumentInfos.Where(i => !i.IsPercussion);
        }

        /// <summary>
        /// 导航到其他页面
        /// </summary>
        public void NavigateTo(string pageName)
        {
            _navigateService.Navigate(pageName);
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
