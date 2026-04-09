using System.Windows.Forms;

namespace HLL_ATassistant
{
    public partial class HotKeyManager
    {
        private void OnB1()
        {
            switch (_currentMode)
            {
                case Mode.Idle:
                    _mainForm.ShowError("Error_NeedCalibFirst", 2000);
                    break;

                case Mode.MultiCalibrating:
                    // 重置位移，开始累积
                    MouseTracker.ResetTheta();
                    MouseTracker.StartAccumulating();
                    _mainForm.UpdateStatus(_mainForm.GetString("Msg_StatusMeasurePoint",
                        _mainForm.CurrentPointIndex + 1, _mainForm.Engine.Points[_mainForm.CurrentPointIndex].Distance));
                    break;

                case Mode.Measuring:
                    // 普通测量模式 或 高低差模式但未设基准点
                    MouseTracker.ResetTheta();
                    MouseTracker.StartAccumulating();
                    _mainForm.UpdateStatus(_mainForm.GetString("Msg_StatusMeasuring"));
                    break;

                case Mode.HeightDiff:
                    // 高低差模式且已设基准点
                    MouseTracker.ResetTheta();
                    MouseTracker.SaveBeta();        // 固定当前 β 值
                    MouseTracker.StartAccumulating();
                    _mainForm.UpdateStatus(_mainForm.GetString("Msg_StatusMeasuring"));
                    break;
            }
        }

        private void OnB2()
        {
            switch (_currentMode)
            {
                case Mode.MultiCalibrating:
                    if (MouseTracker.DeltaTheta > 0)
                    {
                        // 记录位移到当前标定点
                        _mainForm.Engine.Points[_mainForm.CurrentPointIndex].Delta = MouseTracker.DeltaTheta;
                        _mainForm.IncrementCurrentPointIndex();
                        if (_mainForm.CurrentPointIndex < _mainForm.Engine.Points.Count)
                        {
                            MouseTracker.Reset();
                            _mainForm.UpdateStatus(_mainForm.GetString("Msg_StatusNextPoint",
                                _mainForm.CurrentPointIndex + 1, _mainForm.Engine.Points[_mainForm.CurrentPointIndex].Distance));
                        }
                        else
                        {
                            MouseTracker.StopAccumulating();
                            _mainForm.PerformCalibration();  // 拟合多点
                        }
                    }
                    else
                    {
                        _mainForm.ShowError("请移动鼠标后再按确认键", 3000);
                        MouseTracker.Reset();
                        MouseTracker.StartAccumulating();
                    }
                    break;

                case Mode.Measuring:
                case Mode.HeightDiff:
                    MouseTracker.StopAccumulating();
                    _mainForm.UpdateStatus(_mainForm.GetString("Msg_StatusPaused"));
                    break;
            }
        }

        private void ToggleMainFormVisibility()
        {
            if (_mainForm.WindowState == FormWindowState.Minimized || !_mainForm.Visible)
            {
                _mainForm.Show();
                _mainForm.WindowState = FormWindowState.Normal;
            }
            else
            {
                _mainForm.Hide();
            }
        }

        private void ToggleOverlayVisibility()
        {
            var overlay = _mainForm.GetOverlay();
            if (overlay != null)
                overlay.Visible = !overlay.Visible;
        }

        private void OnSetBaseline()
        {
            if (!_mainForm.EnableHeightDiffMode) return;
            if (_settings.SameKeyForBaseline && HeightDiffBaselineSet)
            {
                OnCancelBaseline();
                return;
            }
            MouseTracker.ResetBeta();

            SetPauseFlags(false, false);
            HeightDiffBaselineSet = true;
            _mainForm.SetHeightDiffBaselineSet(true);
            _mainForm.UpdateStatus(_mainForm.GetString("Msg_BaselineSet"));
        }

        private void OnCancelBaseline()
        {
            if (!_mainForm.EnableHeightDiffMode) return;
            HeightDiffBaselineSet = false;
            _mainForm.SetHeightDiffBaselineSet(false);

            MouseTracker.ResetBeta();
            MouseTracker.ResetfixedBeta();

            SetPauseFlags(false, false);
            _mainForm.UpdateStatus(_mainForm.GetString("Msg_BaselineCancelled"));
        }

        private void OnPause1()
        {
            _pause1Active = !_pause1Active;
            SetPauseFlags(_pause1Active, _pause2Active);
            // string status = _pause1Active ? "暂停1已启用" : "暂停1已禁用";
            // _mainForm.UpdateStatus(status);
        }

        private void OnPause2()
        {
            _pause2Active = !_pause2Active;
            SetPauseFlags(_pause1Active, _pause2Active);
            // string status = _pause2Active ? "暂停2已启用" : "暂停2已禁用";
            // _mainForm.UpdateStatus(status);
        }
    }
}