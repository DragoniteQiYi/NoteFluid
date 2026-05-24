using NAudio.Midi;
using NoteFluid.Core.Models;
using NoteFluid.Core.Views;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace NoteFluid.Core.Services
{
    public class WaterfallService : IDisposable
    {
        private readonly VisualizationService _visualizationService;
        private readonly MidiService _midiService;

        private Canvas? _waterfallCanvas;
        private Canvas? _pianoCanvas;

        private DispatcherTimer? _renderTimer;
        private double _currentTimeMs;

        // MIDI音符事件列表（已排序）
        private List<MidiNoteEvent> _noteEvents = [];

        // 当前活跃的瀑布条（按音符编号索引）
        private readonly Dictionary<int, WaterfallBar> _activeBars = [];
        // 跟踪每个音符的开始时间
        private readonly Dictionary<int, double> _noteStartTimestamps = [];

        // 对象池：回收不用的瀑布条
        private readonly ConcurrentBag<WaterfallBar> _barPool = [];
        private const int MaxPoolSize = 100;

        // 待处理的音符事件索引
        private int _nextEventIndex;

        // 瀑布条下落速度（像素/秒）
        public double FallSpeed { get; set; } = 300.0;

        // 画布高度缓存
        private double _canvasHeight;
        private double _scaleX;

        public double CanvasHeight => _canvasHeight;

        // 偏移时间（毫秒）
        public double LookAheadMs { get => _canvasHeight / FallSpeed * 1000; }

        private bool _isRunning;

        private ObservableCollection<PianoKey> _pianoKeys;

        // 缓存 MIDI 音符位置信息，避免重复计算
        private readonly Dictionary<int, (double X, double Width)> _noteLayoutCache = [];

        // 缓存乐器可见性，避免频繁 LINQ 查询
        private readonly Dictionary<int, bool> _instrumentVisibilityCache = [];

        private readonly Dictionary<int, PianoKey> _notePianoKeyCache = [];

        // 待移除的瀑布条列表（复用，避免GC）
        private readonly List<int> _barsToRemove = [];

        public event Action? OnAllBarsCompleted;
        public event Action? OnWaterfallInitialized;

        public WaterfallService(VisualizationService visualizationService, MidiService midiService)
        {
            _visualizationService = visualizationService ?? throw new ArgumentNullException(nameof(visualizationService));
            _midiService = midiService;
        }

        public void SetVisualizationCanvas(Canvas waterfallCanvas, Canvas pianoCanvas, ObservableCollection<PianoKey> pianoKeys)
        {
            _waterfallCanvas = waterfallCanvas ?? throw new ArgumentNullException(nameof(waterfallCanvas));
            _pianoCanvas = pianoCanvas ?? throw new ArgumentNullException(nameof(pianoCanvas));
            _pianoKeys = pianoKeys;

            // 预计算音符布局
            PrecomputeNoteLayouts();

            _waterfallCanvas.SizeChanged += OnCanvasSizeChanged;

            _canvasHeight = _waterfallCanvas.ActualHeight;

            _renderTimer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };
            _renderTimer.Tick += OnRenderTick;  // OnUpdateTick 改为 OnRenderTick
        }

        /// <summary>
        /// 预计算所有88个钢琴键的位置和宽度
        /// </summary>
        private void PrecomputeNoteLayouts()
        {
            if (_pianoCanvas == null || _pianoCanvas.Width <= 0 || _waterfallCanvas == null) return;

            _noteLayoutCache.Clear();
            _scaleX = _waterfallCanvas.ActualWidth / _pianoCanvas.Width;

            for (int midiNote = 21; midiNote <= 108; midiNote++)
            {
                var pianoKey = _pianoKeys.FirstOrDefault(x => x.MidiNote == midiNote);
                if (pianoKey != null)
                {
                    _notePianoKeyCache[midiNote] = pianoKey;
                    _noteLayoutCache[midiNote] = (pianoKey.X, Math.Max(1, pianoKey.Width));
                }
            }
        }

        /// <summary>
        /// 从对象池获取或创建瀑布条
        /// </summary>
        private WaterfallBar GetWaterfallBar()
        {
            if (_barPool.TryTake(out var bar))
            {
                bar.Reset();
                return bar;
            }
            return new WaterfallBar();
        }

        /// <summary>
        /// 将瀑布条归还对象池
        /// </summary>
        private void ReturnWaterfallBar(WaterfallBar bar)
        {
            if (_barPool.Count < MaxPoolSize)
            {
                bar.Visibility = System.Windows.Visibility.Collapsed;
                _barPool.Add(bar);
            }
        }

        public void LoadMidiFile(MidiFile midiFile)
        {
            if (midiFile == null) return;

            var programChanges = new Dictionary<int, List<(long AbsoluteTime, int PatchNumber)>>();

            foreach (var track in midiFile.Events)
            {
                foreach (var midiEvent in track)
                {
                    if (midiEvent is PatchChangeEvent pc)
                    {
                        if (!programChanges.ContainsKey(pc.Channel))
                        {
                            programChanges[pc.Channel] = [];
                        }
                        programChanges[pc.Channel].Add((pc.AbsoluteTime, pc.Patch));
                    }
                }
            }

            foreach (var kvp in programChanges)
            {
                kvp.Value.Sort((a, b) => a.AbsoluteTime.CompareTo(b.AbsoluteTime));
            }

            var tempoEvents = new List<TempoEvent>();
            foreach (var track in midiFile.Events)
            {
                foreach (var midiEvent in track)
                {
                    if (midiEvent is TempoEvent te)
                    {
                        tempoEvents.Add(te);
                    }
                }
            }
            tempoEvents.Sort((a, b) => a.AbsoluteTime.CompareTo(b.AbsoluteTime));
            // 创建一个按时间排序的事件列表
            var allEvents = new List<(long AbsoluteTime, int TrackIndex, MidiEvent Event)>();

            for (int trackIdx = 0; trackIdx < midiFile.Events.Count(); trackIdx++)
            {
                var track = midiFile.Events[trackIdx];
                foreach (var midiEvent in track)
                {
                    allEvents.Add((midiEvent.AbsoluteTime, trackIdx, midiEvent));
                }
            }

            // 按绝对时间排序所有事件
            allEvents.Sort((a, b) => a.AbsoluteTime.CompareTo(b.AbsoluteTime));

            // 使用字典跟踪每个轨道每个通道的挂起音符
            var pendingNotes = new Dictionary<(int Track, int Channel, int NoteNumber), (long AbsoluteTime, int PatchNumber)>();

            foreach (var (absoluteTime, trackIdx, midiEvent) in allEvents)
            {
                if (midiEvent is NoteOnEvent noteOn)
                {
                    var key = (trackIdx, noteOn.Channel, noteOn.NoteNumber);

                    if (noteOn.Velocity > 0)
                    {
                        // 音符开始
                        int patchNumber = GetPatchAtTime(programChanges, noteOn.Channel, absoluteTime);
                        pendingNotes[key] = (absoluteTime, patchNumber);
                    }
                    else
                    {
                        // Velocity 0 表示音符结束
                        if (pendingNotes.TryGetValue(key, out var noteInfo))
                        {
                            var noteEvent = CreateNoteEvent(
                                noteOn.NoteNumber, noteOn.Channel, noteInfo.PatchNumber,
                                noteInfo.AbsoluteTime, absoluteTime, trackIdx,
                                tempoEvents, midiFile.DeltaTicksPerQuarterNote);
                            _noteEvents.Add(noteEvent);
                            pendingNotes.Remove(key);
                        }
                    }
                }
                else if (midiEvent.CommandCode == MidiCommandCode.NoteOff)
                {
                    if (midiEvent is NoteEvent noteOff)
                    {
                        var key = (trackIdx, noteOff.Channel, noteOff.NoteNumber);
                        if (pendingNotes.TryGetValue(key, out var noteInfo))
                        {
                            var noteEvent = CreateNoteEvent(
                                noteOff.NoteNumber, noteOff.Channel, noteInfo.PatchNumber,
                                noteInfo.AbsoluteTime, absoluteTime, trackIdx,
                                tempoEvents, midiFile.DeltaTicksPerQuarterNote);
                            _noteEvents.Add(noteEvent);
                            pendingNotes.Remove(key);
                        }
                    }
                }
            }

            // 处理剩余未关闭的音符
            foreach (var kvp in pendingNotes)
            {
                var noteEvent = CreateNoteEvent(
                    kvp.Key.NoteNumber, kvp.Key.Channel, kvp.Value.PatchNumber,
                    kvp.Value.AbsoluteTime, kvp.Value.AbsoluteTime + midiFile.DeltaTicksPerQuarterNote,
                    kvp.Key.Track, tempoEvents, midiFile.DeltaTicksPerQuarterNote);
                _noteEvents.Add(noteEvent);
            }

            _noteEvents.Sort((a, b) => a.StartTimeMs.CompareTo(b.StartTimeMs));
            AssignColors();
            UpdateInstrumentVisibilityCache();

            Debug.WriteLine($"[WaterfallService] 加载完成，共 {_noteEvents.Count} 个音符事件");
        }

        private int GetPatchAtTime(Dictionary<int, List<(long AbsoluteTime, int PatchNumber)>> programChanges,
                                   int channel, long absoluteTime)
        {
            if (!programChanges.TryGetValue(channel, out var changes) || changes.Count == 0)
                return 0;

            int patch = 0;
            foreach (var change in changes)
            {
                if (change.AbsoluteTime <= absoluteTime)
                    patch = change.PatchNumber;
                else
                    break;
            }
            return patch;
        }

        private MidiNoteEvent CreateNoteEvent(int noteNumber, int channel, int patchNumber,
                                               long startTick, long endTick, int trackIndex,
                                               List<TempoEvent> tempoEvents, int deltaTicksPerQuarterNote)
        {
            double startMs = TicksToMilliseconds(startTick, tempoEvents, deltaTicksPerQuarterNote);
            double endMs = TicksToMilliseconds(endTick, tempoEvents, deltaTicksPerQuarterNote);

            var midiNoteEvent = new MidiNoteEvent
            {
                NoteNumber = noteNumber,
                Channel = channel,
                PatchNumber = patchNumber,
                StartTimeMs = startMs - LookAheadMs,
                EndTimeMs = endMs - LookAheadMs,
                TrackIndex = trackIndex
            };
            return midiNoteEvent;
        }

        private double TicksToMilliseconds(long ticks, List<TempoEvent> tempoEvents, int deltaTicksPerQuarterNote)
        {
            if (ticks <= 0) return 0;

            if (tempoEvents.Count == 0)
                return ticks * 500000.0 / (deltaTicksPerQuarterNote * 1000.0);

            double totalMs = 0;
            long lastTick = 0;
            int tempo = 500000;

            foreach (var tempoEvent in tempoEvents)
            {
                if (tempoEvent.AbsoluteTime > ticks)
                {
                    long deltaTicks = ticks - lastTick;
                    totalMs += deltaTicks * tempo / (deltaTicksPerQuarterNote * 1000.0);
                    return totalMs;
                }

                long segmentTicks = tempoEvent.AbsoluteTime - lastTick;
                totalMs += segmentTicks * tempo / (deltaTicksPerQuarterNote * 1000.0);
                lastTick = tempoEvent.AbsoluteTime;
                tempo = tempoEvent.MicrosecondsPerQuarterNote;
            }

            long finalTicks = ticks - lastTick;
            totalMs += finalTicks * tempo / (deltaTicksPerQuarterNote * 1000.0);

            return totalMs;
        }

        private void AssignColors()
        {
            var instrumentInfos = _visualizationService.InstrumentInfos;

            var colorMap = new Dictionary<(int Channel, int PatchNumber), (int InstrumentId, Color Color, bool IsVisible)>();
            foreach (var info in instrumentInfos)
            {
                var key = (info.Channel, info.PatchNumber);
                if (!colorMap.ContainsKey(key))
                {
                    colorMap[key] = (info.InstrumentId, info.Color, info.IsVisible);
                }
            }

            foreach (var noteEvent in _noteEvents)
            {
                var key = (noteEvent.Channel, noteEvent.PatchNumber);
                if (colorMap.TryGetValue(key, out var info))
                {
                    noteEvent.InstrumentId = info.InstrumentId;
                    noteEvent.Color = info.Color;
                    noteEvent.IsVisible = info.IsVisible;
                }
                else
                {
                    noteEvent.InstrumentId = -1;
                    noteEvent.Color = Colors.Gray;
                    noteEvent.IsVisible = false;
                }
            }
        }

        private void UpdateInstrumentVisibilityCache()
        {
            _instrumentVisibilityCache.Clear();
            foreach (var info in _visualizationService.InstrumentInfos)
            {
                _instrumentVisibilityCache[info.InstrumentId] = info.IsVisible;
            }
        }

        public double CalculateDelayMs()
        {
            if (_canvasHeight <= 0 || FallSpeed <= 0)
                return 0;

            return _canvasHeight / FallSpeed * 1000.0;
        }

        public void Start(double delayMs)
        {
            Stop();
            _nextEventIndex = 0;
            _isRunning = true;
            _renderTimer?.Start();
            _ = _midiService.PlayMidiFile(delayMs);
        }

        public void Pause()
        {
            if (!_isRunning) return;

            _renderTimer?.Stop();
        }

        public void Resume()
        {
            if (!_isRunning) return;

            _renderTimer?.Start();
        }

        public void Stop()
        {
            _isRunning = false;
            _renderTimer?.Stop();
            ClearAllBars();
            _nextEventIndex = 0;
            _noteStartTimestamps.Clear();   // 新增
        }

        private void OnRenderTick(object? sender, EventArgs e)
        {
            if (_waterfallCanvas == null || _waterfallCanvas.ActualHeight <= 0) return;

            _currentTimeMs = _midiService.CurrentTimeMs - LookAheadMs;

            double lookAheadTime = _currentTimeMs + LookAheadMs;

            // 创建所有应该出现在当前时间窗口内的瀑布条
            while (_nextEventIndex < _noteEvents.Count)
            {
                var noteEvent = _noteEvents[_nextEventIndex];

                if (noteEvent.StartTimeMs > _currentTimeMs)
                    break;

                if (noteEvent.NoteNumber >= 21 && noteEvent.NoteNumber <= 108)
                {
                    if (!_instrumentVisibilityCache.TryGetValue(noteEvent.InstrumentId, out bool isVisible) || isVisible)
                    {
                        // 传递当前的索引作为ID
                        CreateWaterfallBar(noteEvent, _nextEventIndex);
                    }
                }

                _nextEventIndex++;
            }

            UpdateBars();
            CleanCompletedBars();

            if (_nextEventIndex >= _noteEvents.Count && _activeBars.Count == 0)
            {
                _renderTimer?.Stop();
                OnAllBarsCompleted?.Invoke();
            }
        }

        private void CleanCompletedBars()
        {
            _barsToRemove.Clear();

            foreach (var kvp in _activeBars)
            {
                if (!kvp.Value.IsActive)
                {
                    _barsToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in _barsToRemove)
            {
                if (_activeBars.Remove(key, out var bar))
                {
                    if (_waterfallCanvas?.Children.Contains(bar) == true)
                    {
                        _waterfallCanvas.Children.Remove(bar);
                    }
                    ReturnWaterfallBar(bar);
                }
            }
        }

        private void CreateWaterfallBar(MidiNoteEvent noteEvent, int barId)
        {
            if (!_noteLayoutCache.TryGetValue(noteEvent.NoteNumber, out var layout)
                || !noteEvent.IsVisible)
                return;

            var bar = GetWaterfallBar();

            double durationSeconds = noteEvent.DurationMs / 1000.0;
            double barHeight = durationSeconds * FallSpeed;
            double width = layout.Width * _scaleX;
            double left = layout.X * _scaleX;

            bar.Initialize(width, barHeight, left, noteEvent.Color);

            // 关键修改：底部应该在画布顶部
            double bottomPosition = 0; // 画布顶部（Y坐标0）

            // 计算顶部位置 = 底部位置 - 高度
            double topPosition = bottomPosition - barHeight;

            Canvas.SetTop(bar, topPosition);
            bar.UpdatePosition(topPosition);

            bar.StartTimeMs = noteEvent.StartTimeMs;
            bar.EndTimeMs = noteEvent.EndTimeMs;

            _waterfallCanvas.Children.Add(bar);

            // 使用正确的ID作为键
            _activeBars[barId] = bar;
            _noteStartTimestamps[barId] = _currentTimeMs;
        }

        private void UpdateBars()
        {
            var barsToRemove = new List<int>();

            foreach (var kv in _activeBars)
            {
                var bar = kv.Value;
                int barId = kv.Key;

                if (!bar.IsActive) continue;

                // 计算已经过的时间（从创建开始）
                if (_noteStartTimestamps.TryGetValue(barId, out double startTimestamp))
                {
                    double elapsedMs = _currentTimeMs - startTimestamp;
                    double elapsedSeconds = elapsedMs / 1000.0;

                    // 计算应该下降的距离
                    double distanceDropped = elapsedSeconds * FallSpeed;

                    // 初始位置（底部在顶部）
                    double initialBottom = 0;
                    double currentBottom = initialBottom + distanceDropped;

                    // 计算新的顶部位置
                    double topPosition = currentBottom - bar.Height;

                    // 更新位置
                    Canvas.SetTop(bar, topPosition);
                    bar.UpdatePosition(topPosition);

                    // 检查是否完全离开屏幕底部
                    if (topPosition > _canvasHeight)
                    {
                        bar.Deactivate();
                    }
                }
            }
        }

        private void ClearAllBars()
        {
            foreach (var bar in _activeBars.Values)
            {
                if (_waterfallCanvas?.Children.Contains(bar) == true)
                {
                    _waterfallCanvas.Children.Remove(bar);
                }
            }
            _activeBars.Clear();
            _barsToRemove.Clear();

            if (_waterfallCanvas != null)
            {
                var remaining = _waterfallCanvas.Children.OfType<WaterfallBar>().ToList();
                foreach (var bar in remaining)
                {
                    _waterfallCanvas.Children.Remove(bar);
                }
            }
        }

        public void Clear()
        {
            Stop();
            _noteEvents.Clear();
            _activeBars.Clear();
            _nextEventIndex = 0;
        }

        private void OnCanvasSizeChanged(object sender, SizeChangedEventArgs e)
        {
            _canvasHeight = _waterfallCanvas.ActualHeight;
            PrecomputeNoteLayouts();
        }

        public void Dispose()
        {
            Stop();
            if (_renderTimer != null)
            {
                _renderTimer.Tick -= OnRenderTick;
            }

            while (_barPool.TryTake(out _)) { }

            _noteLayoutCache.Clear();
            _instrumentVisibilityCache.Clear();
        }
    }
}