using NAudio.Midi;
using NoteFluid.Core.Models;
using NoteFluid.Core.Utilities;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;

namespace NoteFluid.Core.Services
{
    public class MidiService
    {
        public event Action<bool>? OnMidiFilePlaying;
        public event Action<bool>? OnMidiFileResume;
        public event Action<TimeSpan, TimeSpan>? OnProgressChanged;
        public event Action? OnMidiFileCompleted;
        public event Action<MidiFile>? OnMidiFileLoaded;
        public event Action<double>? OnMidiPlaybackStarted;

        private MidiPlayer? _midiPlayer;
        private MidiFile? _currentMidiFile;
        private FileInfo? _currentMidiFileInfo;
        private MidiOut? _midiOut;
        private Timer? _progressTimer;
        private bool _isLoaded;
        private ObservableCollection<InstrumentInfo>? _instrumentInfos;

        public double CurrentTimeMs => _midiPlayer?.CurrentTimeMs ?? 0;
        public bool IsPlaying => _midiPlayer?.IsPlaying ?? false;
        public MidiFile? CurrentMidiFile => _currentMidiFile;

        public async Task<MidiFile?> LoadMidiFile(FileInfo midiFileInfo)
        {
            try
            {
                if (_currentMidiFileInfo != null
                    && _currentMidiFileInfo.FullName.Equals(midiFileInfo.FullName))
                {
                    return _currentMidiFile;
                }

                _isLoaded = false;
                await Task.Run(() =>
                {
                    _currentMidiFile = new MidiFile(midiFileInfo.FullName, false);
                    _currentMidiFileInfo = midiFileInfo;

                    _isLoaded = true;
                    OnMidiFileLoaded?.Invoke(_currentMidiFile);
                    Debug.WriteLine($"MIDI文件格式: {_currentMidiFile.FileFormat}");
                    Debug.WriteLine($"轨道数: {_currentMidiFile.Tracks}");
                    Debug.WriteLine($"时间分辨率: {_currentMidiFile.DeltaTicksPerQuarterNote} ticks/四分音符");
                });
                return _currentMidiFile;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return null;
            }
        }

        public async Task PlayMidiFile(double delayMs = 0)
        {
            try
            {
                if (_currentMidiFile == null || !_isLoaded)
                {
                    throw new Exception("当前MIDI文件为空");
                }

                await Task.Run(() =>
                {
                    if (MidiOut.NumberOfDevices == 0)
                    {
                        Debug.WriteLine("没有找到MIDI输出设备!");
                        return;
                    }

                    _midiOut = new MidiOut(0);
                    Debug.WriteLine($"使用MIDI设备: {MidiOut.DeviceInfo(0).ProductName}");

                    _midiPlayer = new MidiPlayer(_currentMidiFile, _midiOut, _instrumentInfos);
                    _midiPlayer.OnPlaybackCompleted += HandlePlaybackCompleted;

                    // 设置时间偏移，让 CurrentTimeMs 从 -delayMs 开始
                    _midiPlayer.SetTimeOffset(-delayMs);

                    // 立即启动播放器
                    _midiPlayer.Start();

                    OnMidiFilePlaying?.Invoke(true);
                    OnMidiPlaybackStarted?.Invoke(delayMs);
                    StartProgressTimer();
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public void PauseMidiFile()
        {
            if (_midiPlayer == null) return;

            _midiPlayer.Pause();
            StopProgressTimer();
            OnMidiFilePlaying?.Invoke(false);
        }

        public void ResumeMidiFile()
        {
            if (_midiPlayer == null) return;

            _midiPlayer.Resume();
            StartProgressTimer();
            OnMidiFileResume?.Invoke(true);
            OnMidiFilePlaying?.Invoke(true);
        }

        public void StopMidiFile()
        {
            if (_midiPlayer == null) return;

            StopProgressTimer();

            // 取消订阅事件
            if (_midiPlayer != null)
            {
                _midiPlayer.OnPlaybackCompleted -= HandlePlaybackCompleted;
                _midiPlayer.Stop();
                _midiPlayer.Dispose();
            }
            
            _midiOut?.Dispose();
            _midiOut = null;
            _midiPlayer = null;

            OnMidiFilePlaying?.Invoke(false);
        }

        public async Task SetProgress(double value)
        {
            if (_midiPlayer == null) return;

            value = Math.Max(0, Math.Min(1, value));

            await Task.Run(() =>
            {
                // 直接使用毫秒
                double targetMilliseconds = value * _midiPlayer.TotalDurationMs;

                Debug.WriteLine($"目标位置: {targetMilliseconds} 毫秒 = {targetMilliseconds / 1000.0:F2} 秒");

                _midiPlayer.SetPosition(targetMilliseconds);  // 传入毫秒

                Debug.WriteLine($"设置后 CurrentTimeMs: {_midiPlayer.CurrentTimeMs} 毫秒");

                // 更新进度显示
                var currentTime = TimeSpan.FromMilliseconds(targetMilliseconds);
                var totalTime = TimeSpan.FromMilliseconds(_midiPlayer.TotalDurationMs);
                OnProgressChanged?.Invoke(currentTime, totalTime);
            });
        }

        public void NoteOn(int midiNote)
        {
            if (_midiPlayer == null) return;

            _midiPlayer.NoteOn(midiNote);
        }

        public void NoteOff(int midiNote)
        {
            if (_midiPlayer == null) return;

            _midiPlayer.NoteOff(midiNote);
        }

        public void SetInstruments(ObservableCollection<InstrumentInfo> instrumentInfos)
        {
            _instrumentInfos = instrumentInfos;
        }

        private void StartProgressTimer()
        {
            StopProgressTimer();
            _progressTimer = new Timer(UpdateProgress, null, 0, 16);
        }

        private void StopProgressTimer()
        {
            _progressTimer?.Dispose();
            _progressTimer = null;
        }

        private void UpdateProgress(object? state)
        {
            if (_midiPlayer == null) return;

            try
            {
                var currentTime = TimeSpan.FromMilliseconds(_midiPlayer.CurrentTimeMs);
                var totalTime = TimeSpan.FromMilliseconds(_midiPlayer.TotalDurationMs);

                OnProgressChanged?.Invoke(currentTime, totalTime);
            }
            catch
            {
                // 播放器可能已释放
            }
        }

        // 处理播放完成
        private void HandlePlaybackCompleted()
        {
            Console.WriteLine("[MidiService] 播放完成，重置状态");

            // 停止定时器
            StopProgressTimer();

            // 重置进度到开始位置
            var totalTime = _midiPlayer != null ? TimeSpan.FromMilliseconds(_midiPlayer.TotalDurationMs) : TimeSpan.Zero;
            OnProgressChanged?.Invoke(TimeSpan.Zero, totalTime);

            // 触发播放完成事件
            OnMidiFileCompleted?.Invoke();

            // 通知播放状态为 false
            OnMidiFilePlaying?.Invoke(false);

            _midiPlayer?.Dispose();
            _midiPlayer = null;
            _midiOut?.Dispose();
            _midiOut = null;
        }

    }
}