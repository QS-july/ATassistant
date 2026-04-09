using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace HLL_ATassistant
{
    /// <summary>
    /// 鼠标位移跟踪器：通过 Raw Input 捕获鼠标移动，累积位移。
    /// 采用单例模式，需要在主窗体加载时调用 Initialize 方法。
    /// </summary>
    public class MouseDeltaTracker : IDisposable
    {
        #region P/Invoke 结构体和函数

        private const int RID_INPUT = 0x10000003;
        private const int RIDEV_INPUTSINK = 0x00000100;
        private const int RIDEV_REMOVE = 0x00000001;

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTDEVICE
        {
            public ushort usUsagePage;
            public ushort usUsage;
            public uint dwFlags;
            public IntPtr hwndTarget;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTHEADER
        {
            public uint dwType;
            public uint dwSize;
            public IntPtr hDevice;
            public uint wParam;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWMOUSE
        {
            public ushort usFlags;
            public uint ulButtons;
            public uint ulRawButtons;
            public int lLastX;
            public int lLastY;
            public uint ulExtraInformation;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUT
        {
            public RAWINPUTHEADER header;
            public RAWMOUSE mouse;
        }

        [DllImport("user32.dll")]
        private static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] pRawInputDevices, uint uiNumDevices, uint cbSize);

        [DllImport("user32.dll")]
        private static extern uint GetRawInputData(IntPtr hRawInput, uint uiCommand, IntPtr pData, ref uint pcbSize, uint cbSizeHeader);

        #endregion

        private static MouseDeltaTracker? _instance;
        private static readonly object _lock = new object();

        /// <summary>
        /// 获取单例实例，使用前必须先调用 Initialize
        /// </summary>
        public static MouseDeltaTracker Instance
        {
            get
            {
                if (_instance == null)
                    throw new InvalidOperationException("MouseDeltaTracker 未初始化，请先调用 Initialize");
                return _instance;
            }
        }

        /// <summary>
        /// 初始化单例，必须在主窗体句柄有效时调用一次
        /// </summary>
        /// <param name="hwnd">接收原始输入消息的窗口句柄</param>
        public static void Initialize(IntPtr hwnd)
        {
            lock (_lock)
            {
                if (_instance == null)
                    _instance = new MouseDeltaTracker(hwnd);
            }
        }

        // 原始累积位移（用于校准，从 B1 到 B2 的总位移）
        // θ 和 β 独立累积器
        private double _deltaTheta;      // 始终累积（向上移动增加）
        private double _deltaBeta;       // 仅在高低差模式且未暂停时累积
        private double _fixedDeltaBeta = 0;
        // 提供 β 暂停状态的回调
        private Func<bool> _betaPausedProvider;   
        public void SetBetaPausedProvider(Func<bool> provider){_betaPausedProvider = provider;}
        private bool _isAccumulating;
        private double _sensitivity = 1.0;
        private readonly object _syncLock = new object();
        private IntPtr _hwnd;        

        // 鼠标中键状态跟踪
        private bool _lastMButtonState;

        // 公开属性
        public double DeltaTheta
        {
            get { lock (_syncLock) return _deltaTheta; }
            private set { lock (_syncLock) _deltaTheta = value; }
        }

        public double DeltaBeta => _fixedDeltaBeta;

        // 事件
        public event Action<double>? DeltaChanged;      // 增量变化（每次鼠标移动）
        public event Action<double, double>? DisplacementChanged; // θ, β 累积值
        public event Action? OnMiddleButtonPressed;

        private MouseDeltaTracker(IntPtr hwnd)
        {
            _hwnd = hwnd;
            InstallRawInput();
        }

        private void InstallRawInput()
        {
            var rid = new RAWINPUTDEVICE[1];
            rid[0].usUsagePage = 0x01;
            rid[0].usUsage = 0x02;   // 鼠标
            rid[0].dwFlags = RIDEV_INPUTSINK;
            rid[0].hwndTarget = _hwnd;
            if (!RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICE))))
                throw new InvalidOperationException("Raw Input 注册失败");
        }

        public void StartAccumulating()
        {
            lock (_syncLock) _isAccumulating = true;
        }

        public void StopAccumulating()
        {
            lock (_syncLock) _isAccumulating = false;
        }

        public void Reset()
        {
            lock (_syncLock)
            {
                _deltaTheta = 0;
                _deltaBeta = 0;
                _fixedDeltaBeta = 0;
            }
            DisplacementChanged?.Invoke(_deltaTheta, _deltaBeta);
        }

        /// <summary>
        /// 重置 Theta 累积位移（用于设置基准点后从零开始）
        /// </summary>
        public void ResetTheta()
        {
            lock (_syncLock)
            {
                _deltaTheta = 0;
            }
            DisplacementChanged?.Invoke(_deltaTheta, _deltaBeta);
        }

        /// <summary>
        /// 完全重置 Beta 相关状态（清除固定值、恢复累积、位移归零）
        /// </summary>
        public void ResetBeta()
        {
            lock (_syncLock)
            {
                _deltaBeta = 0;
            }
            DisplacementChanged?.Invoke(_deltaTheta, _deltaBeta);
        }

        public void ResetfixedBeta()
        {
            lock (_syncLock)
            {
                _fixedDeltaBeta = 0;
            }
            DisplacementChanged?.Invoke(_deltaTheta, _deltaBeta);
        }

        public void SetSensitivity(double value)
        {
            _sensitivity = value;
        }

        /// <summary>
        /// 固定当前 β 值，供 Overlay 计算 BetaAngle 使用
        /// </summary>
        public void SaveBeta()
        {
            lock (_syncLock)
            {
                _fixedDeltaBeta = _deltaBeta;
            }
        }

        public void ProcessRawInput(IntPtr lParam)
        {
            uint dwSize = 0;
            GetRawInputData(lParam, RID_INPUT, IntPtr.Zero, ref dwSize, (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER)));
            if (dwSize == 0) return;

            IntPtr pBuffer = Marshal.AllocHGlobal((int)dwSize);
            try
            {
                uint bytesWritten = GetRawInputData(lParam, RID_INPUT, pBuffer, ref dwSize, (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER)));
                if (bytesWritten == dwSize)
                {
                    RAWINPUT raw = Marshal.PtrToStructure<RAWINPUT>(pBuffer);
                    if (raw.header.dwType == 0) // RIM_TYPEMOUSE
                    {
                        bool accumulating;
                        lock (_syncLock)
                        {
                            accumulating = _isAccumulating;
                        }

                        if (accumulating)
                        {
                            int dy = raw.mouse.lLastY; // 向上为负，向下为正
                            if (dy != 0)
                            {
                                // 向上移动（dy负）-> 位移增加；向下移动（dy正）-> 位移减少
                                double increment = -dy * _sensitivity;
                                lock (_syncLock)
                                {
                                    // 更新 θ 位移（始终累积）
                                    _deltaTheta += increment;
                                    if (_deltaTheta < 0) _deltaTheta = 0;

                                    // 更新 β 位移（仅在未暂停时累积）
                                    bool betaPaused = _betaPausedProvider?.Invoke() ?? false;
                                    if (!betaPaused)
                                    {
                                        _deltaBeta += increment;
                                    }
                                }
                                DeltaChanged?.Invoke(increment);
                                DisplacementChanged?.Invoke(_deltaTheta, _deltaBeta);
                            }
                        }

                        // 中键按下模拟 B1 热键
                        const uint RI_MOUSE_MIDDLE_BUTTON_DOWN = 0x0020;
                        bool mButtonDown = (raw.mouse.ulButtons & RI_MOUSE_MIDDLE_BUTTON_DOWN) != 0;
                        if (mButtonDown && !_lastMButtonState)
                        {
                            OnMiddleButtonPressed?.Invoke();
                        }
                        _lastMButtonState = mButtonDown;
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(pBuffer);
            }
        }

        public void Dispose()
        {
            var rid = new RAWINPUTDEVICE[1];
            rid[0].usUsagePage = 0x01;
            rid[0].usUsage = 0x02;
            rid[0].dwFlags = RIDEV_REMOVE;
            rid[0].hwndTarget = IntPtr.Zero;
            RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICE)));
        }
    }
}
