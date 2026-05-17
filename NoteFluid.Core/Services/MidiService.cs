using NAudio.Midi;
using NoteFluid.Core.Models;
using System.Diagnostics;
using System.IO;
using System.Windows.Media;

namespace NoteFluid.Core.Services
{
    public class MidiService
    {
        private readonly FileService _fileService;
        private readonly AudioService _audioService;

        public event Action<bool>? OnMidiFilePlaying;
        public event Action<bool>? OnMidiFileResume;
        public event Action<TimeSpan, TimeSpan>? OnProgressChanged;
        public event Action? OnMidiFileCompleted;

        private MidiPlayer? _midiPlayer;
        private MidiFile? _currentMidiFile;
        private MidiOut? _midiOut;
        private Timer? _progressTimer;

        public MidiService(FileService fileService, AudioService audioService)
        {
            _fileService = fileService;
            _audioService = audioService;
        }

        public async Task PlayMidiFile(FileInfo midiFileInfo)
        {
            try
            {
                await Task.Run(() =>
                {
                    _currentMidiFile = new MidiFile(midiFileInfo.FullName, false);
                    Console.WriteLine($"MIDI文件格式: {_currentMidiFile.FileFormat}");
                    Console.WriteLine($"轨道数: {_currentMidiFile.Tracks}");
                    Console.WriteLine($"时间分辨率: {_currentMidiFile.DeltaTicksPerQuarterNote} ticks/四分音符");

                    if (MidiOut.NumberOfDevices == 0)
                    {
                        Console.WriteLine("没有找到MIDI输出设备!");
                        return;
                    }

                    _midiOut = new MidiOut(0);
                    Console.WriteLine($"使用MIDI设备: {MidiOut.DeviceInfo(0).ProductName}");

                    // 创建播放器（内部已计算总时长）
                    _midiPlayer = new MidiPlayer(_currentMidiFile, _midiOut);

                    // 订阅播放完成事件
                    _midiPlayer.OnPlaybackCompleted += HandlePlaybackCompleted;

                    _midiPlayer.Start();

                    OnMidiFilePlaying?.Invoke(true);

                    // 启动进度更新定时器
                    StartProgressTimer();
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
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
            }

            _midiPlayer.Stop();
            _midiPlayer.Dispose();
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

        private void StartProgressTimer()
        {
            StopProgressTimer();
            _progressTimer = new Timer(UpdateProgress, null, 0, 100);
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

                //// 自动停止
                //if (currentTime >= totalTime && _midiPlayer.IsPlaying)
                //{
                //    StopMidiFile();
                //}
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

            // 清理资源（可选，取决于你的需求）
            // 如果你想让用户能够重新播放同一个文件，就不要清理
            _midiPlayer?.Dispose();
            _midiPlayer = null;
            _midiOut?.Dispose();
            _midiOut = null;
        }

    }

    public static class TrackColorHelper
    {
        private static readonly Color[] TrackColors =
        [
            Color.FromRgb(65, 105, 225),  // Royal Blue
            Color.FromRgb(50, 205, 50),   // Lime Green
            Color.FromRgb(255, 69, 0),    // Red Orange
            Color.FromRgb(138, 43, 226),  // Blue Violet
            Color.FromRgb(255, 215, 0),   // Gold
            Color.FromRgb(0, 206, 209),   // Dark Turquoise
            Color.FromRgb(218, 112, 214), // Orchid
            Color.FromRgb(255, 127, 80)   // Coral
        ];

        public static Color GetTrackColor(int trackIndex)
        {
            return TrackColors[trackIndex % TrackColors.Length];
        }
    }

    public static class TimeSpanExtensions
    {
        public static TimeSpan FromMicroseconds(long microseconds)
        {
            return TimeSpan.FromTicks(microseconds * 10);
        }
    }
}