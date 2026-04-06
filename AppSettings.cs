using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace HLL_ATassistant
{
    public class AppSettings : INotifyPropertyChanged
    {
        // 用户配置存储目录：%LocalAppData%\HLL_ATassistant
        private static readonly string ConfigDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HLL_ATassistant");
        // 配置文件名称：hotkeys.json（兼容旧版）
        private static readonly string ConfigPath = Path.Combine(ConfigDir, "hotkeys.json");

        private static AppSettings _instance;
        public static AppSettings Instance => _instance ??= Load();

        // 热键属性
        public int B1Key { get; set; } = (int)System.Windows.Forms.Keys.F1;
        public int B1Modifiers { get; set; }
        public int B2Key { get; set; } = (int)System.Windows.Forms.Keys.F2;
        public int B2Modifiers { get; set; }
        public int ToggleVisibleKey { get; set; } = (int)System.Windows.Forms.Keys.F3;
        public int ToggleVisibleModifiers { get; set; }
        public int ToggleOverlayKey { get; set; } = (int)System.Windows.Forms.Keys.F4;
        public int ToggleOverlayModifiers { get; set; }
        public int StartMultiKey { get; set; } = (int)System.Windows.Forms.Keys.F5;
        public int StartMultiModifiers { get; set; }
        public int SetBaselineKey { get; set; }
        public int SetBaselineModifiers { get; set; }
        public int CancelBaselineKey { get; set; }
        public int CancelBaselineModifiers { get; set; }
        public bool SameKeyForBaseline { get; set; }

        // 显示选项（带通知）
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

        // 其他全局设置
        public double Sensitivity { get; set; } = 1.0;
        public double MaxRange { get; set; } = 1000;
        public bool UseFixedMaxRange { get; set; } = true;

        // 窗体位置存储
        public System.Drawing.Point? MainFormLocation { get; set; }
        public System.Drawing.Point? SettingFormLocation { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void Set<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            Save();
        }

        /// <summary>
        /// 从嵌入资源加载默认配置（出厂设置）
        /// </summary>
        private static AppSettings LoadDefaultFromResource()
        {
            var assembly = Assembly.GetExecutingAssembly();
            // 资源名称：项目默认命名空间 + 文件名（例如 "HLL_ATassistant.default_settings.json"）
            string resourceName = "HLL_ATassistant.default_settings.json";
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream != null)
                {
                    using (var reader = new StreamReader(stream))
                    {
                        string json = reader.ReadToEnd();
                        try
                        {
                            return JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
                        }
                        catch
                        {
                            // 资源文件损坏，返回全新实例
                            return new AppSettings();
                        }
                    }
                }
            }
            // 没有嵌入资源，返回全新实例（默认值）
            return new AppSettings();
        }

        public static AppSettings Load()
        {
            // 如果用户配置文件存在，则加载；否则从嵌入资源加载默认配置
            if (File.Exists(ConfigPath))
            {
                try
                {
                    string json = File.ReadAllText(ConfigPath);
                    return JsonConvert.DeserializeObject<AppSettings>(json) ?? LoadDefaultFromResource();
                }
                catch
                {
                    // 文件损坏，使用默认配置
                    return LoadDefaultFromResource();
                }
            }
            return LoadDefaultFromResource();
        }

        public void Save()
        {
            // 确保目录存在
            Directory.CreateDirectory(ConfigDir);
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(ConfigPath, json);
        }
    }
}