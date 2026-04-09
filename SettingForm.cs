using System;
using System.Drawing;
using System.Windows.Forms;

namespace HLL_ATassistant
{
    public partial class SettingForm : Form
    {
        private readonly MainForm _mainForm;
        private readonly AppSettings _originalSettings;
        private AppSettings _workingCopy;

        // 控件
        private TabControl tabControl;
        private TabPage tabHotkeys, tabHeightDiff;
        private Label lblB1, lblB2, lblToggle, lblOverlay, lblStartMulti;
        private Button btnB1, btnB2, btnToggle, btnOverlay, btnStartMulti;
        private Label lblSetBaseline, lblCancelBaseline;
        private Button btnSetBaseline, btnCancelBaseline;
        private Label lblPause1, lblPause2;
        private Button btnPause1, btnPause2;
        private CheckBox chkSameKey;
        private GroupBox gbBasic, gbAdvanced;
        private CheckBox chkBasicD, chkBasicTheta, chkBasicVx, chkBasicT;
        private CheckBox chkAdvancedL, chkAdvancedX, chkAdvancedBeta, chkAdvancedT, chkTPrime;
        private Button btnSave, btnCancel;

        // 热键捕获相关
        private bool _capturing;
        private Button _captureButton;
        private Action<int> _captureSetKey;
        private Action<int> _captureSetMod;

        public SettingForm(AppSettings settings, MainForm mainForm)
        {
            _mainForm = mainForm;
            _originalSettings = settings;
            _workingCopy = CloneSettings(settings);
            InitializeComponent();
            this.Owner = mainForm;   // 确保在主窗体之上

            _currentLanguage = _mainForm.GetCurrentLanguage();

            if (_originalSettings.SettingFormLocation.HasValue)
            {
                var loc = _originalSettings.SettingFormLocation.Value;
                if (IsOnScreen(loc))
                {
                    this.StartPosition = FormStartPosition.Manual;
                    this.Location = loc;
                }
            }

            // 绑定数据到工作副本
            chkBasicD.DataBindings.Add("Checked", _workingCopy, "ShowDistance");
            chkBasicTheta.DataBindings.Add("Checked", _workingCopy, "ShowAngle");
            chkBasicVx.DataBindings.Add("Checked", _workingCopy, "ShowVx");
            chkBasicT.DataBindings.Add("Checked", _workingCopy, "ShowTime");
            chkAdvancedL.DataBindings.Add("Checked", _workingCopy, "ShowL");
            chkAdvancedX.DataBindings.Add("Checked", _workingCopy, "ShowX");
            chkAdvancedBeta.DataBindings.Add("Checked", _workingCopy, "ShowBeta");
            chkTPrime.DataBindings.Add("Checked", _workingCopy, "ShowTPrime");
            chkAdvancedT.DataBindings.Add("Checked", _workingCopy, "ShowTime");
            chkSameKey.DataBindings.Add("Checked", _workingCopy, "SameKeyForBaseline");

            UpdateButtonTexts();
            ApplyLanguage();
        }

        private AppSettings CloneSettings(AppSettings original)
        {
            return new AppSettings
            {
                B1Key = original.B1Key,
                B1Modifiers = original.B1Modifiers,
                B2Key = original.B2Key,
                B2Modifiers = original.B2Modifiers,
                ToggleVisibleKey = original.ToggleVisibleKey,
                ToggleVisibleModifiers = original.ToggleVisibleModifiers,
                ToggleOverlayKey = original.ToggleOverlayKey,
                ToggleOverlayModifiers = original.ToggleOverlayModifiers,
                StartMultiKey = original.StartMultiKey,
                StartMultiModifiers = original.StartMultiModifiers,
                SetBaselineKey = original.SetBaselineKey,
                SetBaselineModifiers = original.SetBaselineModifiers,
                CancelBaselineKey = original.CancelBaselineKey,
                CancelBaselineModifiers = original.CancelBaselineModifiers,
                Pause1Key = original.Pause1Key,
                Pause1Modifiers = original.Pause1Modifiers,
                Pause2Key = original.Pause2Key,
                Pause2Modifiers = original.Pause2Modifiers,
                SameKeyForBaseline = original.SameKeyForBaseline,
                ShowDistance = original.ShowDistance,
                ShowAngle = original.ShowAngle,
                ShowVx = original.ShowVx,
                ShowTime = original.ShowTime,
                ShowL = original.ShowL,
                ShowX = original.ShowX,
                ShowBeta = original.ShowBeta,
                ShowTPrime = original.ShowTPrime,
                Sensitivity = original.Sensitivity,
                MaxRange = original.MaxRange,
                UseFixedMaxRange = original.UseFixedMaxRange
            };
        }

