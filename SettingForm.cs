using System;
using System.Drawing;
using System.Windows.Forms;

namespace HLL_ATassistant
{
    public class SettingForm : Form
    {
        private MainForm _mainForm;
        private HotKeySettings _current;
        private TabControl tabControl;
        private TabPage tabHotkeys;
        private TabPage tabHeightDiff;

        // 热键页控件
        private Label lblB1, lblB2, lblToggle, lblOverlay, lblStartMulti;
        private Button btnB1, btnB2, btnToggle, btnOverlay, btnStartMulti;
        private bool capturingB1, capturingB2, capturingToggle, capturingOverlay, capturingStartMulti;

        // 高低差模式页控件
        private Label lblSetBaseline, lblCancelBaseline;
        private Button btnSetBaseline, btnCancelBaseline;
        private CheckBox chkSameKey;
        private bool capturingSetBaseline, capturingCancelBaseline;

        // 基础显示项 GroupBox（热键页）
        private GroupBox gbBasic = null!;
        private CheckBox chkBasicD = null!;
        private CheckBox chkBasicTheta = null!;
        private CheckBox chkBasicVx = null!;
        private CheckBox chkBasicT = null!;

        // 高级显示项 GroupBox（高低差页）
        private GroupBox gbAdvanced = null!;
        private CheckBox chkAdvancedL = null!;
        private CheckBox chkAdvancedX = null!;
        private CheckBox chkAdvancedBeta = null!;
        private CheckBox chkAdvancedT = null!;   // 与基础页 t 同步
        private CheckBox chkTPrime = null!;

        private Button btnSave, btnCancel;

        // 热键属性
        public int B1Key { get; private set; }
        public int B1Modifiers { get; private set; }
        public int B2Key { get; private set; }
        public int B2Modifiers { get; private set; }
        public int ToggleVisibleKey { get; private set; }
        public int ToggleVisibleModifiers { get; private set; }
        public int ToggleOverlayKey { get; private set; }
        public int ToggleOverlayModifiers { get; private set; }
        public int StartMultiKey { get; private set; }
        public int StartMultiModifiers { get; private set; }

        // 高低差模式属性
        public int SetBaselineKey { get; private set; }
        public int SetBaselineModifiers { get; private set; }
        public int CancelBaselineKey { get; private set; }
        public int CancelBaselineModifiers { get; private set; }
        public bool SameKeyForBaseline { get; private set; }

        // Overlay 显示选项属性
        public bool ShowDistance { get; private set; }
        public bool ShowAngle { get; private set; }
        public bool ShowVx { get; private set; }
        public bool ShowTime { get; private set; }      // 基础 t
        public bool ShowL { get; private set; }
        public bool ShowX { get; private set; }
        public bool ShowBeta { get; private set; }
        public bool ShowTPrime { get; private set; }    // t'

