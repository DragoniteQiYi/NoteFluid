using NAudio.Midi;
using System.Diagnostics;

namespace NoteFluid.Core.Models
{
    public class MidiPlayer : IDisposable
    {
        private readonly MidiFile _midiFile;
        private readonly MidiOut _midiOut;
        private System.Timers.Timer _timer;
        private int _currentEventIndex;
        private List<MidiEvent> _events;
        private int _currentTick;
        private bool _isPlaying;
        private bool _isPaused;
        private Stopwatch _stopwatch;
        private double _pausedElapsedMs;
        private int[] _activeNotes;
        private double _totalDurationMs;
        private List<TempoEvent> _tempoChanges;

        public bool IsPlaying => _isPlaying;
        public bool IsPaused => _isPaused;
        public double TotalDurationMs => _totalDurationMs;
        public double CurrentTimeMs => GetCurrentTimeMs();

        public event Action? OnPlaybackCompleted;

        public MidiPlayer(MidiFile midiFile, MidiOut midiOut)
        {
            this._midiFile = midiFile;
            this._midiOut = midiOut;

            Console.WriteLine($"[MidiPlayer] 构造函数开始");
            Console.WriteLine($"[MidiPlayer] DeltaTicksPerQuarterNote: {midiFile.DeltaTicksPerQuarterNote}");

            // 收集所有轨道的事件并排序
            _events = [];
            int metaEventCount = 0;
            int tempoEventCount = 0;

            foreach (var track in midiFile.Events)
            {
                foreach (var midiEvent in track)
                {
                    if (midiEvent is TempoEvent)
                    {
                        tempoEventCount++;
                        Console.WriteLine($"[MidiPlayer] 找到TempoEvent: {((TempoEvent)midiEvent).MicrosecondsPerQuarterNote}us/qn");
                    }

                    if (midiEvent.CommandCode == MidiCommandCode.MetaEvent)
                    {
                        metaEventCount++;
                        // 不添加元事件，但保留 TempoEvent
                        if (midiEvent is TempoEvent)
                        {
                            _events.Add(midiEvent);
                        }
                    }
                    else
                    {
                        _events.Add(midiEvent);
                    }
                }
            }

            _events.Sort((a, b) => a.AbsoluteTime.CompareTo(b.AbsoluteTime));

            Console.WriteLine($"[MidiPlayer] 事件总数: {_events.Count}");
            Console.WriteLine($"[MidiPlayer] 元事件总数(已过滤): {metaEventCount}");
            Console.WriteLine($"[MidiPlayer] TempoEvent数量: {tempoEventCount}");

            // 缓存 tempo 变化列表
            _tempoChanges = [.. _events
                .OfType<TempoEvent>()
                .OrderBy(e => e.AbsoluteTime)];

            Console.WriteLine($"[MidiPlayer] 缓存的TempoChanges数量: {_tempoChanges.Count}");

            _activeNotes = new int[128];
            _stopwatch = new Stopwatch();
            _totalDurationMs = CalculateTotalDurationMs();

            Console.WriteLine($"[MidiPlayer] 总时长: {_totalDurationMs}ms");
            Console.WriteLine($"[MidiPlayer] 构造函数完成");
        }
        public void Start()
        {
            Console.WriteLine($"[MidiPlayer] Start() 被调用");
            Console.WriteLine($"[MidiPlayer] 当前状态 - isPlaying={_isPlaying}, isPaused={_isPaused}");

            if (_isPlaying && !_isPaused)
            {
                Console.WriteLine($"[MidiPlayer] 已经在播放中，跳过");
                return;
            }

            if (_isPaused)
            {
                Console.WriteLine($"[MidiPlayer] 从暂停状态恢复");
                Resume();
                return;
            }

            Console.WriteLine($"[MidiPlayer] 从头开始播放");

            StopInternal();
            _currentEventIndex = 0;
            _currentTick = 0;
            _pausedElapsedMs = 0;
            _isPlaying = true;
            _isPaused = false;
            _stopwatch.Restart();

            Console.WriteLine($"[MidiPlayer] 状态设置完成 - isPlaying={_isPlaying}, isPaused={_isPaused}");

            StartTimer();

            // 验证定时器状态
            Console.WriteLine($"[MidiPlayer] 定时器已创建: Enabled={_timer?.Enabled}, Interval={_timer?.Interval}");
        }

