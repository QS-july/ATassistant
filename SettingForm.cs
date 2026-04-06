using System;
using System.Drawing;
using System.Windows.Forms;

namespace HLL_ATassistant
{
    public class SettingForm : Form
    {
        private readonly MainForm _mainForm;
        private readonly AppSettings _originalSettings;
        private AppSettings _workingCopy;   // 临时副本，用于编辑

        // 控件
        private TabControl tabControl;
        private TabPage tabHotkeys, tabHeightDiff;
        private Label lblB1, lblB2, lblToggle, lblOverlay, lblStartMulti;
        private Button btnB1, btnB2, btnToggle, btnOverlay, btnStartMulti;
        private Label lblSetBaseline, lblCancelBaseline;
        private Button btnSetBaseline, btnCancelBaseline;
        private CheckBox chkSameKey;
        private GroupBox gbBasic, gbAdvanced;
        private CheckBox chkBasicD, chkBasicTheta, chkBasicVx, chkBasicT;
        private CheckBox chkAdvancedL, chkAdvancedX, chkAdvancedBeta, chkAdvancedT, chkTPrime;
        private Button btnSave, btnCancel;

        private bool _capturing;
        private int _capturedKey, _capturedModifiers;
        private Action _captureCallback;

        public SettingForm(AppSettings settings, MainForm mainForm)
        {
            _mainForm = mainForm;
            _originalSettings = settings;
            _workingCopy = CloneSettings(settings);
            InitializeComponent();

            // 恢复设置窗体位置
            if (_originalSettings.SettingFormLocation.HasValue)
            {
                var loc = _originalSettings.SettingFormLocation.Value;
                if (IsOnScreen(loc))
                {
                    this.StartPosition = FormStartPosition.Manual;
                    this.Location = loc;
                }
            }

            // 绑定到工作副本
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

            // 初始化热键按钮显示
            UpdateButtonTexts();
            ApplyLanguage();
        }

