using System;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace HLL_ATassistant
{
    public class HotKeySettings
    {
        private const int MOD_ALT = 0x0001;
        private const int MOD_CONTROL = 0x0002;
        private const int MOD_SHIFT = 0x0004;
        private const int MOD_WIN = 0x0008;

        public int B1Key { get; set; } = (int)MouseButtons.Middle;
        public int B1Modifiers { get; set; } = 0;
        public int B2Key { get; set; } = (int)Keys.X;
        public int B2Modifiers { get; set; } = MOD_ALT;
        public int StartMultiKey { get; set; }
        public int StartMultiModifiers { get; set; }

        public int ToggleVisibleKey { get; set; } = (int)Keys.H;
        public int ToggleVisibleModifiers { get; set; } = MOD_CONTROL | MOD_SHIFT;

        public int ToggleOverlayKey { get; set; } = (int)Keys.G;
        public int ToggleOverlayModifiers { get; set; } = MOD_CONTROL | MOD_SHIFT;

        public int ToggleMultiPtKey { get; set; } = (int)Keys.T;
        public int ToggleMultiModifiers { get; set; } = MOD_CONTROL | MOD_SHIFT;

        public int SetBaselineKey { get; set; } = 0;
        public int SetBaselineModifiers { get; set; } = 0;
        public int CancelBaselineKey { get; set; } = 0;
        public int CancelBaselineModifiers { get; set; } = 0;
        public bool SameKeyForBaseline { get; set; } = false;

        // 热键页控制的基础显示项
        public bool ShowDistance { get; set; } = true;   // D
        public bool ShowAngle { get; set; } = true;      // θ
        public bool ShowVx { get; set; } = true;         // vx
        public bool ShowTime { get; set; } = true;       // t

        // 高低差页控制的高级显示项
        public bool ShowL { get; set; } = true;          // L
        public bool ShowX { get; set; } = true;          // X
        public bool ShowBeta { get; set; } = true;       // β
        public bool ShowTPrime { get; set; } = true;     // t'

        private static readonly string ConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HLL_ATassistant", "hotkeys.json");

        public void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
            string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
        }

        public static HotKeySettings Load()
        {
            if (File.Exists(ConfigPath))
            {
                string json = File.ReadAllText(ConfigPath);
                return JsonSerializer.Deserialize<HotKeySettings>(json) ?? new HotKeySettings();
            }
            return new HotKeySettings();
        }
    }
}