using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace HLL_ATassistant
{
    public class OverlayForm : Form
    {
        private MainForm _mainForm;
        private Label lblDisplay;
        private System.Windows.Forms.Timer timer;
        private double? _alpha, _v2g;

        public OverlayForm(MainForm mainForm)
        {
            _mainForm = mainForm;
            InitializeComponent();
        }

        public void SetCalibration(double alpha, double v2g)
        {
            _alpha = alpha;
            _v2g = v2g;
            UpdateDisplay();
        }

        public void ApplyLanguage()
        {
            UpdateDisplay(); // 刷新显示，使状态信息语言更新
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (_mainForm == null) return;

            // double delta = _mainForm.SmoothedDelta;
            double delta = _mainForm.CurrentDelta;
            double maxDelta = _mainForm.MaxCalibratedDelta;

            if (_alpha.HasValue && _v2g.HasValue)
            {
                if (delta <= 0)
                {
                    lblDisplay.Text = "D: ---  θ: 0°";
                    return;
                }
                if (delta > maxDelta)
                {
                    lblDisplay.Text = _mainForm.GetString("DisplayOutofCali", maxDelta);
                    return;
                }

                double thetaRad = _alpha.Value * delta;
                double twoTheta = 2 * thetaRad;
                if (twoTheta <= 0 || twoTheta >= Math.PI)
                {
                    lblDisplay.Text = _mainForm.GetString("DisplayOutofAngle");
                    return;
                }

                double D = _v2g.Value * Math.Sin(twoTheta);
                double thetaDeg = thetaRad * 180 / Math.PI;
                double v = _mainForm.Velocity;

                List<string> lines = new List<string>();

                // ===== 普通测量模式 =====
                if (!(_mainForm.IsHeightDiffModeEnabled && _mainForm.IsHeightDiffBaselineSet))
                {
                    // 第一行：D 和 θ
                    if (_mainForm.ShowDistance && _mainForm.ShowAngle)
                        lines.Add($"D: {D:F1}m  θ: {thetaDeg:F1}°");
                    else if (_mainForm.ShowDistance)
                        lines.Add($"D: {D:F1}m");
                    else if (_mainForm.ShowAngle)
                        lines.Add($"θ: {thetaDeg:F1}°");

                    // 第二行：t 和 vx（t 在前，vx 在后）
                    if (_mainForm.ShowTime || _mainForm.ShowVx)
                    {
                        double vx = v * Math.Cos(thetaRad);
                        string secondLine = "";
                        if (_mainForm.ShowTime)
                        {
                            if (Math.Abs(vx) > 1e-6)
                                secondLine += $"t: {D / vx:F2}s";
                            else
                                secondLine += "t: ---";
                        }
                        if (_mainForm.ShowVx)
                        {
                            if (!string.IsNullOrEmpty(secondLine)) secondLine += "  ";
                            secondLine += $"vx: {vx:F1}m/s";
                        }
                        lines.Add(secondLine);
                    }
                }
                else // ===== 高低差测量模式 =====
                {
                    double delta0 = _mainForm.HeightDiffDelta0;
                    double delta1 = delta;
                    double alpha = _alpha.Value;
                    double v2g = _v2g.Value;

                    // 计算 X
                    double X = v2g * Math.Sin(2 * alpha * (delta0 + delta1));
                    // 计算 L
                    double cosDelta0 = Math.Cos(alpha * delta0);
                    double sinDelta1 = Math.Sin(alpha * delta1);
                    double L = 2 * v2g * sinDelta1 / (cosDelta0 * cosDelta0);
                    if (double.IsInfinity(L) || double.IsNaN(L)) L = 0;

                    double betaDeg = _mainForm.BetaAngle * 180 / Math.PI;
                    double thetaEffective = thetaRad + _mainForm.BetaAngle; // 总射角（弧度）
                    double vx = v * Math.Cos(thetaEffective);

                    // 第一行：D 和 θ（与普通模式一致）
                    if (_mainForm.ShowDistance && _mainForm.ShowAngle)
                        lines.Add($"D: {D:F1}m  θ: {thetaDeg:F1}°");
                    else if (_mainForm.ShowDistance)
                        lines.Add($"D: {D:F1}m");
                    else if (_mainForm.ShowAngle)
                        lines.Add($"θ: {thetaDeg:F1}°");

                    // 第二行：L, β, X
                    List<string> secondRowItems = new List<string>();
                    if (_mainForm.ShowL) secondRowItems.Add($"L: {L:F1}m");
                    if (_mainForm.ShowBeta) secondRowItems.Add($"β: {betaDeg:F1}°");
                    if (_mainForm.ShowX) secondRowItems.Add($"X: {X:F1}m");
                    if (secondRowItems.Count > 0)
                        lines.Add(string.Join("  ", secondRowItems));

                    // 第三行：t', t, vx
                    List<string> thirdRowItems = new List<string>();
                    if (_mainForm.ShowTPrime)
                    {
                        if (Math.Abs(vx) > 1e-6)
                            thirdRowItems.Add($"t': {L / vx:F2}s");
                        else
                            thirdRowItems.Add("t': ---");
                    }
                    if (_mainForm.ShowTime)
                    {
                        double vxNormal = v * Math.Cos(thetaRad);
                        if (Math.Abs(vxNormal) > 1e-6)
                            thirdRowItems.Add($"t: {D / vxNormal:F2}s");
                        else
                            thirdRowItems.Add("t: ---");
                    }
                    if (_mainForm.ShowVx)
                        thirdRowItems.Add($"vx: {vx:F1}m/s");
                    if (thirdRowItems.Count > 0)
                        lines.Add(string.Join("  ", thirdRowItems));
                }

                lblDisplay.Text = string.Join("\n", lines);
            }
            else
            {
                lblDisplay.Text = _mainForm.GetString("DisplayWaitforCali");
            }
        }

        private void InitializeComponent()
        {
            FormBorderStyle = FormBorderStyle.None;
            TopMost = true;
            ShowInTaskbar = false;
            BackColor = Color.Magenta;
            TransparencyKey = Color.Magenta;
            Size = new Size(300, 180); // 增加高度以容纳三行
            StartPosition = FormStartPosition.Manual;

            lblDisplay = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("微软雅黑", 14, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                UseCompatibleTextRendering = true
            };
            Controls.Add(lblDisplay);

            Load += (s, e) =>
            {
                var screen = Screen.PrimaryScreen.WorkingArea;
                Location = new Point(screen.Width / 2 + Width / 2, screen.Height / 2);
            };

            timer = new System.Windows.Forms.Timer { Interval = 50 };
            timer.Tick += Timer_Tick;
            timer.Start();
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