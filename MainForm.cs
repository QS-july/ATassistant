using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;


namespace HLL_ATassistant
{
    public partial class MainForm : Form
    {
        // 组件
        private readonly AppSettings _settings;
        private readonly CalibrationEngine _engine;
        private readonly MouseDeltaTracker _mouseTracker;
        private HotKeyManager _hotKeyManager;
        private OverlayForm _overlay;

        // 状态机
        private enum Mode { Idle, Measuring, MultiCalibrating }
        private Mode _currentMode = Mode.Idle;
        private int _currentPointIndex;

        // 高低差模式专用
        private bool _enableHeightDiffMode;
        private double _heightDiffDelta0;
        private enum HeightDiffState { Idle, BaselineSet, Measuring }
        private HeightDiffState _heightDiffState = HeightDiffState.Idle;

        // UI 控件（由设计器生成，此处仅声明引用）
        private Label lblMaxRange, lblSensitivity, lblDistances, lblStatus, lblError, lblErrorStats, lblWarning, lblMultiStatus;
        private NumericUpDown nudSensitivity, nudMaxRange;
        private CheckBox chkUseBuiltin, chkEnableHeightDiff;
        private TextBox txtDistances;
        private Button btnStartMulti, btnReset, btnSave, btnLoad, btnRefresh, btnSettings, btnLanguage;
        private NotifyIcon notifyIcon;

        // 语言管理
        private enum Language { Chinese, English }
        private Language _currentLanguage = Language.Chinese;


        // 公开属性供 OverlayForm 使用
        public AppSettings Settings => _settings;
        public CalibrationEngine Engine => _engine;
        public double CurrentDelta => _mouseTracker.CurrentDelta;
        public double MaxCalibratedDelta => _engine.MaxDeltaLimit;
        public double Alpha => _engine.Alpha;
        public double V2G => _engine.V2G;
        public double Velocity => _engine.Velocity;

        public bool ShowDistance => _settings.ShowDistance;
        public bool ShowAngle => _settings.ShowAngle;
        public bool ShowVx => _settings.ShowVx;
        public bool ShowTime => _settings.ShowTime;
        public bool ShowL => _settings.ShowL;
        public bool ShowX => _settings.ShowX;
        public bool ShowBeta => _settings.ShowBeta;
        public bool ShowTPrime => _settings.ShowTPrime;

        public bool IsHeightDiffModeEnabled => _enableHeightDiffMode;
        public bool IsHeightDiffBaselineSet => _heightDiffState != HeightDiffState.Idle;
        public double HeightDiffDelta0 => _heightDiffDelta0;
        public double BetaAngle => _engine.Alpha * _heightDiffDelta0;
        public event EventHandler<double> DeltaChanged;
        

