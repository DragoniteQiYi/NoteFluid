using MaterialDesignThemes.Wpf;
using System.Windows.Media;

namespace NoteFluid.Core.Services
{
    public class ThemeService
    {
        private readonly PaletteHelper _paletteHelper = new();

        // 获取当前主题
        public Theme GetCurrentTheme()
        {
            return _paletteHelper.GetTheme();
        }

        // 切换基础主题（Light/Dark）
        public void ToggleBaseTheme()
        {
            Theme theme = _paletteHelper.GetTheme();
            var newBaseTheme = theme.GetBaseTheme() == BaseTheme.Dark ?
                              BaseTheme.Light : BaseTheme.Dark;
            theme.SetBaseTheme(newBaseTheme);
            _paletteHelper.SetTheme(theme);
        }

        // 设置基础主题
        public void SetBaseTheme(BaseTheme baseTheme)
        {
            Theme theme = _paletteHelper.GetTheme();
            theme.SetBaseTheme(baseTheme);
            _paletteHelper.SetTheme(theme);
        }

        // 更改主题颜色 - 修复版本
        public void ChangePrimaryColor(Color color)
        {
            // 获取当前主题和基础主题
            Theme theme = _paletteHelper.GetTheme();
            BaseTheme baseTheme = theme.GetBaseTheme();

            // 创建新的主题实例
            Theme newTheme = new Theme();
            newTheme.SetBaseTheme(baseTheme);
            newTheme.SetPrimaryColor(color);

            // 应用新主题
            _paletteHelper.SetTheme(newTheme);
        }

        public void ChangeSecondaryColor(Color color)
        {
            // 获取当前主题和基础主题
            Theme theme = _paletteHelper.GetTheme();
            BaseTheme baseTheme = theme.GetBaseTheme();

            // 创建新的主题实例
            Theme newTheme = new Theme();
            newTheme.SetBaseTheme(baseTheme);

            newTheme.SetSecondaryColor(color);

            // 应用新主题
            _paletteHelper.SetTheme(newTheme);
        }

        // 获取是否为深色主题
        public bool IsDarkTheme()
        {
            var theme = _paletteHelper.GetTheme();
            return theme.GetBaseTheme() == BaseTheme.Dark;
        }
    }

    public enum AppTheme
    {
        Light,
        Dark
    }
}