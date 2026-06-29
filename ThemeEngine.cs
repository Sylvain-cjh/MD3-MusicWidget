using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Color = System.Windows.Media.Color;
using Colors = System.Windows.Media.Colors;

namespace MusicWidget
{
    public static class ThemeEngine
    {
        private static byte Clamp(double value) => (byte)Math.Max(0, Math.Min(255, value));

        public static (Color Background, Color SecondaryContainer, Color PrimaryContainer, Color TextPrimary, Color TextSecondary) ExtractPalette(BitmapSource? bitmap, bool isLight)
        {
            if (bitmap == null)
            {
                if (isLight)
                    return (Color.FromRgb(240, 244, 248), Color.FromRgb(200, 210, 220), Color.FromRgb(120, 140, 160), Color.FromRgb(30, 40, 50), Color.FromArgb((byte)178, 30, 40, 50));
                else
                    return (Color.FromRgb(15, 20, 25), Color.FromRgb(45, 55, 65), Color.FromRgb(80, 100, 120), Colors.White, Color.FromArgb((byte)178, 255, 255, 255));
            }

            try
            {
                var renderTarget = new RenderTargetBitmap(1, 1, 96, 96, PixelFormats.Pbgra32);
                var drawingVisual = new DrawingVisual();
                using (var drawingContext = drawingVisual.RenderOpen())
                {
                    drawingContext.DrawImage(bitmap, new Rect(0, 0, 1, 1));
                }
                renderTarget.Render(drawingVisual);

                var pixels = new byte[4];
                renderTarget.CopyPixels(pixels, 4, 0);
                byte b = pixels[0], g = pixels[1], r = pixels[2];

                if (isLight)
                {
                    byte bgR = Clamp(r * 0.05 + 242), bgG = Clamp(g * 0.05 + 242), bgB = Clamp(b * 0.05 + 242);
                    byte secR = Clamp(r * 0.25 + 175), secG = Clamp(g * 0.25 + 175), secB = Clamp(b * 0.25 + 175);
                    byte priR = Clamp(r * 0.6 + 60), priG = Clamp(g * 0.6 + 60), priB = Clamp(b * 0.6 + 60);
                    byte textR = Clamp(r * 0.2 + 20), textG = Clamp(g * 0.2 + 20), textB = Clamp(b * 0.2 + 20);

                    return (Color.FromRgb(bgR, bgG, bgB), Color.FromRgb(secR, secG, secB), Color.FromRgb(priR, priG, priB), Color.FromRgb(textR, textG, textB), Color.FromArgb((byte)190, textR, textG, textB));
                }
                else
                {
                    byte bgR = Clamp(r * 0.1 + 15), bgG = Clamp(g * 0.1 + 15), bgB = Clamp(b * 0.1 + 15);
                    byte secR = Clamp(r * 0.2 + 45), secG = Clamp(g * 0.2 + 45), secB = Clamp(b * 0.2 + 45);
                    byte priR = Clamp(r * 0.4 + 75), priG = Clamp(g * 0.4 + 75), priB = Clamp(b * 0.4 + 75);
                    byte textR = Clamp(r * 0.05 + 230), textG = Clamp(g * 0.05 + 230), textB = Clamp(b * 0.05 + 230);

                    return (Color.FromRgb(bgR, bgG, bgB), Color.FromRgb(secR, secG, secB), Color.FromRgb(priR, priG, priB), Color.FromRgb(textR, textG, textB), Color.FromArgb((byte)190, textR, textG, textB));
                }
            }
            catch
            {
                return isLight
                    ? (Color.FromRgb(240, 244, 248), Color.FromRgb(200, 210, 220), Color.FromRgb(120, 140, 160), Color.FromRgb(30, 40, 50), Color.FromArgb((byte)178, 30, 40, 50))
                    : (Color.FromRgb(15, 20, 25), Color.FromRgb(45, 55, 65), Color.FromRgb(80, 100, 120), Colors.White, Color.FromArgb((byte)178, 255, 255, 255));
            }
        }
    }
}