        private AppSettings CloneSettings(AppSettings original)
        {
            // 简单克隆（值类型和字符串，浅拷贝足够）
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
            this.Size = new Size(450, 520);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;

            tabControl = new TabControl { Dock = DockStyle.Fill };
            tabHotkeys = new TabPage();
            tabHeightDiff = new TabPage();

            int y = 20, labelX = 20, btnX = 200, btnWidth = 200;

            // 热键页
            lblB1 = new Label { Location = new Point(labelX, y), AutoSize = true };
            btnB1 = new Button { Location = new Point(btnX, y - 3), Width = btnWidth };
            btnB1.Click += (s, e) => StartCapture(btnB1, () => _workingCopy.B1Key, v => _workingCopy.B1Key = v, () => _workingCopy.B1Modifiers, m => _workingCopy.B1Modifiers = m);
            tabHotkeys.Controls.Add(lblB1); tabHotkeys.Controls.Add(btnB1); y += 40;

            lblB2 = new Label { Location = new Point(labelX, y), AutoSize = true };
            btnB2 = new Button { Location = new Point(btnX, y - 3), Width = btnWidth };
            btnB2.Click += (s, e) => StartCapture(btnB2, () => _workingCopy.B2Key, v => _workingCopy.B2Key = v, () => _workingCopy.B2Modifiers, m => _workingCopy.B2Modifiers = m);
            tabHotkeys.Controls.Add(lblB2); tabHotkeys.Controls.Add(btnB2); y += 40;

            lblToggle = new Label { Location = new Point(labelX, y), AutoSize = true };
            btnToggle = new Button { Location = new Point(btnX, y - 3), Width = btnWidth };
            btnToggle.Click += (s, e) => StartCapture(btnToggle, () => _workingCopy.ToggleVisibleKey, v => _workingCopy.ToggleVisibleKey = v, () => _workingCopy.ToggleVisibleModifiers, m => _workingCopy.ToggleVisibleModifiers = m);
            tabHotkeys.Controls.Add(lblToggle); tabHotkeys.Controls.Add(btnToggle); y += 40;

            lblOverlay = new Label { Location = new Point(labelX, y), AutoSize = true };
            btnOverlay = new Button { Location = new Point(btnX, y - 3), Width = btnWidth };
            btnOverlay.Click += (s, e) => StartCapture(btnOverlay, () => _workingCopy.ToggleOverlayKey, v => _workingCopy.ToggleOverlayKey = v, () => _workingCopy.ToggleOverlayModifiers, m => _workingCopy.ToggleOverlayModifiers = m);
            tabHotkeys.Controls.Add(lblOverlay); tabHotkeys.Controls.Add(btnOverlay); y += 40;

            lblStartMulti = new Label { Location = new Point(labelX, y), AutoSize = true };
            btnStartMulti = new Button { Location = new Point(btnX, y - 3), Width = btnWidth };
            btnStartMulti.Click += (s, e) => StartCapture(btnStartMulti, () => _workingCopy.StartMultiKey, v => _workingCopy.StartMultiKey = v, () => _workingCopy.StartMultiModifiers, m => _workingCopy.StartMultiModifiers = m);
            tabHotkeys.Controls.Add(lblStartMulti); tabHotkeys.Controls.Add(btnStartMulti); y += 40;

            gbBasic = new GroupBox { Location = new Point(labelX, y + 10), Size = new Size(380, 100) };
            chkBasicD = new CheckBox { Text = "D", Location = new Point(10, 25), AutoSize = true };
            chkBasicTheta = new CheckBox { Text = "θ", Location = new Point(80, 25), AutoSize = true };
            chkBasicVx = new CheckBox { Text = "vx", Location = new Point(150, 25), AutoSize = true };
            chkBasicT = new CheckBox { Text = "t", Location = new Point(220, 25), AutoSize = true };
            gbBasic.Controls.AddRange(new Control[] { chkBasicD, chkBasicTheta, chkBasicVx, chkBasicT });
            tabHotkeys.Controls.Add(gbBasic);
            y += 120;

            // 高低差页
            y = 20;
            lblSetBaseline = new Label { Location = new Point(labelX, y), AutoSize = true };
            btnSetBaseline = new Button { Location = new Point(btnX, y - 3), Width = btnWidth };
            btnSetBaseline.Click += (s, e) => StartCapture(btnSetBaseline, () => _workingCopy.SetBaselineKey, v => _workingCopy.SetBaselineKey = v, () => _workingCopy.SetBaselineModifiers, m => _workingCopy.SetBaselineModifiers = m);
            tabHeightDiff.Controls.Add(lblSetBaseline); tabHeightDiff.Controls.Add(btnSetBaseline); y += 40;

            lblCancelBaseline = new Label { Location = new Point(labelX, y), AutoSize = true };
            btnCancelBaseline = new Button { Location = new Point(btnX, y - 3), Width = btnWidth };
            btnCancelBaseline.Click += (s, e) => StartCapture(btnCancelBaseline, () => _workingCopy.CancelBaselineKey, v => _workingCopy.CancelBaselineKey = v, () => _workingCopy.CancelBaselineModifiers, m => _workingCopy.CancelBaselineModifiers = m);
            tabHeightDiff.Controls.Add(lblCancelBaseline); tabHeightDiff.Controls.Add(btnCancelBaseline); y += 40;

            chkSameKey = new CheckBox { Location = new Point(btnX, y), AutoSize = true };
            tabHeightDiff.Controls.Add(chkSameKey); y += 40;

            gbAdvanced = new GroupBox { Location = new Point(labelX, y + 10), Size = new Size(380, 150) };
            int col1 = 20, col2 = 200, row = 25;
            chkAdvancedL = new CheckBox { Text = "L", Location = new Point(col1, row), AutoSize = true };
            chkAdvancedX = new CheckBox { Text = "X", Location = new Point(col2, row), AutoSize = true };
            row += 30;
            chkAdvancedBeta = new CheckBox { Text = "β", Location = new Point(col1, row), AutoSize = true };
            chkAdvancedT = new CheckBox { Text = "t", Location = new Point(col2, row), AutoSize = true };
            row += 30;
            chkTPrime = new CheckBox { Text = "t'", Location = new Point(col1, row), AutoSize = true };
            gbAdvanced.Controls.AddRange(new Control[] { chkAdvancedL, chkAdvancedX, chkAdvancedBeta, chkAdvancedT, chkTPrime });
            tabHeightDiff.Controls.Add(gbAdvanced);

            tabControl.Controls.Add(tabHotkeys);
            tabControl.Controls.Add(tabHeightDiff);

            var buttonPanel = new Panel { Dock = DockStyle.Bottom, Height = 50 };
            btnSave = new Button { Text = "保存", Location = new Point(150, 10), Width = 80 };
            btnCancel = new Button { Text = "取消", Location = new Point(250, 10), Width = 80 };
            btnSave.Click += BtnSave_Click;
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;
            buttonPanel.Controls.Add(btnSave);
            buttonPanel.Controls.Add(btnCancel);

            Controls.Add(tabControl);
            Controls.Add(buttonPanel);
            KeyPreview = true;
            KeyDown += SettingForm_KeyDown;
        }

