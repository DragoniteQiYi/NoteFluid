using CommunityToolkit.Mvvm.Input;
using NoteFluid.Core.Models;
using NoteFluid.Core.Services;
using NoteFluid.Core.Utilities;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace NoteFluid.Core.ViewModels
{
    public class VisualizationViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly NavigateService _navigateService;
        private readonly ConfigService _configService;
        private readonly MidiService _midiService;
        private readonly FileService _fileService;

        public event PropertyChangedEventHandler? PropertyChanged;

        private const int START_MIDI_NOTE = 21;
        private const int TOTAL_KEYS = 88;
        private const int WHITE_KEY_COUNT = 52;
        private const double BASE_WHITE_KEY_HEIGHT = 130;
        private const double BASE_BLACK_KEY_HEIGHT = 80;

        private readonly string[] keyboardNoteSequence =
            { "A", "A#", "B", "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#" };

        private double _progressValue;

        // 新增：钢琴键集合
        public ObservableCollection<PianoKey> PianoKeys { get; private set; }
        public ObservableCollection<PianoKey> WhiteKeys { get; private set; }
        public ObservableCollection<PianoKey> BlackKeys { get; private set; }
        public bool ShowPitchName { get; set; }
        public bool ShowOctave { get; set; }

        public double ProgressValue
        {
            get => _progressValue;
            set
            {
                _progressValue = value;
                OnPropertyChanged();
            }
        }

        public VisualizationViewModel(NavigateService navigateService,
            ConfigService configService, MidiService midiService,
            FileService fileService)
        {
            _navigateService = navigateService;
            _configService = configService;
            _midiService = midiService;
            _fileService = fileService;

            PianoKeys = [];
            WhiteKeys = [];
            BlackKeys = [];

            _midiService.OnProgressChanged += HandleProgressValueChanged;
        }

        public void NavigateTo(string pageName)
        {
            _midiService.StopMidiFile();
            _navigateService.Navigate(pageName);
            Dispose();
        }

        // 生成所有钢琴键数据
        public void GeneratePianoKeys(double availableWidth)
        {
            if (availableWidth <= 0) availableWidth = 1000;

            PianoKeys.Clear();
            WhiteKeys.Clear();
            BlackKeys.Clear();

            double whiteKeyWidth = availableWidth / WHITE_KEY_COUNT;
            double blackKeyWidth = whiteKeyWidth * 0.6;

            if (_configService?.ConfigData?.Visualization != null)
            {
                ShowPitchName = _configService.ConfigData.Visualization.ShowPitchName;
                ShowOctave = _configService.ConfigData.Visualization.ShowOctave;
            }

            int whiteKeyIndex = 0;

            for (int i = 0; i < TOTAL_KEYS; i++)
            {
                int midiNote = START_MIDI_NOTE + i;
                int sequenceIndex = i % 12;
                string noteName = keyboardNoteSequence[sequenceIndex];
                int octave = GetOctaveNumber(midiNote);
                bool isBlack = noteName.Contains('#');

                var key = new PianoKey
                {
                    MidiNote = midiNote,
                    NoteName = noteName,
                    Octave = octave,
                    IsBlackKey = isBlack,
                };

                if (!isBlack)
                {
                    // 白键
                    key.Width = whiteKeyWidth;
                    key.Height = BASE_WHITE_KEY_HEIGHT;
                    key.X = whiteKeyIndex * whiteKeyWidth;
                    key.Y = 5;

                    if (ShowPitchName)
                        key.DisplayText = $"{noteName}{octave}";
                    else if (noteName == "C" && ShowOctave)
                        key.DisplayText = $"C{octave}";

                    WhiteKeys.Add(key);
                    whiteKeyIndex++;
                }
                else
                {
                    // 黑键
                    key.Width = blackKeyWidth;
                    key.Height = BASE_BLACK_KEY_HEIGHT;
                    key.X = CalculateBlackKeyPosition(whiteKeyIndex, noteName, whiteKeyWidth, blackKeyWidth);
                    key.Y = 5;

                    if (ShowPitchName)
                        key.DisplayText = $"{noteName}{octave}";

                    BlackKeys.Add(key);
                }

                // 为每个键创建命令
                int capturedMidiNote = midiNote;
                key.KeyClickCommand = new RelayCommand(() =>
                {
                    Debug.WriteLine($"Button点击 - MIDI音符: {capturedMidiNote}");
                    OnKeyClicked(capturedMidiNote);
                });

                PianoKeys.Add(key);
            }
        }

        // 保留原有的 DrawPiano 方法，添加点击事件处理
        public void DrawPiano(Canvas pianoCanvas, double actualWidth)
        {
            pianoCanvas.Children.Clear();

            double availableWidth = actualWidth;
            if (availableWidth <= 0) availableWidth = 1000;

            GeneratePianoKeys(availableWidth);

            pianoCanvas.Width = availableWidth;

            // 绘制白键
            foreach (var whiteKey in WhiteKeys)
            {
                var keyBorder = CreateWhiteKeyBorder(whiteKey);
                Canvas.SetLeft(keyBorder, whiteKey.X);
                Canvas.SetTop(keyBorder, whiteKey.Y);
                pianoCanvas.Children.Add(keyBorder);
            }

            // 绘制黑键
            foreach (var blackKey in BlackKeys)
            {
                var keyBorder = CreateBlackKeyBorder(blackKey);
                Canvas.SetLeft(keyBorder, blackKey.X);
                Canvas.SetTop(keyBorder, blackKey.Y);
                Canvas.SetZIndex(keyBorder, blackKey.ZIndex);
                pianoCanvas.Children.Add(keyBorder);
            }
        }

        public async Task PlayMidiFile()
        {
            var currentFile = _fileService.SelectedFile;
            await _midiService.PlayMidiFile(currentFile);
        }

        // 创建白键边框（添加按下/释放事件）
        private Border CreateWhiteKeyBorder(PianoKey key)
        {
            var border = new Border
            {
                Width = key.Width,
                Height = key.Height,
                Background = key.IsPressed ?
                    new SolidColorBrush(Colors.LightGray) :
                    new SolidColorBrush(Colors.White),
                BorderBrush = new SolidColorBrush(Colors.Black),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(0, 0, 3, 3),
                Tag = key.MidiNote,
                Cursor = Cursors.Hand
            };

            int capturedMidiNote = key.MidiNote;

            // 鼠标按下时开始播放
            border.MouseLeftButtonDown += (s, e) =>
            {
                Debug.WriteLine($"白键按下 - MIDI音符: {capturedMidiNote}");
                border.Background = new SolidColorBrush(Colors.LightGray);
                PressKey(capturedMidiNote);
                border.CaptureMouse(); // 捕获鼠标以接收 MouseLeftButtonUp
                e.Handled = true;
            };

            // 鼠标释放时停止播放
            border.MouseLeftButtonUp += (s, e) =>
            {
                Debug.WriteLine($"白键释放 - MIDI音符: {capturedMidiNote}");
                ReleaseKey(capturedMidiNote);
                border.ReleaseMouseCapture();

                // 检查鼠标是否仍在琴键上
                Point mousePos = e.GetPosition(border);
                if (mousePos.X >= 0 && mousePos.X <= border.ActualWidth &&
                    mousePos.Y >= 0 && mousePos.Y <= border.ActualHeight)
                {
                    // 鼠标仍在琴键上，显示悬停高亮
                    border.Background = new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0));
                }
                else
                {
                    // 鼠标已移出，恢复默认颜色
                    border.Background = new SolidColorBrush(Colors.White);
                }

                e.Handled = true;
            };

            // 鼠标进入时高亮
            border.MouseEnter += (s, e) =>
            {
                // 如果被捕获说明正在按住此键，保持按下高亮
                if (border.IsMouseCaptured)
                {
                    border.Background = new SolidColorBrush(Colors.LightGray);
                }
                else
                {
                    // 未捕获时显示悬停高亮
                    border.Background = new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0));
                }
            };

            // 鼠标离开时处理
            border.MouseLeave += (s, e) =>
            {
                // 如果被捕获说明正在按住此键，保持按下高亮（不恢复）
                if (border.IsMouseCaptured)
                {
                    border.Background = new SolidColorBrush(Colors.LightGray);
                }
                else
                {
                    // 未捕获时恢复白色
                    border.Background = new SolidColorBrush(Colors.White);
                }
            };

            if (!string.IsNullOrEmpty(key.DisplayText))
            {
                var label = new TextBlock
                {
                    Text = key.DisplayText,
                    FontSize = Math.Max(6, key.Width * 0.4),
                    Foreground = new SolidColorBrush(Colors.Gray),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(0, 0, 0, 8),
                    IsHitTestVisible = false
                };

                var grid = new Grid();
                grid.Children.Add(label);
                border.Child = grid;
            }

            return border;
        }

        // 创建黑键边框（添加按下/释放事件）
        private Border CreateBlackKeyBorder(PianoKey key)
        {
            var border = new Border
            {
                Width = key.Width,
                Height = key.Height,
                Background = key.IsPressed ?
                    new SolidColorBrush(Colors.DarkGray) :
                    new SolidColorBrush(Colors.Black),
                BorderBrush = new SolidColorBrush(Colors.Black),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(0, 0, 3, 3),
                Tag = key.MidiNote,
                Cursor = Cursors.Hand
            };

            int capturedMidiNote = key.MidiNote;

            // 鼠标按下时开始播放
            border.MouseLeftButtonDown += (s, e) =>
            {
                Debug.WriteLine($"黑键按下 - MIDI音符: {capturedMidiNote}");
                border.Background = new SolidColorBrush(Colors.DarkGray);
                PressKey(capturedMidiNote);
                border.CaptureMouse();
                e.Handled = true;
            };

            // 鼠标释放时停止播放
            border.MouseLeftButtonUp += (s, e) =>
            {
                Debug.WriteLine($"黑键释放 - MIDI音符: {capturedMidiNote}");
                ReleaseKey(capturedMidiNote);
                border.ReleaseMouseCapture();

                // 检查鼠标是否仍在琴键上
                Point mousePos = e.GetPosition(border);
                if (mousePos.X >= 0 && mousePos.X <= border.ActualWidth &&
                    mousePos.Y >= 0 && mousePos.Y <= border.ActualHeight)
                {
                    // 鼠标仍在琴键上，显示悬停高亮
                    border.Background = new SolidColorBrush(Color.FromRgb(0x40, 0x40, 0x40));
                }
                else
                {
                    // 鼠标已移出，恢复默认颜色
                    border.Background = new SolidColorBrush(Colors.Black);
                }

                e.Handled = true;
            };

            // 鼠标进入时高亮
            border.MouseEnter += (s, e) =>
            {
                if (border.IsMouseCaptured)
                {
                    border.Background = new SolidColorBrush(Colors.DarkGray);
                }
                else
                {
                    border.Background = new SolidColorBrush(Color.FromRgb(0x40, 0x40, 0x40));
                }
            };

            // 鼠标离开时处理
            border.MouseLeave += (s, e) =>
            {
                // 如果被捕获说明正在按住此键，保持按下高亮（不恢复）
                if (border.IsMouseCaptured)
                {
                    border.Background = new SolidColorBrush(Colors.DarkGray);
                }
                else
                {
                    // 未捕获时恢复黑色
                    border.Background = new SolidColorBrush(Colors.Black);
                }
            };

            if (!string.IsNullOrEmpty(key.DisplayText))
            {
                var label = new TextBlock
                {
                    Text = key.DisplayText,
                    FontSize = Math.Max(5, key.Width * 0.4),
                    Foreground = new SolidColorBrush(Colors.White),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(0, 0, 0, 5),
                    IsHitTestVisible = false
                };

                var grid = new Grid();
                grid.Children.Add(label);
                border.Child = grid;
            }

            return border;
        }

        // 按键按下事件处理 - 发送 NoteOn 播放音符
        public async void PressKey(int midiNote)
        {
            var key = PianoKeys.FirstOrDefault(k => k.MidiNote == midiNote);
            if (key != null)
            {
                key.IsPressed = true;
                Debug.WriteLine($"按键按下: {key.NoteName}{key.Octave}");

                // 只发送 NoteOn，不自动停止
                await Task.Run(() =>
                {
                    _midiService.NoteOn(midiNote);
                });
            }
        }

        // 按键释放事件处理 - 发送 NoteOff 停止音符
        public void ReleaseKey(int midiNote)
        {
            var key = PianoKeys.FirstOrDefault(k => k.MidiNote == midiNote);
            if (key != null)
            {
                key.IsPressed = false;
                Debug.WriteLine($"按键释放: {key.NoteName}{key.Octave}");

                // 发送 NoteOff 停止音符
                _midiService.NoteOff(midiNote);
            }
        }

        private void HandleProgressValueChanged(TimeSpan currentTime, TimeSpan totalTime)
        {
            _progressValue = currentTime / totalTime * 100;
            OnPropertyChanged(nameof(ProgressValue));
        }

        private double CalculateBlackKeyPosition(int whiteKeyIndex, string noteName,
            double whiteKeyWidth, double blackKeyWidth)
        {
            return noteName switch
            {
                "C#" => whiteKeyIndex * whiteKeyWidth - blackKeyWidth * 0.5,
                "D#" => whiteKeyIndex * whiteKeyWidth - blackKeyWidth * 0.5,
                "F#" => whiteKeyIndex * whiteKeyWidth - blackKeyWidth * 0.5,
                "G#" => whiteKeyIndex * whiteKeyWidth - blackKeyWidth * 0.55,
                "A#" => whiteKeyIndex * whiteKeyWidth - blackKeyWidth * 0.55,
                _ => whiteKeyIndex * whiteKeyWidth,
            };
        }

        private int GetOctaveNumber(int midiNote)
        {
            return (midiNote - 12) / 12;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void OnKeyClicked(int midiNote)
        {
            Debug.WriteLine($"=== OnKeyClicked 被调用 ===");
            Debug.WriteLine($"MIDI音符: {midiNote}");

            var key = PianoKeys.FirstOrDefault(k => k.MidiNote == midiNote);
            if (key != null)
            {
                Debug.WriteLine($"找到琴键: {key.NoteName}{key.Octave}, 黑键: {key.IsBlackKey}");
                PressKey(midiNote);
            }
            else
            {
                Debug.WriteLine($"错误：未找到 MIDI 音符 {midiNote} 对应的琴键");
            }
        }

        public void ChangePitchNameDisplay(bool state)
        {
            ShowPitchName = state;
            if (_configService.ConfigData.Visualization != null)
            {
                _configService.ConfigData.Visualization.ShowPitchName = state;
                _configService.Save();
            }
            OnPropertyChanged(nameof(ShowPitchName));
        }

        public void ChangeOctaveDisplay(bool state)
        {
            ShowOctave = state;
            if (_configService.ConfigData.Visualization != null)
            {
                _configService.ConfigData.Visualization.ShowOctave = state;
                _configService.Save();
            }
            OnPropertyChanged(nameof(ShowOctave));
        }

        public void Dispose()
        {
            _midiService.OnProgressChanged -= HandleProgressValueChanged;
        }
    }
}
