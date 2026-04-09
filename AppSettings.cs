using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Forms;

namespace HLL_ATassistant
{
    /// <summary>
    /// 应用程序设置管理：热键配置、显示选项、窗体位置、配置文件的加载与保存。
    /// 实现 INotifyPropertyChanged 以便 UI 实时响应。
    /// </summary>
    public class AppSettings : INotifyPropertyChanged
    {
        // 热键修饰符常量（与 Win32 一致）
        public const int MOD_ALT = 0x0001;
        public const int MOD_CONTROL = 0x0002;
        public const int MOD_SHIFT = 0x0004;
        public const int MOD_WIN = 0x0008;

        // 配置文件路径：%LocalAppData%\HLL_ATassistant\settings.json
        private static readonly string ConfigDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HLL_ATassistant");
        private static readonly string ConfigPath = Path.Combine(ConfigDir, "settings.json");

        // 单例实例
        private static AppSettings? _instance;
        public static AppSettings Instance => _instance ??= Load();

        // ========== 热键属性 ==========
        public int B1Key { get; set; } = (int)MouseButtons.Middle;
        public int B1Modifiers { get; set; } = 0;

        public int B2Key { get; set; } = (int)Keys.X;
        public int B2Modifiers { get; set; } = MOD_ALT;

        public int ToggleVisibleKey { get; set; } = (int)Keys.H;
        public int ToggleVisibleModifiers { get; set; } = MOD_CONTROL | MOD_SHIFT;

        public int ToggleOverlayKey { get; set; } = (int)Keys.G;
        public int ToggleOverlayModifiers { get; set; } = MOD_CONTROL | MOD_SHIFT;

        public int StartMultiKey { get; set; } = (int)Keys.T;
        public int StartMultiModifiers { get; set; } = MOD_CONTROL | MOD_SHIFT;

        public int SetBaselineKey { get; set; } = 0;
        public int SetBaselineModifiers { get; set; } = 0;
        public int CancelBaselineKey { get; set; } = 0;
        public int CancelBaselineModifiers { get; set; } = 0;
        public bool SameKeyForBaseline { get; set; } = false;

        public int Pause1Key { get; set; } = (int)Keys.Tab;
        public int Pause1Modifiers { get; set; } = 0;

        public int Pause2Key { get; set; } = (int)Keys.M;
        public int Pause2Modifiers { get; set; } = 0;

        // ========== 显示选项（带通知） ==========
        private bool _showDistance = true;
        private bool _showAngle = true;
        private bool _showVx = true;
        private bool _showTime = true;
        private bool _showL = true;
        private bool _showX = true;
        private bool _showBeta = true;
        private bool _showTPrime = true;

        public bool ShowDistance { get => _showDistance; set => Set(ref _showDistance, value); }
        public bool ShowAngle   { get => _showAngle;   set => Set(ref _showAngle, value); }
        public bool ShowVx      { get => _showVx;      set => Set(ref _showVx, value); }
        public bool ShowTime    { get => _showTime;    set => Set(ref _showTime, value); }
        public bool ShowL       { get => _showL;       set => Set(ref _showL, value); }
        public bool ShowX       { get => _showX;       set => Set(ref _showX, value); }
        public bool ShowBeta    { get => _showBeta;    set => Set(ref _showBeta, value); }
        public bool ShowTPrime  { get => _showTPrime;  set => Set(ref _showTPrime, value); }

        // ========== 窗体位置存储 ==========
        public Point? MainFormLocation { get; set; }
        public Point? SettingFormLocation { get; set; }

        // ========== 其他全局设置 ==========
        public double Sensitivity { get; set; } = 1.0;
        public double SmoothFactor { get; set; } = 0.2;
        public double MaxRange { get; set; } = 1000;
        public bool UseFixedMaxRange { get; set; } = true;

        // ========== INotifyPropertyChanged 实现 ==========
        public event PropertyChangedEventHandler? PropertyChanged;

        private void Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // ========== 加载与保存 ==========
        private static AppSettings LoadDefault()
        {
            // 可扩展：从嵌入资源读取出厂设置，此处返回全新实例
            return new AppSettings();
        }

        public static AppSettings Load()
        {
            if (File.Exists(ConfigPath))
            {
                try
                {
                    string json = File.ReadAllText(ConfigPath);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? LoadDefault();
                }
                catch
                {
                    // 文件损坏，使用默认配置
                    return LoadDefault();
                }
            }
            return LoadDefault();
        }

        public void Save()
        {
            Directory.CreateDirectory(ConfigDir);
            string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
        }
    }
}