        public MainForm()
        {
            _settings = AppSettings.Instance;
            _engine = new CalibrationEngine();
            _mouseTracker = new MouseDeltaTracker(this.Handle);
            _hotKeyManager = new HotKeyManager(this.Handle, _settings);

            InitializeComponent();  // 设计器生成的控件初始化

            // 恢复主窗体位置
            if (_settings.MainFormLocation.HasValue)
            {
                var loc = _settings.MainFormLocation.Value;
                // 确保位置在屏幕可见区域内
                if (IsOnScreen(loc))
                {
                    this.StartPosition = FormStartPosition.Manual;
                    this.Location = loc;
                }
            }

            chkEnableHeightDiff.CheckedChanged += ChkEnableHeightDiff_CheckedChanged;

            // 初始化 UI 值与设置同步
            nudSensitivity.Value = (decimal)_settings.Sensitivity;
            nudMaxRange.Value = (decimal)_settings.MaxRange;
            chkUseBuiltin.Checked = _settings.UseFixedMaxRange;
            chkEnableHeightDiff.Checked = _enableHeightDiffMode;

            // 事件订阅
            _mouseTracker.DeltaChanged += OnDeltaChanged;
            _mouseTracker.OnMiddleButtonPressed += () => _hotKeyManager.ProcessHotKey(1);

            _hotKeyManager.B1Pressed += OnB1;
            _hotKeyManager.B2Pressed += OnB2;
            _hotKeyManager.ToggleVisiblePressed += ToggleMainFormVisibility;
            _hotKeyManager.ToggleOverlayPressed += () => _overlay?.Invoke(new Action(() => _overlay.Visible = !_overlay.Visible));
            _hotKeyManager.StartMultiPressed += () => BtnStartMulti_Click(null, null);
            _hotKeyManager.SetBaselinePressed += OnSetBaseline;
            _hotKeyManager.CancelBaselinePressed += OnCancelBaseline;

            _engine.CalibrationChanged += () =>
            {
                if (InvokeRequired) BeginInvoke(new Action(() => { UpdateErrorStats(); UpdateAbsoluteErrorStats(); _overlay?.RefreshDisplay(); }));
                else { UpdateErrorStats(); UpdateAbsoluteErrorStats(); _overlay?.RefreshDisplay(); }
            };

            // 创建 Overlay
            _overlay = new OverlayForm(this);
            _overlay.Show();

            // 设置窗体图标（标题栏左侧）
            string iconPath = Path.Combine(Application.StartupPath, "rsc", "icon.ico");
            if (File.Exists(iconPath))
            {
                this.Icon = new Icon(iconPath);
            }

            // 加载图标（使用字节数组，避免流位置问题）
            try
            {
                // 嵌入式资源的名称：默认命名空间 + 文件夹名 + 文件名
                string resourceName = "HLL_ATassistant.rsc.icon.ico";
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                {
                    if (stream != null && stream.Length > 0)
                    {
                        // 读取全部字节到数组
                        byte[] iconBytes = new byte[stream.Length];
                        stream.Read(iconBytes, 0, iconBytes.Length);
                        
                        // 为窗体图标创建独立内存流
                        using (var ms = new MemoryStream(iconBytes))
                        {
                            this.Icon = new Icon(ms);
                        }
                        // 为托盘图标创建独立内存流
                        using (var ms = new MemoryStream(iconBytes))
                        {
                            notifyIcon.Icon = new Icon(ms);
                        }
                    }
                    else
                    {
                        notifyIcon.Icon = SystemIcons.Application;
                    }
                }
            }
            catch (Exception ex)
            {
                // 任何加载失败的情况都使用系统默认图标，程序不会崩溃
                System.Diagnostics.Debug.WriteLine($"图标加载失败: {ex.Message}");
                notifyIcon.Icon = SystemIcons.Application;
            }

            ContextMenuStrip trayMenu = new ContextMenuStrip();
            ToolStripMenuItem settingsItem = new ToolStripMenuItem("设置");
            ToolStripMenuItem exitItem = new ToolStripMenuItem("退出");
            settingsItem.Click += (s, e) => BtnSettings_Click(null, null);
            exitItem.Click += (s, e) => Application.Exit();
            trayMenu.Items.Add(settingsItem);
            trayMenu.Items.Add(exitItem);
            notifyIcon.ContextMenuStrip = trayMenu;

            // 托盘双击-显示
            notifyIcon.DoubleClick += (s, e) =>
            {
                this.Show();
                this.WindowState = FormWindowState.Normal;
            };

            // 其他初始化
            TopMost = true;
            ApplyLanguage();
            UpdateStatus(GetString("StatusIdle"));
        }


        #region 鼠标位移变化处理
        private void OnDeltaChanged(double delta)
        {
            if (InvokeRequired) { BeginInvoke(new Action<double>(OnDeltaChanged), delta); return; }
            DeltaChanged?.Invoke(this, delta);
        }
        #endregion

