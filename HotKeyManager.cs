using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace HLL_ATassistant
{
    public class HotKeyManager : IDisposable
    {
        private IntPtr _hwnd;
        private AppSettings _settings;
        private const int WM_HOTKEY = 0x0312;

        // 注册结果回调（可选），参数：热键名称，是否成功
        public event Action<string, bool> HotKeyRegistered;

        public event Action B1Pressed;
        public event Action B2Pressed;
        public event Action ToggleVisiblePressed;
        public event Action ToggleOverlayPressed;
        public event Action StartMultiPressed;
        public event Action SetBaselinePressed;
        public event Action CancelBaselinePressed;

        public HotKeyManager(IntPtr hwnd, AppSettings settings)
        {
            _hwnd = hwnd;
            _settings = settings;
            RegisterAll();
        }

        /// <summary>
        /// 重新注册所有热键（设置变更后调用）
        /// </summary>
        public void ReRegister()
        {
            RegisterAll();
        }

        private void RegisterAll()
        {
            UnregisterAll();

            // 注册各个热键，并捕获结果
            RegisterWithFeedback("B1", HotKeyId.B1, _settings.B1Key, _settings.B1Modifiers);
            RegisterWithFeedback("B2", HotKeyId.B2, _settings.B2Key, _settings.B2Modifiers);
            RegisterWithFeedback("ToggleVisible", HotKeyId.ToggleVisible, _settings.ToggleVisibleKey, _settings.ToggleVisibleModifiers);
            RegisterWithFeedback("ToggleOverlay", HotKeyId.ToggleOverlay, _settings.ToggleOverlayKey, _settings.ToggleOverlayModifiers);
            RegisterWithFeedback("StartMulti", HotKeyId.StartMulti, _settings.StartMultiKey, _settings.StartMultiModifiers);
            RegisterWithFeedback("SetBaseline", HotKeyId.SetBaseline, _settings.SetBaselineKey, _settings.SetBaselineModifiers);
            RegisterWithFeedback("CancelBaseline", HotKeyId.CancelBaseline, _settings.CancelBaselineKey, _settings.CancelBaselineModifiers);
        }

        private void RegisterWithFeedback(string name, HotKeyId id, int key, int modifiers)
        {
            if (key == 0)
            {
                HotKeyRegistered?.Invoke(name, false);
                return;
            }

            // 特殊处理：B1 如果设置为鼠标中键，则不注册（因为中键已通过 Raw Input 处理）
            if (id == HotKeyId.B1 && key == (int)MouseButtons.Middle)
            {
                HotKeyRegistered?.Invoke(name, true); // 视为“成功”（因为不需要注册）
                return;
            }

            bool success = RegisterHotKey(_hwnd, (int)id, (uint)modifiers, key);
            HotKeyRegistered?.Invoke(name, success);
            if (!success)
            {
                // 可以在这里记录日志，但为了不干扰用户，暂时不弹窗
                System.Diagnostics.Debug.WriteLine($"热键 {name} 注册失败: key={key}, mod={modifiers}");
            }
        }

        private void Register(HotKeyId id, int key, int modifiers)
        {
            if (key != 0)
                RegisterHotKey(_hwnd, (int)id, (uint)modifiers, key);
        }

        public void ProcessHotKey(int id)
        {
            switch ((HotKeyId)id)
            {
                case HotKeyId.B1: B1Pressed?.Invoke(); break;
                case HotKeyId.B2: B2Pressed?.Invoke(); break;
                case HotKeyId.ToggleVisible: ToggleVisiblePressed?.Invoke(); break;
                case HotKeyId.ToggleOverlay: ToggleOverlayPressed?.Invoke(); break;
                case HotKeyId.StartMulti: StartMultiPressed?.Invoke(); break;
                case HotKeyId.SetBaseline: SetBaselinePressed?.Invoke(); break;
                case HotKeyId.CancelBaseline: CancelBaselinePressed?.Invoke(); break;
            }
        }

        private void UnregisterAll()
        {
            foreach (HotKeyId id in Enum.GetValues(typeof(HotKeyId)))
                UnregisterHotKey(_hwnd, (int)id);
        }

        public void Dispose()
        {
            UnregisterAll();
        }

        private enum HotKeyId { B1 = 1, B2, ToggleVisible, ToggleOverlay, StartMulti, SetBaseline, CancelBaseline }

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, int vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}