        private void InitializeComponent()
        {
            int sizeX = 430, sizeY = 450;
            this.Size = new Size(sizeX, sizeY);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;

            tabControl = new TabControl { Dock = DockStyle.Fill };
            tabHotkeys = new TabPage();
            tabHeightDiff = new TabPage();

            int y = 20;
            const int labelX = 20, btnX = 200, btnWidth = 150;

            // ========== 热键页 ==========
            AddHotkeyRow(tabHotkeys, ref y, labelX, btnX, btnWidth,
                out lblB1, out btnB1,
                () => _workingCopy.B1Key, v => _workingCopy.B1Key = v,
                () => _workingCopy.B1Modifiers, m => _workingCopy.B1Modifiers = m);

            AddHotkeyRow(tabHotkeys, ref y, labelX, btnX, btnWidth,
                out lblB2, out btnB2,
                () => _workingCopy.B2Key, v => _workingCopy.B2Key = v,
                () => _workingCopy.B2Modifiers, m => _workingCopy.B2Modifiers = m);

            AddHotkeyRow(tabHotkeys, ref y, labelX, btnX, btnWidth,
                out lblToggle, out btnToggle,
                () => _workingCopy.ToggleVisibleKey, v => _workingCopy.ToggleVisibleKey = v,
                () => _workingCopy.ToggleVisibleModifiers, m => _workingCopy.ToggleVisibleModifiers = m);

            AddHotkeyRow(tabHotkeys, ref y, labelX, btnX, btnWidth,
                out lblOverlay, out btnOverlay,
                () => _workingCopy.ToggleOverlayKey, v => _workingCopy.ToggleOverlayKey = v,
                () => _workingCopy.ToggleOverlayModifiers, m => _workingCopy.ToggleOverlayModifiers = m);

            AddHotkeyRow(tabHotkeys, ref y, labelX, btnX, btnWidth,
                out lblStartMulti, out btnStartMulti,
                () => _workingCopy.StartMultiKey, v => _workingCopy.StartMultiKey = v,
                () => _workingCopy.StartMultiModifiers, m => _workingCopy.StartMultiModifiers = m);

            // 基本显示选项分组框
            int col1 = 20, col2 = 220, row = 25;
            gbBasic = new GroupBox { Location = new Point(labelX, y + 10), Size = new Size(360, 100) };
            chkBasicD = new CheckBox { Location = new Point(col1, row), AutoSize = true };
            chkBasicTheta = new CheckBox { Location = new Point(col2, row), AutoSize = true };
            row += 30;
            chkBasicVx = new CheckBox { Location = new Point(col1, row), AutoSize = true };
            chkBasicT = new CheckBox { Location = new Point(col2, row), AutoSize = true };
            gbBasic.Controls.AddRange(new Control[] { chkBasicD, chkBasicTheta, chkBasicVx, chkBasicT });
            tabHotkeys.Controls.Add(gbBasic);
            y += 120;

            // ========== 高低差页 ==========
            y = 20;

            AddHotkeyRow(tabHeightDiff, ref y, labelX, btnX, btnWidth,
                out lblSetBaseline, out btnSetBaseline,
                () => _workingCopy.SetBaselineKey, v => _workingCopy.SetBaselineKey = v,
                () => _workingCopy.SetBaselineModifiers, m => _workingCopy.SetBaselineModifiers = m);

            AddHotkeyRow(tabHeightDiff, ref y, labelX, btnX, btnWidth,
                out lblCancelBaseline, out btnCancelBaseline,
                () => _workingCopy.CancelBaselineKey, v => _workingCopy.CancelBaselineKey = v,
                () => _workingCopy.CancelBaselineModifiers, m => _workingCopy.CancelBaselineModifiers = m);

            AddHotkeyRow(tabHeightDiff, ref y, labelX, btnX, btnWidth,
                out lblPause1, out btnPause1,
                () => _workingCopy.Pause1Key, v => _workingCopy.Pause1Key = v,
                () => _workingCopy.Pause1Modifiers, m => _workingCopy.Pause1Modifiers = m);

            AddHotkeyRow(tabHeightDiff, ref y, labelX, btnX, btnWidth,
                out lblPause2, out btnPause2,
                () => _workingCopy.Pause2Key, v => _workingCopy.Pause2Key = v,
                () => _workingCopy.Pause2Modifiers, m => _workingCopy.Pause2Modifiers = m);

            chkSameKey = new CheckBox { Location = new Point(btnX, y), AutoSize = true };
            tabHeightDiff.Controls.Add(chkSameKey);
            y += 30;

            gbAdvanced = new GroupBox { Location = new Point(labelX, y + 10), Size = new Size(360, 160) };
            // 勾选框
            row = 25;
            chkAdvancedL = new CheckBox { Location = new Point(col1, row), AutoSize = true };
            chkAdvancedX = new CheckBox { Location = new Point(col2, row), AutoSize = true };
            row += 30;
            chkAdvancedBeta = new CheckBox { Location = new Point(col1, row), AutoSize = true };
            chkAdvancedT = new CheckBox { Location = new Point(col2, row), AutoSize = true };
            row += 30;
            chkTPrime = new CheckBox { Location = new Point(col1, row), AutoSize = true };
            gbAdvanced.Controls.AddRange(new Control[] { chkAdvancedL, chkAdvancedX, chkAdvancedBeta, chkAdvancedT, chkTPrime });
            tabHeightDiff.Controls.Add(gbAdvanced);

            tabControl.Controls.Add(tabHotkeys);
            tabControl.Controls.Add(tabHeightDiff);

            var buttonPanel = new Panel { Dock = DockStyle.Bottom, Height = 50 };
            btnSave = new Button { Location = new Point(sizeX/2-10-80, 10), Width = 80 };
            btnCancel = new Button { Location = new Point(sizeX/2+10, 10), Width = 80 };
            btnSave.Click += BtnSave_Click;
            btnCancel.Click += (s, e) => {this.DialogResult = DialogResult.Cancel; this.Close(); };
            buttonPanel.Controls.Add(btnSave);
            buttonPanel.Controls.Add(btnCancel);

            Controls.Add(tabControl);
            Controls.Add(buttonPanel);
            KeyPreview = true;

            this.TopMost = true;
        }

