using NoteFluid.Core.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NoteFluid.Core.Views
{
    /// <summary>
    /// MidiVisualization.xaml 的交互逻辑
    /// </summary>
    public partial class MidiVisualization : Page
    {
        // 88键钢琴从A0开始 (MIDI音符21)
        private const int START_MIDI_NOTE = 21;
        private const int TOTAL_KEYS = 88;
        private const int WHITE_KEY_COUNT = 52;
        private const int BLACK_KEY_COUNT = 36;

        // 基准尺寸
        private const double BASE_WHITE_KEY_WIDTH = 19.23;  // 1000/52
        private const double BASE_WHITE_KEY_HEIGHT = 130;
        private const double BASE_BLACK_KEY_WIDTH = 12;
        private const double BASE_BLACK_KEY_HEIGHT = 80;

        // 键盘序列 - 从A0开始
        private readonly string[] keyboardNoteSequence =
            { "A", "A#", "B", "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#" };

        // 标记哪些是黑键
        private readonly bool[] isBlackInSequence =
            { false, true, false, false, true, false, true, false, false, true, false, true };

        private readonly VisualizationViewModel _viewModel;


        public MidiVisualization(VisualizationViewModel viewModel)
        {
            InitializeComponent();
            SizeChanged += PianoKeyboard_SizeChanged;
            DataContext = viewModel;
            _viewModel = viewModel;
            DrawPiano();
        }

        private void PianoKeyboard_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // 窗口大小改变时重新绘制
            DrawPiano();
        }

        private void DrawPiano()
        {
            PianoCanvas.Children.Clear();

            // 获取可用宽度
            double availableWidth = this.ActualWidth - 40; // 减去边距
            if (availableWidth <= 0) availableWidth = 1000;

            // 根据可用宽度计算白键宽度
            double whiteKeyWidth = availableWidth / WHITE_KEY_COUNT;
            double whiteKeyHeight = BASE_WHITE_KEY_HEIGHT;
            double blackKeyWidth = whiteKeyWidth * 0.6;
            double blackKeyHeight = BASE_BLACK_KEY_HEIGHT;

            // 更新Canvas基准宽度（Viewbox会根据这个缩放）
            PianoCanvas.Width = availableWidth;

            // 第一遍：绘制所有白键
            int whiteKeyIndex = 0;

            for (int i = 0; i < TOTAL_KEYS; i++)
            {
                int midiNote = START_MIDI_NOTE + i;
                int sequenceIndex = i % 12;
                string noteName = keyboardNoteSequence[sequenceIndex];

                // 判断是否为白键
                if (!noteName.Contains("#"))
                {
                    // 创建白键
                    var whiteKey = new Border
                    {
                        Width = whiteKeyWidth,
                        Height = whiteKeyHeight,
                        Background = new SolidColorBrush(Colors.White),
                        BorderBrush = new SolidColorBrush(Colors.Black),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(0, 0, 3, 3),
                        Tag = midiNote
                    };

                    // 添加音名标签（只在键足够宽时显示）
                    if (whiteKeyWidth > 15)
                    {
                        int octave = GetOctaveNumber(midiNote);
                        var label = new TextBlock
                        {
                            Text = $"{noteName}{octave}",
                            FontSize = Math.Max(6, whiteKeyWidth * 0.4),
                            Foreground = new SolidColorBrush(Colors.Gray),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Bottom,
                            Margin = new Thickness(0, 0, 0, 8)
                        };

                        var grid = new Grid();
                        grid.Children.Add(label);
                        whiteKey.Child = grid;
                    }

                    // 定位白键
                    Canvas.SetLeft(whiteKey, whiteKeyIndex * whiteKeyWidth);
                    Canvas.SetTop(whiteKey, 5);
                    PianoCanvas.Children.Add(whiteKey);

                    whiteKeyIndex++;
                }
            }

            // 第二遍：绘制黑键
            whiteKeyIndex = 0;

            for (int i = 0; i < TOTAL_KEYS; i++)
            {
                int midiNote = START_MIDI_NOTE + i;
                int sequenceIndex = i % 12;
                string noteName = keyboardNoteSequence[sequenceIndex];

                if (!noteName.Contains("#"))
                {
                    // 白键，增加计数
                    whiteKeyIndex++;
                }
                else
                {
                    // 计算黑键位置
                    double blackKeyX = CalculateBlackKeyPosition(whiteKeyIndex, noteName, whiteKeyWidth, blackKeyWidth);

                    // 创建黑键
                    var blackKey = new Border
                    {
                        Width = blackKeyWidth,
                        Height = blackKeyHeight,
                        Background = new SolidColorBrush(Colors.Black),
                        BorderBrush = new SolidColorBrush(Colors.Black),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(0, 0, 3, 3),
                        Tag = midiNote
                    };

                    // 添加音名标签（只在键足够宽时显示）
                    if (blackKeyWidth > 10)
                    {
                        int octave = GetOctaveNumber(midiNote);
                        var label = new TextBlock
                        {
                            Text = $"{noteName}{octave}",
                            FontSize = Math.Max(5, blackKeyWidth * 0.4),
                            Foreground = new SolidColorBrush(Colors.White),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Bottom,
                            Margin = new Thickness(0, 0, 0, 5)
                        };

                        var grid = new Grid();
                        grid.Children.Add(label);
                        blackKey.Child = grid;
                    }

                    // 定位黑键
                    Canvas.SetLeft(blackKey, blackKeyX);
                    Canvas.SetTop(blackKey, 5);
                    Canvas.SetZIndex(blackKey, 1);  // 黑键置于白键之上
                    PianoCanvas.Children.Add(blackKey);
                }
            }
        }

        private double CalculateBlackKeyPosition(int whiteKeyIndex, string noteName, double whiteKeyWidth, double blackKeyWidth)
        {
            double position;

            switch (noteName)
            {
                case "C#":
                    // C# 位于 C 和 D 之间
                    position = whiteKeyIndex * whiteKeyWidth - blackKeyWidth * 0.5;
                    break;
                case "D#":
                    // D# 位于 D 和 E 之间
                    position = whiteKeyIndex * whiteKeyWidth - blackKeyWidth * 0.5;
                    break;
                case "F#":
                    // F# 位于 F 和 G 之间
                    position = whiteKeyIndex * whiteKeyWidth - blackKeyWidth * 0.5;
                    break;
                case "G#":
                    // G# 位于 G 和 A 之间
                    position = whiteKeyIndex * whiteKeyWidth - blackKeyWidth * 0.55;
                    break;
                case "A#":
                    // A# 位于 A 和 B 之间
                    position = whiteKeyIndex * whiteKeyWidth - blackKeyWidth * 0.55;
                    break;
                default:
                    position = whiteKeyIndex * whiteKeyWidth;
                    break;
            }

            return position;
        }

        private int GetOctaveNumber(int midiNote)
        {
            // A0 = MIDI 21
            return (midiNote - 12) / 12;
        }

        private void BackToFileListButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.NavigateTo("FileList");
        }
    }
}
