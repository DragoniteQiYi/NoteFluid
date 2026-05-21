using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NoteFluid.Core.Views
{
    public partial class WaterfallBar : UserControl
    {
        private DateTime _lastFrameTime;
        private bool _isAnimating = false;
        private bool _isGrowing = false;
        private bool _isMovingUp = false;

        // 移动和增长参数
        private const double MOVE_UP_SPEED = 200; // 向上移动速度（像素/秒）
        private const double GROW_SPEED = 200; // 向上增长速度（像素/秒）
        private double _currentTop = 0;
        private double _currentHeight = 0;
        private double _canvasHeight = 0;

        public WaterfallBar()
        {
            InitializeComponent();
            Visibility = Visibility.Collapsed;
            VerticalAlignment = VerticalAlignment.Top;
        }

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
    }
}