        /// <summary>
        /// 添加一行热键设置（标签 + 热键按钮 + 清空按钮）
        /// </summary>
        private void AddHotkeyRow(TabPage page, ref int y, int labelX, int btnX, int btnWidth,
            out Label label, out Button hotkeyBtn,
            Func<int> getKey, Action<int> setKey,
            Func<int> getMod, Action<int> setMod)
        {
            label = new Label { Location = new Point(labelX, y), AutoSize = true };
            var btn = new Button { Location = new Point(btnX, y - 3), Width = btnWidth };
            btn.Click += (s, e) => StartCapture(btn, getKey, setKey, getMod, setMod);
            hotkeyBtn = btn;   // 将局部变量赋给 out 参数
            page.Controls.Add(label);
            page.Controls.Add(btn);

            var clearBtn = CreateClearButton(btn, setKey, setMod);
            page.Controls.Add(clearBtn);

            y += 40;
        }

        private Button CreateClearButton(Button hotkeyBtn, Action<int> setKey, Action<int> setMod)
        {
            Button clearBtn = new Button
            {
                Size = new Size(hotkeyBtn.Height, hotkeyBtn.Height),
                Location = new Point(hotkeyBtn.Right + 5, hotkeyBtn.Top),
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = true
            };
            // 尝试从系统画图程序提取图标（橡皮擦风格）
            clearBtn.Text = "";
            clearBtn.Click += (s, e) =>
            {
                setKey(0);
                setMod(0);
                hotkeyBtn.Text = KeyToString(0, 0);
            };
            return clearBtn;
        }

