using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using MaterialDesignThemes.Wpf;
using Windows.Media.Control;

using Application = System.Windows.Application;
using Control = System.Windows.Controls.Control;
using Point = System.Windows.Point;
using Color = System.Windows.Media.Color;
using TextBlock = System.Windows.Controls.TextBlock;

namespace MusicWidget
{
    public partial class MainWindow : Window
    {
        private GlobalSystemMediaTransportControlsSessionManager? _sessionManager;
        private GlobalSystemMediaTransportControlsSession? _currentSession;
        private System.Windows.Forms.NotifyIcon? _notifyIcon;
        private SettingsWindow? _settingsWindow;

        private int _slideDirection = -1;
        private bool _isThemeChanging = false;
        private int _updateNonce = 0;

        public (Color Background, Color SecondaryContainer, Color PrimaryContainer, Color TextPrimary, Color TextSecondary) CurrentPalette { get; private set; } =
            (Color.FromRgb(15, 15, 15), Color.FromRgb(45, 45, 45), Color.FromRgb(80, 80, 80), Colors.White, Color.FromArgb(178, 255, 255, 255));

        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int GWL_EXSTYLE = -20;
        [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hwnd, int index);
        [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        public MainWindow()
        {
            AppConfig.Load();
            AppConfig.UpdateGlobalThemeColor();

            InitializeComponent();
            SetupTrayIcon();
            InitSMTC();

            SetupParallaxAndSpotlight(CoverContainer, CoverBgLayer, CoverImage, CoverSpotlight);
            SetupParallaxAndSpotlight(PrevContainer, PrevBgLayer, PrevIcon3DLayer, PrevSpotlight);
            SetupParallaxAndSpotlight(PlayContainer, PlayBgLayer, PlayIcon3DLayer, PlaySpotlight);
            SetupParallaxAndSpotlight(NextContainer, NextBgLayer, NextIcon3DLayer, NextSpotlight);
        }

        protected override void OnSourceInitialized(EventArgs e) { base.OnSourceInitialized(e); ApplyConfig(); }

        public void ApplyConfig()
        {
            this.Topmost = AppConfig.Current.IsTopmost;
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd != IntPtr.Zero)
            {
                int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                if (AppConfig.Current.IsClickThrough) SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
                else SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle & ~WS_EX_TRANSPARENT);
            }
        }

        public void ForceRefreshColors() { _isThemeChanging = true; RefreshMediaInfo(); }

        public void ToggleBgFade()
        {
            bool enable = AppConfig.Current.EnableBgFade;
            bool isLight = AppConfig.Current.IsLightMode;

            // 【核心调整】浅色模式大幅提升透明度，防止白底吃掉光效
            if (enable) BlurBgImage.Opacity = isLight ? 0.75 : 0.45;

            double targetRadius = enable ? 1.5 : 0.0;

            var anim = new DoubleAnimation(targetRadius, TimeSpan.FromMilliseconds(750)) { EasingFunction = new CircleEase { EasingMode = EasingMode.EaseOut } };
            if (!enable) anim.Completed += (s, e) => BlurBgImage.Opacity = 0;

            BgFadeMask.BeginAnimation(RadialGradientBrush.RadiusXProperty, anim);
            BgFadeMask.BeginAnimation(RadialGradientBrush.RadiusYProperty, anim);
        }

