using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace NoteFluid.Core.Views
{
    public class PianoKeyboard : Canvas
    {
        // 钢琴键定义
        private readonly string[] whiteKeyNames = { "C", "D", "E", "F", "G", "A", "B" };
        private readonly string[] blackKeyNames = { "C#", "D#", "F#", "G#", "A#" };

        // 白键和黑键之间的位置关系
        private readonly int[] blackKeyPositions = { 0, 1, 3, 4, 5 }; // 黑键在白键之间的位置

        private List<Border> whiteKeys = new List<Border>();
        private List<Border> blackKeys = new List<Border>();

        // 键盘配置
        public int StartOctave { get; set; } = 3;
        public int NumberOfOctaves { get; set; } = 2;
        public double WhiteKeyWidth { get; set; } = 50;
        public double WhiteKeyHeight { get; set; } = 150;

        // 颜色配置
        public Brush WhiteKeyColor { get; set; } = Brushes.White;
        public Brush WhiteKeyPressedColor { get; set; } = Brushes.LightBlue;
        public Brush BlackKeyColor { get; set; } = Brushes.Black;
        public Brush BlackKeyPressedColor { get; set; } = Brushes.DarkBlue;

        public PianoKeyboard()
        {
            ClipToBounds = true;
            Background = Brushes.Transparent;
            DrawKeyboard();
        }

        public void DrawKeyboard()
        {
            this.Children.Clear();
            whiteKeys.Clear();
            blackKeys.Clear();

            // 绘制白键
            DrawWhiteKeys();

            // 绘制黑键（必须在白键之后绘制，以便显示在上层）
            DrawBlackKeys();

            // 订阅鼠标事件
            SubscribeMouseEvents();
        }

        private void DrawWhiteKeys()
        {
            double currentX = 0;

            for (int octave = StartOctave; octave < StartOctave + NumberOfOctaves; octave++)
            {
                for (int note = 0; note < whiteKeyNames.Length; note++)
                {
                    if (note == whiteKeyNames.Length - 1 && octave == StartOctave + NumberOfOctaves - 1)
                    {
                        // 添加最后一个C键以完成键盘
                        CreateWhiteKey(currentX, 0, WhiteKeyWidth, WhiteKeyHeight,
                            $"{whiteKeyNames[0]}{octave + 1}", octave, 0);
                        break;
                    }
                    else
                    {
                        CreateWhiteKey(currentX, 0, WhiteKeyWidth, WhiteKeyHeight,
                            $"{whiteKeyNames[note]}{octave}", octave, note);
                    }

                    currentX += WhiteKeyWidth;
                }
            }
        }

        private void CreateWhiteKey(double x, double y, double width, double height,
            string noteName, int octave, int noteIndex)
        {
            Border key = new Border
            {
                Width = width,
                Height = height,
                Background = WhiteKeyColor,
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(0.5),
                CornerRadius = new CornerRadius(0, 0, 3, 3),
                Tag = noteName
            };

            // 添加音符标签
            TextBlock label = new TextBlock
            {
                Text = noteName,
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10),
                FontSize = 10,
                Foreground = Brushes.Gray
            };
            key.Child = label;

            Canvas.SetLeft(key, x);
            Canvas.SetTop(key, y);

            this.Children.Add(key);
            whiteKeys.Add(key);
        }

        private void DrawBlackKeys()
        {
            double whiteKeyWidth = WhiteKeyWidth;
            double blackKeyWidth = WhiteKeyWidth * 0.6;
            double blackKeyHeight = WhiteKeyHeight * 0.6;

            for (int octave = StartOctave; octave < StartOctave + NumberOfOctaves; octave++)
            {
                for (int i = 0; i < blackKeyNames.Length; i++)
                {
                    int position = blackKeyPositions[i];
                    double x = (octave - StartOctave) * 7 * whiteKeyWidth +
                               position * whiteKeyWidth +
                               whiteKeyWidth - blackKeyWidth / 2;

                    CreateBlackKey(x, 0, blackKeyWidth, blackKeyHeight,
                        $"{blackKeyNames[i]}{octave}", octave, i);
                }
            }
        }

        private void CreateBlackKey(double x, double y, double width, double height,
            string noteName, int octave, int noteIndex)
        {
            Border key = new Border
            {
                Width = width,
                Height = height,
                Background = BlackKeyColor,
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(0, 0, 2, 2),
                Tag = noteName
            };

            Canvas.SetLeft(key, x);
            Canvas.SetTop(key, y);
            Canvas.SetZIndex(key, 1); // 确保黑键显示在白键上方

            this.Children.Add(key);
            blackKeys.Add(key);
        }

        private void SubscribeMouseEvents()
        {
            foreach (Border key in whiteKeys)
            {
                key.MouseLeftButtonDown += Key_MouseLeftButtonDown;
                key.MouseLeftButtonUp += Key_MouseLeftButtonUp;
                key.MouseEnter += Key_MouseEnter;
                key.MouseLeave += Key_MouseLeave;
            }

            foreach (Border key in blackKeys)
            {
                key.MouseLeftButtonDown += Key_MouseLeftButtonDown;
                key.MouseLeftButtonUp += Key_MouseLeftButtonUp;
                key.MouseEnter += Key_MouseEnter;
                key.MouseLeave += Key_MouseLeave;
            }
        }

        private void Key_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Border? key = sender as Border;
            if (key != null)
            {
                string? noteName = key.Tag as string;
                bool isBlackKey = blackKeys.Contains(key);

                // 改变按键颜色
                key.Background = isBlackKey ? BlackKeyPressedColor : WhiteKeyPressedColor;

                // 触发音符事件
                OnNoteOn(noteName);

                // 捕获鼠标以确保能收到MouseUp事件
                key.CaptureMouse();
            }
        }

        private void Key_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Border? key = sender as Border;
            if (key != null)
            {
                bool isBlackKey = blackKeys.Contains(key);
                key.Background = isBlackKey ? BlackKeyColor : WhiteKeyColor;

                string? noteName = key.Tag as string;
                OnNoteOff(noteName);

                key.ReleaseMouseCapture();
            }
        }

        private void Key_MouseEnter(object sender, MouseEventArgs e)
        {
            Border key = sender as Border;
            if (key != null && Mouse.LeftButton == MouseButtonState.Pressed)
            {
                bool isBlackKey = blackKeys.Contains(key);
                key.Background = isBlackKey ? BlackKeyPressedColor : WhiteKeyPressedColor;

                string? noteName = key.Tag as string;
                OnNoteOn(noteName);
            }
        }

        private void Key_MouseLeave(object sender, MouseEventArgs e)
        {
            Border key = sender as Border;
            if (key != null && Mouse.LeftButton == MouseButtonState.Pressed)
            {
                bool isBlackKey = blackKeys.Contains(key);
                key.Background = isBlackKey ? BlackKeyColor : WhiteKeyColor;

                string noteName = key.Tag as string;
                OnNoteOff(noteName);
            }
        }

        // 音符事件
        public event EventHandler<NoteEventArgs> NoteOn;
        public event EventHandler<NoteEventArgs> NoteOff;

        protected virtual void OnNoteOn(string noteName)
        {
            NoteOn?.Invoke(this, new NoteEventArgs(noteName, true));
        }

        protected virtual void OnNoteOff(string noteName)
        {
            NoteOff?.Invoke(this, new NoteEventArgs(noteName, false));
        }
    }

    public class NoteEventArgs : EventArgs
    {
        public string NoteName { get; set; }
        public bool IsNoteOn { get; set; }

        public NoteEventArgs(string noteName, bool isNoteOn)
        {
            NoteName = noteName;
            IsNoteOn = isNoteOn;
        }
    }
}
