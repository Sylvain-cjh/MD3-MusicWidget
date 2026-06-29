using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Application = System.Windows.Application;

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

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed) this.DragMove();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => this.Close();

        public void ApplyCurrentTheme()
        {
            if (Application.Current.MainWindow is MainWindow mainWin)
            {
                var palette = mainWin.CurrentPalette;
                RootBorder.Background = new SolidColorBrush(palette.Background);
                HeaderTxt.Foreground = new SolidColorBrush(palette.TextPrimary);
                CloseIcon.Foreground = new SolidColorBrush(palette.TextPrimary);
                SettingsCard.Background = new SolidColorBrush(palette.SecondaryContainer);
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