using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Application = System.Windows.Application;

using Color = System.Windows.Media.Color;
using Colors = System.Windows.Media.Colors;
using Control = System.Windows.Controls.Control;
using Border = System.Windows.Controls.Border;
using TextBlock = System.Windows.Controls.TextBlock;

namespace MusicWidget
{
    public partial class SettingsWindow : Window
    {
        private bool _isLoaded = false;
        public SettingsWindow()
        {
            InitializeComponent();
            SyncFromConfig();
            this.Topmost = AppConfig.Current.IsTopmost;
            ApplyCurrentTheme();
        }

        public void SyncFromConfig()
        {
            _isLoaded = false;
            ChkTopmost.IsChecked = AppConfig.Current.IsTopmost;
            ChkClickThrough.IsChecked = AppConfig.Current.IsClickThrough;
            ChkLightMode.IsChecked = AppConfig.Current.IsLightMode;
            ChkBgFade.IsChecked = AppConfig.Current.EnableBgFade;
            _isLoaded = true;
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.ButtonState == MouseButtonState.Pressed) this.DragMove(); }
        private void BtnClose_Click(object sender, RoutedEventArgs e) => this.Close();

        public void ApplyCurrentTheme()
        {
            if (Application.Current.MainWindow is MainWindow mainWin)
            {
                var palette = mainWin.CurrentPalette;

                RootBorder.AnimateColorTo(Border.BackgroundProperty, palette.Background);
                SettingsCard.AnimateColorTo(Control.BackgroundProperty, palette.SecondaryContainer);
                HeaderTxt.AnimateColorTo(TextBlock.ForegroundProperty, palette.TextPrimary);
                CloseIcon.AnimateColorTo(Control.ForegroundProperty, palette.TextPrimary);
                SettingsCard.Foreground = new SolidColorBrush(palette.TextPrimary);
            }
        }

        private void Topmost_Changed(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;
            AppConfig.Current.IsTopmost = ChkTopmost.IsChecked == true;
            AppConfig.Save();
            this.Topmost = AppConfig.Current.IsTopmost;
            if (Application.Current.MainWindow is MainWindow mainWin) mainWin.ApplyConfig();
        }

        private void ClickThrough_Changed(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;
            AppConfig.Current.IsClickThrough = ChkClickThrough.IsChecked == true;
            AppConfig.Save();
            if (Application.Current.MainWindow is MainWindow mainWin) mainWin.ApplyConfig();
        }

        private void LightMode_Changed(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;
            AppConfig.Current.IsLightMode = ChkLightMode.IsChecked == true;
            AppConfig.Save();
            AppConfig.UpdateGlobalThemeColor();
            if (Application.Current.MainWindow is MainWindow mainWin) mainWin.ForceRefreshColors();
        }

        private void BgFade_Changed(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;
            AppConfig.Current.EnableBgFade = ChkBgFade.IsChecked == true;
            AppConfig.Save();
            if (Application.Current.MainWindow is MainWindow mainWin) mainWin.ToggleBgFade();
        }
    }
}