        public SettingForm(HotKeySettings current, MainForm mainForm)
        {
            _current = current;
            _mainForm = mainForm;

            // 加载热键
            B1Key = current.B1Key;
            B1Modifiers = current.B1Modifiers;
            B2Key = current.B2Key;
            B2Modifiers = current.B2Modifiers;
            ToggleVisibleKey = current.ToggleVisibleKey;
            ToggleVisibleModifiers = current.ToggleVisibleModifiers;
            ToggleOverlayKey = current.ToggleOverlayKey;
            ToggleOverlayModifiers = current.ToggleOverlayModifiers;
            StartMultiKey = current.StartMultiKey;
            StartMultiModifiers = current.StartMultiModifiers;

            // 加载高低差热键
            SetBaselineKey = current.SetBaselineKey;
            SetBaselineModifiers = current.SetBaselineModifiers;
            CancelBaselineKey = current.CancelBaselineKey;
            CancelBaselineModifiers = current.CancelBaselineModifiers;
            SameKeyForBaseline = current.SameKeyForBaseline;

            // 加载显示选项
            ShowDistance = current.ShowDistance;
            ShowAngle = current.ShowAngle;
            ShowVx = current.ShowVx;
            ShowTime = current.ShowTime;
            ShowL = current.ShowL;
            ShowX = current.ShowX;
            ShowBeta = current.ShowBeta;
            ShowTPrime = current.ShowTPrime;

            InitializeComponent();

            // 设置初始勾选状态
            chkBasicD.Checked = ShowDistance;
            chkBasicTheta.Checked = ShowAngle;
            chkBasicVx.Checked = ShowVx;
            chkBasicT.Checked = ShowTime;
            chkAdvancedL.Checked = ShowL;
            chkAdvancedX.Checked = ShowX;
            chkAdvancedBeta.Checked = ShowBeta;
            chkAdvancedT.Checked = ShowTime;      // 与基础 t 同步
            chkTPrime.Checked = ShowTPrime;

            // 同步 t 复选框（基础页与高级页）
            chkBasicT.CheckedChanged += (s, e) =>
            {
                chkAdvancedT.Checked = chkBasicT.Checked;
                ShowTime = chkBasicT.Checked;
            };
            chkAdvancedT.CheckedChanged += (s, e) =>
            {
                chkBasicT.Checked = chkAdvancedT.Checked;
                ShowTime = chkAdvancedT.Checked;
            };

            ApplyLanguage();
            UpdateButtonTexts();
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

            // ========== 热键页 ==========
            int y = 20;
            int labelX = 20;
            int btnX = 150;
            int btnWidth = 200;

            lblB1 = new Label { Location = new Point(labelX, y), AutoSize = true };
            btnB1 = new Button { Location = new Point(btnX, y - 3), Width = btnWidth };
            btnB1.Click += (s, e) => StartCapture(ref capturingB1, btnB1);
            tabHotkeys.Controls.Add(lblB1);
            tabHotkeys.Controls.Add(btnB1);
            y += 40;

            lblB2 = new Label { Location = new Point(labelX, y), AutoSize = true };
            btnB2 = new Button { Location = new Point(btnX, y - 3), Width = btnWidth };
            btnB2.Click += (s, e) => StartCapture(ref capturingB2, btnB2);
            tabHotkeys.Controls.Add(lblB2);
            tabHotkeys.Controls.Add(btnB2);
            y += 40;

            lblToggle = new Label { Location = new Point(labelX, y), AutoSize = true };
            btnToggle = new Button { Location = new Point(btnX, y - 3), Width = btnWidth };
            btnToggle.Click += (s, e) => StartCapture(ref capturingToggle, btnToggle);
            tabHotkeys.Controls.Add(lblToggle);
            tabHotkeys.Controls.Add(btnToggle);
            y += 40;

            lblOverlay = new Label { Location = new Point(labelX, y), AutoSize = true };
            btnOverlay = new Button { Location = new Point(btnX, y - 3), Width = btnWidth };
            btnOverlay.Click += (s, e) => StartCapture(ref capturingOverlay, btnOverlay);
            tabHotkeys.Controls.Add(lblOverlay);
            tabHotkeys.Controls.Add(btnOverlay);
            y += 40;

            lblStartMulti = new Label { Location = new Point(labelX, y), AutoSize = true };
            btnStartMulti = new Button { Location = new Point(btnX, y - 3), Width = btnWidth };
            btnStartMulti.Click += (s, e) => StartCapture(ref capturingStartMulti, btnStartMulti);
            tabHotkeys.Controls.Add(lblStartMulti);
            tabHotkeys.Controls.Add(btnStartMulti);
            y += 40;

            // 基础显示项 GroupBox
            gbBasic = new GroupBox
            {
                Location = new Point(labelX, y + 10),
                Size = new Size(380, 100)
            };
            chkBasicD = new CheckBox { Text = "D", Location = new Point(10, 25), AutoSize = true };
            chkBasicTheta = new CheckBox { Text = "θ", Location = new Point(80, 25), AutoSize = true };
            chkBasicVx = new CheckBox { Text = "vx", Location = new Point(150, 25), AutoSize = true };
            chkBasicT = new CheckBox { Text = "t", Location = new Point(220, 25), AutoSize = true };
            gbBasic.Controls.AddRange(new Control[] { chkBasicD, chkBasicTheta, chkBasicVx, chkBasicT });
            tabHotkeys.Controls.Add(gbBasic);
            y += 120;

            // ========== 高低差模式页 ==========
            y = 20;
            lblSetBaseline = new Label { Location = new Point(labelX, y), AutoSize = true };
            btnSetBaseline = new Button { Location = new Point(btnX, y - 3), Width = btnWidth };
            btnSetBaseline.Click += (s, e) => StartCapture(ref capturingSetBaseline, btnSetBaseline);
            tabHeightDiff.Controls.Add(lblSetBaseline);
            tabHeightDiff.Controls.Add(btnSetBaseline);
            y += 40;

            lblCancelBaseline = new Label { Location = new Point(labelX, y), AutoSize = true };
            btnCancelBaseline = new Button { Location = new Point(btnX, y - 3), Width = btnWidth };
            btnCancelBaseline.Click += (s, e) => StartCapture(ref capturingCancelBaseline, btnCancelBaseline);
            tabHeightDiff.Controls.Add(lblCancelBaseline);
            tabHeightDiff.Controls.Add(btnCancelBaseline);
            y += 40;

            chkSameKey = new CheckBox
            {
                Location = new Point(btnX, y),
                AutoSize = true,
                Checked = SameKeyForBaseline
            };
            chkSameKey.CheckedChanged += (s, e) => SameKeyForBaseline = chkSameKey.Checked;
            tabHeightDiff.Controls.Add(chkSameKey);
            y += 40;

            // 高级显示项 GroupBox
            gbAdvanced = new GroupBox
            {
                Location = new Point(labelX, y + 10),
                Size = new Size(380, 150)
            };
            int col1 = 20, col2 = 200;
            int row = 25;
            chkAdvancedL = new CheckBox { Text = "L", Location = new Point(col1, row), AutoSize = true };
            chkAdvancedX = new CheckBox { Text = "X", Location = new Point(col2, row), AutoSize = true };
            row += 30;
            chkAdvancedBeta = new CheckBox { Text = "β", Location = new Point(col1, row), AutoSize = true };
            chkAdvancedT = new CheckBox { Text = "t", Location = new Point(col2, row), AutoSize = true };
            row += 30;
            chkTPrime = new CheckBox { Text = "t'", Location = new Point(col1, row), AutoSize = true };
            gbAdvanced.Controls.AddRange(new Control[] { chkAdvancedL, chkAdvancedX, chkAdvancedBeta, chkAdvancedT, chkTPrime });
            tabHeightDiff.Controls.Add(gbAdvanced);
            y += 170;

            // 选项卡
            tabControl.Controls.Add(tabHotkeys);
            tabControl.Controls.Add(tabHeightDiff);

            // 按钮面板
            Panel buttonPanel = new Panel { Dock = DockStyle.Bottom, Height = 50 };
            btnSave = new Button { Text = "保存", Location = new Point(150, 10), Width = 80 };
            btnCancel = new Button { Text = "取消", Location = new Point(250, 10), Width = 80 };
            btnSave.Click += BtnSave_Click;
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;
            buttonPanel.Controls.Add(btnSave);
            buttonPanel.Controls.Add(btnCancel);

            this.Controls.Add(tabControl);
            this.Controls.Add(buttonPanel);
            buttonPanel.BringToFront();

            this.KeyPreview = true;
            this.KeyDown += SettingForm_KeyDown;
        }