        public void Pause()
        {
            if (!_isPlaying || _isPaused) return;

            _isPaused = true;
            _timer?.Stop();
            _stopwatch.Stop();

            _pausedElapsedMs += _stopwatch.Elapsed.TotalMilliseconds;
            _stopwatch.Reset();

            AllNotesOff();
        }

        public void Resume()
        {
            if (!_isPlaying || !_isPaused) return;

            _isPaused = false;
            _stopwatch.Restart();

            StartTimer();
        }

        public void Stop()
        {
            StopInternal();
            AllNotesOff();
        }

        private void StopInternal()
        {
            _isPlaying = false;
            _isPaused = false;
            _timer?.Stop();
            _stopwatch.Reset();
            _currentTick = 0;
            _pausedElapsedMs = 0;
        }

        public void SetPosition(double positionMs)
        {
            if (positionMs < 0) positionMs = 0;
            if (positionMs > _totalDurationMs) positionMs = _totalDurationMs;

            Debug.WriteLine($"[MidiPlayer] === SetPosition ===");
            Debug.WriteLine($"[MidiPlayer] 输入: {positionMs:F2}ms");
            Debug.WriteLine($"[MidiPlayer] TotalDurationMs: {_totalDurationMs:F2}ms");
            Debug.WriteLine($"[MidiPlayer] DeltaTicksPerQuarterNote: {_midiFile.DeltaTicksPerQuarterNote}");
            Debug.WriteLine($"[MidiPlayer] TempoChanges数量: {_tempoChanges.Count}");

            // 打印所有 tempo 变化
            for (int i = 0; i < _tempoChanges.Count; i++)
            {
                var t = _tempoChanges[i];
                Debug.WriteLine($"[MidiPlayer] Tempo[{i}]: AbsoluteTime={t.AbsoluteTime}, MicrosecondsPerQuarterNote={t.MicrosecondsPerQuarterNote}");
            }

            // === 关键验证：CalculateTickFromMs 的输入输出 ===
            int calculatedTick = CalculateTickFromMs(positionMs);
            Debug.WriteLine($"[MidiPlayer] CalculateTickFromMs({positionMs:F2}) = {calculatedTick}");

            // 验证逆运算
            double backToMs = CalculateMsFromTick(calculatedTick); // 需要实现这个方法
            Debug.WriteLine($"[MidiPlayer] 逆运算验证: tick={calculatedTick} -> ms={backToMs:F2}");

            Debug.WriteLine($"[MidiPlayer] SetPosition({positionMs:F0}ms) 开始");

            bool wasPlaying = _isPlaying;
            bool wasPaused = _isPaused;

            // 临时停止定时器和音频，但保持 isPlaying 状态
            _timer?.Stop();
            AllNotesOff();

            // 更新位置（此时 isPlaying 仍然为 true）
            _pausedElapsedMs = positionMs;
            _currentTick = CalculateTickFromMs(positionMs);
            _currentEventIndex = FindEventIndexAtTick(_currentTick);

            Debug.WriteLine($"[MidiPlayer] 设置后 pausedElapsedMs={_pausedElapsedMs:F0}");

            // 恢复播放
            if (wasPlaying)
            {
                if (wasPaused)
                {
                    _isPaused = true;
                    _stopwatch.Reset();
                }
                else
                {
                    _isPaused = false;
                    _stopwatch.Restart();
                    StartTimer();
                }
            }
            else
            {
                _isPlaying = false;
                _isPaused = false;
                _stopwatch.Reset();
            }

            Debug.WriteLine($"[MidiPlayer] SetPosition完成, CurrentTimeMs={GetCurrentTimeMs():F0}");
        }

        private double CalculateMsFromTick(int tick)
        {
            if (tick <= 0) return 0;

            if (_tempoChanges.Count == 0)
            {
                // 默认 120 BPM = 500000 微秒/四分音符
                return tick * 500000.0 / (_midiFile.DeltaTicksPerQuarterNote * 1000.0);
            }

            double totalMs = 0;
            long lastTick = 0;
            int tempo = 500000; // 默认 120 BPM

            foreach (var tempoEvent in _tempoChanges)
            {
                if (tempoEvent.AbsoluteTime > tick)
                {
                    // tick 在这一段内
                    long deltaTicks = tick - lastTick;
                    totalMs += deltaTicks * tempo / (_midiFile.DeltaTicksPerQuarterNote * 1000.0);
                    return totalMs;
                }

                long segmentTicks = tempoEvent.AbsoluteTime - lastTick;
                totalMs += segmentTicks * tempo / (_midiFile.DeltaTicksPerQuarterNote * 1000.0);
                lastTick = tempoEvent.AbsoluteTime;
                tempo = tempoEvent.MicrosecondsPerQuarterNote;
            }

            // tick 在最后一段
            long finalTicks = tick - lastTick;
            totalMs += finalTicks * tempo / (_midiFile.DeltaTicksPerQuarterNote * 1000.0);

            return totalMs;
        }

