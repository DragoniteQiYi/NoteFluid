using NAudio.Midi;
using NoteFluid.Core.Command;
using NoteFluid.Core.Models;
using NoteFluid.Core.Services;
using NoteFluid.Core.Utilities;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace NoteFluid.Core.ViewModels
{
    public class FreePlayViewModel : INotifyPropertyChanged
    {
        private readonly NavigateService _navigateService;
        private readonly ConfigService _configService;
        private readonly MidiService _midiService;

        private readonly MidiPlayer _midiPlayer;
        private readonly MidiOut _midiOut;

        public event PropertyChangedEventHandler? PropertyChanged;

        private const int START_MIDI_NOTE = 21;
        private const int TOTAL_KEYS = 88;
        private const int WHITE_KEY_COUNT = 52;
        private const double BASE_WHITE_KEY_HEIGHT = 130;
        private const double BASE_BLACK_KEY_HEIGHT = 80;

        private readonly string[] keyboardNoteSequence =
            { "A", "A#", "B", "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#" };

        // 新增：钢琴键集合
        public ObservableCollection<PianoKey> PianoKeys { get; private set; }
        public ObservableCollection<PianoKey> WhiteKeys { get; private set; }
        public ObservableCollection<PianoKey> BlackKeys { get; private set; }
        public bool ShowPitchName { get; set; }
        public bool ShowOctave { get; set; }

        public FreePlayViewModel(NavigateService navigateService,
            ConfigService configService, MidiService midiService,
            FileService fileService)
        {
            _navigateService = navigateService;
            _configService = configService;
            _midiService = midiService;

            PianoKeys = [];
            WhiteKeys = [];
            BlackKeys = [];

            _midiOut = new MidiOut(0);
            _midiPlayer = new MidiPlayer(_midiOut);
        }

        public void NavigateTo(string pageName)
        {
            _midiOut.Dispose();
            _midiPlayer.Dispose();
            _navigateService.Navigate(pageName);
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

            double availableWidth = actualWidth - 40;
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

        // 创建白键边框（添加点击事件）
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
                Cursor = System.Windows.Input.Cursors.Hand  // 添加手型光标
            };

            // 添加鼠标事件处理
            int capturedMidiNote = key.MidiNote;
            border.MouseLeftButtonDown += (s, e) =>
            {
                Debug.WriteLine($"白键被点击 - MIDI音符: {capturedMidiNote}");
                OnKeyClicked(capturedMidiNote);
                e.Handled = true;
            };

            // 鼠标进入时高亮
            border.MouseEnter += (s, e) =>
            {
                if (!key.IsPressed)
                {
                    border.Background = new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0));
                }
            };

            // 鼠标离开时恢复
            border.MouseLeave += (s, e) =>
            {
                if (!key.IsPressed)
                {
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
                    IsHitTestVisible = false  // 让点击事件穿透到 Border
                };

                var grid = new Grid();
                grid.Children.Add(label);
                border.Child = grid;
            }

            return border;
        }

        // 创建黑键边框（添加点击事件）
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
                Cursor = Cursors.Hand  // 添加手型光标
            };

            // 添加鼠标事件处理
            int capturedMidiNote = key.MidiNote;
            border.MouseLeftButtonDown += (s, e) =>
            {
                Debug.WriteLine($"黑键被点击 - MIDI音符: {capturedMidiNote}");
                OnKeyClicked(capturedMidiNote);
                e.Handled = true;
            };

            // 鼠标进入时高亮
            border.MouseEnter += (s, e) =>
            {
                if (!key.IsPressed)
                {
                    border.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x40, 0x40, 0x40));
                }
            };

            // 鼠标离开时恢复
            border.MouseLeave += (s, e) =>
            {
                if (!key.IsPressed)
                {
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
                    IsHitTestVisible = false  // 让点击事件穿透到 Border
                };

                var grid = new Grid();
                grid.Children.Add(label);
                border.Child = grid;
            }

            return border;
        }

        // 按键按下事件处理
        public async void PressKey(int midiNote)
        {
            var key = PianoKeys.FirstOrDefault(k => k.MidiNote == midiNote);
            if (key != null)
            {
                key.IsPressed = true;
                Debug.WriteLine($"按键按下: {key.NoteName}{key.Octave}");
                // 手动绘制的 UI 需要通过重新绘制来更新
                await Task.Run(async () =>
                {
                    await _midiService.PlayNoteAsync(_midiPlayer, midiNote);
                });
            }
        }

        // 按键释放事件处理
        public void ReleaseKey(int midiNote)
        {
            var key = PianoKeys.FirstOrDefault(k => k.MidiNote == midiNote);
            if (key != null)
            {
                key.IsPressed = false;
                Debug.WriteLine($"按键释放: {key.NoteName}{key.Octave}");
                // 手动绘制的 UI 需要通过重新绘制来更新
            }
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

        protected void OnPropertyChanged(string propertyName)
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
    }
}
