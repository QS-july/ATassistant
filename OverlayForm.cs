using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace HLL_ATassistant
{
    public partial class OverlayForm : Form
    {
        private readonly CalibrationEngine _engine;
        private readonly AppSettings _settings;
        private readonly MainForm _mainForm;
        private readonly HotKeyManager _hotKeyManager;
        private Label lblDisplay;

        private MouseDeltaTracker MouseTracker => MouseDeltaTracker.Instance;

        // 语言字段，使用 MainForm.Language 类型
        private MainForm.Language _currentLanguage;

        public OverlayForm(CalibrationEngine engine, AppSettings settings, MainForm mainForm, HotKeyManager hotKeyManager)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _mainForm = mainForm ?? throw new ArgumentNullException(nameof(mainForm));
            _hotKeyManager = hotKeyManager ?? throw new ArgumentNullException(nameof(hotKeyManager));

            InitializeComponent();

            // 初始化语言（与主窗体同步）
            _currentLanguage = _mainForm.GetCurrentLanguage();

            // 订阅事件
            MouseTracker.DisplacementChanged += (theta, beta) => UpdateDisplay();
            _engine.CalibrationChanged += UpdateDisplay;
            _settings.PropertyChanged += (s, e) => UpdateDisplay();
            _hotKeyManager.OnBaselineSetChanged += (set) => UpdateDisplay();

            UpdateDisplay();
        }

        public void RefreshDisplay() => UpdateDisplay();

        private void UpdateDisplay()
        {
            if (IsDisposed) return;

            double delta = MouseTracker.DeltaTheta;
            double maxDelta = _engine.MaxDeltaLimit;
            double alpha = _engine.Alpha;
            double v2g = _engine.V2G;
            double v = _engine.Velocity;

            bool heightDiffMode = _mainForm.EnableHeightDiffMode;
            bool baselineSet = _hotKeyManager.HeightDiffBaselineSet;

            if (alpha != 0 && v2g != 0)
            {
                if (delta <= 0)
                {
                    lblDisplay.Text = GetString("DisplayNoMovement");
                    return;
                }
                if (delta > maxDelta)
                {
                    lblDisplay.Text = GetString("DisplayOutofCali", maxDelta);
                    return;
                }

                double thetaRad = alpha * delta;
                double twoTheta = 2 * thetaRad;
                if (twoTheta <= 0 || twoTheta >= Math.PI)
                {
                    lblDisplay.Text = GetString("DisplayOutofAngle");
                    return;
                }

                double D = v2g * Math.Sin(twoTheta);
                double thetaDeg = thetaRad * 180 / Math.PI;

                List<string> lines = new List<string>();

                // 普通模式
                if (!(heightDiffMode && baselineSet))
                {
                    // 第一行
                    if (_settings.ShowDistance && _settings.ShowAngle)
                        lines.Add($"D: {D:F1}m  θ: {thetaDeg:F1}°");
                    else if (_settings.ShowDistance)
                        lines.Add($"D: {D:F1}m");
                    else if (_settings.ShowAngle)
                        lines.Add($"θ: {thetaDeg:F1}°");

                    // 第二行
                    if (_settings.ShowTime || _settings.ShowVx)
                    {
                        double vx = v * Math.Cos(thetaRad);
                        string secondLine = "";
                        if (_settings.ShowTime)
                            secondLine += (Math.Abs(vx) > 1e-6) ? $"t: {D / vx:F2}s" : "t: ---";
                        if (_settings.ShowVx)
                        {
                            if (secondLine.Length > 0) secondLine += "  ";
                            secondLine += $"vx: {vx:F1}m/s";
                        }
                        lines.Add(secondLine);
                    }
                }
                else  // 高低差模式
                {
                    double delta0 = MouseTracker.DeltaBeta;
                    double X = v2g * Math.Sin(2 * alpha * (delta0 + delta));
                    double cosDelta0 = Math.Cos(alpha * delta0);
                    double sinDelta1 = Math.Sin(alpha * delta);
                    double L = 2 * v2g * sinDelta1 / (cosDelta0 * cosDelta0);
                    if (double.IsInfinity(L) || double.IsNaN(L)) L = 0;

                    double betaDeg = (alpha * delta0) * 180 / Math.PI;
                    double thetaEffective = thetaRad + alpha * delta0;
                    double vx = v * Math.Cos(thetaEffective);

                    // 第一行
                    if (_settings.ShowDistance && _settings.ShowAngle)
                        lines.Add($"D: {D:F1}m  θ: {thetaDeg:F1}°");
                    else if (_settings.ShowDistance)
                        lines.Add($"D: {D:F1}m");
                    else if (_settings.ShowAngle)
                        lines.Add($"θ: {thetaDeg:F1}°");

                    // 第二行
                    List<string> secondRow = new List<string>();
                    if (_settings.ShowL) secondRow.Add($"L: {L:F1}m");
                    if (_settings.ShowBeta) secondRow.Add($"β: {betaDeg:F1}°");
                    if (_settings.ShowX) secondRow.Add($"X: {X:F1}m");
                    if (secondRow.Count > 0) lines.Add(string.Join("  ", secondRow));

                    // 第三行
                    List<string> thirdRow = new List<string>();
                    if (_settings.ShowTPrime)
                        thirdRow.Add((Math.Abs(vx) > 1e-6) ? $"t': {L / vx:F2}s" : "t': ---");
                    if (_settings.ShowTime)
                    {
                        double vxNormal = v * Math.Cos(thetaRad);
                        thirdRow.Add((Math.Abs(vxNormal) > 1e-6) ? $"t: {D / vxNormal:F2}s" : "t: ---");
                    }
                    if (_settings.ShowVx)
                        thirdRow.Add($"vx: {vx:F1}m/s");
                    if (thirdRow.Count > 0) lines.Add(string.Join("  ", thirdRow));
                }

                lblDisplay.Text = string.Join("\n", lines);
            }
            else
            {
                lblDisplay.Text = GetString("DisplayWaitforCali");
            }
        }

        private void InitializeComponent()
        {
            FormBorderStyle = FormBorderStyle.None;
            TopMost = true;
            ShowInTaskbar = false;
            BackColor = Color.Magenta;
            TransparencyKey = Color.Magenta;
            Size = new Size(300, 180);
            StartPosition = FormStartPosition.Manual;

            lblDisplay = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("微软雅黑", 14, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };
            Controls.Add(lblDisplay);

            Load += (s, e) =>
            {
                var screen = Screen.PrimaryScreen.WorkingArea;
                Location = new Point(screen.Width / 2 + Width / 2, screen.Height / 2);
            };
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x00000020; // WS_EX_TRANSPARENT
                cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
                return cp;
            }
        }
    }
}
