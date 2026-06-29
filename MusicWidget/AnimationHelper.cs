using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Control = System.Windows.Controls.Control;
using TextBlock = System.Windows.Controls.TextBlock;

using Color = System.Windows.Media.Color;
using Brush = System.Windows.Media.Brush;
using Colors = System.Windows.Media.Colors;

namespace MusicWidget
{
    public static class AnimationHelper
    {
        public static void AnimateColorTo(this UIElement element, DependencyProperty property, Color targetColor, double durationMs = 350)
        {
            Color startColor = Colors.Transparent;
            Brush? currentBrush = null;

            if (property == Control.BackgroundProperty) currentBrush = (Brush)element.GetValue(Control.BackgroundProperty);
            else if (property == Control.ForegroundProperty) currentBrush = (Brush)element.GetValue(Control.ForegroundProperty);
            else if (property == TextBlock.ForegroundProperty) currentBrush = (Brush)element.GetValue(TextBlock.ForegroundProperty);

            if (currentBrush is SolidColorBrush scb) startColor = scb.Color;
            else startColor = targetColor;

            SolidColorBrush animatedBrush = new SolidColorBrush(startColor);
            element.SetValue(property, animatedBrush);

            ColorAnimation animation = new ColorAnimation
            {
                To = targetColor,
                Duration = TimeSpan.FromMilliseconds(durationMs),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            animatedBrush.BeginAnimation(SolidColorBrush.ColorProperty, animation);
        }
    }
}