        private void StartCapture(ref bool capturing, Button btn)
        {
            capturingB1 = capturingB2 = capturingToggle = capturingOverlay = capturingStartMulti =
            capturingSetBaseline = capturingCancelBaseline = false;
            capturing = true;
            btn.Text = "按下新键...";
        }

        private void SettingForm_KeyDown(object? sender, KeyEventArgs e)
        {
            if (!capturingB1 && !capturingB2 && !capturingToggle && !capturingOverlay && !capturingStartMulti &&
                !capturingSetBaseline && !capturingCancelBaseline)
                return;

            if (e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.Menu || e.KeyCode == Keys.ShiftKey)
                return;

            int modifiers = 0;
            if (e.Control) modifiers |= 0x0002;
            if (e.Alt) modifiers |= 0x0001;
            if (e.Shift) modifiers |= 0x0004;

            int keyCode = (int)e.KeyCode;

            if (capturingB1) { B1Key = keyCode; B1Modifiers = modifiers; capturingB1 = false; }
            else if (capturingB2) { B2Key = keyCode; B2Modifiers = modifiers; capturingB2 = false; }
            else if (capturingToggle) { ToggleVisibleKey = keyCode; ToggleVisibleModifiers = modifiers; capturingToggle = false; }
            else if (capturingOverlay) { ToggleOverlayKey = keyCode; ToggleOverlayModifiers = modifiers; capturingOverlay = false; }
            else if (capturingStartMulti) { StartMultiKey = keyCode; StartMultiModifiers = modifiers; capturingStartMulti = false; }
            else if (capturingSetBaseline) { SetBaselineKey = keyCode; SetBaselineModifiers = modifiers; capturingSetBaseline = false; }
            else if (capturingCancelBaseline) { CancelBaselineKey = keyCode; CancelBaselineModifiers = modifiers; capturingCancelBaseline = false; }

            UpdateButtonTexts();
            e.SuppressKeyPress = true;
        }

