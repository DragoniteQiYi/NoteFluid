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

        private DateTime _lastFrameTime;
        private bool _isAnimating = false;
        private bool _isGrowing;
        private bool _isMovingUp;

        private const double MOVE_UP_SPEED = 200; // 向上移动速度（像素/秒）
        private const double GROW_SPEED = 200; // 向上增长速度（像素/秒）
        private double _growHeight;
        private double _canvasHeight;
        private double _currentTop = 0;
        private double _currentHeight = 0;


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

            // 重置状态 - 初始高度为0，位置在画布底部
            _currentHeight = 0;
            _currentTop = canvasHeight; // 底部位置
            BarBorder.Height = 0;
            BarBorder.Opacity = 1.0;

            // 设置初始位置在底部
            Canvas.SetTop(this, _currentTop);
            Visibility = Visibility.Visible;

            // 开始动画循环
            if (!_isAnimating)
            {
                _isAnimating = true;
                _lastFrameTime = DateTime.Now;
                CompositionTarget.Rendering += OnRendering;
            }
        }

        private void OnRendering(object sender, EventArgs e)
        {
            if (!_isAnimating) return;

            DateTime now = DateTime.Now;
            double deltaTime = (now - _lastFrameTime).TotalSeconds;
            _lastFrameTime = now;

            // 限制最大帧间隔
            if (deltaTime > 0.05) deltaTime = 0.05;

            // 如果正在增长，增长高度并向上移动位置以保持底部固定
            if (_isGrowing)
            {
                double heightIncrease = GROW_SPEED * deltaTime;
                _currentHeight += heightIncrease;

                // 限制最大高度
                _currentHeight = Math.Min(_currentHeight, _canvasHeight);
                BarBorder.Height = _currentHeight;

                // 当高度增长时，需要向上移动顶部位置，保持底部在画布底部
                _currentTop = _canvasHeight - _currentHeight;
                Canvas.SetTop(this, _currentTop);
            }

            // 如果正在向上移动（松开按键后），整体向上移动
            if (_isMovingUp)
            {
                _currentTop -= MOVE_UP_SPEED * deltaTime;
                Canvas.SetTop(this, _currentTop);
            }

            // 检查是否完全移出画布（整个控件移出顶部）
            if (_currentTop + _currentHeight < 0)
            {
                // 瀑布条完全移出画布，移除
                StopAndRemove();
            }
        }

        private void StopAndRemove()
        {
            _isAnimating = false;
            _isGrowing = false;
            _isMovingUp = false;
            CompositionTarget.Rendering -= OnRendering;

            if (Parent is Panel parent)
            {
                parent.Children.Remove(this);
            }
        }

        // 外部调用停止（释放按键时）
        public void Release()
        {
            _isGrowing = false;      // 停止增长
            _isMovingUp = true;      // 开始向上移动
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