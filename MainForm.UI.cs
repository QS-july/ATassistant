using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace HLL_ATassistant
{
    public partial class MainForm
    {
        private void BtnSettings_Click(object? sender, EventArgs e)
        {
            using (var form = new SettingForm(_hotKeySettings, this))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    // 保存热键设置
                    _hotKeySettings.B1Key = form.B1Key;
                    _hotKeySettings.B1Modifiers = form.B1Modifiers;
                    _hotKeySettings.B2Key = form.B2Key;
                    _hotKeySettings.B2Modifiers = form.B2Modifiers;

                    _hotKeySettings.ToggleVisibleKey = form.ToggleVisibleKey;
                    _hotKeySettings.ToggleVisibleModifiers = form.ToggleVisibleModifiers;
                    _hotKeySettings.ToggleOverlayKey = form.ToggleOverlayKey;
                    _hotKeySettings.ToggleOverlayModifiers = form.ToggleOverlayModifiers;

                    _hotKeySettings.StartMultiKey = form.StartMultiKey;
                    _hotKeySettings.StartMultiModifiers = form.StartMultiModifiers;

                    _hotKeySettings.SetBaselineKey = form.SetBaselineKey;
                    _hotKeySettings.SetBaselineModifiers = form.SetBaselineModifiers;
                    _hotKeySettings.CancelBaselineKey = form.CancelBaselineKey;
                    _hotKeySettings.CancelBaselineModifiers = form.CancelBaselineModifiers;
                    _hotKeySettings.SameKeyForBaseline = form.SameKeyForBaseline;

                    _hotKeySettings.ShowDistance = form.ShowDistance;
                    _hotKeySettings.ShowAngle = form.ShowAngle;
                    _hotKeySettings.ShowL = form.ShowL;
                    _hotKeySettings.ShowX = form.ShowX;
                    _hotKeySettings.ShowBeta = form.ShowBeta;
                    _hotKeySettings.ShowVx = form.ShowVx;
                    _hotKeySettings.ShowTPrime = form.ShowTPrime;

                    // 更新本地变量（高低差测量模式）
                    _heightDiffSetBaselineKey = form.SetBaselineKey;
                    _heightDiffSetBaselineModifiers = form.SetBaselineModifiers;
                    _heightDiffCancelBaselineKey = form.CancelBaselineKey;
                    _heightDiffCancelBaselineModifiers = form.CancelBaselineModifiers;
                    _heightDiffSameKey = form.SameKeyForBaseline;

                    _hotKeySettings.Save();
                    RegisterAllHotKeys();
                    ShowError("Msg_HotkeyUpdated", 2000);
                }
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (_calibrationPoints.Count == 0 && (_alpha == 0 || _v2g == 0))
            {
                ShowError("Error_NoCalibData");
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "JSON 文件|*.json|所有文件|*.*";
                sfd.DefaultExt = "json";
                sfd.FileName = "calibration.json";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var data = new CalibrationData
                        {
                            Points = _calibrationPoints.Select(p => new CalibrationPointData { Delta = p.Delta, Distance = p.Distance }).ToList(),
                            Alpha = _alpha,
                            V2G = _v2g,
                            V = _v,
                            G = _g,
                            MaxDeltaLimit = _maxDeltaLimit
                        };
                        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                        System.IO.File.WriteAllText(sfd.FileName, json);
                        ShowError("Msg_CalibSaved", 2000);
                    }
                    catch (Exception ex)
                    {
                        ShowError("Error_SaveFail", 3000, ex.Message);
                    }
                }
            }
        }

        private void BtnLoad_Click(object? sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "JSON 文件|*.json|所有文件|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string json = System.IO.File.ReadAllText(ofd.FileName);
                        var data = JsonConvert.DeserializeObject<CalibrationData>(json);
                        if (data == null) throw new Exception(GetString("Error_InvalidCalibFile"));

                        _calibrationPoints = data.Points.Select(p => new CalibrationPoint { Delta = p.Delta, Distance = p.Distance }).ToList();
                        _alpha = data.Alpha;
                        _v2g = data.V2G;
                        _g = data.G != 0 ? data.G : 9.8;
                        _v = data.V != 0 ? data.V : (_v2g > 0 ? Math.Sqrt(_v2g * _g) : 0);
                        _maxDeltaLimit = data.MaxDeltaLimit;

                        if (_maxDeltaLimit == 0 && _calibrationPoints.Count > 0)
                            _maxDeltaLimit = _calibrationPoints.Max(p => p.Delta);
                        else if (_maxDeltaLimit == 0 && _alpha != 0)
                            _maxDeltaLimit = Math.PI / (4 * _alpha);

                        if (chkUseBuiltin.Checked && _calibrationPoints.Count > 0)
                            RecalculateWithFixedC();

                        _currentMode = Mode.Measuring;
                        lock (_deltaLock)
                        {
                            _currentDelta = 0;
                            // _smoothedDelta = 0;
                        }
                        _isAccumulating = true;
                        ResetMousePosition();
                        UpdateStatus(GetString("Msg_StatusLoadComplete", _alpha, _v2g));
                        ShowError("Msg_CalibLoaded", 5000);
                        UpdateErrorDisplay();
                        overlay.SetCalibration(_alpha, _v2g);
                        UpdateAbsoluteErrorStats();
                    }
                    catch (Exception ex)
                    {
                        ShowError("Error_LoadFail", 3000, ex.Message);
                    }
                }
            }
        }

        private void BtnStartMulti_Click(object? sender, EventArgs e)
        {
            string[] lines = txtDistances.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var distances = new List<double>();
            foreach (var line in lines)
            {
                if (double.TryParse(line.Trim(), out double d))
                    distances.Add(d);
                else
                {
                    ShowError("Error_InvalidDistance", 3000, line);
                    return;
                }
            }
            if (distances.Count < 2)
            {
                ShowError("Error_NeedTwoDistances");
                return;
            }

            _calibrationPoints = distances.Select(d => new CalibrationPoint { Distance = d }).ToList();
            _currentPointIndex = 0;
            _currentMode = Mode.MultiCalibrating;
            UpdateStatus(GetString("Msg_StatusMultiStart", _calibrationPoints[0].Distance));
        }

        private void BtnReset_Click(object? sender, EventArgs e)
        {
            _currentMode = Mode.Idle;
            _heightDiffState = HeightDiffState.Idle;
            lock (_deltaLock)
            {
                _currentDelta = 0;
                // _smoothedDelta = 0;
            }
            _isAccumulating = false;
            _maxDeltaLimit = 0;
            _alpha = 0;
            _v2g = 0;
            _v = 0;
            UpdateStatus(GetString("Msg_StatusReset"));
            lblErrorStats.Text = _currentLanguage == Language.Chinese ? "误差统计: 无数据" : "Error stats: No data";
            lblMultiStatus.Text = "";
        }

        private void BtnRefresh_Click(object? sender, EventArgs e)
        {
            if (!chkUseBuiltin.Checked)
            {
                ShowError("Error_FixedRangeNotChecked");
                return;
            }
            if (_calibrationPoints.Count == 0)
            {
                ShowError("Error_NoPointsToRefresh");
                return;
            }
            RecalculateWithFixedC();
        }

        private void ChkUseBuiltin_CheckedChanged(object? sender, EventArgs e)
        {
            btnRefresh.Enabled = chkUseBuiltin.Checked;
            if (chkUseBuiltin.Checked)
            {
                lblWarning.Visible = false;
            }
            else
            {
                lblWarning.Text = GetString("WarningNoFixed");
                lblWarning.ForeColor = Color.Red;
                lblWarning.Visible = true;
            }
        }

        private void NudSensitivity_ValueChanged(object? sender, EventArgs e)
        {
            var nud = sender as NumericUpDown;
            if (nud == null) return;

            double newSensitivity = (double)nud.Value;

            if ((_calibrationPoints.Count > 0 || _alpha != 0 || _v2g != 0) && _currentMode != Mode.Idle)
            {
                var result = MessageBox.Show(
                    GetString("SensitivityConfirmMessage"),
                    GetString("SensitivityConfirmTitle"),
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    BtnReset_Click(null, EventArgs.Empty);
                    _sensitivity = newSensitivity;
                }
                else
                {
                    nud.ValueChanged -= NudSensitivity_ValueChanged;
                    nud.Value = (decimal)_sensitivity;
                    nud.ValueChanged += NudSensitivity_ValueChanged;
                }
            }
            else
            {
                _sensitivity = newSensitivity;
            }
        }

        // private void NudSmoothFactor_ValueChanged(object? sender, EventArgs e)
        // {
        //     _smoothFactor = (double)((NumericUpDown)sender).Value;
        // }

        // 最大射程文字更新
        // private void NudMaxRange_ValueChanged(object? sender, EventArgs e)
        // {
        //     UpdateUseFixedRangeText();
        // }

        private void BtnLanguage_Click(object? sender, EventArgs e)
        {
            _currentLanguage = _currentLanguage == Language.Chinese ? Language.English : Language.Chinese;
            ApplyLanguage();
            overlay.ApplyLanguage();
        }
    }
}