        private void UpdateButtonTexts()
        {
            btnB1.Text = KeyCodeToString(B1Key, B1Modifiers);
            btnB2.Text = KeyCodeToString(B2Key, B2Modifiers);
            btnToggle.Text = KeyCodeToString(ToggleVisibleKey, ToggleVisibleModifiers);
            btnOverlay.Text = KeyCodeToString(ToggleOverlayKey, ToggleOverlayModifiers);
            btnStartMulti.Text = KeyCodeToString(StartMultiKey, StartMultiModifiers);
            btnSetBaseline.Text = KeyCodeToString(SetBaselineKey, SetBaselineModifiers);
            btnCancelBaseline.Text = KeyCodeToString(CancelBaselineKey, CancelBaselineModifiers);
        }

        private string KeyCodeToString(int key, int modifiers)
        {
            string result = "";
            if ((modifiers & 0x0002) != 0) result += "Ctrl+";
            if ((modifiers & 0x0001) != 0) result += "Alt+";
            if ((modifiers & 0x0004) != 0) result += "Shift+";
            result += ((Keys)key).ToString();
            return result;
        }

        private void ApplyLanguage()
        {
            this.Text = _mainForm.GetString("SettingsTitle");
            tabHotkeys.Text = _mainForm.GetString("TabHotkeys");
            tabHeightDiff.Text = _mainForm.GetString("TabHeightDiff");
            lblB1.Text = _mainForm.GetString("HotKey_B1");
            lblB2.Text = _mainForm.GetString("HotKey_B2");
            lblToggle.Text = _mainForm.GetString("HotKey_ToggleVisible");
            lblOverlay.Text = _mainForm.GetString("HotKey_ToggleOverlay");
            lblStartMulti.Text = _mainForm.GetString("HotKey_StartMulti");
            lblSetBaseline.Text = _mainForm.GetString("HotKey_SetBaseline");
            lblCancelBaseline.Text = _mainForm.GetString("HotKey_CancelBaseline");
            chkSameKey.Text = _mainForm.GetString("SameKeyForBaseline");
            gbBasic.Text = _mainForm.GetString("BasicDisplayGroup");
            gbAdvanced.Text = _mainForm.GetString("AdvancedDisplayGroup");
            btnSave.Text = _mainForm.GetString("HotKey_BtnSave");
            btnCancel.Text = _mainForm.GetString("HotKey_BtnCancel");
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            // 直接更新传入的 HotKeySettings 对象
            _current.ShowTime = chkBasicT.Checked;
            // 同步更新公共属性（供 MainForm 读取）
            ShowTime = chkBasicT.Checked;

            // 其他显示项同理
            _current.ShowDistance = chkBasicD.Checked;
            _current.ShowAngle = chkBasicTheta.Checked;
            _current.ShowVx = chkBasicVx.Checked;
            _current.ShowL = chkAdvancedL.Checked;
            _current.ShowX = chkAdvancedX.Checked;
            _current.ShowBeta = chkAdvancedBeta.Checked;
            _current.ShowTPrime = chkTPrime.Checked;

            this.DialogResult = DialogResult.OK;
        }
    }
}