        private void StartCapture(Button btn, Func<int> getKey, Action<int> setKey, Func<int> getMod, Action<int> setMod)
        {
            _capturing = true;
            _captureSetKey = setKey;
            _captureSetMod = setMod;
            _captureButton = btn;
            btn.Text = GetString("Msg_PressAnyKey");
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (_capturing)
            {
                Keys keyCode = keyData & Keys.KeyCode;
                Keys modifiers = keyData & Keys.Modifiers;

                if (keyCode == Keys.ControlKey || keyCode == Keys.Menu || keyCode == Keys.ShiftKey)
                    return true;

                int mod = 0;
                if ((modifiers & Keys.Control) != 0) mod |= 0x0002;
                if ((modifiers & Keys.Alt) != 0) mod |= 0x0001;
                if ((modifiers & Keys.Shift) != 0) mod |= 0x0004;

                _captureSetKey((int)keyCode);
                _captureSetMod(mod);
                _captureButton.Text = KeyToString((int)keyCode, mod);
                _workingCopy.Save();

                _capturing = false;
                _captureButton = null;
                _captureSetKey = null;
                _captureSetMod = null;
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void UpdateButtonTexts()
        {
            btnB1.Text = KeyToString(_workingCopy.B1Key, _workingCopy.B1Modifiers);
            btnB2.Text = KeyToString(_workingCopy.B2Key, _workingCopy.B2Modifiers);
            btnToggle.Text = KeyToString(_workingCopy.ToggleVisibleKey, _workingCopy.ToggleVisibleModifiers);
            btnOverlay.Text = KeyToString(_workingCopy.ToggleOverlayKey, _workingCopy.ToggleOverlayModifiers);
            btnStartMulti.Text = KeyToString(_workingCopy.StartMultiKey, _workingCopy.StartMultiModifiers);
            btnSetBaseline.Text = KeyToString(_workingCopy.SetBaselineKey, _workingCopy.SetBaselineModifiers);
            btnCancelBaseline.Text = KeyToString(_workingCopy.CancelBaselineKey, _workingCopy.CancelBaselineModifiers);
            btnPause1.Text = KeyToString(_workingCopy.Pause1Key, _workingCopy.Pause1Modifiers);
            btnPause2.Text = KeyToString(_workingCopy.Pause2Key, _workingCopy.Pause2Modifiers);
        }

        private string KeyToString(int key, int mod)
        {
            if (key == 0) return GetString("None");
            string s = "";
            if ((mod & 0x0002) != 0) s += "Ctrl+";
            if ((mod & 0x0001) != 0) s += "Alt+";
            if ((mod & 0x0004) != 0) s += "Shift+";
            s += ((Keys)key).ToString();
            return s;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            _originalSettings.B1Key = _workingCopy.B1Key;
            _originalSettings.B1Modifiers = _workingCopy.B1Modifiers;
            _originalSettings.B2Key = _workingCopy.B2Key;
            _originalSettings.B2Modifiers = _workingCopy.B2Modifiers;
            _originalSettings.ToggleVisibleKey = _workingCopy.ToggleVisibleKey;
            _originalSettings.ToggleVisibleModifiers = _workingCopy.ToggleVisibleModifiers;
            _originalSettings.ToggleOverlayKey = _workingCopy.ToggleOverlayKey;
            _originalSettings.ToggleOverlayModifiers = _workingCopy.ToggleOverlayModifiers;
            _originalSettings.StartMultiKey = _workingCopy.StartMultiKey;
            _originalSettings.StartMultiModifiers = _workingCopy.StartMultiModifiers;
            _originalSettings.SetBaselineKey = _workingCopy.SetBaselineKey;
            _originalSettings.SetBaselineModifiers = _workingCopy.SetBaselineModifiers;
            _originalSettings.CancelBaselineKey = _workingCopy.CancelBaselineKey;
            _originalSettings.CancelBaselineModifiers = _workingCopy.CancelBaselineModifiers;
            _originalSettings.Pause1Key = _workingCopy.Pause1Key;
            _originalSettings.Pause1Modifiers = _workingCopy.Pause1Modifiers;
            _originalSettings.Pause2Key = _workingCopy.Pause2Key;
            _originalSettings.Pause2Modifiers = _workingCopy.Pause2Modifiers;
            _originalSettings.SameKeyForBaseline = _workingCopy.SameKeyForBaseline;
            _originalSettings.ShowDistance = _workingCopy.ShowDistance;
            _originalSettings.ShowAngle = _workingCopy.ShowAngle;
            _originalSettings.ShowVx = _workingCopy.ShowVx;
            _originalSettings.ShowTime = _workingCopy.ShowTime;
            _originalSettings.ShowL = _workingCopy.ShowL;
            _originalSettings.ShowX = _workingCopy.ShowX;
            _originalSettings.ShowBeta = _workingCopy.ShowBeta;
            _originalSettings.ShowTPrime = _workingCopy.ShowTPrime;
            _originalSettings.Sensitivity = _workingCopy.Sensitivity;
            _originalSettings.MaxRange = _workingCopy.MaxRange;
            _originalSettings.UseFixedMaxRange = _workingCopy.UseFixedMaxRange;

            _originalSettings.Save();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (this.WindowState == FormWindowState.Normal)
                _originalSettings.SettingFormLocation = this.Location;
            else if (this.WindowState == FormWindowState.Maximized)
                _originalSettings.SettingFormLocation = this.RestoreBounds.Location;
            _originalSettings.Save();
            base.OnFormClosing(e);
        }

        private bool IsOnScreen(Point location)
        {
            foreach (var screen in Screen.AllScreens)
                if (screen.WorkingArea.Contains(location))
                    return true;
            return false;
        }
    }
}
