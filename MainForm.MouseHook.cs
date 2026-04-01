using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace HLL_ATassistant
{
    public partial class MainForm
    {
        // 钩子常量
        private const int WH_MOUSE_LL = 14;
        private const int WM_MOUSEMOVE = 0x0200;
        private const int WM_MBUTTONDOWN = 0x0207;

        // 钩子相关
        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
        private LowLevelMouseProc _mouseProc;
        private IntPtr _mouseHookID = IntPtr.Zero;

        // 鼠标位置追踪（用于计算移动量）
        private volatile int _lastMouseY;
        private volatile bool _hasLastMousePos;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        // 钩子平滑算法亚像素变量
        private int _lastDy = 0;           // 上次的位移量（用于判断方向）
        private double _fractionalDelta = 0; // 累积的小数部分位移


        // 注册表
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private void InstallMouseHook()
        {
            _mouseProc = HookCallback;
            using (var curProcess = Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                _mouseHookID = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc, GetModuleHandle(curModule.ModuleName), 0);
                if (_mouseHookID == IntPtr.Zero)
                {
                    MessageBox.Show("鼠标钩子安装失败，鼠标移动将无法检测。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void UninstallMouseHook()
        {
            if (_mouseHookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_mouseHookID);
                _mouseHookID = IntPtr.Zero;
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                if (wParam == (IntPtr)WM_MOUSEMOVE)
                {
                    try
                    {
                        var hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                        int y = hookStruct.pt.y;
                        if (_hasLastMousePos)
                        {
                            int dy = y - _lastMouseY;  // 向下为正（屏幕坐标）
                            if (_isAccumulating)
                            {
                                lock (_deltaLock)
                                {
                                    // 处理实际位移（dy != 0）
                                    if (dy != 0)
                                    {
                                        // 先将累积的小数部分合并到 dy 中
                                        if (_fractionalDelta != 0)
                                        {
                                            dy += (int)Math.Round(_fractionalDelta);
                                            _fractionalDelta = 0;
                                        }
                                        // 更新原始累积值
                                        _currentDelta -= dy * _sensitivity;
                                        _lastDy = dy;
                                    }
                                    // else // dy == 0，无整数位移
                                    // {
                                    //     // 如果有历史移动方向，则添加模拟位移
                                    //     if (_lastDy != 0)
                                    //     {
                                    //         // 每事件增加 0.1 像素的模拟位移，方向与上次一致
                                    //         double microStep = 0.1 * Math.Sign(_lastDy);
                                    //         _fractionalDelta += microStep;
                                    //         // 当小数部分累积超过 1 时，转换为整数位移
                                    //         if (Math.Abs(_fractionalDelta) >= 1.0)
                                    //         {
                                    //             int intPart = (int)Math.Floor(Math.Abs(_fractionalDelta)) * Math.Sign(_fractionalDelta);
                                    //             _currentDelta -= intPart * _sensitivity;
                                    //             _fractionalDelta -= intPart;
                                    //         }
                                    //     }
                                        // 若 _lastDy == 0（刚启动或重置），则不做任何模拟
                                    // }

                                    // 限制非负
                                    if (_currentDelta < 0) _currentDelta = 0;

                                    // // 动态平滑因子：下降时使用较小值，上升时使用原始值
                                    // double effectiveSmoothFactor = (_currentDelta < _smoothedDelta) ? _smoothFactor * 0.2 : _smoothFactor;
                                    // _smoothedDelta = (1.0 - effectiveSmoothFactor) * _currentDelta + effectiveSmoothFactor * _smoothedDelta;
                                }
                            }
                            _lastMouseY = y;
                        }
                        else
                        {
                            _lastMouseY = y;
                            _hasLastMousePos = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                }
                else if (wParam == (IntPtr)WM_MBUTTONDOWN)
                {
                    BeginInvoke(new Action(OnHotKeyB1));
                }
            }
            return CallNextHookEx(_mouseHookID, nCode, wParam, lParam);
        }

        // 重置鼠标位置（在开始测量时调用，避免累积之前的移动）
        private void ResetMousePosition()
        {
            var pos = Cursor.Position;
            _lastMouseY = pos.Y;
            _hasLastMousePos = true;
        }
    }
}