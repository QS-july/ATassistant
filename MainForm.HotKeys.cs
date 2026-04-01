using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;


namespace HLL_ATassistant
{
    public partial class MainForm
    {
        // 热键修饰符常量
        private const int MOD_ALT = 0x0001;
        private const int MOD_CONTROL = 0x0002;
        private const int MOD_SHIFT = 0x0004;
        private const int MOD_WIN = 0x0008;

        // 热键ID
        private const int HOTKEY_ID_B1 = 1;
        private const int HOTKEY_ID_B2 = 2;
        private const int HOTKEY_ID_TOGGLE_VISIBLE = 3;
        private const int HOTKEY_ID_TOGGLE_OVERLAY = 4;
        private const int HOTKEY_ID_START_MULTI = 5;
        private const int HOTKEY_ID_SET_BASELINE = 6;
        private const int HOTKEY_ID_CANCEL_BASELINE = 7;
        

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, int vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private void RegisterAllHotKeys()
        {
            UnregisterHotKey(Handle, HOTKEY_ID_B1);
            UnregisterHotKey(Handle, HOTKEY_ID_B2);
            UnregisterHotKey(Handle, HOTKEY_ID_TOGGLE_VISIBLE);
            UnregisterHotKey(Handle, HOTKEY_ID_TOGGLE_OVERLAY);
            UnregisterHotKey(Handle, HOTKEY_ID_START_MULTI);

            // B1：如果设置为鼠标中键，则跳过热键注册（已通过钩子处理）
            if (_hotKeySettings.B1Key != (int)MouseButtons.Middle)
            {
                if (!RegisterHotKey(Handle, HOTKEY_ID_B1, (uint)_hotKeySettings.B1Modifiers, _hotKeySettings.B1Key))
                    ShowError("Error_HotkeyRegFail", 3000, "1");
            }

            if (!RegisterHotKey(Handle, HOTKEY_ID_B2, (uint)_hotKeySettings.B2Modifiers, _hotKeySettings.B2Key))
                ShowError("Error_HotkeyRegFail", 3000, "2");
            if (!RegisterHotKey(Handle, HOTKEY_ID_TOGGLE_VISIBLE, (uint)_hotKeySettings.ToggleVisibleModifiers, _hotKeySettings.ToggleVisibleKey))
                ShowError("Error_HotkeyRegFail", 3000, "显示/隐藏");
            if (!RegisterHotKey(Handle, HOTKEY_ID_TOGGLE_OVERLAY, (uint)_hotKeySettings.ToggleOverlayModifiers, _hotKeySettings.ToggleOverlayKey))
                ShowError("Error_HotkeyRegFail", 3000, "覆盖窗口");
            if (!RegisterHotKey(Handle, HOTKEY_ID_START_MULTI, (uint)_hotKeySettings.StartMultiModifiers, _hotKeySettings.StartMultiKey))
                ShowError("Error_HotkeyRegFail", 3000, "开始多点校准");
            
            // 高低差测量按键逻辑
            if (_heightDiffSetBaselineKey != 0)
            {
                if (!RegisterHotKey(Handle, HOTKEY_ID_SET_BASELINE, (uint)_heightDiffSetBaselineModifiers, _heightDiffSetBaselineKey))
                    ShowError("Error_HotkeyRegFail", 3000, "设置基准点位");
            }
            if (_heightDiffCancelBaselineKey != 0)
            {
                if (!RegisterHotKey(Handle, HOTKEY_ID_CANCEL_BASELINE, (uint)_heightDiffCancelBaselineModifiers, _heightDiffCancelBaselineKey))
                    ShowError("Error_HotkeyRegFail", 3000, "取消基准点位");
            }
        }

