using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace HLL_ATassistant
{
    /// <summary>
    /// 主窗体类，负责UI交互、校准逻辑和鼠标钩子管理。
    /// </summary>
    public partial class MainForm : Form
    {
        // 状态机：空闲、测量中、多点校准中
        private enum Mode { Idle, Measuring, MultiCalibrating }
        private Mode _currentMode = Mode.Idle;

        // 物理量
        private double _alpha;          // 射角系数
        private double _v2g;             // v²/g
        private double _v = 100.0;       // 初速度 (m/s)
        private double _g = 9.8;         // 重力加速度 (m/s²)

        // 高低差测量模式
        private bool _enableHeightDiffMode = false;
        private double _heightDiffDelta0 = 0;   // δ₀
        private int _heightDiffSetBaselineKey = 0;
        private int _heightDiffSetBaselineModifiers = 0;
        private int _heightDiffCancelBaselineKey = 0;
        private int _heightDiffCancelBaselineModifiers = 0;
        private bool _heightDiffSameKey = false;
        private enum HeightDiffState { Idle, BaselineSet, Measuring }
        private HeightDiffState _heightDiffState = HeightDiffState.Idle;
        private CheckBox chkEnableHeightDiff = null!;

        // 鼠标位移累积量
        private double _currentDelta;
        // private double _smoothedDelta;
        // public double SmoothedDelta => _smoothedDelta;
        private readonly object _deltaLock = new object();
        private volatile bool _isAccumulating;   // 是否正在累积位移
        private double _sensitivity = 1.0;        // 灵敏度系数
        // private double _smoothFactor = 0.2;       // 平滑因子
        private double _maxDeltaLimit;             // 最大有效位移

        // 控件字段
        private Label lblMaxRange = null!;
        // private Label lblSmooth = null!;
        private Label lblSensitivity = null!;
        private Label lblDistances = null!;
        private Label lblStatus = null!;
        private Label lblError = null!;
        private Label lblErrorStats = null!;
        private Label lblWarning = null!;
        private Label lblMultiStatus = null!;
        private Button btnReset = null!;
        private Button btnSave = null!;
        private Button btnLoad = null!;
        private Button btnRefresh = null!;
        private Button btnLanguage = null!;
        private Button btnSettings = null!;
        private Button btnStartMulti = null!;
        private NumericUpDown nudSensitivity = null!;
        private NumericUpDown nudMaxRange = null!;
        // private NumericUpDown nudSmoothFactor = null!;
        private CheckBox chkUseBuiltin = null!;
        private TextBox txtDistances = null!;
        private NotifyIcon notifyIcon = null!;
        private OverlayForm overlay = null!;
        private HotKeySettings _hotKeySettings = null!;

        // 校准点集合
        private List<CalibrationPoint> _calibrationPoints = new List<CalibrationPoint>();
        private int _currentPointIndex = -1;

        // 语言枚举
        private enum Language { Chinese, English }
        private Language _currentLanguage = Language.Chinese;

        // 公开属性（供 OverlayForm 使用）
        public double MaxCalibratedDelta => _maxDeltaLimit;
        public double CurrentDelta => _currentDelta;
        public bool IsHeightDiffModeEnabled => _enableHeightDiffMode;
        public double HeightDiffDelta0 => _heightDiffDelta0;
        public double Velocity => _v;
        public double BetaAngle => _alpha * _heightDiffDelta0;   // β = α * δ₀

        // Overlay 显示项（从热键设置中读取）
        public bool IsHeightDiffBaselineSet => _heightDiffState != HeightDiffState.Idle;
        public bool ShowDistance => _hotKeySettings.ShowDistance;
        public bool ShowAngle => _hotKeySettings.ShowAngle;
        public bool ShowL => _hotKeySettings.ShowL;
        public bool ShowX => _hotKeySettings.ShowX;
        public bool ShowBeta => _hotKeySettings.ShowBeta;
        public bool ShowVx => _hotKeySettings.ShowVx;
        public bool ShowTime => _hotKeySettings.ShowTime;
        public bool ShowTPrime => _hotKeySettings.ShowTPrime;


        /// <summary>
        /// 构造函数：初始化组件、注册热键、安装鼠标钩子、创建覆盖窗口。
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
            TopMost = true;

            _hotKeySettings = HotKeySettings.Load();
            // 加载高低差热键设置
            _heightDiffSetBaselineKey = _hotKeySettings.SetBaselineKey;
            _heightDiffSetBaselineModifiers = _hotKeySettings.SetBaselineModifiers;
            _heightDiffCancelBaselineKey = _hotKeySettings.CancelBaselineKey;
            _heightDiffCancelBaselineModifiers = _hotKeySettings.CancelBaselineModifiers;
            _heightDiffSameKey = _hotKeySettings.SameKeyForBaseline;

            RegisterAllHotKeys();
            InstallMouseHook();

            overlay = new OverlayForm(this);
            overlay.Show();

            chkUseBuiltin.Checked = true;

            FormClosing += (s, e) =>
            {
                UninstallMouseHook();
                UnregisterHotKey(Handle, HOTKEY_ID_B1);
                UnregisterHotKey(Handle, HOTKEY_ID_B2);
                UnregisterHotKey(Handle, HOTKEY_ID_TOGGLE_VISIBLE);
                UnregisterHotKey(Handle, HOTKEY_ID_TOGGLE_OVERLAY);
                UnregisterHotKey(Handle, HOTKEY_ID_START_MULTI);
                notifyIcon.Visible = false;
                notifyIcon.Dispose();
                overlay.Close();
            };

            ApplyLanguage();
            UpdateVFromV2G();
        }

        private void UpdateVFromV2G()
        {
            _v = (_v2g > 0 && _g > 0) ? Math.Sqrt(_v2g * _g) : 0;
        }

        private void UpdateStatus(string text)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(UpdateStatus), text);
                return;
            }
            lblStatus.Text = text;
        }

        private async void ShowErrorMessage(string message, int timeoutMs)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string, int>(ShowErrorMessage), message, timeoutMs);
                return;
            }
            lblError.Text = message;
            if (timeoutMs > 0)
            {
                await Task.Delay(timeoutMs);
                lblError.Text = "";
            }
        }

        public class CalibrationPoint
        {
            public double Distance { get; set; }
            public double Delta { get; set; }
        }

        [Serializable]
        public class CalibrationPointData
        {
            public double Delta { get; set; }
            public double Distance { get; set; }
        }

        [Serializable]
        public class CalibrationData
        {
            public List<CalibrationPointData> Points { get; set; } = new List<CalibrationPointData>();
            public double Alpha { get; set; }
            public double V2G { get; set; }
            public double V { get; set; }
            public double G { get; set; }
            public double MaxDeltaLimit { get; set; }
        }

        private void ChkEnableHeightDiff_CheckedChanged(object? sender, EventArgs e)
        {
            _enableHeightDiffMode = chkEnableHeightDiff.Checked;
            if (_enableHeightDiffMode)
            {
                lblWarning.Text = GetString("HeightDiffModeWarning");
                lblWarning.ForeColor = Color.Red;
                lblWarning.Visible = true;
            }
            else
            {
                lblWarning.Visible = false;
                _heightDiffState = HeightDiffState.Idle;
                _isAccumulating = false;
                lock (_deltaLock)
                {
                    _currentDelta = 0;
                    // _smoothedDelta = 0;
                }
            }
        }

        private Button CreateStyledButton(string text, int x, int y, int width, EventHandler clickHandler)
        {
            Button btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Width = width,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(80, 80, 80),
                ForeColor = Color.White,
                FlatAppearance = { BorderSize = 0 }
            };
            btn.Click += clickHandler;
            return btn;
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0312) // WM_HOTKEY
            {
                int id = m.WParam.ToInt32();
                switch (id)
                {
                    case HOTKEY_ID_B1: OnHotKeyB1(); break;
                    case HOTKEY_ID_B2: OnHotKeyB2(); break;
                    case HOTKEY_ID_TOGGLE_VISIBLE: ToggleMainFormVisibility(); break;
                    case HOTKEY_ID_TOGGLE_OVERLAY: ToggleOverlayVisibility(); break;
                    case HOTKEY_ID_START_MULTI: BtnStartMulti_Click(null, null); break;
                    case HOTKEY_ID_SET_BASELINE: OnSetBaseline(); break;
                    case HOTKEY_ID_CANCEL_BASELINE: OnCancelBaseline(); break;
                }
                return;
            }
            base.WndProc(ref m);
        }

        private void InitializeComponent()
        {
            BackColor = Color.FromArgb(30, 30, 30);
            ForeColor = Color.FromArgb(240, 240, 240);
            Text = "筒子校准测距";
            Size = new Size(300, 470);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            int yPos = 20;

            // 最大射程标签
            lblMaxRange = new Label
            {
                Name = "lblMaxRange",
                Text = "最大射程 (米):",
                Location = new Point(10, yPos),
                AutoSize = true,
                ForeColor = Color.White
            };
            Controls.Add(lblMaxRange);

            // 数值框
            nudMaxRange = new NumericUpDown
            {
                Location = new Point(120, yPos - 2),
                Width = 80,
                Minimum = 1,
                Maximum = 5000,
                Value = 1000,
                Increment = 5,
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White
            };
            // nudMaxRange.ValueChanged += NudMaxRange_ValueChanged;            //最大射程随调节框内容显示，留作日后使用
            Controls.Add(nudMaxRange);

            // “使用最大射程”复选框（无文字）
            chkUseBuiltin = new CheckBox
            {
                Location = new Point(nudMaxRange.Right + 5, yPos + 3),
                AutoSize = true,
                Checked = true,
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                Text = ""   
            };
            chkUseBuiltin.CheckedChanged += ChkUseBuiltin_CheckedChanged;
            Controls.Add(chkUseBuiltin);

            // 刷新按钮
            btnRefresh = CreateStyledButton("刷新", 220, yPos - 2, 50, BtnRefresh_Click);
            Controls.Add(btnRefresh);

            yPos += 30;

            // // 平滑系数
            // lblSmooth = new Label
            // {
            //     Name = "lblSmooth",
            //     Text = "平滑系数:",
            //     Location = new Point(10, yPos),
            //     AutoSize = true,
            //     ForeColor = Color.White
            // };
            // Controls.Add(lblSmooth);

            // nudSmoothFactor = new NumericUpDown
            // {
            //     Location = new Point(120, yPos - 2),
            //     Width = 80,
            //     Minimum = 0.01m,
            //     Maximum = 1.0m,
            //     DecimalPlaces = 2,
            //     Increment = 0.01m,
            //     Value = 0.2m,
            //     BackColor = Color.FromArgb(50, 50, 50),
            //     ForeColor = Color.White
            // };
            // nudSmoothFactor.ValueChanged += NudSmoothFactor_ValueChanged;
            // Controls.Add(nudSmoothFactor);
            // yPos += 30;

            // 灵敏度系数
            lblSensitivity = new Label
            {
                Name = "lblSensitivity",
                Text = "灵敏度系数:",
                Location = new Point(10, yPos),
                AutoSize = true,
                ForeColor = Color.White
            };
            Controls.Add(lblSensitivity);

            nudSensitivity = new NumericUpDown
            {
                Location = new Point(120, yPos - 2),
                Width = 80,
                Minimum = 0.01m,
                Maximum = 2.0m,
                DecimalPlaces = 2,
                Increment = 0.01m,
                Value = 1.0m,
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White
            };
            nudSensitivity.ValueChanged += NudSensitivity_ValueChanged;
            Controls.Add(nudSensitivity);
            yPos += 30;

            // 高低差测量模式复选框（原“使用最大射程”带文字的位置）
            chkEnableHeightDiff = new CheckBox
            {
                Text = "启用高低差测量模式",
                Location = new Point(10, yPos),
                AutoSize = true,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Checked = false
            };
            chkEnableHeightDiff.CheckedChanged += ChkEnableHeightDiff_CheckedChanged;
            Controls.Add(chkEnableHeightDiff);

            // 语言切换按钮
            btnLanguage = CreateStyledButton("English", 210, yPos - 2, 60, BtnLanguage_Click);
            btnLanguage.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            Controls.Add(btnLanguage);
            yPos += 25;

            // 警告标签
            lblWarning = new Label
            {
                Location = new Point(10, yPos),
                Size = new Size(260, 20),
                ForeColor = Color.Red,
                Visible = false
            };
            Controls.Add(lblWarning);
            yPos += 25;

            // 距离列表标签
            lblDistances = new Label
            {
                Name = "lblDistances",
                Text = "距离列表\n(每行一个):",
                Location = new Point(10, yPos),
                AutoSize = true,
                ForeColor = Color.White
            };
            Controls.Add(lblDistances);

            // 距离输入框
            txtDistances = new TextBox
            {
                Location = new Point(120, yPos),
                Width = 150,
                Height = 60,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White
            };
            Controls.Add(txtDistances);
            yPos += 65;

            // 功能按钮
            btnStartMulti = CreateStyledButton("开始校准", 10, yPos, 80, BtnStartMulti_Click);
            Controls.Add(btnStartMulti);

            btnReset = CreateStyledButton("重置校准", 100, yPos, 80, BtnReset_Click);
            Controls.Add(btnReset);
            yPos += 30;

            lblError = new Label { Location = new Point(10, yPos), Size = new Size(260, 30), ForeColor = Color.Red };
            Controls.Add(lblError);
            yPos += 30;

            lblStatus = new Label { Text = "状态: 等待校准", Location = new Point(10, yPos), AutoSize = true };
            Controls.Add(lblStatus);
            yPos += 25;

            lblMultiStatus = new Label { Location = new Point(10, yPos), AutoSize = true, ForeColor = Color.LightBlue };
            Controls.Add(lblMultiStatus);
            yPos += 25;

            lblErrorStats = new Label
            {
                Location = new Point(10, yPos),
                Size = new Size(260, 40),
                ForeColor = Color.LightGreen,
                Text = "误差统计: 无数据"
            };
            Controls.Add(lblErrorStats);
            yPos += 45;

            btnLoad = CreateStyledButton("加载校准", 10, yPos, 80, BtnLoad_Click);
            Controls.Add(btnLoad);

            btnSave = CreateStyledButton("保存校准", 100, yPos, 80, BtnSave_Click);
            Controls.Add(btnSave);

            btnSettings = CreateStyledButton("设置", 190, yPos, 80, BtnSettings_Click);
            Controls.Add(btnSettings);
            yPos += 35;

            // 系统托盘图标
            notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Visible = true,
                Text = "筒子校准测距"
            };
            notifyIcon.Click += (s, e) => { Show(); WindowState = FormWindowState.Normal; };
            Resize += (s, e) => { if (WindowState == FormWindowState.Minimized) Hide(); };
        }
    }
}