using System.Collections.Generic;

namespace HLL_ATassistant
{
    public partial class MainForm
    {
        #region 语言字典

        private static readonly Dictionary<string, string> ChineseTexts = new Dictionary<string, string>
        {
            // ==================== 主窗体 ====================
            { "Title", "筒子校准测距" },
            { "MaxRangeLabel", "最大射程 (米):" },
            { "SensitivityLabel", "灵敏度系数:" },
            { "UseFixedRange", "使用最大射程 ({0}m)" },
            { "HeightDiffModeCheckbox", "启用高低差测量模式" },
            { "BtnStartMulti", "开始校准" },
            { "BtnReset", "重置校准" },
            { "BtnRefresh", "刷新" },
            { "BtnLoad", "加载校准" },
            { "BtnSave", "保存校准" },
            { "BtnSettings", "设置" },
            { "BtnLanguage", "English" },
            { "DistanceListLabel", "距离列表\n(每行一个):" },
            { "StatusIdle", "状态: 等待校准" },
            { "ErrorStatsDefault", "误差统计: 无数据" },
            // Warning
            { "WarningNoFixed", "未勾选最大射程，最大射程仅为距离列表中最大值" },
            { "HeightDiffModeWarning", "功能开启，请确认基准点位快捷键已设置" },
            // Msg
            { "Msg_StatusCalibComplete", "校准完成 k={0:E4}  X={1:F1}米" },
            { "Msg_StatusLoadComplete", "加载校准完成 k={0:E4}  X={1:F1}米" },
            { "Msg_StatusRecalcComplete", "重新拟合完成 k={0:E4}  X={1:F1}米" },
            { "Msg_StatusMultiStart", "多点校准启动，请测量点 1: 距离 {0} 米，按F1开始" },
            { "Msg_StatusMeasurePoint", "测量点 {0}: 距离 {1} 米，移动鼠标后按F2结束" },
            { "Msg_StatusNextPoint", "请准备测量点 {0}: 距离 {1} 米，按F1开始" },
            { "Msg_StatusMeasuring", "测量模式开启... 按F1归位" },
            { "Msg_StatusPaused", "测量暂停" },
            { "Msg_StatusReset", "已重置，请进行多点校准或加载校准文件" },
            { "Msg_StatusMeasuringNormal", "普通测量模式" },
            { "Msg_StatusMeasuringReset", "测量模式已重置，位移从零开始累积" },
            { "Msg_CalibSaved", "校准数据保存成功" },
            { "Msg_CalibLoaded", "校准数据加载成功" },
            { "Msg_HotkeyUpdated", "热键已更新，请使用新组合键。" },
            { "Msg_BaselineSet", "水平基准点位已设置" },
            { "Msg_BaselineCancelled", "水平基准点位已取消" },
            // Error
            { "Error_NeedTwoDistances", "至少需要输入两个距离值" },
            { "Error_NeedCalibFirst", "请先进行多点校准或加载校准文件" },
            { "Error_InvalidDistance", "无效的距离值: {0}" },
            { "Error_NeedTwoValidPoints", "至少需要两个有效标定点，或勾选使用最大射程" },
            { "Error_HotkeyRegFail", "热键{0}注册失败，可能被其他程序占用" },
            { "Error_NoCalibData", "没有可保存的校准数据" },
            { "Error_SaveFail", "保存失败: {0}" },
            { "Error_LoadFail", "加载失败: {0}" },
            { "Error_InvalidCalibFile", "无效的校准文件" },
            { "Error_NoPoints", "没有标定点，无法重新计算" },
            { "Error_FitFail", "无法拟合合适的参数，请检查标定点数据" },
            { "Error_NeedAtLeastOnePointWithFixed", "使用最大射程时仍需至少一个标定点" },
            { "Error_NoFitParams", "未能找到合适的参数，请检查标定点数据" },
            { "Error_FixedRangeNotChecked", "请先勾选“使用最大射程”" },
            { "Error_NoPointsToRefresh", "没有标定点，无法刷新" },
            { "Error_NeedBaselineFirst", "请先设置水平基准点位" },
            { "MultiErrorFormat", "平均绝对误差: {0:F2}m, 最大误差: {1:F2}m" },
            { "MultiErrorRatio", "平均误差率：{0:P2}, 最大误差率: {1:P2}\n(最大误差点：{2:F1}m @ {3:F0}m)" },
            // pop out
            { "SensitivityConfirmTitle", "灵敏度调整" },
            { "SensitivityConfirmMessage", "改变灵敏度会使现有校准数据失效，是否继续？\n选择“是”将重置校准并应用新灵敏度。" },
            // 托盘菜单
            { "TraySettings", "设置" },
            { "TrayExit", "退出" }
        };