        private void OnHotKeyB1()
        {
            switch (_currentMode)
            {
                case Mode.Idle:
                    ShowError("Error_NeedCalibFirst", 2000);
                    break;

                case Mode.MultiCalibrating:
                    // 多点校准模式下的 B1 逻辑（原有）
                    lock (_deltaLock)
                    {
                        _currentDelta = 0;
                        // _smoothedDelta = 0;
                    }
                    _isAccumulating = true;
                    ResetMousePosition();
                    UpdateStatus(GetString("Msg_StatusMeasurePoint",
                        _currentPointIndex + 1, _calibrationPoints[_currentPointIndex].Distance));
                    break;

                case Mode.Measuring:
                    if (_enableHeightDiffMode)
                    {
                        if (_heightDiffState == HeightDiffState.BaselineSet)
                        {
                            // 基准已设，开始高低差测量
                            // lock (_deltaLock) { _currentDelta = 0; _smoothedDelta = 0; }
                            lock (_deltaLock) { _currentDelta = 0;}
                            _heightDiffState = HeightDiffState.Measuring;
                            _isAccumulating = true;
                            ResetMousePosition();
                            UpdateStatus(GetString("Msg_StatusMeasuring"));
                        }
                        else if (_heightDiffState == HeightDiffState.Idle)
                        {
                            // 未设置基准点，但仍允许普通测量（降级）
                            // lock (_deltaLock) { _currentDelta = 0; _smoothedDelta = 0; }
                            lock (_deltaLock) { _currentDelta = 0;}
                            _isAccumulating = true;
                            ResetMousePosition();
                            ShowError("Error_NeedBaselineFirst", 2000);
                            UpdateStatus(GetString("Msg_StatusMeasuringNormal")); 
                        }
                        else if (_heightDiffState == HeightDiffState.Measuring)
                        {
                            // 重置位移（高低差模式中）
                            // lock (_deltaLock) { _currentDelta = 0; _smoothedDelta = 0; }
                            lock (_deltaLock) { _currentDelta = 0;}
                            ResetMousePosition();
                            UpdateStatus(GetString("Msg_StatusMeasuringReset"));
                        }
                    }
                    else
                    {
                        // 普通测量模式（原有逻辑）
                        lock (_deltaLock)
                        {
                            _currentDelta = 0;
                            // _smoothedDelta = 0;
                        }
                        ResetMousePosition();
                        _isAccumulating = true;
                        UpdateStatus(GetString("Msg_StatusMeasuring"));
                    }
                    break;
            }
        }

        private void OnHotKeyB2()
        {
            switch (_currentMode)
            {
                case Mode.MultiCalibrating:
                    // 多点校准模式下的 B2 逻辑（原有）
                    if (_isAccumulating)
                    {
                        _isAccumulating = false;
                        double tempDelta;
                        lock (_deltaLock)
                        {
                            tempDelta = _currentDelta;
                        }
                        if (tempDelta <= 0)
                        {
                            ShowError($"请移动鼠标后再按确认键 (当前值={tempDelta})", 3000);
                            lock (_deltaLock)
                            {
                                _currentDelta = 0;
                                // _smoothedDelta = 0;
                            }
                            _isAccumulating = true;
                            return;
                        }
                        _calibrationPoints[_currentPointIndex].Delta = tempDelta;
                        _currentPointIndex++;

                        if (_currentPointIndex < _calibrationPoints.Count)
                        {
                            UpdateStatus(GetString("Msg_StatusNextPoint",
                                _currentPointIndex + 1, _calibrationPoints[_currentPointIndex].Distance));
                        }
                        else
                        {
                            FitMultiPoints();
                        }
                    }
                    break;

                case Mode.Measuring:
                    if (_enableHeightDiffMode && _heightDiffState == HeightDiffState.Measuring)
                    {
                        // 高低差测量模式下暂停累积，但不清零 δ₁
                        _isAccumulating = false;
                        UpdateStatus(GetString("Msg_StatusPaused"));
                    }
                    else
                    {
                        // 普通测量模式
                        _isAccumulating = false;
                        UpdateStatus(GetString("Msg_StatusPaused"));
                    }
                    break;
            }
        }

        /// 切换主窗体显示/隐藏。
        private void ToggleMainFormVisibility()
        {
            if (WindowState == FormWindowState.Minimized || !Visible)
            {
                Show();
                WindowState = FormWindowState.Normal;
            }
            else
            {
                Hide();
            }
        }

        /// 切换覆盖窗口显示/隐藏。
        private void ToggleOverlayVisibility()
        {
            overlay.Visible = !overlay.Visible;
        }

        // 设置水平基准点位
        private void OnSetBaseline()
        {
            if (!_enableHeightDiffMode) return;
            if (_heightDiffSameKey && _heightDiffState != HeightDiffState.Idle)
            {
                OnCancelBaseline(); // 同一按键：第二次按下取消
                return;
            }
            // 记录当前位移作为 δ₀（通常此时 _currentDelta 应为0，但保险清零）
            lock (_deltaLock)
            {
                _heightDiffDelta0 = _currentDelta;
                _currentDelta = 0;
                // _smoothedDelta = 0;
            }
            _heightDiffState = HeightDiffState.BaselineSet;
            _isAccumulating = false; // 禁止自动累积，等待用户按 B1 开始
            UpdateStatus(GetString("Msg_BaselineSet"));
        }

        // 取消水平基准点位
        private void OnCancelBaseline()
        {
            if (!_enableHeightDiffMode) return;
            _heightDiffState = HeightDiffState.Idle;
            _isAccumulating = false;
            // lock (_deltaLock) { _currentDelta = 0; _smoothedDelta = 0; }
            lock (_deltaLock) { _currentDelta = 0;}
            UpdateStatus(GetString("Msg_BaselineCancelled"));
        }
    }
}