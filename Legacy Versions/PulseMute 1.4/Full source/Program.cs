using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PulseMute
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            EnableDpiAwareness();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        private static void EnableDpiAwareness()
        {
            try
            {
                if (SetProcessDpiAwarenessContext(new IntPtr(-4)))
                    return;
            }
            catch
            {
            }

            try
            {
                SetProcessDPIAware();
            }
            catch
            {
            }
        }

        [DllImport("user32.dll")]
        private static extern bool SetProcessDpiAwarenessContext(IntPtr dpiContext);

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();
    }

    internal sealed class MainForm : Form
    {
        private const int WhKeyboardLl = 13;
        private const int WmKeydown = 0x0100;
        private const int WmKeyup = 0x0101;
        private const int WmSyskeydown = 0x0104;
        private const int WmSyskeyup = 0x0105;

        private readonly MicController mic = new MicController();
        private readonly Icon appIcon;
        private readonly NotifyIcon tray;
        private readonly Timer refreshTimer;
        private readonly Label titleLabel;
        private readonly Label statusLabel;
        private readonly Label deviceLabel;
        private readonly Label shortcutLabel;
        private readonly Label shortcutValueLabel;
        private readonly Button shortcutButton;
        private readonly Button settingsButton;
        private readonly Button topButton;
        private readonly Button refreshButton;
        private readonly Button hideButton;
        private readonly Label creditLabel;
        private readonly Panel creditRuleLeft;
        private readonly Panel creditRuleRight;
        private readonly RoundButton toggleButton;
        private readonly LowLevelKeyboardProc keyboardProc;
        private IntPtr keyboardHook = IntPtr.Zero;
        private uint currentKey = (uint)Keys.F8;
        private bool captureNextShortcut;
        private bool shortcutHeld;
        private bool stayOnTop;
        private bool hideFromTaskbar;
        private bool rememberWindowPlacement = true;
        private bool darkMode = true;
        private bool windowSettingsReady;

        public MainForm()
        {
            keyboardProc = KeyboardHookCallback;
            appIcon = LoadAppIcon();

            Text = "PulseMute";
            Icon = appIcon;
            Width = 300;
            Height = 340;
            MinimumSize = new Size(220, 260);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(17, 20, 24);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 10F);
            AutoScaleMode = AutoScaleMode.Dpi;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;

            titleLabel = new Label();
            titleLabel.Text = "PulseMute";
            titleLabel.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold);
            titleLabel.AutoSize = false;
            titleLabel.AutoEllipsis = true;
            titleLabel.Location = new Point(18, 18);

            statusLabel = new Label();
            statusLabel.Text = "Checking...";
            statusLabel.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            statusLabel.AutoSize = false;
            statusLabel.TextAlign = ContentAlignment.MiddleCenter;
            statusLabel.Location = new Point(189, 23);
            statusLabel.Size = new Size(74, 28);
            statusLabel.BackColor = Color.FromArgb(47, 58, 68);
            statusLabel.ForeColor = Color.White;

            deviceLabel = new Label();
            deviceLabel.Text = "Default microphone";
            deviceLabel.AutoEllipsis = true;
            deviceLabel.Location = new Point(20, 64);
            deviceLabel.Size = new Size(242, 24);
            deviceLabel.ForeColor = Color.FromArgb(180, 190, 200);

            toggleButton = new RoundButton();
            toggleButton.Location = new Point(79, 92);
            toggleButton.Size = new Size(126, 126);
            toggleButton.Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold);
            toggleButton.Text = "Toggle";
            toggleButton.Cursor = Cursors.Hand;
            toggleButton.Click += delegate { ToggleMute(); };

            shortcutLabel = new Label();
            shortcutLabel.Text = "Key";
            shortcutLabel.AutoSize = false;
            shortcutLabel.Location = new Point(20, 229);
            shortcutLabel.Size = new Size(42, 24);
            shortcutLabel.ForeColor = Color.FromArgb(144, 154, 166);

            shortcutValueLabel = new Label();
            shortcutValueLabel.Text = HotkeyText(currentKey);
            shortcutValueLabel.AutoEllipsis = true;
            shortcutValueLabel.Location = new Point(62, 229);
            shortcutValueLabel.Size = new Size(90, 24);
            shortcutValueLabel.ForeColor = Color.FromArgb(226, 231, 235);

            shortcutButton = CreateSmallButton("Change", new Point(167, 224));
            shortcutButton.Size = new Size(96, 30);
            shortcutButton.Click += delegate
            {
                captureNextShortcut = true;
                shortcutButton.Text = "Press key";
                shortcutButton.Focus();
            };
            shortcutButton.KeyDown += CaptureShortcut;

            settingsButton = CreateSmallButton("\u2699", new Point(72, 22));
            settingsButton.Size = new Size(32, 28);
            settingsButton.Font = new Font("Segoe UI Symbol", 12F, FontStyle.Regular);
            settingsButton.AccessibleName = "Settings";
            settingsButton.Click += delegate { ShowSettings(); };
            ToolTip settingsTip = new ToolTip();
            settingsTip.SetToolTip(settingsButton, "Settings");

            topButton = CreateSmallButton("Top", new Point(108, 286));
            topButton.Size = new Size(62, 28);
            topButton.Click += delegate { ToggleStayOnTop(); };

            creditLabel = new Label();
            creditLabel.Text = "BUILT BY WRAKEEB";
            creditLabel.AutoSize = false;
            creditLabel.TextAlign = ContentAlignment.MiddleCenter;
            creditLabel.Location = new Point(58, 261);
            creditLabel.Size = new Size(168, 20);
            creditLabel.Font = new Font("Segoe UI Semibold", 8F, FontStyle.Bold);
            creditLabel.ForeColor = Color.FromArgb(170, 180, 191);

            creditRuleLeft = new Panel();
            creditRuleLeft.BackColor = Color.FromArgb(193, 53, 69);
            creditRuleRight = new Panel();
            creditRuleRight.BackColor = Color.FromArgb(193, 53, 69);

            refreshButton = CreateSmallButton("Refresh", new Point(20, 286));
            refreshButton.Size = new Size(76, 28);
            refreshButton.Click += delegate { RefreshState(); };

            hideButton = CreateSmallButton("Hide", new Point(187, 286));
            hideButton.Size = new Size(76, 28);
            hideButton.Click += delegate { HideToTray(); };

            Controls.Add(titleLabel);
            Controls.Add(statusLabel);
            Controls.Add(deviceLabel);
            Controls.Add(toggleButton);
            Controls.Add(shortcutLabel);
            Controls.Add(shortcutValueLabel);
            Controls.Add(shortcutButton);
            Controls.Add(settingsButton);
            Controls.Add(topButton);
            Controls.Add(creditRuleLeft);
            Controls.Add(creditLabel);
            Controls.Add(creditRuleRight);
            Controls.Add(refreshButton);
            Controls.Add(hideButton);

            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("Show", null, delegate { ShowFromTray(); });
            menu.Items.Add("Toggle mute", null, delegate { ToggleMute(); });
            menu.Items.Add("Exit", null, delegate { Close(); });

            tray = new NotifyIcon();
            tray.Text = "PulseMute";
            tray.Icon = appIcon;
            tray.ContextMenuStrip = menu;
            tray.Visible = true;
            tray.DoubleClick += delegate { ShowFromTray(); };

            refreshTimer = new Timer();
            refreshTimer.Interval = 1200;
            refreshTimer.Tick += delegate { RefreshState(); };
            refreshTimer.Start();

            Load += delegate
            {
                LoadShortcut();
                LoadWindowSettings();
                ApplyTheme();
                InstallKeyboardHook();
                RefreshState();
                windowSettingsReady = true;
            };
            FormClosing += delegate
            {
                SaveWindowSettings();
                UninstallKeyboardHook();
                tray.Visible = false;
                tray.Dispose();
            };
            Move += delegate { SaveWindowSettings(); };
            ResizeEnd += delegate { SaveWindowSettings(); };
            Resize += delegate
            {
                if (WindowState == FormWindowState.Minimized)
                    HideToTray();
                else
                    ApplyResponsiveLayout();
            };

            ApplyResponsiveLayout();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplyCaptionTheme(Handle, darkMode);
        }

        private Button CreateSmallButton(string text, Point location)
        {
            Button button = new Button();
            button.Text = text;
            button.Location = location;
            button.Size = new Size(82, 32);
            button.FlatStyle = FlatStyle.Flat;
            button.BackColor = Color.FromArgb(31, 36, 43);
            button.ForeColor = Color.White;
            button.Cursor = Cursors.Hand;
            button.FlatAppearance.BorderColor = Color.FromArgb(54, 62, 72);
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(42, 49, 59);
            return button;
        }

        private static Icon LoadAppIcon()
        {
            try
            {
                Icon extracted = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (extracted != null)
                    return extracted;
            }
            catch
            {
            }

            return IconFactory.Create(false, false);
        }

        private void ApplyTheme()
        {
            Color background = darkMode ? Color.FromArgb(17, 20, 24) : Color.FromArgb(245, 247, 250);
            Color primaryText = darkMode ? Color.White : Color.FromArgb(24, 28, 34);
            Color secondaryText = darkMode ? Color.FromArgb(180, 190, 200) : Color.FromArgb(75, 84, 96);
            Color mutedText = darkMode ? Color.FromArgb(144, 154, 166) : Color.FromArgb(96, 106, 118);

            BackColor = background;
            ForeColor = primaryText;
            titleLabel.ForeColor = primaryText;
            deviceLabel.ForeColor = secondaryText;
            shortcutLabel.ForeColor = mutedText;
            shortcutValueLabel.ForeColor = primaryText;
            creditLabel.ForeColor = darkMode ? Color.FromArgb(170, 180, 191) : Color.FromArgb(78, 87, 99);

            ApplyButtonTheme(shortcutButton);
            ApplyButtonTheme(settingsButton);
            ApplyButtonTheme(topButton);
            ApplyButtonTheme(refreshButton);
            ApplyButtonTheme(hideButton);
            if (stayOnTop)
            {
                topButton.BackColor = Color.FromArgb(26, 124, 88);
                topButton.ForeColor = Color.White;
            }

            ApplyCaptionTheme(Handle, darkMode);
            Invalidate(true);
        }

        private void ApplyButtonTheme(Button button)
        {
            button.BackColor = darkMode ? Color.FromArgb(31, 36, 43) : Color.White;
            button.ForeColor = darkMode ? Color.White : Color.FromArgb(32, 37, 44);
            button.FlatAppearance.BorderColor = darkMode ? Color.FromArgb(54, 62, 72) : Color.FromArgb(195, 202, 211);
            button.FlatAppearance.MouseOverBackColor = darkMode ? Color.FromArgb(42, 49, 59) : Color.FromArgb(232, 236, 241);
        }

        private static void ApplyCaptionTheme(IntPtr windowHandle, bool useDarkMode)
        {
            int enabled = useDarkMode ? 1 : 0;
            DwmSetWindowAttribute(windowHandle, 20, ref enabled, sizeof(int));
            DwmSetWindowAttribute(windowHandle, 19, ref enabled, sizeof(int));

            Color background = useDarkMode ? Color.FromArgb(17, 20, 24) : Color.FromArgb(245, 247, 250);
            Color foreground = useDarkMode ? Color.White : Color.FromArgb(24, 28, 34);
            int captionColor = ColorRef(background);
            int borderColor = ColorRef(background);
            int textColor = ColorRef(foreground);
            DwmSetWindowAttribute(windowHandle, 35, ref captionColor, sizeof(int));
            DwmSetWindowAttribute(windowHandle, 34, ref borderColor, sizeof(int));
            DwmSetWindowAttribute(windowHandle, 36, ref textColor, sizeof(int));
        }

        private static int ColorRef(Color color)
        {
            return color.R | (color.G << 8) | (color.B << 16);
        }

        private void ShowSettings()
        {
            using (Form dialog = new Form())
            {
                dialog.Text = "PulseMute settings";
                dialog.ClientSize = new Size(380, 330);
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;
                dialog.ShowInTaskbar = false;
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.BackColor = Color.FromArgb(17, 20, 24);
                dialog.ForeColor = Color.White;
                dialog.Font = new Font("Segoe UI", 10F);
                dialog.HandleCreated += delegate { ApplyCaptionTheme(dialog.Handle, darkMode); };

                Label heading = new Label();
                heading.Text = "Settings";
                heading.Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold);
                heading.SetBounds(18, 14, 210, 34);

                Label autoLabel = new Label();
                autoLabel.Text = "Start with Windows";
                autoLabel.SetBounds(20, 60, 205, 28);
                autoLabel.TextAlign = ContentAlignment.MiddleLeft;

                CheckBox autoToggle = new CheckBox();
                autoToggle.Appearance = Appearance.Button;
                autoToggle.FlatStyle = FlatStyle.Flat;
                autoToggle.FlatAppearance.BorderColor = Color.FromArgb(54, 62, 72);
                autoToggle.TextAlign = ContentAlignment.MiddleCenter;
                autoToggle.Cursor = Cursors.Hand;
                autoToggle.SetBounds(286, 57, 74, 30);
                autoToggle.Checked = IsAutoStartEnabled();
                autoToggle.Text = autoToggle.Checked ? "On" : "Off";
                autoToggle.BackColor = autoToggle.Checked ? Color.FromArgb(26, 124, 88) : Color.FromArgb(31, 36, 43);

                Label taskbarLabel = new Label();
                taskbarLabel.Text = "Hide window from taskbar";
                taskbarLabel.SetBounds(20, 101, 230, 28);
                taskbarLabel.TextAlign = ContentAlignment.MiddleLeft;

                CheckBox taskbarToggle = new CheckBox();
                taskbarToggle.Appearance = Appearance.Button;
                taskbarToggle.FlatStyle = FlatStyle.Flat;
                taskbarToggle.FlatAppearance.BorderColor = Color.FromArgb(54, 62, 72);
                taskbarToggle.TextAlign = ContentAlignment.MiddleCenter;
                taskbarToggle.Cursor = Cursors.Hand;
                taskbarToggle.SetBounds(286, 96, 74, 30);
                taskbarToggle.Checked = hideFromTaskbar;
                taskbarToggle.Text = taskbarToggle.Checked ? "On" : "Off";
                taskbarToggle.BackColor = taskbarToggle.Checked ? Color.FromArgb(26, 124, 88) : Color.FromArgb(31, 36, 43);

                Label rememberLabel = new Label();
                rememberLabel.Text = "Remember size and position";
                rememberLabel.SetBounds(20, 135, 245, 28);
                rememberLabel.TextAlign = ContentAlignment.MiddleLeft;

                CheckBox rememberToggle = new CheckBox();
                rememberToggle.Appearance = Appearance.Button;
                rememberToggle.FlatStyle = FlatStyle.Flat;
                rememberToggle.TextAlign = ContentAlignment.MiddleCenter;
                rememberToggle.Cursor = Cursors.Hand;
                rememberToggle.SetBounds(286, 134, 74, 30);
                rememberToggle.Checked = rememberWindowPlacement;
                rememberToggle.Text = rememberToggle.Checked ? "On" : "Off";

                Label appearanceLabel = new Label();
                appearanceLabel.Text = "Appearance";
                appearanceLabel.SetBounds(20, 176, 150, 28);
                appearanceLabel.TextAlign = ContentAlignment.MiddleLeft;

                RadioButton darkChoice = new RadioButton();
                darkChoice.Appearance = Appearance.Button;
                darkChoice.FlatStyle = FlatStyle.Flat;
                darkChoice.Text = "Dark";
                darkChoice.TextAlign = ContentAlignment.MiddleCenter;
                darkChoice.Cursor = Cursors.Hand;
                darkChoice.SetBounds(214, 175, 72, 30);
                darkChoice.Checked = darkMode;

                RadioButton lightChoice = new RadioButton();
                lightChoice.Appearance = Appearance.Button;
                lightChoice.FlatStyle = FlatStyle.Flat;
                lightChoice.Text = "White";
                lightChoice.TextAlign = ContentAlignment.MiddleCenter;
                lightChoice.Cursor = Cursors.Hand;
                lightChoice.SetBounds(288, 175, 72, 30);
                lightChoice.Checked = !darkMode;

                Label microphoneLabel = new Label();
                microphoneLabel.Text = "Microphone";
                microphoneLabel.SetBounds(20, 216, 340, 22);

                ComboBox microphoneBox = new ComboBox();
                microphoneBox.DropDownStyle = ComboBoxStyle.DropDownList;
                microphoneBox.FlatStyle = FlatStyle.Flat;
                microphoneBox.BackColor = Color.FromArgb(31, 36, 43);
                microphoneBox.ForeColor = Color.White;
                microphoneBox.SetBounds(20, 240, 340, 30);

                List<MicDeviceInfo> devices = mic.GetCaptureDevices();
                devices.Insert(0, new MicDeviceInfo(null, "Windows default microphone"));
                int selectedIndex = 0;
                for (int i = 0; i < devices.Count; i++)
                {
                    microphoneBox.Items.Add(devices[i]);
                    if (!string.IsNullOrEmpty(mic.SelectedDeviceId) &&
                        string.Equals(devices[i].Id, mic.SelectedDeviceId, StringComparison.OrdinalIgnoreCase))
                        selectedIndex = i;
                }
                microphoneBox.SelectedIndex = selectedIndex;

                Button soundSettingsButton = CreateSmallButton("Open sound settings", new Point(20, 288));
                soundSettingsButton.Size = new Size(178, 30);
                soundSettingsButton.Click += delegate { OpenWindowsSoundSettings(dialog); };

                Button closeButton = CreateSmallButton("Close", new Point(286, 289));
                closeButton.Size = new Size(74, 28);
                closeButton.Click += delegate { dialog.Close(); };
                dialog.AcceptButton = closeButton;

                Action applyDialogTheme = delegate
                {
                    Color background = darkMode ? Color.FromArgb(17, 20, 24) : Color.FromArgb(245, 247, 250);
                    Color foreground = darkMode ? Color.White : Color.FromArgb(24, 28, 34);
                    Color buttonBackground = darkMode ? Color.FromArgb(31, 36, 43) : Color.White;
                    Color border = darkMode ? Color.FromArgb(54, 62, 72) : Color.FromArgb(195, 202, 211);

                    dialog.BackColor = background;
                    dialog.ForeColor = foreground;
                    foreach (Label label in new Label[] { heading, autoLabel, taskbarLabel, rememberLabel, appearanceLabel, microphoneLabel })
                        label.ForeColor = foreground;

                    foreach (CheckBox toggle in new CheckBox[] { autoToggle, taskbarToggle, rememberToggle })
                    {
                        toggle.ForeColor = toggle.Checked ? Color.White : foreground;
                        toggle.BackColor = toggle.Checked ? Color.FromArgb(26, 124, 88) : buttonBackground;
                        toggle.FlatAppearance.BorderColor = border;
                        toggle.Text = toggle.Checked ? "On" : "Off";
                    }

                    foreach (RadioButton choice in new RadioButton[] { darkChoice, lightChoice })
                    {
                        choice.ForeColor = choice.Checked ? Color.White : foreground;
                        choice.BackColor = choice.Checked ? Color.FromArgb(193, 53, 69) : buttonBackground;
                        choice.FlatAppearance.BorderColor = border;
                    }

                    microphoneBox.BackColor = buttonBackground;
                    microphoneBox.ForeColor = foreground;
                    foreach (Button button in new Button[] { soundSettingsButton, closeButton })
                    {
                        button.BackColor = buttonBackground;
                        button.ForeColor = foreground;
                        button.FlatAppearance.BorderColor = border;
                        button.FlatAppearance.MouseOverBackColor = darkMode ? Color.FromArgb(42, 49, 59) : Color.FromArgb(232, 236, 241);
                    }

                    ApplyCaptionTheme(dialog.Handle, darkMode);
                    dialog.Invalidate(true);
                };

                bool changingAutoStart = false;
                autoToggle.CheckedChanged += delegate
                {
                    if (changingAutoStart)
                        return;

                    try
                    {
                        SetAutoStartEnabled(autoToggle.Checked);
                        applyDialogTheme();
                    }
                    catch (Exception ex)
                    {
                        changingAutoStart = true;
                        autoToggle.Checked = !autoToggle.Checked;
                        changingAutoStart = false;
                        applyDialogTheme();
                        MessageBox.Show(dialog, ex.Message, "PulseMute", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                };

                taskbarToggle.CheckedChanged += delegate
                {
                    hideFromTaskbar = taskbarToggle.Checked;
                    ShowInTaskbar = !hideFromTaskbar;
                    SaveSettingsFile();
                    applyDialogTheme();
                };

                rememberToggle.CheckedChanged += delegate
                {
                    rememberWindowPlacement = rememberToggle.Checked;
                    SaveSettingsFile();
                    applyDialogTheme();
                };

                darkChoice.CheckedChanged += delegate
                {
                    if (!darkChoice.Checked)
                        return;

                    darkMode = true;
                    ApplyTheme();
                    SaveSettingsFile();
                    applyDialogTheme();
                };

                lightChoice.CheckedChanged += delegate
                {
                    if (!lightChoice.Checked)
                        return;

                    darkMode = false;
                    ApplyTheme();
                    SaveSettingsFile();
                    applyDialogTheme();
                };

                microphoneBox.SelectedIndexChanged += delegate
                {
                    MicDeviceInfo selected = microphoneBox.SelectedItem as MicDeviceInfo;
                    if (selected == null)
                        return;

                    mic.SelectedDeviceId = selected.Id;
                    SaveSettingsFile();
                    RefreshState();
                };

                dialog.Controls.Add(heading);
                dialog.Controls.Add(autoLabel);
                dialog.Controls.Add(autoToggle);
                dialog.Controls.Add(taskbarLabel);
                dialog.Controls.Add(taskbarToggle);
                dialog.Controls.Add(rememberLabel);
                dialog.Controls.Add(rememberToggle);
                dialog.Controls.Add(appearanceLabel);
                dialog.Controls.Add(darkChoice);
                dialog.Controls.Add(lightChoice);
                dialog.Controls.Add(microphoneLabel);
                dialog.Controls.Add(microphoneBox);
                dialog.Controls.Add(soundSettingsButton);
                dialog.Controls.Add(closeButton);
                applyDialogTheme();
                dialog.ShowDialog(this);
            }
        }

        private static void OpenWindowsSoundSettings(IWin32Window owner)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo("ms-settings:sound");
                startInfo.UseShellExecute = true;
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show(owner, ex.Message, "PulseMute", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ToggleMute()
        {
            try
            {
                bool next = !mic.GetMuted();
                mic.SetMuted(next);
                RefreshState();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "PulseMute", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                RefreshState();
            }
        }

        private void RefreshState()
        {
            try
            {
                bool muted = mic.GetMuted();
                deviceLabel.Text = mic.GetDeviceName();
                statusLabel.Text = muted ? "Muted" : "Live";
                statusLabel.BackColor = muted ? Color.FromArgb(160, 42, 57) : Color.FromArgb(26, 124, 88);
                toggleButton.Text = muted ? "Unmute" : "Mute";
                toggleButton.PrimaryColor = muted ? Color.FromArgb(193, 53, 69) : Color.FromArgb(24, 151, 104);
                toggleButton.SecondaryColor = muted ? Color.FromArgb(117, 35, 48) : Color.FromArgb(21, 91, 83);
                toggleButton.Invalidate();
                tray.Icon = appIcon;
                tray.Text = muted ? "PulseMute - muted" : "PulseMute - live";
            }
            catch (Exception ex)
            {
                statusLabel.Text = "No mic";
                statusLabel.BackColor = Color.FromArgb(103, 65, 25);
                toggleButton.Text = "No mic";
                toggleButton.PrimaryColor = Color.FromArgb(94, 69, 40);
                toggleButton.SecondaryColor = Color.FromArgb(132, 86, 37);
                deviceLabel.Text = ex.Message;
                tray.Icon = appIcon;
                tray.Text = "PulseMute - microphone unavailable";
            }
        }

        private void HideToTray()
        {
            Hide();
            WindowState = FormWindowState.Normal;
            tray.ShowBalloonTip(900, "PulseMute", "Still listening for " + HotkeyText(currentKey) + ".", ToolTipIcon.Info);
        }

        private void ShowFromTray()
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }

        private void ToggleStayOnTop()
        {
            stayOnTop = !stayOnTop;
            TopMost = stayOnTop;
            topButton.Text = stayOnTop ? "On top" : "Top";
            ApplyTheme();
            SaveWindowSettings();
        }

        private void ApplyResponsiveLayout()
        {
            int width = Math.Max(1, ClientSize.Width);
            int height = Math.Max(1, ClientSize.Height);
            int pad = Clamp(Math.Min(width, height) / 18, 8, 22);
            int gap = Clamp(Math.Min(width, height) / 42, 4, 12);
            float scale = Math.Max(0.72f, Math.Min(1.8f, Math.Min(width / 300f, height / 340f)));
            float fontScale = Math.Max(0.9f, scale);

            SetControlFont(titleLabel, "Segoe UI Semibold", 18f * fontScale, FontStyle.Bold);
            SetControlFont(statusLabel, "Segoe UI Semibold", 9.5f * fontScale, FontStyle.Bold);
            SetControlFont(deviceLabel, "Segoe UI", 9.5f * fontScale, FontStyle.Regular);
            SetControlFont(shortcutLabel, "Segoe UI", 9f * fontScale, FontStyle.Regular);
            SetControlFont(shortcutValueLabel, "Segoe UI Semibold", 9.5f * fontScale, FontStyle.Bold);
            SetControlFont(shortcutButton, "Segoe UI", 8.8f * fontScale, FontStyle.Regular);
            SetControlFont(settingsButton, "Segoe UI Symbol", 12f * fontScale, FontStyle.Regular);
            SetControlFont(topButton, "Segoe UI", 8.8f * fontScale, FontStyle.Regular);
            SetControlFont(refreshButton, "Segoe UI", 8.8f * fontScale, FontStyle.Regular);
            SetControlFont(hideButton, "Segoe UI", 8.8f * fontScale, FontStyle.Regular);
            SetControlFont(creditLabel, "Segoe UI Semibold", 7.8f * fontScale, FontStyle.Bold);

            int topRowHeight = Clamp((int)(28 * scale), 22, 42);
            int statusWidth = Clamp((int)(74 * scale), 54, Math.Max(54, width / 3));
            int topWidth = Clamp((int)(62 * scale), 48, Math.Max(48, width / 4));
            int settingsWidth = Clamp((int)(34 * scale), 28, 48);
            statusLabel.SetBounds(width - pad - statusWidth, pad, statusWidth, topRowHeight);
            topButton.SetBounds(statusLabel.Left - gap - topWidth, pad, topWidth, topRowHeight);
            settingsButton.SetBounds(topButton.Left - gap - settingsWidth, pad, settingsWidth, topRowHeight);
            int titleRight = Math.Max(pad, settingsButton.Left - gap);
            titleLabel.SetBounds(pad, pad - 2, Math.Max(20, titleRight - pad), topRowHeight + 8);

            int deviceTop = pad + topRowHeight + gap;
            deviceLabel.SetBounds(pad, deviceTop, Math.Max(20, width - (pad * 2)), Clamp((int)(24 * scale), 18, 34));

            int bottomButtonHeight = Clamp((int)(28 * scale), 24, 40);
            int bottomButtonWidth = Clamp((int)(76 * scale), 58, Math.Max(58, (width - pad * 2 - gap * 2) / 3));
            int bottomTop = height - pad - bottomButtonHeight;
            refreshButton.SetBounds(pad, bottomTop, bottomButtonWidth, bottomButtonHeight);
            topButton.Height = topRowHeight;
            hideButton.SetBounds(width - pad - bottomButtonWidth, bottomTop, bottomButtonWidth, bottomButtonHeight);

            int creditHeight = Clamp((int)(20 * scale), 16, 28);
            int creditTop = bottomTop - gap - creditHeight;
            int creditTextWidth = Clamp((int)(138 * scale), 104, Math.Max(104, width - pad * 2));
            int creditLeft = (width - creditTextWidth) / 2;
            creditLabel.SetBounds(creditLeft, creditTop, creditTextWidth, creditHeight);
            int ruleY = creditTop + creditHeight / 2;
            int ruleGap = Clamp((int)(8 * scale), 5, 12);
            creditRuleLeft.SetBounds(pad, ruleY, Math.Max(0, creditLabel.Left - pad - ruleGap), 1);
            creditRuleRight.SetBounds(creditLabel.Right + ruleGap, ruleY, Math.Max(0, width - pad - creditLabel.Right - ruleGap), 1);

            int shortcutHeight = Clamp((int)(28 * scale), 22, 36);
            int shortcutTop = creditLabel.Top - gap - shortcutHeight;
            int shortcutButtonWidth = Clamp((int)(96 * scale), 70, Math.Max(70, width / 3));
            shortcutButton.SetBounds(width - pad - shortcutButtonWidth, shortcutTop, shortcutButtonWidth, shortcutHeight);
            shortcutLabel.SetBounds(pad, shortcutTop + 2, Clamp((int)(42 * scale), 28, 62), shortcutHeight);
            shortcutValueLabel.SetBounds(shortcutLabel.Right + gap, shortcutTop + 2, Math.Max(20, shortcutButton.Left - shortcutLabel.Right - gap * 2), shortcutHeight);

            int circleTop = deviceLabel.Bottom + gap;
            int circleBottom = shortcutTop - gap;
            int maxCircle = Math.Min(width - (pad * 2), circleBottom - circleTop);
            int circleSize = Clamp(maxCircle, 58, Math.Max(58, Math.Min(width - (pad * 2), height - (pad * 2))));
            int circleLeft = (width - circleSize) / 2;
            int adjustedCircleTop = circleTop + Math.Max(0, (circleBottom - circleTop - circleSize) / 2);
            toggleButton.SetBounds(circleLeft, adjustedCircleTop, circleSize, circleSize);
            SetControlFont(toggleButton, "Segoe UI Semibold", Math.Max(8f, circleSize / 9f), FontStyle.Bold);
        }

        private static int Clamp(int value, int minimum, int maximum)
        {
            return Math.Max(minimum, Math.Min(maximum, value));
        }

        private static void SetControlFont(Control control, string family, float size, FontStyle style)
        {
            float rounded = (float)Math.Round(size * 2f) / 2f;
            if (control.Font.FontFamily.Name == family && Math.Abs(control.Font.Size - rounded) < 0.1f && control.Font.Style == style)
                return;

            control.Font = new Font(family, rounded, style);
        }

        private void CaptureShortcut(object sender, KeyEventArgs e)
        {
            e.SuppressKeyPress = true;

            Keys keyCode = e.KeyCode;
            if (keyCode == Keys.ControlKey || keyCode == Keys.ShiftKey || keyCode == Keys.Menu || keyCode == Keys.LWin || keyCode == Keys.RWin)
                return;

            captureNextShortcut = false;
            ApplyShortcut((uint)keyCode, true);
            shortcutButton.Text = "Change";
        }

        private void ApplyShortcut(uint key, bool persist)
        {
            currentKey = key;
            shortcutHeld = false;
            shortcutValueLabel.Text = HotkeyText(currentKey);
            if (persist)
                SaveShortcut();
        }

        private void LoadShortcut()
        {
            string path = ConfigPath();
            if (!File.Exists(path))
                return;

            try
            {
                string[] lines = File.ReadAllLines(path);
                uint key = currentKey;
                bool legacyComboSetting = false;
                foreach (string line in lines)
                {
                    if (line.StartsWith("Modifiers=", StringComparison.OrdinalIgnoreCase))
                        legacyComboSetting = true;
                    if (line.StartsWith("Key=", StringComparison.OrdinalIgnoreCase))
                        uint.TryParse(line.Substring("Key=".Length), out key);
                }

                if (legacyComboSetting)
                {
                    currentKey = (uint)Keys.F8;
                    shortcutValueLabel.Text = HotkeyText(currentKey);
                    SaveShortcut();
                }
                else if (key != 0)
                {
                    currentKey = key;
                    shortcutValueLabel.Text = HotkeyText(currentKey);
                }
            }
            catch
            {
                shortcutValueLabel.Text = HotkeyText(currentKey);
            }
        }

        private void SaveShortcut()
        {
            if (windowSettingsReady)
            {
                SaveSettingsFile();
            }
            else
            {
                string path = ConfigPath();
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, "Key=" + currentKey + Environment.NewLine);
            }
        }

        private void LoadWindowSettings()
        {
            string path = ConfigPath();
            if (!File.Exists(path))
                return;

            try
            {
                string[] lines = File.ReadAllLines(path);
                int x = Left;
                int y = Top;
                int width = Width;
                int height = Height;
                bool savedTopMost = stayOnTop;
                bool hasBounds = false;

                foreach (string line in lines)
                {
                    if (line.StartsWith("X=", StringComparison.OrdinalIgnoreCase))
                    {
                        hasBounds = int.TryParse(line.Substring("X=".Length), out x) || hasBounds;
                    }
                    else if (line.StartsWith("Y=", StringComparison.OrdinalIgnoreCase))
                    {
                        hasBounds = int.TryParse(line.Substring("Y=".Length), out y) || hasBounds;
                    }
                    else if (line.StartsWith("Width=", StringComparison.OrdinalIgnoreCase))
                    {
                        hasBounds = int.TryParse(line.Substring("Width=".Length), out width) || hasBounds;
                    }
                    else if (line.StartsWith("Height=", StringComparison.OrdinalIgnoreCase))
                    {
                        hasBounds = int.TryParse(line.Substring("Height=".Length), out height) || hasBounds;
                    }
                    else if (line.StartsWith("TopMost=", StringComparison.OrdinalIgnoreCase))
                    {
                        bool.TryParse(line.Substring("TopMost=".Length), out savedTopMost);
                    }
                    else if (line.StartsWith("DeviceId=", StringComparison.OrdinalIgnoreCase))
                    {
                        string savedDeviceId = line.Substring("DeviceId=".Length);
                        mic.SelectedDeviceId = string.IsNullOrEmpty(savedDeviceId) ? null : savedDeviceId;
                    }
                    else if (line.StartsWith("HideFromTaskbar=", StringComparison.OrdinalIgnoreCase))
                    {
                        bool.TryParse(line.Substring("HideFromTaskbar=".Length), out hideFromTaskbar);
                    }
                    else if (line.StartsWith("RememberWindowPlacement=", StringComparison.OrdinalIgnoreCase))
                    {
                        bool.TryParse(line.Substring("RememberWindowPlacement=".Length), out rememberWindowPlacement);
                    }
                    else if (line.StartsWith("Theme=", StringComparison.OrdinalIgnoreCase))
                    {
                        darkMode = !line.Substring("Theme=".Length).Equals("White", StringComparison.OrdinalIgnoreCase);
                    }
                }

                Rectangle savedBounds = new Rectangle(x, y, Math.Max(MinimumSize.Width, width), Math.Max(MinimumSize.Height, height));
                if (rememberWindowPlacement && hasBounds && IsOnAnyScreen(savedBounds))
                {
                    StartPosition = FormStartPosition.Manual;
                    Bounds = savedBounds;
                }

                stayOnTop = savedTopMost;
                TopMost = stayOnTop;
                ShowInTaskbar = !hideFromTaskbar;
                topButton.Text = stayOnTop ? "On top" : "Top";
                ApplyTheme();
                ApplyResponsiveLayout();
            }
            catch
            {
            }
        }

        private void SaveWindowSettings()
        {
            if (!windowSettingsReady || WindowState != FormWindowState.Normal)
                return;

            SaveSettingsFile();
        }

        private void SaveSettingsFile()
        {
            string path = ConfigPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(
                path,
                "Key=" + currentKey + Environment.NewLine +
                "X=" + Left + Environment.NewLine +
                "Y=" + Top + Environment.NewLine +
                "Width=" + Width + Environment.NewLine +
                "Height=" + Height + Environment.NewLine +
                "TopMost=" + stayOnTop + Environment.NewLine +
                "DeviceId=" + (mic.SelectedDeviceId ?? string.Empty) + Environment.NewLine +
                "HideFromTaskbar=" + hideFromTaskbar + Environment.NewLine +
                "RememberWindowPlacement=" + rememberWindowPlacement + Environment.NewLine +
                "Theme=" + (darkMode ? "Dark" : "White") + Environment.NewLine);
        }

        private static bool IsOnAnyScreen(Rectangle bounds)
        {
            foreach (Screen screen in Screen.AllScreens)
            {
                if (Rectangle.Intersect(screen.WorkingArea, bounds).Width > 40 &&
                    Rectangle.Intersect(screen.WorkingArea, bounds).Height > 40)
                    return true;
            }

            return false;
        }

        private static bool IsAutoStartEnabled()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false))
            {
                return key != null && key.GetValue("PulseMuteAutoStart") != null;
            }
        }

        private static void SetAutoStartEnabled(bool enabled)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
            {
                if (key == null)
                    throw new InvalidOperationException("Windows startup settings are unavailable.");

                if (enabled)
                    key.SetValue("PulseMuteAutoStart", "\"" + Application.ExecutablePath + "\"");
                else
                    key.DeleteValue("PulseMuteAutoStart", false);
            }
        }

        private static string ConfigPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PulseMute", "settings.txt");
        }

        private static string HotkeyText(uint key)
        {
            return ((Keys)key).ToString();
        }

        private void InstallKeyboardHook()
        {
            if (keyboardHook != IntPtr.Zero)
                return;

            keyboardHook = SetWindowsHookEx(WhKeyboardLl, keyboardProc, GetModuleHandle(null), 0);
            shortcutValueLabel.Text = HotkeyText(currentKey);
        }

        private void UninstallKeyboardHook()
        {
            if (keyboardHook == IntPtr.Zero)
                return;

            UnhookWindowsHookEx(keyboardHook);
            keyboardHook = IntPtr.Zero;
        }

        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && !captureNextShortcut)
            {
                int message = wParam.ToInt32();
                Keys key = (Keys)Marshal.ReadInt32(lParam);

                if (message == WmKeydown || message == WmSyskeydown)
                {
                    if (ShortcutMatches(key) && !shortcutHeld)
                    {
                        shortcutHeld = true;
                        BeginInvoke((MethodInvoker)delegate { ToggleMute(); });
                    }
                }
                else if (message == WmKeyup || message == WmSyskeyup)
                {
                    if ((uint)key == currentKey || !ShortcutIsCurrentlyDown())
                        shortcutHeld = false;
                }
            }

            return CallNextHookEx(keyboardHook, nCode, wParam, lParam);
        }

        private bool ShortcutMatches(Keys key)
        {
            return (uint)key == currentKey;
        }

        private bool ShortcutIsCurrentlyDown()
        {
            return IsKeyDown((Keys)currentKey);
        }

        private static bool IsKeyDown(Keys key)
        {
            return (GetAsyncKeyState((int)key) & 0x8000) != 0;
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int valueSize);
    }

    internal sealed class RoundButton : Button
    {
        public Color PrimaryColor = Color.FromArgb(24, 151, 104);
        public Color SecondaryColor = Color.FromArgb(21, 91, 83);

        protected override void OnPaint(PaintEventArgs pevent)
        {
            Graphics g = pevent.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent == null ? Color.Black : Parent.BackColor);

            using (GraphicsPath path = new GraphicsPath())
            using (LinearGradientBrush brush = new LinearGradientBrush(ClientRectangle, PrimaryColor, SecondaryColor, 45f))
            using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(55, Color.Black)))
            using (Pen pen = new Pen(Color.FromArgb(80, Color.White), 2))
            {
                path.AddEllipse(2, 2, Width - 5, Height - 5);
                g.FillEllipse(shadowBrush, 8, 10, Width - 16, Height - 16);
                g.FillPath(brush, path);
                g.DrawPath(pen, path);
            }

            TextRenderer.DrawText(
                g,
                Text,
                Font,
                ClientRectangle,
                ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }

    internal static class IconFactory
    {
        public static Icon Create(bool muted, bool warning)
        {
            Bitmap bitmap = new Bitmap(32, 32);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                Color fill = warning ? Color.FromArgb(210, 132, 47) : muted ? Color.FromArgb(204, 54, 70) : Color.FromArgb(34, 169, 114);
                using (SolidBrush bg = new SolidBrush(fill))
                using (Pen white = new Pen(Color.White, 3))
                using (SolidBrush whiteBrush = new SolidBrush(Color.White))
                {
                    white.StartCap = LineCap.Round;
                    white.EndCap = LineCap.Round;

                    g.FillEllipse(bg, 1, 1, 30, 30);
                    FillRoundedRectangle(g, whiteBrush, new Rectangle(13, 7, 6, 13), 3);
                    g.DrawArc(white, 9, 13, 14, 12, 0, 180);
                    g.DrawLine(white, 16, 25, 16, 28);
                    g.DrawLine(white, 12, 28, 20, 28);

                    if (muted || warning)
                        g.DrawLine(white, 9, 8, 23, 24);
                }
            }

            IntPtr handle = bitmap.GetHicon();
            Icon icon = (Icon)Icon.FromHandle(handle).Clone();
            DestroyIcon(handle);
            bitmap.Dispose();
            return icon;
        }

        private static void FillRoundedRectangle(Graphics graphics, Brush brush, Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
                path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
                path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
                path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
                path.CloseFigure();
                graphics.FillPath(brush, path);
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);
    }

    internal sealed class MicDeviceInfo
    {
        public readonly string Id;
        public readonly string Name;

        public MicDeviceInfo(string id, string name)
        {
            Id = id;
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    internal sealed class MicController
    {
        public string SelectedDeviceId { get; set; }

        public bool GetMuted()
        {
            using (ComReleaser<IAudioEndpointVolume> endpoint = GetEndpointVolume())
            {
                bool muted;
                endpoint.Value.GetMute(out muted).ThrowIfFailed();
                return muted;
            }
        }

        public void SetMuted(bool muted)
        {
            using (ComReleaser<IAudioEndpointVolume> endpoint = GetEndpointVolume())
            {
                endpoint.Value.SetMute(muted, Guid.Empty).ThrowIfFailed();
            }
        }

        public string GetDeviceName()
        {
            IMMDevice device = GetCaptureDevice();
            try
            {
                return GetDeviceFriendlyName(device);
            }
            finally
            {
                Marshal.ReleaseComObject(device);
            }
        }

        public List<MicDeviceInfo> GetCaptureDevices()
        {
            List<MicDeviceInfo> devices = new List<MicDeviceInfo>();
            IMMDeviceEnumerator enumerator = (IMMDeviceEnumerator)(new MMDeviceEnumerator());
            try
            {
                IMMDeviceCollection collection;
                enumerator.EnumAudioEndpoints(EDataFlow.Capture, 1, out collection).ThrowIfFailed();
                try
                {
                    uint count;
                    collection.GetCount(out count).ThrowIfFailed();
                    for (uint i = 0; i < count; i++)
                    {
                        IMMDevice device;
                        collection.Item(i, out device).ThrowIfFailed();
                        try
                        {
                            devices.Add(new MicDeviceInfo(GetDeviceId(device), GetDeviceFriendlyName(device)));
                        }
                        finally
                        {
                            Marshal.ReleaseComObject(device);
                        }
                    }
                }
                finally
                {
                    Marshal.ReleaseComObject(collection);
                }
            }
            finally
            {
                Marshal.ReleaseComObject(enumerator);
            }

            devices.Sort(delegate(MicDeviceInfo left, MicDeviceInfo right)
            {
                return string.Compare(left.Name, right.Name, StringComparison.CurrentCultureIgnoreCase);
            });
            return devices;
        }

        private ComReleaser<IAudioEndpointVolume> GetEndpointVolume()
        {
            IMMDevice device = GetCaptureDevice();
            try
            {
                Guid iid = typeof(IAudioEndpointVolume).GUID;
                object obj;
                device.Activate(ref iid, 23, IntPtr.Zero, out obj).ThrowIfFailed();
                return new ComReleaser<IAudioEndpointVolume>((IAudioEndpointVolume)obj);
            }
            finally
            {
                Marshal.ReleaseComObject(device);
            }
        }

        private IMMDevice GetCaptureDevice()
        {
            IMMDeviceEnumerator enumerator = (IMMDeviceEnumerator)(new MMDeviceEnumerator());
            try
            {
                IMMDevice device;
                int result;
                if (!string.IsNullOrEmpty(SelectedDeviceId))
                {
                    result = enumerator.GetDevice(SelectedDeviceId, out device);
                }
                else
                {
                    result = enumerator.GetDefaultAudioEndpoint(EDataFlow.Capture, ERole.Communications, out device);
                    if (result < 0)
                        result = enumerator.GetDefaultAudioEndpoint(EDataFlow.Capture, ERole.Multimedia, out device);
                }
                result.ThrowIfFailed();
                return device;
            }
            finally
            {
                Marshal.ReleaseComObject(enumerator);
            }
        }

        private static string GetDeviceId(IMMDevice device)
        {
            IntPtr idPointer;
            device.GetId(out idPointer).ThrowIfFailed();
            try
            {
                return Marshal.PtrToStringUni(idPointer);
            }
            finally
            {
                Marshal.FreeCoTaskMem(idPointer);
            }
        }

        private static string GetDeviceFriendlyName(IMMDevice device)
        {
            IPropertyStore store;
            device.OpenPropertyStore(0, out store).ThrowIfFailed();
            try
            {
                PropertyKey key = new PropertyKey(new Guid(0xA45C254E, 0xDF1C, 0x4EFD, 0x80, 0x20, 0x67, 0xD1, 0x46, 0xA8, 0x50, 0xE0), 14);
                PropVariant value;
                store.GetValue(ref key, out value).ThrowIfFailed();
                try
                {
                    return string.IsNullOrEmpty(value.Value) ? "Unnamed microphone" : value.Value;
                }
                finally
                {
                    PropVariantClear(ref value);
                }
            }
            finally
            {
                Marshal.ReleaseComObject(store);
            }
        }

        [DllImport("ole32.dll")]
        private static extern int PropVariantClear(ref PropVariant propVariant);
    }

    internal sealed class ComReleaser<T> : IDisposable where T : class
    {
        public readonly T Value;

        public ComReleaser(T value)
        {
            Value = value;
        }

        public void Dispose()
        {
            if (Value != null && Marshal.IsComObject(Value))
                Marshal.ReleaseComObject(Value);
        }
    }

    internal static class HResultExtensions
    {
        public static void ThrowIfFailed(this int hresult)
        {
            if (hresult < 0)
                Marshal.ThrowExceptionForHR(hresult);
        }
    }

    internal enum EDataFlow
    {
        Render,
        Capture,
        All
    }

    internal enum ERole
    {
        Console,
        Multimedia,
        Communications
    }

    [ComImport]
    [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    internal class MMDeviceEnumerator
    {
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
    internal interface IMMDeviceEnumerator
    {
        int EnumAudioEndpoints(EDataFlow dataFlow, uint dwStateMask, out IMMDeviceCollection ppDevices);
        int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice ppDevice);
        int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string pwstrId, out IMMDevice ppDevice);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E")]
    internal interface IMMDeviceCollection
    {
        int GetCount(out uint pcDevices);
        int Item(uint nDevice, out IMMDevice ppDevice);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
    internal interface IMMDevice
    {
        int Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);
        int OpenPropertyStore(int stgmAccess, out IPropertyStore ppProperties);
        int GetId(out IntPtr ppstrId);
        int GetState(out uint pdwState);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99")]
    internal interface IPropertyStore
    {
        int GetCount(out uint cProps);
        int GetAt(uint iProp, out PropertyKey pkey);
        int GetValue(ref PropertyKey key, out PropVariant pv);
        int SetValue(ref PropertyKey key, ref PropVariant propvar);
        int Commit();
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("5CDF2C82-841E-4546-9722-0CF74078229A")]
    internal interface IAudioEndpointVolume
    {
        int RegisterControlChangeNotify(IntPtr pNotify);
        int UnregisterControlChangeNotify(IntPtr pNotify);
        int GetChannelCount(out uint pnChannelCount);
        int SetMasterVolumeLevel(float fLevelDB, Guid pguidEventContext);
        int SetMasterVolumeLevelScalar(float fLevel, Guid pguidEventContext);
        int GetMasterVolumeLevel(out float pfLevelDB);
        int GetMasterVolumeLevelScalar(out float pfLevel);
        int SetChannelVolumeLevel(uint nChannel, float fLevelDB, Guid pguidEventContext);
        int SetChannelVolumeLevelScalar(uint nChannel, float fLevel, Guid pguidEventContext);
        int GetChannelVolumeLevel(uint nChannel, out float pfLevelDB);
        int GetChannelVolumeLevelScalar(uint nChannel, out float pfLevel);
        int SetMute([MarshalAs(UnmanagedType.Bool)] bool bMute, Guid pguidEventContext);
        int GetMute([MarshalAs(UnmanagedType.Bool)] out bool pbMute);
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PropertyKey
    {
        private readonly Guid formatId;
        private readonly int propertyId;

        public PropertyKey(Guid formatId, int propertyId)
        {
            this.formatId = formatId;
            this.propertyId = propertyId;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PropVariant
    {
        private readonly ushort vt;
        private readonly ushort reserved1;
        private readonly ushort reserved2;
        private readonly ushort reserved3;
        private readonly IntPtr pointer;
        private readonly int pointer2;

        public string Value
        {
            get { return vt == 31 && pointer != IntPtr.Zero ? Marshal.PtrToStringUni(pointer) : null; }
        }
    }
}
