using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace NoteFluid.Core.Views
{
    public partial class WaterfallBar : UserControl
    {
        // ======== Piano Roll 数据 ========

        public double StartTimeMs { get; set; }

        public double EndTimeMs { get; set; }

        public bool IsActive { get; set; }

        public bool ReachedBottom { get; set; }

        public Color Color { get; set; }

        public int NoteIndex { get; set; }

        // ======== Free Play ========

        private bool _isGrowing;
        private bool _isMovingUp;

        private double _growHeight;
        private double _canvasHeight;

        // ======== UI缓存 ========

        private readonly LinearGradientBrush _gradientBrush;

        private readonly DropShadowEffect _shadowEffect;

        public event Action<int, Color>? OnBarReached;
        public event Action<int>? OnBarDeactive;

        public WaterfallBar()
        {
            InitializeComponent();

            Visibility = Visibility.Collapsed;

            VerticalAlignment = VerticalAlignment.Top;

            _gradientBrush = new LinearGradientBrush
            {
                StartPoint = new Point(0, 1),
                EndPoint = new Point(0, 0)
            };

            _shadowEffect = new DropShadowEffect
            {
                BlurRadius = 10,
                ShadowDepth = 0,
                Opacity = 0.8
            };

            BarBorder.Background = _gradientBrush;
            BarBorder.Effect = _shadowEffect;
        }

        // ============================================
        // Piano Roll 模式（完全外部驱动）
        // ============================================

        public void Initialize(
            double width,
            double height,
            double left,
            Color color,
            int noteIndex)
        {
            Width = width;
            Height = height;
            NoteIndex = noteIndex;

            BarBorder.Width = width;
            BarBorder.Height = height;

            SetColor(color);

            Canvas.SetLeft(this, left);

            Visibility = Visibility.Visible;

            IsActive = true;
        }

        public void UpdatePosition(double top)
        {
            Canvas.SetTop(this, top);
        }

        public void Deactivate()
        {
            IsActive = false;
            ReachedBottom = false;
            Visibility = Visibility.Collapsed;
            OnBarDeactive?.Invoke(NoteIndex);
        }

        public void ReachBottom()
        {
            if (!ReachedBottom)
            {
                OnBarReached?.Invoke(NoteIndex, Color);
            }
            ReachedBottom = true;
        }

        // ============================================
        // Free Play 模式（保留）
        // ============================================

        public void StartGrowing(double canvasHeight)
        {
            _canvasHeight = canvasHeight;

            _isGrowing = true;
            _isMovingUp = false;

            _growHeight = 0;

            BarBorder.Height = 0;

            Canvas.SetTop(this, canvasHeight);

            Visibility = Visibility.Visible;
        }

        public void UpdateGrowing(double deltaSeconds)
        {
            if (!_isGrowing && !_isMovingUp)
                return;

            const double growSpeed = 300;
            const double moveSpeed = 300;

            if (_isGrowing)
            {
                _growHeight += growSpeed * deltaSeconds;

                BarBorder.Height = _growHeight;

                Canvas.SetTop(this,
                    _canvasHeight - _growHeight);
            }
            else if (_isMovingUp)
            {
                double top = Canvas.GetTop(this);

                top -= moveSpeed * deltaSeconds;

                Canvas.SetTop(this, top);

                if (top + BarBorder.Height < 0)
                {
                    Deactivate();
                }
            }
        }

        public void Release()
        {
            _isGrowing = false;
            _isMovingUp = true;
        }

        // ============================================
        // 外观
        // ============================================

        public void Reset()
        {
            Visibility = Visibility.Collapsed;

            Opacity = 1.0;

            IsActive = false;

            _isGrowing = false;
            _isMovingUp = false;
        }

        public void SetColor(Color color)
        {
            _gradientBrush.GradientStops.Clear();

            _gradientBrush.GradientStops.Add(
                new GradientStop(color, 0));

            _gradientBrush.GradientStops.Add(
                new GradientStop(color, 0.3));

            byte r = (byte)(color.R * 0.5);
            byte g = (byte)(color.G * 0.5);
            byte b = (byte)(color.B * 0.5);

            _gradientBrush.GradientStops.Add(
                new GradientStop(
                    Color.FromRgb(r, g, b), 0.6));

            _gradientBrush.GradientStops.Add(
                new GradientStop(
                    Color.FromArgb(0, r, g, b), 1));

            _shadowEffect.Color = color;

            Color = color;
        }
    }
}