        #region 热键行为
        private void OnB1()
        {
            switch (_currentMode)
            {
                case Mode.Idle:
                    ShowError("Error_NeedCalibFirst", 2000);
                    break;

                case Mode.MultiCalibrating:
                    // 重置位移，开始累积
                    _mouseTracker.Reset();
                    _mouseTracker.StartAccumulating();
                    UpdateStatus(GetString("Msg_StatusMeasurePoint",
                        _currentPointIndex + 1, _engine.Points[_currentPointIndex].Distance));
                    break;

                case Mode.Measuring:
                    if (_enableHeightDiffMode)
                    {
                        if (_heightDiffState == HeightDiffState.BaselineSet)
                        {
                            _mouseTracker.Reset();
                            _heightDiffState = HeightDiffState.Measuring;
                            _mouseTracker.StartAccumulating();
                            UpdateStatus(GetString("Msg_StatusMeasuring"));
                        }
                        else if (_heightDiffState == HeightDiffState.Idle)
                        {
                            _mouseTracker.Reset();
                            _mouseTracker.StartAccumulating();
                            ShowError("Error_NeedBaselineFirst", 2000);
                            UpdateStatus(GetString("Msg_StatusMeasuringNormal"));
                        }
                        else if (_heightDiffState == HeightDiffState.Measuring)
                        {
                            _mouseTracker.Reset();
                            UpdateStatus(GetString("Msg_StatusMeasuringReset"));
                        }
                    }
                    else
                    {
                        _mouseTracker.Reset();
                        _mouseTracker.StartAccumulating();
                        UpdateStatus(GetString("Msg_StatusMeasuring"));
                    }
                    break;
            }
        }

        private void OnB2()
        {
            switch (_currentMode)
            {
                case Mode.MultiCalibrating:
                    if (_mouseTracker.CurrentDelta > 0)
                    {
                        // 记录当前位移到当前标定点
                        _engine.Points[_currentPointIndex].Delta = _mouseTracker.CurrentDelta;
                        _currentPointIndex++;
                        if (_currentPointIndex < _engine.Points.Count)
                        {
                            _mouseTracker.Reset();
                            UpdateStatus(GetString("Msg_StatusNextPoint",
                                _currentPointIndex + 1, _engine.Points[_currentPointIndex].Distance));
                        }
                        else
                        {
                            _mouseTracker.StopAccumulating();
                            PerformCalibration();  // 拟合多点
                        }
                    }
                    else
                    {
                        ShowError("请移动鼠标后再按确认键", 3000);
                        _mouseTracker.Reset();
                        _mouseTracker.StartAccumulating();
                    }
                    break;

                case Mode.Measuring:
                    _mouseTracker.StopAccumulating();
                    UpdateStatus(GetString("Msg_StatusPaused"));
                    break;
            }
        }

        private void PerformCalibration()
        {
            try
            {
                bool useFixed = _settings.UseFixedMaxRange;  // 或 chkUseBuiltin.Checked
                double fixedRange = (double)nudMaxRange.Value;
                _engine.FitMultiPoints(useFixed, fixedRange);
                _currentMode = Mode.Measuring;
                _mouseTracker.Reset();
                _mouseTracker.StartAccumulating();
                UpdateStatus(GetString("Msg_StatusCalibComplete", _engine.Alpha, _engine.V2G));
            }
            catch (Exception ex)
            {
                ShowError(ex.Message, 3000);
                _currentMode = Mode.Idle;
            }
        }

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

        private void OnSetBaseline()
        {
            if (!_enableHeightDiffMode) return;
            if (_settings.SameKeyForBaseline && _heightDiffState != HeightDiffState.Idle)
            {
                OnCancelBaseline();
                return;
            }
            _heightDiffDelta0 = _mouseTracker.CurrentDelta;
            _mouseTracker.Reset();
            _heightDiffState = HeightDiffState.BaselineSet;
            _mouseTracker.StopAccumulating();
            UpdateStatus(GetString("Msg_BaselineSet"));
        }

        private void OnCancelBaseline()
        {
            if (!_enableHeightDiffMode) return;
            _heightDiffState = HeightDiffState.Idle;
            _mouseTracker.Reset();
            UpdateStatus(GetString("Msg_BaselineCancelled"));
        }
        #endregion

        #region 按钮事件
        private void BtnStartMulti_Click(object sender, EventArgs e)
        {
            var distances = txtDistances.Text
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => double.TryParse(line.Trim(), out double d) ? d : (double?)null)
                .Where(d => d.HasValue)
                .Select(d => d.Value)
                .ToList();

