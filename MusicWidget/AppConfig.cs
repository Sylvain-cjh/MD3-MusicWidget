using System.IO;
using System.Text.Json;
using MaterialDesignThemes.Wpf;

namespace MusicWidget
{
    public class AppConfig
    {
        public bool IsTopmost { get; set; } = true;
        public bool IsClickThrough { get; set; } = false;
        public bool IsLightMode { get; set; } = false;
        public bool EnableBgFade { get; set; } = true;

        public static AppConfig Current { get; set; } = new AppConfig();
        private static readonly string ConfigPath = Path.Combine(System.AppContext.BaseDirectory, "config.json");

        public static void Load()
        {
            if (File.Exists(ConfigPath))
            {
                try
                {
                    string json = File.ReadAllText(ConfigPath);
                    Current = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                }
                catch { Current = new AppConfig(); }
            }
        }

        public static void Save()
        {
            string json = JsonSerializer.Serialize(Current);
            File.WriteAllText(ConfigPath, json);
        }

        public static void UpdateGlobalThemeColor(System.Windows.Media.Color? primaryColor = null)
        {
            var paletteHelper = new PaletteHelper();
            var theme = paletteHelper.GetTheme();
            theme.SetBaseTheme(Current.IsLightMode ? BaseTheme.Light : BaseTheme.Dark);
            if (primaryColor.HasValue) theme.SetPrimaryColor(primaryColor.Value);
            paletteHelper.SetTheme(theme);
        }
    }
}