        private void SetupTrayIcon()
        {
            _notifyIcon = new System.Windows.Forms.NotifyIcon();
            string exePath = Environment.ProcessPath ?? string.Empty;
            if (!string.IsNullOrEmpty(exePath)) _notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(exePath);
            _notifyIcon.Visible = true;
            _notifyIcon.Text = "Music Widget";

            var menu = new System.Windows.Forms.ContextMenuStrip();
            var settingsItem = new System.Windows.Forms.ToolStripMenuItem("⚙️ 设置 (Settings)");
            settingsItem.Click += (s, e) => OpenSettings();
            var unlockItem = new System.Windows.Forms.ToolStripMenuItem("🔓 解除鼠标穿透 (Unlock Click-Through)");
            unlockItem.Click += (s, e) => { AppConfig.Current.IsClickThrough = false; AppConfig.Save(); ApplyConfig(); _settingsWindow?.SyncFromConfig(); };
            var exitItem = new System.Windows.Forms.ToolStripMenuItem("❌ 退出 (Exit)");
            exitItem.Click += (s, e) => Application.Current.Shutdown();

            menu.Items.Add(settingsItem);
            menu.Items.Add(unlockItem);
            menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
            menu.Items.Add(exitItem);

            _notifyIcon.ContextMenuStrip = menu;
            _notifyIcon.DoubleClick += (s, e) => OpenSettings();
        }

        protected override void OnClosed(EventArgs e) { if (_notifyIcon != null) { _notifyIcon.Visible = false; _notifyIcon.Dispose(); } _settingsWindow?.Close(); base.OnClosed(e); }
        private void MenuSettings_Click(object sender, RoutedEventArgs e) => OpenSettings();
        private void MenuExit_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

