using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace HLL_ATassistant
{
    public class MouseDeltaTracker : IDisposable
    {
        private const int WM_INPUT = 0x00FF;
        private const int RIDEV_INPUTSINK = 0x00000100;
        private const int RID_INPUT = 0x10000003;
        private const int RIDEV_REMOVE = 0x00000001;

        private double _currentDelta;
        private readonly object _lock = new object();
        private bool _isAccumulating;
        private double _sensitivity = 1.0;
        private IntPtr _hwnd;

        public double CurrentDelta
        {
            get { lock (_lock) return _currentDelta; }
            private set { lock (_lock) _currentDelta = value; }
        }

        public event Action<double> DeltaChanged;

        public MouseDeltaTracker(IntPtr hwnd)
        {
            _hwnd = hwnd;
            InstallRawInput();
        }

        private void InstallRawInput()
        {
            var rid = new RAWINPUTDEVICE[1];
            rid[0].usUsagePage = 0x01;
            rid[0].usUsage = 0x02;
            rid[0].dwFlags = RIDEV_INPUTSINK;
            rid[0].hwndTarget = _hwnd;
            if (!RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICE))))
                throw new InvalidOperationException("Raw Input 注册失败");
        }

        public void StartAccumulating()
        {
            lock (_lock) _isAccumulating = true;
        }

        public void StopAccumulating()
        {
            lock (_lock) _isAccumulating = false;
        }

        public void Reset()
        {
            lock (_lock) _currentDelta = 0;
            DeltaChanged?.Invoke(0);
        }

        public void SetSensitivity(double value)
        {
            _sensitivity = value;
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
                        lock (_lock) accumulating = _isAccumulating;

                        if (accumulating)
                        {
                            int dy = raw.mouse.lLastY; // 向上为负，向下为正
                            if (dy != 0)
                            {
                                lock (_lock)
                                {
                                    _currentDelta -= dy * _sensitivity;
                                    if (_currentDelta < 0) _currentDelta = 0;
                                }
                                DeltaChanged?.Invoke(_currentDelta);
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

        private bool _lastMButtonState;
        public event Action OnMiddleButtonPressed;

        public void Dispose()
        {
            var rid = new RAWINPUTDEVICE[1];
            rid[0].usUsagePage = 0x01;
            rid[0].usUsage = 0x02;
            rid[0].dwFlags = RIDEV_REMOVE;
            rid[0].hwndTarget = IntPtr.Zero;
            RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICE)));
        }

        // P/Invoke
        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTDEVICE { public ushort usUsagePage; public ushort usUsage; public uint dwFlags; public IntPtr hwndTarget; }
        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTHEADER { public uint dwType; public uint dwSize; public IntPtr hDevice; public uint wParam; }
        [StructLayout(LayoutKind.Sequential)]
        private struct RAWMOUSE { public ushort usFlags; public uint ulButtons; public uint ulRawButtons; public int lLastX; public int lLastY; public uint ulExtraInformation; }
        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUT { public RAWINPUTHEADER header; public RAWMOUSE mouse; }

        [DllImport("user32.dll")]
        private static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] pRawInputDevices, uint uiNumDevices, uint cbSize);
        [DllImport("user32.dll")]
        private static extern uint GetRawInputData(IntPtr hRawInput, uint uiCommand, IntPtr pData, ref uint pcbSize, uint cbSizeHeader);
    }
}