using System.Collections.Generic;

namespace HLL_ATassistant
{
    public partial class SettingForm
    {
        private MainForm.Language _currentLanguage;

        #region 语言字典

        private static readonly Dictionary<string, string> ChineseTexts = new Dictionary<string, string>
        {
            { "SettingsTitle", "设置" },
            { "TabHotkeys", "热键设置" },
            { "TabHeightDiff", "高低差测量模式" },
            { "HotKey_B1", "开始测量:" },
            { "HotKey_B2", "结束测量:" },
            { "HotKey_ToggleVisible", "显示/隐藏主窗体:" },
            { "HotKey_ToggleOverlay", "显示/隐藏覆盖窗口:" },
            { "HotKey_StartMulti", "开始校准:" },
            { "HotKey_SetBaseline", "设置水平基准点位:" },
            { "HotKey_CancelBaseline", "取消水平基准点位:" },
            { "HotKey_Pause1", "暂停 β 累积 (1):" },
            { "HotKey_Pause2", "暂停 β 累积 (2):" },
            { "SameKeyForBaseline", "将设置/取消设为同一按键" },
            { "BasicDisplayGroup", "基础显示项" },
            { "AdvancedDisplayGroup", "高级显示项" },
            { "CheckBox_D", "距离 (D)" },
            { "CheckBox_Theta", "仰角 (θ)" },
            { "CheckBox_Vx", "水平速度 (vx)" },
            { "CheckBox_T", "飞行时间 (t)" },
            { "CheckBox_L", "水平偏移 (L)" },
            { "CheckBox_X", "斜距 (X)" },
            { "CheckBox_Beta", "基准角 (β)" },
            { "CheckBox_TPrime", "额外飞行时间 (t')" },
            { "HotKey_BtnSave", "保存" },
            { "HotKey_BtnCancel", "取消" },
            { "Msg_PressAnyKey", "按下新键..." },
            { "None", "无" }
        };

        private static readonly Dictionary<string, string> EnglishTexts = new Dictionary<string, string>
        {
            { "SettingsTitle", "Settings" },
            { "TabHotkeys", "Hotkeys" },
            { "TabHeightDiff", "Height Difference Mode" },
            { "HotKey_B1", "Start Measure:" },
            { "HotKey_B2", "Stop Measure:" },
            { "HotKey_ToggleVisible", "Show/Hide Main Form:" },
            { "HotKey_ToggleOverlay", "Show/Hide Overlay:" },
            { "HotKey_StartMulti", "Start Calibration:" },
            { "HotKey_SetBaseline", "Set Horizontal Baseline:" },
            { "HotKey_CancelBaseline", "Cancel Horizontal Baseline:" },
            { "HotKey_Pause1", "Pause β Accumulation (1):" },
            { "HotKey_Pause2", "Pause β Accumulation (2):" },
            { "SameKeyForBaseline", "Use same key for set/cancel" },
            { "BasicDisplayGroup", "Basic Display" },
            { "AdvancedDisplayGroup", "Advanced Display" },
            { "CheckBox_D", "Distance (D)" },
            { "CheckBox_Theta", "Elevation (θ)" },
            { "CheckBox_Vx", "Horizontal Speed (vx)" },
            { "CheckBox_T", "Flight Time (t)" },
            { "CheckBox_L", "Horizontal Offset (L)" },
            { "CheckBox_X", "Slant Range (X)" },
            { "CheckBox_Beta", "Base Angle (β)" },
            { "CheckBox_TPrime", "Extra Flight Time (t')" },
            { "HotKey_BtnSave", "Save" },
            { "HotKey_BtnCancel", "Cancel" },
            { "Msg_PressAnyKey", "Press any key..." },
            { "None", "None" }
        };

        #endregion

        private string GetString(string key, params object[] args)
        {
            var texts = _currentLanguage == MainForm.Language.Chinese ? ChineseTexts : EnglishTexts;
            if (texts.TryGetValue(key, out string template))
                return args.Length > 0 ? string.Format(template, args) : template;
            return key;
        }

        private void ApplyLanguage()
        {
            this.Text = GetString("SettingsTitle");
            tabHotkeys.Text = GetString("TabHotkeys");
            tabHeightDiff.Text = GetString("TabHeightDiff");
            lblB1.Text = GetString("HotKey_B1");
            lblB2.Text = GetString("HotKey_B2");
            lblToggle.Text = GetString("HotKey_ToggleVisible");
            lblOverlay.Text = GetString("HotKey_ToggleOverlay");
            lblStartMulti.Text = GetString("HotKey_StartMulti");
            lblSetBaseline.Text = GetString("HotKey_SetBaseline");
            lblCancelBaseline.Text = GetString("HotKey_CancelBaseline");
            lblPause1.Text = GetString("HotKey_Pause1");
            lblPause2.Text = GetString("HotKey_Pause2");
            chkSameKey.Text = GetString("SameKeyForBaseline");
            gbBasic.Text = GetString("BasicDisplayGroup");
            gbAdvanced.Text = GetString("AdvancedDisplayGroup");

            // 设置复选框文本
            chkBasicD.Text = GetString("CheckBox_D");
            chkBasicTheta.Text = GetString("CheckBox_Theta");
            chkBasicVx.Text = GetString("CheckBox_Vx");
            chkBasicT.Text = GetString("CheckBox_T");
            chkAdvancedL.Text = GetString("CheckBox_L");
            chkAdvancedX.Text = GetString("CheckBox_X");
            chkAdvancedBeta.Text = GetString("CheckBox_Beta");
            chkAdvancedT.Text = GetString("CheckBox_T");
            chkTPrime.Text = GetString("CheckBox_TPrime");

            btnSave.Text = GetString("HotKey_BtnSave");
            btnCancel.Text = GetString("HotKey_BtnCancel");
        }
    }
}