        private static readonly Dictionary<string, string> EnglishTexts = new Dictionary<string, string>
        {
            { "Title", "ATassistant" },
            { "MaxRangeLabel", "Max Range (m):" },
            { "SensitivityLabel", "Sensitivity:" },
            { "UseFixedRange", "Use Max Range ({0}m)" },
            { "HeightDiffModeCheckbox", "Enable Height-Diff Mode" },
            { "BtnStartMulti", "Start Calibration" },
            { "BtnReset", "Reset" },
            { "BtnRefresh", "Refresh" },
            { "BtnLoad", "Load" },
            { "BtnSave", "Save" },
            { "BtnSettings", "Settings" },
            { "BtnLanguage", "中文" },
            { "DistanceListLabel", "Distances\n(one per line):" },
            { "StatusIdle", "Status: Waiting for calibration" },
            { "ErrorStatsDefault", "Error stats: No data" },
            // warning
            { "WarningNoFixed", "Max Range is the largest range in list." },
            { "HeightDiffModeWarning", "Height difference mode enabled. Please set baseline hotkey." },
            // Msg
            { "Msg_StatusCalibComplete", "Calibration complete k={0:E4}  X={1:F1}m" },
            { "Msg_StatusLoadComplete", "Loaded calibration k={0:E4}  X={1:F1}m" },
            { "Msg_StatusRecalcComplete", "Recalculation complete k={0:E4}  X={1:F1}m" },
            { "Msg_StatusMultiStart", "Multi-calibration started, measure point 1: distance {0}m, press F1 to start" },
            { "Msg_StatusMeasurePoint", "Measure point {0}: distance {1}m, move mouse then press F2 to finish" },
            { "Msg_StatusNextPoint", "Prepare for point {0}: distance {1}m, press F1 to start" },
            { "Msg_StatusMeasuring", "Measuring mode... Press F1 to reset" },
            { "Msg_StatusPaused", "Measurement paused" },
            { "Msg_StatusReset", "Reset, please multi-cal or load file" },
            { "Msg_StatusMeasuringNormal", "Normal measurement mode" },
            { "Msg_StatusMeasuringReset", "Measuring reset, displacement starts from zero" },
            { "Msg_CalibSaved", "Calibration data saved successfully" },
            { "Msg_CalibLoaded", "Calibration data loaded successfully" },
            { "Msg_HotkeyUpdated", "Hotkeys updated, please use new combinations." },
            { "Msg_BaselineSet", "Baseline set" },
            { "Msg_BaselineCancelled", "Baseline cancelled" },
            // Error
            { "Error_NeedTwoDistances", "At least two distance values required" },
            { "Error_InvalidDistance", "Invalid distance value: {0}" },
            { "Error_HotkeyRegFail", "Hotkey {0} registration failed, possibly occupied by another program" },
            { "Error_NoCalibData", "No calibration data to save" },
            { "Error_SaveFail", "Save failed: {0}" },
            { "Error_LoadFail", "Load failed: {0}" },
            { "Error_InvalidCalibFile", "Invalid calibration file" },
            { "Error_NoPoints", "No calibration points, cannot recalculate" },
            { "Error_FitFail", "Cannot fit appropriate parameters, please check calibration points" },
            { "Error_NeedTwoValidPoints", "At least two valid calibration points required, or check 'Use Max Range'" },
            { "Error_NeedAtLeastOnePointWithFixed", "At least one calibration point required when using Max Range" },
            { "Error_NoFitParams", "Could not find appropriate parameters, please check calibration points" },
            { "Error_NeedCalibFirst", "Please perform multi-point calibration or load calibration file first" },
            { "Error_FixedRangeNotChecked", "Please check 'Use Max Range' first" },
            { "Error_NoPointsToRefresh", "No calibration points to refresh" },
            { "Error_NeedBaselineFirst", "Please set baseline first" },
            { "MultiErrorFormat", "Avg Abs Error: {0:F2}m, Max Error: {1:F2}m" },
            { "MultiErrorRatio", "Avg Error Ratio：{0:P2}, Max Error Ratio: {1:P2}\n(Max Error Pt：{2:F1}m @ {3:F0}m)" },
            // pop out
            { "SensitivityConfirmTitle", "Sensitivity Adjustment" },
            { "SensitivityConfirmMessage", "Changing sensitivity will invalidate current calibration. Continue?\nYes to reset and apply new sensitivity." },
            // menu
            { "TraySettings", "Settings" },
            { "TrayExit", "Exit" }
        };

        #endregion

        /// <summary>
        /// 获取本地化字符串
        /// </summary>
        internal string GetString(string key, params object[] args)
        {
            var texts = _currentLanguage == Language.Chinese ? ChineseTexts : EnglishTexts;
            if (texts.TryGetValue(key, out string template))
            {
                args ??= System.Array.Empty<object>();
                return args.Length > 0 ? string.Format(template, args) : template;
            }
            return key;
        }

        /// <summary>
        /// 获取当前语言（供 OverlayForm 使用）
        /// </summary>
        public Language GetCurrentLanguage() => _currentLanguage;

        /// <summary>
        /// 应用语言到主窗体控件
        /// </summary>
        private void ApplyLanguage()
        {
            var texts = _currentLanguage == Language.Chinese ? ChineseTexts : EnglishTexts;
            Text = texts["Title"];
            lblMaxRange.Text = texts["MaxRangeLabel"];
            lblSensitivity.Text = texts["SensitivityLabel"];
            lblDistances.Text = texts["DistanceListLabel"];
            btnStartMulti.Text = texts["BtnStartMulti"];
            btnReset.Text = texts["BtnReset"];
            btnLoad.Text = texts["BtnLoad"];
            btnSave.Text = texts["BtnSave"];
            btnSettings.Text = texts["BtnSettings"];
            btnRefresh.Text = texts["BtnRefresh"];
            btnLanguage.Text = texts["BtnLanguage"];
            chkEnableHeightDiff.Text = texts["HeightDiffModeCheckbox"];
            notifyIcon.Text = GetString("Title");
            UpdateErrorStats();
            UpdateAbsoluteErrorStats();
        }
    }
}