            if (distances.Count < 2)
            {
                ShowError("Error_NeedTwoDistances", 2000);
                return;
            }

            _engine.Points.Clear();
            foreach (var d in distances)
                _engine.Points.Add(new CalibrationPoint { Distance = d, Delta = 0 });

            _currentPointIndex = 0;
            _currentMode = Mode.MultiCalibrating;
            _mouseTracker.StopAccumulating();
            UpdateStatus(GetString("Msg_StatusMultiStart", _engine.Points[0].Distance));
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            _currentMode = Mode.Idle;
            _heightDiffState = HeightDiffState.Idle;
            _mouseTracker.StopAccumulating();
            _mouseTracker.Reset();
            _engine.Reset();
            UpdateStatus(GetString("Msg_StatusReset"));
            lblErrorStats.Text = GetString("ErrorStatsDefault");
            lblMultiStatus.Text = "";
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (_engine.Points.Count == 0 && (_engine.Alpha == 0 || _engine.V2G == 0))
            {
                ShowError("Error_NoCalibData", 2000);
                return;
            }

            using (var sfd = new SaveFileDialog { Filter = "JSON 文件|*.json", DefaultExt = "json", FileName = "calibration.json" })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var data = _engine.ExportData();
                        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                        File.WriteAllText(sfd.FileName, json);
                        ShowError("Msg_CalibSaved", 2000);
                    }
                    catch (Exception ex) { ShowError($"保存失败: {ex.Message}", 3000); }
                }
            }
        }

        private void BtnLoad_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog { Filter = "JSON 文件|*.json" })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string json = File.ReadAllText(ofd.FileName);
                        var data = JsonConvert.DeserializeObject<CalibrationData>(json);
                        if (data == null) throw new Exception(GetString("Error_InvalidCalibFile"));
                        _engine.LoadFromData(data);
                        if (_settings.UseFixedMaxRange && _engine.Points.Count > 0)
                            _engine.RecalculateWithFixedRange((double)nudMaxRange.Value);
                        _currentMode = Mode.Measuring;
                        _mouseTracker.Reset();
                        _mouseTracker.StartAccumulating();
                        UpdateStatus(GetString("Msg_StatusLoadComplete", _engine.Alpha, _engine.V2G));
                        ShowError("Msg_CalibLoaded", 2000);
                    }
                    catch (Exception ex) { ShowError($"加载失败: {ex.Message}", 3000); }
                }
            }
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            if (!_settings.UseFixedMaxRange)
            {
                ShowError("Error_FixedRangeNotChecked", 2000);
                return;
            }
            if (_engine.Points.Count == 0)
            {
                ShowError("Error_NoPointsToRefresh", 2000);
                return;
            }
            try
            {
                _engine.RecalculateWithFixedRange((double)nudMaxRange.Value);
                UpdateStatus(GetString("Msg_StatusRecalcComplete", _engine.Alpha, _engine.V2G));
            }
            catch (Exception ex) { ShowError(ex.Message, 3000); }
        }

        private void BtnSettings_Click(object sender, EventArgs e)
        {
            using (var form = new SettingForm(_settings, this))
            {
                form.ShowDialog(this);
                // 热键变化后重新注册
                _hotKeyManager.Dispose();
                // 重新创建 HotKeyManager 以应用新设置
                var newManager = new HotKeyManager(this.Handle, _settings);
                // 复制事件订阅
                newManager.B1Pressed += OnB1;
                newManager.B2Pressed += OnB2;
                newManager.ToggleVisiblePressed += ToggleMainFormVisibility;
                newManager.ToggleOverlayPressed += () => _overlay?.Invoke(new Action(() => _overlay.Visible = !_overlay.Visible));
                newManager.StartMultiPressed += () => BtnStartMulti_Click(null, null);
                newManager.SetBaselinePressed += OnSetBaseline;
                newManager.CancelBaselinePressed += OnCancelBaseline;
                // 替换旧管理器
                var old = _hotKeyManager;
                _hotKeyManager = newManager;
                old.Dispose();
                _hotKeyManager.ReRegister();

                ShowError("Msg_HotkeyUpdated", 2000);
            }
        }

        private void BtnLanguage_Click(object sender, EventArgs e)
        {
            _currentLanguage = _currentLanguage == Language.Chinese ? Language.English : Language.Chinese;
            ApplyLanguage();           // 更新主窗体界面
            _overlay?.ApplyLanguage(); // 更新覆盖窗口界面
        }

        private void ChkEnableHeightDiff_CheckedChanged(object sender, EventArgs e)
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
                _mouseTracker.StopAccumulating();
                _mouseTracker.Reset();
            }
        }

        private void NudSensitivity_ValueChanged(object sender, EventArgs e)
        {
            double newSensitivity = (double)nudSensitivity.Value;
            if ((_engine.Points.Count > 0 || _engine.Alpha != 0) && _currentMode != Mode.Idle)
            {
                var result = MessageBox.Show(
                    GetString("SensitivityConfirmMessage"),
                    GetString("SensitivityConfirmTitle"),
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                if (result == DialogResult.Yes)
                {
                    BtnReset_Click(null, EventArgs.Empty);
                    _settings.Sensitivity = newSensitivity;
                    _mouseTracker.SetSensitivity(newSensitivity);
                }
                else
                {
                    nudSensitivity.ValueChanged -= NudSensitivity_ValueChanged;
                    nudSensitivity.Value = (decimal)_settings.Sensitivity;
                    nudSensitivity.ValueChanged += NudSensitivity_ValueChanged;
                }
            }
            else
            {
                _settings.Sensitivity = newSensitivity;
                _mouseTracker.SetSensitivity(newSensitivity);
            }
        }

        private void ChkUseBuiltin_CheckedChanged(object sender, EventArgs e)
        {
            _settings.UseFixedMaxRange = chkUseBuiltin.Checked;
            btnRefresh.Enabled = chkUseBuiltin.Checked;
            if (chkUseBuiltin.Checked)
                lblWarning.Visible = false;
            else
            {
                lblWarning.Text = GetString("WarningNoFixed");
                lblWarning.ForeColor = Color.Red;
                lblWarning.Visible = true;
            }
        }
        #endregion

        #region 辅助方法
        private void UpdateStatus(string text)
        {
            if (InvokeRequired) { BeginInvoke(new Action<string>(UpdateStatus), text); return; }
            lblStatus.Text = text;
        }

        private void ShowError(string key, int timeoutMs = 3000, params object[] args)
        {
            string msg = GetString(key, args);
            if (InvokeRequired) { BeginInvoke(new Action(() => ShowErrorMessage(msg, timeoutMs))); return; }
            ShowErrorMessage(msg, timeoutMs);
        }

        private void ShowErrorMessage(string message, int timeoutMs)
        {
            lblError.Text = message;
            if (timeoutMs > 0)
                System.Threading.Tasks.Task.Delay(timeoutMs).ContinueWith(_ => { if (!IsDisposed) BeginInvoke(new Action(() => lblError.Text = "")); });
        }

        private void UpdateErrorStats()
        {
            if (_engine.Points.Count == 0 || _engine.Alpha == 0 || _engine.V2G == 0)
            {
                lblErrorStats.Text = GetString("ErrorStatsDefault");
                return;
            }

            double totalRel = 0, maxRel = 0, maxAbs = 0, maxAbsDist = 0;
            foreach (var p in _engine.Points)
            {
                double pred = _engine.V2G * Math.Sin(2 * _engine.Alpha * p.Delta);
                double absErr = Math.Abs(pred - p.Distance);
                double relErr = absErr / p.Distance;
                totalRel += relErr;
                if (relErr > maxRel) maxRel = relErr;
                if (absErr > maxAbs) { maxAbs = absErr; maxAbsDist = p.Distance; }
            }
            double avgRel = totalRel / _engine.Points.Count;
            lblErrorStats.Text = GetString("MultiErrorRatio", avgRel, maxRel, maxAbs, maxAbsDist);
        }

        private void UpdateAbsoluteErrorStats()
        {
            var validPoints = _engine.Points.Where(p => p.Delta > 0).ToList();
            if (validPoints.Count == 0 || _engine.Alpha == 0 || _engine.V2G == 0)
            {
                lblMultiStatus.Text = "";
                return;
            }
            double totalAbs = 0, maxAbs = 0;
            foreach (var p in validPoints)
            {
                double pred = _engine.V2G * Math.Sin(2 * _engine.Alpha * p.Delta);
                double absErr = Math.Abs(pred - p.Distance);
                totalAbs += absErr;
                if (absErr > maxAbs) maxAbs = absErr;
            }
            double avgAbs = totalAbs / validPoints.Count;
            lblMultiStatus.Text = GetString("MultiErrorFormat", avgAbs, maxAbs);
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

        private bool IsOnScreen(Point location)
        {
            foreach (var screen in Screen.AllScreens)
            {
                if (screen.WorkingArea.Contains(location))
                    return true;
            }
            return false;
        }

        #endregion


        #region 窗体事件
        protected override void WndProc(ref Message m)
        {
            const int WM_INPUT = 0x00FF;
            const int WM_HOTKEY = 0x0312;
            if (m.Msg == WM_INPUT)
            {
                _mouseTracker.ProcessRawInput(m.LParam);
                return;
            }
            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                _hotKeyManager.ProcessHotKey(id);
                return;
            }
            base.WndProc(ref m);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // 保存主窗体位置（仅当窗口处于正常状态时）
            if (this.WindowState == FormWindowState.Normal)
            {
                _settings.MainFormLocation = this.Location;
            }
            else if (this.WindowState == FormWindowState.Maximized)
            {
                // 如果最大化，则保存之前正常状态下的位置（RestoreBounds）
                _settings.MainFormLocation = this.RestoreBounds.Location;
            }
            // 注意：如果是最小化，通常不保存位置（或者可以忽略）
            
            _settings.Save();

            // ... 原有的资源释放代码
            _mouseTracker.Dispose();
            _hotKeyManager.Dispose();
            _overlay?.Close();
            notifyIcon.Visible = false;
            base.OnFormClosing(e);
        }
        #endregion

        #region 窗口排版
        private void InitializeComponent()
        {
            BackColor = Color.FromArgb(30, 30, 30);
            ForeColor = Color.FromArgb(240, 240, 240);
            Text = "筒子校准测距";
            Size = new Size(300, 470);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            // 设置窗体图标（显示在标题栏左侧）
            string iconPath = Path.Combine(Application.StartupPath, "rsc", "icon.ico");
            if (File.Exists(iconPath))
            {
                this.Icon = new Icon(iconPath);
            }

            int yPos = 20;

            lblMaxRange = new Label
            {
                Name = "lblMaxRange",
                Text = "最大射程 (米):",
                Location = new Point(10, yPos),
                AutoSize = true,
                ForeColor = Color.White
            };
            Controls.Add(lblMaxRange);

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
            Controls.Add(nudMaxRange);

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

            btnRefresh = CreateStyledButton("刷新", 220, yPos - 2, 50, BtnRefresh_Click);
            Controls.Add(btnRefresh);

            yPos += 30;

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

            btnLanguage = CreateStyledButton("English", 210, yPos - 2, 60, BtnLanguage_Click);
            btnLanguage.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            Controls.Add(btnLanguage);
            yPos += 25;

            lblWarning = new Label
            {
                Location = new Point(10, yPos),
                Size = new Size(260, 20),
                ForeColor = Color.Red,
                Visible = false
            };
            Controls.Add(lblWarning);
            yPos += 25;

            lblDistances = new Label
            {
                Name = "lblDistances",
                Text = "距离列表\n(每行一个):",
                Location = new Point(10, yPos),
                AutoSize = true,
                ForeColor = Color.White
            };
            Controls.Add(lblDistances);

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

            notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Visible = true,
                Text = "筒子校准测距"
            };
            notifyIcon.Click += (s, e) => { Show(); WindowState = FormWindowState.Normal; };
            Resize += (s, e) => { if (WindowState == FormWindowState.Minimized) Hide(); };
        }
        #endregion
    }
}