using System.Windows.Media;

namespace NoteFluid.Core.Models
{
    public class ColorItem
    {
        public string? Name { get; set; }
        public string? ColorHex { get; set; }
        public Color Color => 
            (Color)ColorConverter.ConvertFromString(ColorHex);
        public SolidColorBrush ColorBrush => new(
            (Color)ColorConverter.ConvertFromString(ColorHex));
    }
}