        private void StartCapture(Button btn, Func<int> getKey, Action<int> setKey, Func<int> getMod, Action<int> setMod)
        {
            _capturing = true;
            btn.Text = "按下新键...";
            _captureCallback = () =>
            {
                setKey(_capturedKey);
                setMod(_capturedModifiers);
                UpdateButtonTexts();
                _capturing = false;
                _captureCallback = null;
            };
        }

        private void SettingForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (!_capturing) return;
            if (e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.Menu || e.KeyCode == Keys.ShiftKey) return;

            int modifiers = 0;
            if (e.Control) modifiers |= 0x0002;
            if (e.Alt) modifiers |= 0x0001;
            if (e.Shift) modifiers |= 0x0004;
            _capturedKey = (int)e.KeyCode;
            _capturedModifiers = modifiers;
            _captureCallback?.Invoke();
            e.SuppressKeyPress = true;
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
        }

        private string KeyToString(int key, int mod)
        {
            string s = "";
            if ((mod & 0x0002) != 0) s += "Ctrl+";
            if ((mod & 0x0001) != 0) s += "Alt+";
            if ((mod & 0x0004) != 0) s += "Shift+";
            s += ((Keys)key).ToString();
            return s;
        }

        private void ApplyLanguage()
        {
            this.Text = _mainForm.GetString("SettingsTitle", null);
            tabHotkeys.Text = _mainForm.GetString("TabHotkeys", null);
            tabHeightDiff.Text = _mainForm.GetString("TabHeightDiff", null);
            lblB1.Text = _mainForm.GetString("HotKey_B1", null);
            lblB2.Text = _mainForm.GetString("HotKey_B2", null);
            lblToggle.Text = _mainForm.GetString("HotKey_ToggleVisible", null);
            lblOverlay.Text = _mainForm.GetString("HotKey_ToggleOverlay", null);
            lblStartMulti.Text = _mainForm.GetString("HotKey_StartMulti", null);
            lblSetBaseline.Text = _mainForm.GetString("HotKey_SetBaseline", null);
            lblCancelBaseline.Text = _mainForm.GetString("HotKey_CancelBaseline", null);
            chkSameKey.Text = _mainForm.GetString("SameKeyForBaseline", null);
            gbBasic.Text = _mainForm.GetString("BasicDisplayGroup", null);
            gbAdvanced.Text = _mainForm.GetString("AdvancedDisplayGroup", null);
            btnSave.Text = _mainForm.GetString("HotKey_BtnSave", null);
            btnCancel.Text = _mainForm.GetString("HotKey_BtnCancel", null);
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            // 将工作副本的值复制到原始设置对象
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
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (this.WindowState == FormWindowState.Normal)
            {
                _originalSettings.SettingFormLocation = this.Location;
            }
            else if (this.WindowState == FormWindowState.Maximized)
            {
                _originalSettings.SettingFormLocation = this.RestoreBounds.Location;
            }
            _originalSettings.Save();
            base.OnFormClosing(e);
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
    }
}