        private double CalculateTotalDurationMs()
        {
            if (_events.Count == 0) return 0;

            var lastEvent = _events[_events.Count - 1];
            long totalTicks = lastEvent.AbsoluteTime;

            if (_tempoChanges.Count == 0)
            {
                return totalTicks * 500000.0 / _midiFile.DeltaTicksPerQuarterNote / 1000.0;
            }

            double totalMs = 0;
            long lastTick = 0;
            int tempo = 500000;

            foreach (var tempoEvent in _tempoChanges)
            {
                long deltaTicks = tempoEvent.AbsoluteTime - lastTick;
                totalMs += deltaTicks * tempo / (_midiFile.DeltaTicksPerQuarterNote * 1000.0);
                lastTick = tempoEvent.AbsoluteTime;
                tempo = tempoEvent.MicrosecondsPerQuarterNote;
            }

            // 最后一段
            long finalDeltaTicks = totalTicks - lastTick;
            totalMs += finalDeltaTicks * tempo / (_midiFile.DeltaTicksPerQuarterNote * 1000.0);

            return totalMs;
        }

        private int CalculateTickFromMs(double targetMs)
        {
            if (targetMs <= 0) return 0;

            // 如果没有 tempo 变化事件，使用默认 tempo 计算
            if (_tempoChanges.Count == 0)
            {
                int defaultTempo = 500000;
                return (int)(targetMs * _midiFile.DeltaTicksPerQuarterNote / (defaultTempo / 1000.0));
            }

            double accumulatedMs = 0;
            long lastTick = 0;
            int tempo = 500000; // 默认 120 BPM

            foreach (var tempoEvent in _tempoChanges)
            {
                // 计算这一段的时间（毫秒）
                long deltaTicks = tempoEvent.AbsoluteTime - lastTick;
                double segmentMs = deltaTicks * tempo / (_midiFile.DeltaTicksPerQuarterNote * 1000.0);

                if (accumulatedMs + segmentMs >= targetMs)
                {
                    // 目标在这一段内
                    double remainingMs = targetMs - accumulatedMs;
                    // ticks = ms * ticks_per_quarter_note / (microseconds_per_quarter_note / 1000)
                    int additionalTicks = (int)(remainingMs * _midiFile.DeltaTicksPerQuarterNote / (tempo / 1000.0));
                    return (int)lastTick + additionalTicks;
                }

                accumulatedMs += segmentMs;
                lastTick = tempoEvent.AbsoluteTime;
                tempo = tempoEvent.MicrosecondsPerQuarterNote;
            }

            // 在最后一段
            double finalRemainingMs = targetMs - accumulatedMs;
            int finalAdditionalTicks = (int)(finalRemainingMs * _midiFile.DeltaTicksPerQuarterNote / (tempo / 1000.0));
            return (int)lastTick + finalAdditionalTicks;
        }

        private int FindEventIndexAtTick(int tick)
        {
            for (int i = 0; i < _events.Count; i++)
            {
                if (_events[i].AbsoluteTime >= tick)
                    return i;
            }
            return _events.Count;
        }

        private void StartTimer()
        {
            Console.WriteLine($"[MidiPlayer] StartTimer() 开始");

            _timer?.Dispose();
            _timer = new System.Timers.Timer(10);
            _timer.Elapsed += OnTimerElapsed;
            _timer.AutoReset = true;
            _timer.Enabled = true;

            Console.WriteLine($"[MidiPlayer] 定时器启动: Enabled={_timer.Enabled}, Interval={_timer.Interval}");

            // 立即手动调用一次，确保事件被处理
            Console.WriteLine($"[MidiPlayer] 手动触发第一次UpdatePlayback");
            UpdatePlayback();
        }

        private void OnTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Console.WriteLine($"[MidiPlayer] 定时器触发! 时间={DateTime.Now:HH:mm:ss.fff}");
            UpdatePlayback();
        }

