using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace HLL_ATassistant
{
    public class OverlayForm : Form
    {
        private readonly MainForm _mainForm;
        private Label lblDisplay;

        public OverlayForm(MainForm mainForm)
        {
            _mainForm = mainForm;
            InitializeComponent();

            // 订阅事件，实现实时刷新
            _mainForm.DeltaChanged += (s, delta) => UpdateDisplay();
            _mainForm.Settings.PropertyChanged += (s, e) => UpdateDisplay();
            _mainForm.Engine.CalibrationChanged += () => UpdateDisplay();

            // 初始刷新一次
            UpdateDisplay();
        }

        public void RefreshDisplay() => UpdateDisplay();
        public void ApplyLanguage() => UpdateDisplay();

        private void UpdateDisplay()
        {
            if (_mainForm == null) return;

            double delta = _mainForm.CurrentDelta;
            double maxDelta = _mainForm.MaxCalibratedDelta;

            if (_mainForm.Alpha != 0 && _mainForm.V2G != 0)
            {
                if (delta <= 0)
                {
                    lblDisplay.Text = _mainForm.GetString("DisplayNoMovement", null);
                    return;
                }
                if (delta > maxDelta)
                {
                    lblDisplay.Text = string.Format(_mainForm.GetString("DisplayOutofCali", null), maxDelta);
                    return;
                }

                double thetaRad = _mainForm.Alpha * delta;
                double twoTheta = 2 * thetaRad;
                if (twoTheta <= 0 || twoTheta >= Math.PI)
                {
                    lblDisplay.Text = _mainForm.GetString("DisplayOutofAngle", null);
                    return;
                }

                double D = _mainForm.V2G * Math.Sin(twoTheta);
                double thetaDeg = thetaRad * 180 / Math.PI;
                double v = _mainForm.Velocity;

                List<string> lines = new List<string>();

                // 普通模式
                if (!(_mainForm.IsHeightDiffModeEnabled && _mainForm.IsHeightDiffBaselineSet))
                {
                    // 第一行
                    if (_mainForm.ShowDistance && _mainForm.ShowAngle)
                        lines.Add($"D: {D:F1}m  θ: {thetaDeg:F1}°");
                    else if (_mainForm.ShowDistance)
                        lines.Add($"D: {D:F1}m");
                    else if (_mainForm.ShowAngle)
                        lines.Add($"θ: {thetaDeg:F1}°");

                    // 第二行
                    if (_mainForm.ShowTime || _mainForm.ShowVx)
                    {
                        double vx = v * Math.Cos(thetaRad);
                        string secondLine = "";
                        if (_mainForm.ShowTime)
                            secondLine += (Math.Abs(vx) > 1e-6) ? $"t: {D / vx:F2}s" : "t: ---";
                        if (_mainForm.ShowVx)
                        {
                            if (secondLine.Length > 0) secondLine += "  ";
                            secondLine += $"vx: {vx:F1}m/s";
                        }
                        lines.Add(secondLine);
                    }
                }
                else  // 高低差模式
                {
                    double delta0 = _mainForm.HeightDiffDelta0;
                    double X = _mainForm.V2G * Math.Sin(2 * _mainForm.Alpha * (delta0 + delta));
                    double cosDelta0 = Math.Cos(_mainForm.Alpha * delta0);
                    double sinDelta1 = Math.Sin(_mainForm.Alpha * delta);
                    double L = 2 * _mainForm.V2G * sinDelta1 / (cosDelta0 * cosDelta0);
                    if (double.IsInfinity(L) || double.IsNaN(L)) L = 0;

                    double betaDeg = _mainForm.BetaAngle * 180 / Math.PI;
                    double thetaEffective = thetaRad + _mainForm.BetaAngle;
                    double vx = v * Math.Cos(thetaEffective);

                    // 第一行
                    if (_mainForm.ShowDistance && _mainForm.ShowAngle)
                        lines.Add($"D: {D:F1}m  θ: {thetaDeg:F1}°");
                    else if (_mainForm.ShowDistance)
                        lines.Add($"D: {D:F1}m");
                    else if (_mainForm.ShowAngle)
                        lines.Add($"θ: {thetaDeg:F1}°");

                    // 第二行
                    List<string> secondRow = new List<string>();
                    if (_mainForm.ShowL) secondRow.Add($"L: {L:F1}m");
                    if (_mainForm.ShowBeta) secondRow.Add($"β: {betaDeg:F1}°");
                    if (_mainForm.ShowX) secondRow.Add($"X: {X:F1}m");
                    if (secondRow.Count > 0) lines.Add(string.Join("  ", secondRow));

                    // 第三行
                    List<string> thirdRow = new List<string>();
                    if (_mainForm.ShowTPrime)
                        thirdRow.Add((Math.Abs(vx) > 1e-6) ? $"t': {L / vx:F2}s" : "t': ---");
                    if (_mainForm.ShowTime)
                    {
                        double vxNormal = v * Math.Cos(thetaRad);
                        thirdRow.Add((Math.Abs(vxNormal) > 1e-6) ? $"t: {D / vxNormal:F2}s" : "t: ---");
                    }
                    if (_mainForm.ShowVx)
                        thirdRow.Add($"vx: {vx:F1}m/s");
                    if (thirdRow.Count > 0) lines.Add(string.Join("  ", thirdRow));
                }

                lblDisplay.Text = string.Join("\n", lines);
            }
            else
            {
                lblDisplay.Text = _mainForm.GetString("DisplayWaitforCali", null);
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