        private void OpenSettings()
        {
            if (_settingsWindow != null) { if (_settingsWindow.WindowState == WindowState.Minimized) _settingsWindow.WindowState = WindowState.Normal; _settingsWindow.Activate(); return; }
            _settingsWindow = new SettingsWindow();
            _settingsWindow.Closed += (s, e) => _settingsWindow = null;
            _settingsWindow.Show();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.ButtonState == MouseButtonState.Pressed) this.DragMove(); }
        private async void InitSMTC() { _sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync(); _sessionManager.CurrentSessionChanged += (s, e) => UpdateSession(); UpdateSession(); }
        private void UpdateSession() { if (_currentSession != null) { _currentSession.MediaPropertiesChanged -= Session_MediaPropertiesChanged; _currentSession.PlaybackInfoChanged -= Session_PlaybackInfoChanged; } _currentSession = _sessionManager?.GetCurrentSession(); if (_currentSession != null) { _currentSession.MediaPropertiesChanged += Session_MediaPropertiesChanged; _currentSession.PlaybackInfoChanged += Session_PlaybackInfoChanged; RefreshMediaInfo(); } }

        private void Session_PlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
        {
            Dispatcher.Invoke(() => {
                if (_currentSession == null) return;
                var info = _currentSession.GetPlaybackInfo();
                var newKind = info.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing ? PackIconKind.Pause : PackIconKind.Play;

                if (PlayPauseIcon.Kind != newKind)
                {
                    var fadeOut = new DoubleAnimation(0, TimeSpan.FromMilliseconds(120));
                    fadeOut.Completed += (s2, e2) => { PlayPauseIcon.Kind = newKind; var fadeIn = new DoubleAnimation(1, TimeSpan.FromMilliseconds(120)); PlayPauseIcon.BeginAnimation(UIElement.OpacityProperty, fadeIn); };
                    PlayPauseIcon.BeginAnimation(UIElement.OpacityProperty, fadeOut);
                }
            });
        }

        private void Session_MediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args) => RefreshMediaInfo();

        private async void RefreshMediaInfo()
        {
            int currentNonce = Interlocked.Increment(ref _updateNonce);
            await Task.Delay(150);
            if (currentNonce != _updateNonce) return;

            if (_currentSession == null) return;

            var props = await _currentSession.TryGetMediaPropertiesAsync();
            MemoryStream? imageStream = null;
            if (props.Thumbnail != null) { try { using var streamRef = await props.Thumbnail.OpenReadAsync(); using var stream = streamRef.AsStream(); imageStream = new MemoryStream(); await stream.CopyToAsync(imageStream); imageStream.Position = 0; } catch { imageStream = null; } }

            string displayTitle = props.Title ?? "未知曲目";
            string artist = props.Artist ?? "";
            string albumArtist = props.AlbumArtist ?? "";
            string displayArtist = artist;

            if (!string.IsNullOrWhiteSpace(albumArtist) && albumArtist != artist) { if (albumArtist.Contains(artist) || albumArtist.Contains("/") || albumArtist.Contains(",")) displayArtist = albumArtist; else if (!artist.Contains(albumArtist)) displayArtist = $"{artist} / {albumArtist}"; }
            if (string.IsNullOrWhiteSpace(displayArtist)) displayArtist = "未知艺术家";

            await Dispatcher.InvokeAsync(async () => {
                BitmapImage? bitmap = null;
                if (imageStream != null) { bitmap = new BitmapImage(); bitmap.BeginInit(); bitmap.StreamSource = imageStream; bitmap.CacheOption = BitmapCacheOption.OnLoad; bitmap.DecodePixelWidth = 300; bitmap.EndInit(); bitmap.Freeze(); }

                var palette = ThemeEngine.ExtractPalette(bitmap, AppConfig.Current.IsLightMode);

                if (!(TrackInfoContainer.RenderTransform is TranslateTransform)) TrackInfoContainer.RenderTransform = new TranslateTransform();
                var translate = (TranslateTransform)TrackInfoContainer.RenderTransform;

                // 【核心调整】动态设定目标发散浓度
                bool isLight = AppConfig.Current.IsLightMode;
                double targetBlurOpacity = AppConfig.Current.EnableBgFade ? (isLight ? 0.75 : 0.45) : 0.0;
                double targetMaskRadius = AppConfig.Current.EnableBgFade ? 1.5 : 0.0;

                BgFadeMask.BeginAnimation(RadialGradientBrush.RadiusXProperty, null);
                BgFadeMask.BeginAnimation(RadialGradientBrush.RadiusYProperty, null);
                BgFadeMask.RadiusX = targetMaskRadius;
                BgFadeMask.RadiusY = targetMaskRadius;

                if (_isThemeChanging)
                {
                    var fadeOut = new DoubleAnimation(0, TimeSpan.FromMilliseconds(150));
                    TrackInfoContainer.BeginAnimation(UIElement.OpacityProperty, fadeOut);
                    BlurBgImage.BeginAnimation(UIElement.OpacityProperty, fadeOut);

                    await Task.Delay(150);

                    TitleText.Text = displayTitle; ArtistText.Text = displayArtist; CoverImage.Source = bitmap; BlurBgImage.Source = bitmap;
                    ApplyColorsAndLaunchColorEngine(palette);

                    var fadeIn = new DoubleAnimation(1, TimeSpan.FromMilliseconds(200));
                    var blurFadeIn = new DoubleAnimation(targetBlurOpacity, TimeSpan.FromMilliseconds(200));
                    TrackInfoContainer.BeginAnimation(UIElement.OpacityProperty, fadeIn);
                    BlurBgImage.BeginAnimation(UIElement.OpacityProperty, blurFadeIn);

                    _isThemeChanging = false;
                }
                else
                {
                    double slideDistance = 35 * _slideDirection;
                    var slideOut = new DoubleAnimation(slideDistance, TimeSpan.FromMilliseconds(120)) { EasingFunction = new SineEase { EasingMode = EasingMode.EaseIn } };
                    var fadeOut = new DoubleAnimation(0, TimeSpan.FromMilliseconds(120));

                    translate.BeginAnimation(TranslateTransform.XProperty, slideOut); TrackInfoContainer.BeginAnimation(UIElement.OpacityProperty, fadeOut); BlurBgImage.BeginAnimation(UIElement.OpacityProperty, fadeOut);
                    await Task.Delay(130);

                    TitleText.Text = displayTitle; ArtistText.Text = displayArtist; CoverImage.Source = bitmap; BlurBgImage.Source = bitmap;
                    var info = _currentSession.GetPlaybackInfo(); PlayPauseIcon.Kind = info.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing ? PackIconKind.Pause : PackIconKind.Play;
                    ApplyColorsAndLaunchColorEngine(palette);

                    translate.BeginAnimation(TranslateTransform.XProperty, null); translate.X = -slideDistance;

                    var slideIn = new DoubleAnimation(0, TimeSpan.FromMilliseconds(350)) { EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut } };
                    var fadeIn = new DoubleAnimation(1, TimeSpan.FromMilliseconds(250));
                    var blurFadeIn = new DoubleAnimation(targetBlurOpacity, TimeSpan.FromMilliseconds(350));

                    translate.BeginAnimation(TranslateTransform.XProperty, slideIn); TrackInfoContainer.BeginAnimation(UIElement.OpacityProperty, fadeIn); BlurBgImage.BeginAnimation(UIElement.OpacityProperty, blurFadeIn);
                }

                _slideDirection = -1; imageStream?.Dispose();
            });
        }

        private void ApplyColorsAndLaunchColorEngine((Color Background, Color SecondaryContainer, Color PrimaryContainer, Color TextPrimary, Color TextSecondary) palette)
        {
            CurrentPalette = palette;
            AppConfig.UpdateGlobalThemeColor(palette.PrimaryContainer);

            CardInnerBorder.AnimateColorTo(Control.BackgroundProperty, palette.Background);
            BtnPrev.AnimateColorTo(Control.BackgroundProperty, palette.SecondaryContainer);
            BtnNext.AnimateColorTo(Control.BackgroundProperty, palette.SecondaryContainer);
            BtnPlayPause.AnimateColorTo(Control.BackgroundProperty, palette.PrimaryContainer);

            TitleText.AnimateColorTo(TextBlock.ForegroundProperty, palette.TextPrimary);
            ArtistText.AnimateColorTo(TextBlock.ForegroundProperty, palette.TextSecondary);

            PrevIcon.AnimateColorTo(Control.ForegroundProperty, palette.TextPrimary);
            NextIcon.AnimateColorTo(Control.ForegroundProperty, palette.TextPrimary);
            PlayPauseIcon.AnimateColorTo(Control.ForegroundProperty, palette.Background);

            _settingsWindow?.ApplyCurrentTheme();
        }

        private void SetupParallaxAndSpotlight(FrameworkElement container, FrameworkElement backgroundLayer, FrameworkElement foregroundLayer, System.Windows.Shapes.Rectangle spotlight)
        {
            var bgTransformGroup = new TransformGroup(); var bgScale = new ScaleTransform(1, 1); var bgTranslate = new TranslateTransform(0, 0); bgTransformGroup.Children.Add(bgScale); bgTransformGroup.Children.Add(bgTranslate); backgroundLayer.RenderTransformOrigin = new Point(0.5, 0.5); backgroundLayer.RenderTransform = bgTransformGroup;
            var fgTransformGroup = new TransformGroup(); var fgScale = new ScaleTransform(1, 1); var fgTranslate = new TranslateTransform(0, 0); fgTransformGroup.Children.Add(fgScale); fgTransformGroup.Children.Add(fgTranslate); foregroundLayer.RenderTransformOrigin = new Point(0.5, 0.5); foregroundLayer.RenderTransform = fgTransformGroup;
            if (spotlight.Fill is RadialGradientBrush brush)
            {
                container.PreviewMouseLeftButtonDown += (s, e) => { var shrink = new DoubleAnimation(0.92, TimeSpan.FromMilliseconds(100)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } }; bgScale.BeginAnimation(ScaleTransform.ScaleXProperty, shrink); bgScale.BeginAnimation(ScaleTransform.ScaleYProperty, shrink); fgScale.BeginAnimation(ScaleTransform.ScaleXProperty, shrink); fgScale.BeginAnimation(ScaleTransform.ScaleYProperty, shrink); };
                container.PreviewMouseLeftButtonUp += (s, e) => { var bounce = new DoubleAnimation(1.05, TimeSpan.FromMilliseconds(400)) { EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 2.5 } }; var fgBounce = new DoubleAnimation(1.15, TimeSpan.FromMilliseconds(400)) { EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 2.5 } }; bgScale.BeginAnimation(ScaleTransform.ScaleXProperty, bounce); bgScale.BeginAnimation(ScaleTransform.ScaleYProperty, bounce); fgScale.BeginAnimation(ScaleTransform.ScaleXProperty, fgBounce); fgScale.BeginAnimation(ScaleTransform.ScaleYProperty, fgBounce); };
                container.MouseEnter += (s, e) => { bgTranslate.BeginAnimation(TranslateTransform.XProperty, null); bgTranslate.BeginAnimation(TranslateTransform.YProperty, null); fgTranslate.BeginAnimation(TranslateTransform.XProperty, null); fgTranslate.BeginAnimation(TranslateTransform.YProperty, null); bgScale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(1.05, TimeSpan.FromMilliseconds(150))); bgScale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(1.05, TimeSpan.FromMilliseconds(150))); fgScale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(1.15, TimeSpan.FromMilliseconds(150))); fgScale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(1.15, TimeSpan.FromMilliseconds(150))); spotlight.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(150))); };
                container.PreviewMouseMove += (s, e) => { var pos = e.GetPosition(container); var w = container.ActualWidth; var h = container.ActualHeight; double offsetX = (pos.X - w / 2) / (w / 2); double offsetY = (pos.Y - h / 2) / (h / 2); brush.Center = new Point(pos.X / w, pos.Y / h); brush.GradientOrigin = new Point(pos.X / w, pos.Y / h); bgTranslate.X = offsetX * 4; bgTranslate.Y = offsetY * 4; fgTranslate.X = offsetX * 12; fgTranslate.Y = offsetY * 12; };
                container.MouseLeave += (s, e) => { var smoothReset = new DoubleAnimation(0, TimeSpan.FromMilliseconds(250)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } }; var scaleReset = new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(250)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } }; bgScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleReset); bgScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleReset); bgTranslate.BeginAnimation(TranslateTransform.XProperty, smoothReset); bgTranslate.BeginAnimation(TranslateTransform.YProperty, smoothReset); fgScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleReset); fgScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleReset); fgTranslate.BeginAnimation(TranslateTransform.XProperty, smoothReset); fgTranslate.BeginAnimation(TranslateTransform.YProperty, smoothReset); spotlight.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(250))); };
            }
        }

        private async void BtnPrev_Click(object sender, RoutedEventArgs e) { _slideDirection = 1; if (PrevIcon.RenderTransform is TransformGroup tg && tg.Children[1] is TranslateTransform trans) { var bounce = new DoubleAnimation(-20, 0, TimeSpan.FromMilliseconds(500)) { EasingFunction = new ElasticEase { EasingMode = EasingMode.EaseOut, Oscillations = 1, Springiness = 4 } }; trans.BeginAnimation(TranslateTransform.XProperty, bounce); } await _currentSession?.TrySkipPreviousAsync(); }
        private async void BtnNext_Click(object sender, RoutedEventArgs e) { _slideDirection = -1; if (NextIcon.RenderTransform is TransformGroup tg && tg.Children[1] is TranslateTransform trans) { var bounce = new DoubleAnimation(20, 0, TimeSpan.FromMilliseconds(500)) { EasingFunction = new ElasticEase { EasingMode = EasingMode.EaseOut, Oscillations = 1, Springiness = 4 } }; trans.BeginAnimation(TranslateTransform.XProperty, bounce); } await _currentSession?.TrySkipNextAsync(); }
        private async void BtnPlayPause_Click(object sender, RoutedEventArgs e) => await _currentSession?.TryTogglePlayPauseAsync();
    }
}