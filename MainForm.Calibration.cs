using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;


namespace HLL_ATassistant
{
    public partial class MainForm
    {
        private void FitMultiPoints()
        {
            var points = _calibrationPoints.Where(p => p.Delta > 0).ToList();
            if (points.Count < 2 && !chkUseBuiltin.Checked)
            {
                ShowError("Error_NeedTwoValidPoints");
                return;
            }

            double maxDelta = points.Count > 0 ? points.Max(p => p.Delta) : 1;
            double alphaMax = Math.PI / (2.0 * maxDelta) * 0.99;
            int steps = 2000;
            double bestAlpha = 0, bestV2G = 0, bestError = double.MaxValue;
            bool useFixedC = chkUseBuiltin.Checked;
            double fixedMaxRange = (double)nudMaxRange.Value;

            for (int i = 1; i <= steps; i++)
            {
                double alpha = alphaMax * i / steps;
                bool valid = true;
                double error = 0;

                if (useFixedC)
                {
                    if (points.Count == 0)
                    {
                        ShowError("Error_NeedAtLeastOnePointWithFixed");
                        return;
                    }
                    foreach (var p in points)
                    {
                        double arg = 2.0 * alpha * p.Delta;
                        if (arg <= 0 || arg >= Math.PI / 2.0) { valid = false; break; }
                        double pred = fixedMaxRange * Math.Sin(arg);
                        error += (pred - p.Distance) * (pred - p.Distance);
                    }
                    if (!valid) continue;
                    if (error < bestError)
                    {
                        bestError = error;
                        bestAlpha = alpha;
                        bestV2G = fixedMaxRange;
                    }
                }
                else
                {
                    double sumDSin = 0, sumSin2 = 0;
                    foreach (var p in points)
                    {
                        double arg = 2.0 * alpha * p.Delta;
                        if (arg <= 0 || arg >= Math.PI / 2.0) { valid = false; break; }
                        double sinVal = Math.Sin(arg);
                        sumDSin += p.Distance * sinVal;
                        sumSin2 += sinVal * sinVal;
                    }
                    if (!valid || sumSin2 == 0) continue;
                    double v2g = sumDSin / sumSin2;
                    foreach (var p in points)
                    {
                        double arg = 2.0 * alpha * p.Delta;
                        double pred = v2g * Math.Sin(arg);
                        error += (pred - p.Distance) * (pred - p.Distance);
                    }
                    if (error < bestError)
                    {
                        bestError = error;
                        bestAlpha = alpha;
                        bestV2G = v2g;
                    }
                }
            }

            if (bestAlpha == 0)
            {
                ShowError("Error_NoFitParams");
                _currentMode = Mode.Idle;
                return;
            }

            _alpha = bestAlpha;
            _v2g = bestV2G;
            UpdateVFromV2G();
            _currentMode = Mode.Measuring;
            lock (_deltaLock)
            {
                _currentDelta = 0;
                // _smoothedDelta = 0;
            }
            _isAccumulating = true;
            ResetMousePosition();

            _maxDeltaLimit = useFixedC ? Math.Round(Math.PI / (4 * _alpha), 1) : points.Max(p => p.Delta);

            UpdateAbsoluteErrorStats();
            UpdateStatus(GetString("Msg_StatusCalibComplete", _alpha, _v2g));
            UpdateErrorDisplay();
            overlay.SetCalibration(_alpha, _v2g);
        }

        private void RecalculateWithFixedC()
        {
            if (!chkUseBuiltin.Checked) return;
            if (_calibrationPoints.Count == 0)
            {
                ShowError("Error_NoPoints");
                return;
            }

            double fixedMaxRange = (double)nudMaxRange.Value;
            double maxDelta = _calibrationPoints.Max(p => p.Delta);
            double alphaMax = Math.PI / (2.0 * maxDelta) * 0.99;
            int steps = 2000;
            double bestAlpha = 0, bestError = double.MaxValue;

            for (int i = 1; i <= steps; i++)
            {
                double alpha = alphaMax * i / steps;
                bool valid = true;
                double error = 0;
                foreach (var p in _calibrationPoints)
                {
                    double arg = 2.0 * alpha * p.Delta;
                    if (arg <= 0 || arg >= Math.PI / 2.0) { valid = false; break; }
                    double pred = fixedMaxRange * Math.Sin(arg);
                    error += (pred - p.Distance) * (pred - p.Distance);
                }
                if (!valid) continue;
                if (error < bestError)
                {
                    bestError = error;
                    bestAlpha = alpha;
                }
            }

            if (bestAlpha == 0)
            {
                ShowError("Error_FitFail");
                return;
            }

            _alpha = bestAlpha;
            _v2g = fixedMaxRange;
            UpdateVFromV2G();
            _maxDeltaLimit = Math.Round(Math.PI / (4 * _alpha), 1);
            lock (_deltaLock)
            {
                _currentDelta = 0;
                // _smoothedDelta = 0;
            }
            _isAccumulating = true;
            ResetMousePosition();
            UpdateStatus(GetString("Msg_StatusRecalcComplete", _alpha, _v2g));
            UpdateErrorDisplay();
            overlay.SetCalibration(_alpha, _v2g);
            UpdateAbsoluteErrorStats();
        }

        private void UpdateErrorDisplay()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(UpdateErrorDisplay));
                return;
            }

            if (_calibrationPoints.Count == 0 || _alpha == 0 || _v2g == 0)
            {
                lblErrorStats.Text = _currentLanguage == Language.Chinese ? "误差统计: 无有效标定点" : "Error stats: No valid points";
                return;
            }

            double totalRelError = 0, maxRelError = 0, maxAbsError = 0, maxAbsErrorDist = 0;
            foreach (var p in _calibrationPoints)
            {
                double arg = 2.0 * _alpha * p.Delta;
                double pred = _v2g * Math.Sin(arg);
                double absError = Math.Abs(pred - p.Distance);
                double relError = absError / p.Distance;

                totalRelError += relError;
                if (relError > maxRelError) maxRelError = relError;
                if (absError > maxAbsError)
                {
                    maxAbsError = absError;
                    maxAbsErrorDist = p.Distance;
                }
            }

            double avgRelError = totalRelError / _calibrationPoints.Count;
            lblErrorStats.Text = string.Format(GetString("MultiErrorRatio"), avgRelError, maxRelError, maxAbsError, maxAbsErrorDist);
        }

        private void UpdateAbsoluteErrorStats()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(UpdateAbsoluteErrorStats));
                return;
            }

            var points = _calibrationPoints.Where(p => p.Delta > 0).ToList();
            if (points.Count == 0 || _alpha == 0 || _v2g == 0)
            {
                lblMultiStatus.Text = "";
                return;
            }

            double totalAbsError = 0, maxAbsError = 0;
            foreach (var p in points)
            {
                double pred = _v2g * Math.Sin(2 * _alpha * p.Delta);
                double absError = Math.Abs(pred - p.Distance);
                totalAbsError += absError;
                if (absError > maxAbsError) maxAbsError = absError;
            }
            double avgAbsError = totalAbsError / points.Count;

            lblMultiStatus.Text = GetString("MultiErrorFormat", avgAbsError, maxAbsError);
        }
    }
}