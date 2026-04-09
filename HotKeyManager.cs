using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace HLL_ATassistant
{
    /// <summary>
    /// 全局热键管理器：使用低级键盘钩子监听热键，并管理状态机。
    /// 热键的具体行为定义在 HotKeyManager.Events.cs 中。
    /// </summary>
    public partial class HotKeyManager : IDisposable
    {
        // 状态机模式
        public enum Mode
        {
            Idle,               // 未校准或无数据
            MultiCalibrating,   // 多点校准中
            Measuring,          // 普通测量模式
            HeightDiff          // 高低差测量模式（已设置基准点）
        }

        private readonly IntPtr _hwnd;
        private readonly AppSettings _settings;
        private readonly MainForm _mainForm;

        private Mode _currentMode = Mode.Idle;
        public Mode CurrentMode => _currentMode;

        private bool _heightDiffBaselineSet = false;
        public bool HeightDiffBaselineSet
        {
            get => _heightDiffBaselineSet;
            private set
            {
                if (_heightDiffBaselineSet != value)
                {
                    _heightDiffBaselineSet = value;
                    OnBaselineSetChanged?.Invoke(value);
                    UpdateMode();
                }
            }
        }

        // 暂停标志
        private bool _pause1Active = false, _pause2Active = false;
        public void SetPauseFlags(bool pause1, bool pause2)
        {
            _pause1Active = pause1;
            _pause2Active = pause2;
            SetBetaPaused(pause1 || pause2);
        }

        private bool _betaPaused = false;
        public bool BetaPaused => _betaPaused;
        private void SetBetaPaused(bool paused) => _betaPaused = paused;

        // 事件
        public event Action<bool>? OnBaselineSetChanged;
        public event Action<Mode>? OnModeChanged;

        // 鼠标跟踪器
        private MouseDeltaTracker MouseTracker => MouseDeltaTracker.Instance;

        // ==================== 键盘钩子相关 ====================
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private LowLevelKeyboardProc _proc;
        private IntPtr _hookID = IntPtr.Zero;

        // 修饰键常量
        private const int MOD_ALT = 0x0001;
        private const int MOD_CONTROL = 0x0002;
        private const int MOD_SHIFT = 0x0004;
        // ======================================================

        public HotKeyManager(IntPtr hwnd, AppSettings settings, MainForm mainForm)
        {
            _hwnd = hwnd;
            _settings = settings;
            _mainForm = mainForm;

            MouseDeltaTracker.Instance.SetBetaPausedProvider(() => this.BetaPaused);
            MouseDeltaTracker.Instance.OnMiddleButtonPressed += OnB1;

            // 安装键盘钩子
            InstallKeyboardHook();
        }

        /// <summary>
        /// 重新注册所有热键（设置变更后调用）-> 实际为重新安装钩子
        /// </summary>
        public void ReRegister()
        {
            UninstallKeyboardHook();
            InstallKeyboardHook();
        }

        /// <summary>
        /// 由 MainForm 在状态变化时调用，更新内部模式
        /// </summary>
        public void NotifyStateChanged() => UpdateMode();

        /// <summary>
        /// 取消基准点（供外部调用，如关闭高低差模式时）
        /// </summary>
        public void CancelBaselineExternally() => OnCancelBaseline();

        private void UpdateMode()
        {
            Mode newMode;
            if (_mainForm.CurrentMode == MainForm.UIMode.MultiCalibrating)
                newMode = Mode.MultiCalibrating;
            else if (_mainForm.EnableHeightDiffMode && HeightDiffBaselineSet)
                newMode = Mode.HeightDiff;
            else if (_mainForm.EnableHeightDiffMode && !HeightDiffBaselineSet)
                newMode = Mode.Measuring;
            else if (_mainForm.CurrentMode == MainForm.UIMode.Measuring)
                newMode = Mode.Measuring;
            else
                newMode = Mode.Idle;

            if (_currentMode != newMode)
            {
                _currentMode = newMode;
                OnModeChanged?.Invoke(newMode);
            }
        }

        // ==================== 键盘钩子实现 ====================
        private void InstallKeyboardHook()
        {
            if (_hookID != IntPtr.Zero) return;
            _proc = HookCallback;
            _hookID = SetHook(_proc);
        }

        private void UninstallKeyboardHook()
        {
            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
            }
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Keys key = (Keys)vkCode;
                int modifiers = GetModifiers();

                if (MatchHotkey(key, modifiers, out Action action))
                {
                    // 异步执行动作，避免阻塞钩子
                    _mainForm.BeginInvoke(action);
                    // 注意：不返回1，让按键继续传递给其他应用程序
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private int GetModifiers()
        {
            int mod = 0;
            if ((Control.ModifierKeys & Keys.Control) != 0) mod |= MOD_CONTROL;
            if ((Control.ModifierKeys & Keys.Alt) != 0) mod |= MOD_ALT;
            if ((Control.ModifierKeys & Keys.Shift) != 0) mod |= MOD_SHIFT;
            return mod;
        }

        private bool MatchHotkey(Keys key, int modifiers, out Action action)
        {
            action = null;

            // B1（仅键盘键，鼠标中键已在 MouseDeltaTracker 中单独处理）
            if (_settings.B1Key != (int)MouseButtons.Middle &&
                (int)key == _settings.B1Key && modifiers == _settings.B1Modifiers)
            {
                action = () => OnB1();
                return true;
            }

            // B2
            if ((int)key == _settings.B2Key && modifiers == _settings.B2Modifiers)
            {
                action = () => OnB2();
                return true;
            }

            // 切换主窗体可见性
            if ((int)key == _settings.ToggleVisibleKey && modifiers == _settings.ToggleVisibleModifiers)
            {
                action = () => ToggleMainFormVisibility();
                return true;
            }

            // 切换覆盖窗口可见性
            if ((int)key == _settings.ToggleOverlayKey && modifiers == _settings.ToggleOverlayModifiers)
            {
                action = () => ToggleOverlayVisibility();
                return true;
            }

            // 开始多点校准
            if ((int)key == _settings.StartMultiKey && modifiers == _settings.StartMultiModifiers)
            {
                action = () => _mainForm.StartMultiCalibration();
                return true;
            }

            // 设置基准点
            if (_settings.SetBaselineKey != 0 &&
                (int)key == _settings.SetBaselineKey && modifiers == _settings.SetBaselineModifiers)
            {
                action = () => OnSetBaseline();
                return true;
            }

            // 取消基准点
            if (_settings.CancelBaselineKey != 0 &&
                (int)key == _settings.CancelBaselineKey && modifiers == _settings.CancelBaselineModifiers)
            {
                action = () => OnCancelBaseline();
                return true;
            }

            return false;
        }

        public void Dispose()
        {
            UninstallKeyboardHook();
            MouseDeltaTracker.Instance.OnMiddleButtonPressed -= OnB1;
        }

        // ==================== P/Invoke ====================
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}