        private void UpdatePlayback()
        {
            Console.WriteLine($"[MidiPlayer] UpdatePlayback() 被调用");
            Console.WriteLine($"[MidiPlayer] isPlaying={_isPlaying}, isPaused={_isPaused}");
            Console.WriteLine($"[MidiPlayer] currentEventIndex={_currentEventIndex}, events.Count={_events.Count}");

            if (!_isPlaying || _isPaused)
            {
                Console.WriteLine($"[MidiPlayer] 播放暂停或停止，跳过");
                return;
            }

            double elapsed = _pausedElapsedMs + _stopwatch.Elapsed.TotalMilliseconds;
            int targetTick = CalculateTickFromMs(elapsed);

            Console.WriteLine($"[MidiPlayer] elapsed={elapsed:F2}ms, targetTick={targetTick}");

            int eventsProcessed = 0;
            while (_currentEventIndex < _events.Count &&
                   _events[_currentEventIndex].AbsoluteTime <= targetTick)
            {
                var midiEvent = _events[_currentEventIndex];
                ProcessMidiEvent(midiEvent);
                _currentEventIndex++;
                eventsProcessed++;
            }

            Console.WriteLine($"[MidiPlayer] 本次处理了 {eventsProcessed} 个事件");

            _currentTick = targetTick;

            if (_currentEventIndex >= _events.Count)
            {
                Console.WriteLine($"[MidiPlayer] 播放完毕");
                StopInternal();
                AllNotesOff();

                // 触发播放完成事件
                OnPlaybackCompleted?.Invoke();
            }
        }

        private void ProcessMidiEvent(MidiEvent midiEvent)
        {
            if (midiEvent is TempoEvent)
                return;

            try
            {
                if (midiEvent is NoteOnEvent noteOn)
                {
                    if (noteOn.Velocity > 0)
                    {
                        // Note On
                        _activeNotes[noteOn.NoteNumber]++;
                        int msg = noteOn.GetAsShortMessage();
                        _midiOut.Send(msg);
                    }
                    else
                    {
                        // Velocity=0 在 MIDI 标准中代表 Note Off
                        _activeNotes[noteOn.NoteNumber] = Math.Max(0, _activeNotes[noteOn.NoteNumber] - 1);

                        // 直接利用 NoteOnEvent 的 OffEvent 属性，或者用相同通道和音符构建正确的 Note Off 消息
                        // 方法一（推荐）：直接发送原有的 Velocity=0 消息，标准的 Note On with Velocity 0 就是 Note Off
                        int noteOffMsg = noteOn.GetAsShortMessage(); // 这正是 Velocity=0 的 Note On，等同于 Note Off
                        _midiOut.Send(noteOffMsg);
                    }
                }
                else if (midiEvent.CommandCode == MidiCommandCode.NoteOff)
                {
                    // 这里假设是 NoteEvent，直接发送
                    if (midiEvent is NoteEvent noteOff)
                    {
                        _activeNotes[noteOff.NoteNumber] = Math.Max(0, _activeNotes[noteOff.NoteNumber] - 1);
                        _midiOut.Send(noteOff.GetAsShortMessage());
                    }
                }
                else if (midiEvent is PatchChangeEvent patchChange)
                {
                    _midiOut.Send(patchChange.GetAsShortMessage());
                }
                else if (midiEvent is ControlChangeEvent controlChange)
                {
                    _midiOut.Send(controlChange.GetAsShortMessage());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MidiPlayer] 事件处理错误: {ex.Message}");
            }
        }

        private void AllNotesOff()
        {
            try
            {
                for (int channel = 1; channel <= 16; channel++)
                {
                    _midiOut.Send(MidiMessage.ChangeControl(123, 0, channel).RawData);
                    _midiOut.Send(MidiMessage.ChangeControl(120, 0, channel).RawData);
                }
                Array.Clear(_activeNotes, 0, _activeNotes.Length);
                Console.WriteLine($"[MidiPlayer] AllNotesOff完成");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MidiPlayer] AllNotesOff出错: {ex.Message}");
            }
        }

        private double GetCurrentTimeMs()
        {
            if (_isPaused) return _pausedElapsedMs;
            if (!_isPlaying) return _pausedElapsedMs;  // 即使停止也返回最后的位置
            return _pausedElapsedMs + _stopwatch.Elapsed.TotalMilliseconds;
        }

        public void Dispose()
        {
            Console.WriteLine($"[MidiPlayer] Dispose()");
            Stop();
            _timer?.Dispose();
        }
    }
}