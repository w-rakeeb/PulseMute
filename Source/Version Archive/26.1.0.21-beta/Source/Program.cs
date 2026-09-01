using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Media;
using System.Reflection;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using HidSharp;

namespace PulseMute
{
    internal static class Program
    {
        private static System.Threading.Mutex releaseMutex;

        [STAThread]
        private static void Main()
        {
            AppDomain.CurrentDomain.AssemblyResolve += LoadEmbeddedAssembly;
            EnableDpiAwareness();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            bool ownsMutex;
            releaseMutex = new System.Threading.Mutex(true, MainForm.ReleaseMutexName(), out ownsMutex);
            if (!ownsMutex)
            {
                releaseMutex.Dispose();
                releaseMutex = null;
                return;
            }

            try
            {
                Application.Run(new MainForm());
            }
            finally
            {
                releaseMutex.ReleaseMutex();
                releaseMutex.Dispose();
                releaseMutex = null;
            }
        }

        private static Assembly LoadEmbeddedAssembly(object sender, ResolveEventArgs args)
        {
            if (!string.Equals(new AssemblyName(args.Name).Name, "HidSharp", StringComparison.OrdinalIgnoreCase))
                return null;

            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("HidSharp.dll"))
            {
                if (stream == null)
                    return null;
                byte[] bytes = new byte[stream.Length];
                int offset = 0;
                while (offset < bytes.Length)
                {
                    int count = stream.Read(bytes, offset, bytes.Length - offset);
                    if (count <= 0)
                        break;
                    offset += count;
                }
                return Assembly.Load(bytes);
            }
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

    internal sealed partial class MainForm : Form
    {
        private const string ReleaseChannel = "Beta";
        private const string ReleaseVersion = "26.1.0.21-beta";
        private static readonly int DesignVariant = 0;
        private const int WhKeyboardLl = 13;
        private const int WhMouseLl = 14;
        private const int WmKeydown = 0x0100;
        private const int WmKeyup = 0x0101;
        private const int WmSyskeydown = 0x0104;
        private const int WmSyskeyup = 0x0105;
        private const int GaRoot = 2;
        private const uint GwOwner = 4;
        private const uint LlMhfInjected = 0x00000001;

        private readonly MicController mic = new MicController();
        private readonly DualSenseControllerService dualSense = new DualSenseControllerService();
        private readonly NotificationSoundEngine notificationSound = new NotificationSoundEngine();
        private readonly Icon appIcon;
        private readonly NotifyIcon tray;
        private readonly Timer refreshTimer;
        private readonly Label titleLabel;
        private readonly Label statusLabel;
        private readonly Label deviceLabel;
        private readonly Label shortcutLabel;
        private readonly Label shortcutValueLabel;
        private readonly Button shortcutButton;
        private readonly Label shortcutTwoLabel;
        private readonly Label shortcutTwoValueLabel;
        private readonly Button shortcutTwoButton;
        private readonly Button settingsButton;
        private readonly Button topButton;
        private readonly Button refreshButton;
        private readonly Button hideButton;
        private readonly Label creditLabel;
        private readonly Panel creditRuleLeft;
        private readonly Panel creditRuleRight;
        private readonly ToolTip toolTips;
        private readonly RoundButton toggleButton;
        private readonly LowLevelKeyboardProc keyboardProc;
        private readonly LowLevelMouseProc mouseProc;
        private IntPtr keyboardHook = IntPtr.Zero;
        private IntPtr mouseHook = IntPtr.Zero;
        private uint currentKey = (uint)Keys.F8;
        private ShortcutSource shortcutSource = ShortcutSource.Keyboard;
        private DualSenseButton currentControllerButton = DualSenseButton.MicrophoneMute;
        private MouseHotkey currentMouseButton = MouseHotkey.Middle;
        private uint secondKey = (uint)Keys.F9;
        private ShortcutSource secondShortcutSource = ShortcutSource.Keyboard;
        private DualSenseButton secondControllerButton = DualSenseButton.Touchpad;
        private MouseHotkey secondMouseButton = MouseHotkey.XButton1;
        private int captureShortcutSlot;
        private bool shortcutOneHeld;
        private bool shortcutTwoHeld;
        private bool dualHotkeyEnabled = true;
        private bool developerModeEnabled;
        private bool soundFeedbackEnabled = true;
        private string soundPreset = NotificationSoundEngine.DefaultPreset;
        private int soundVolume = 70;
        private bool animationsEnabled = true;
        private bool hotkeyStripAboveCredit;
        private bool legacySettingsEnabled;
        private int muteButtonDesign;
        private bool stayOnTop;
        private bool hideFromTaskbar;
        private bool rememberWindowPlacement = true;
        private bool darkMode = true;
        private bool customColorsEnabled;
        private Color customAccentColor = Color.FromArgb(193, 53, 69);
        private Color customCreatorColor = Color.FromArgb(193, 53, 69);
        private Color customBackgroundColor = Color.FromArgb(17, 20, 24);
        private Color customSurfaceColor = Color.FromArgb(28, 33, 40);
        private Color customPrimaryTextColor = Color.FromArgb(238, 241, 245);
        private Color customSecondaryTextColor = Color.FromArgb(139, 149, 161);
        private Color customSettingsSidebarColor = Color.FromArgb(17, 20, 24);
        private Color customSettingsBorderColor = Color.FromArgb(54, 62, 72);
        private bool windowSettingsReady;

        public MainForm()
        {
            keyboardProc = KeyboardHookCallback;
            mouseProc = MouseHookCallback;
            appIcon = LoadAppIcon();

            Text = "PulseMute Beta";
            Icon = appIcon;
            Width = 300;
            Height = 380;
            MinimumSize = new Size(200, 250);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(17, 20, 24);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 10F);
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            DoubleBuffered = true;
            ResizeRedraw = true;
            KeyPreview = true;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            ConfigureEditionWindow();

            titleLabel = new Label();
            titleLabel.Text = "PulseMute Beta";
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
            toggleButton.VisualStyle = muteButtonDesign;
            toggleButton.Cursor = Cursors.Hand;
            toggleButton.Click += delegate { ToggleMute(); };

            shortcutLabel = new Label();
            shortcutLabel.Text = "KEY 1";
            shortcutLabel.AutoSize = false;
            shortcutLabel.TextAlign = ContentAlignment.MiddleLeft;
            shortcutLabel.Location = new Point(20, 229);
            shortcutLabel.Size = new Size(42, 24);
            shortcutLabel.ForeColor = Color.FromArgb(144, 154, 166);

            shortcutValueLabel = new Label();
            shortcutValueLabel.Text = "1  " + CurrentShortcutText(1);
            shortcutValueLabel.AutoEllipsis = true;
            shortcutValueLabel.TextAlign = ContentAlignment.MiddleCenter;
            shortcutValueLabel.BorderStyle = BorderStyle.FixedSingle;
            shortcutValueLabel.Location = new Point(62, 229);
            shortcutValueLabel.Size = new Size(90, 24);
            shortcutValueLabel.ForeColor = Color.FromArgb(226, 231, 235);
            shortcutValueLabel.Cursor = Cursors.Hand;
            shortcutValueLabel.AccessibleName = "Change Key 1 hotkey";

            toolTips = new ToolTip();
            toolTips.AutoPopDelay = 5000;
            toolTips.InitialDelay = 450;

            shortcutButton = CreateSmallButton("\uE70F", new Point(229, 224));
            shortcutButton.Size = new Size(34, 30);
            shortcutButton.Font = new Font("Segoe Fluent Icons", 11F, FontStyle.Regular);
            shortcutButton.AccessibleName = "Change global hotkey";
            toolTips.SetToolTip(shortcutButton, "Change global hotkey");
            shortcutButton.Click += delegate
            {
                BeginShortcutCapture(1);
            };
            shortcutButton.KeyDown += CaptureShortcut;
            shortcutButton.Visible = false;

            shortcutTwoLabel = new Label();
            shortcutTwoLabel.Text = "KEY 2";
            shortcutTwoLabel.AutoSize = false;
            shortcutTwoLabel.TextAlign = ContentAlignment.MiddleLeft;
            shortcutTwoLabel.ForeColor = Color.FromArgb(144, 154, 166);

            shortcutTwoValueLabel = new Label();
            shortcutTwoValueLabel.Text = "2  " + CurrentShortcutText(2);
            shortcutTwoValueLabel.AutoEllipsis = true;
            shortcutTwoValueLabel.TextAlign = ContentAlignment.MiddleCenter;
            shortcutTwoValueLabel.BorderStyle = BorderStyle.FixedSingle;
            shortcutTwoValueLabel.ForeColor = Color.FromArgb(226, 231, 235);
            shortcutTwoValueLabel.Cursor = Cursors.Hand;
            shortcutTwoValueLabel.AccessibleName = "Change Key 2 hotkey";

            shortcutTwoButton = CreateSmallButton("\uE70F", new Point(229, 252));
            shortcutTwoButton.Size = new Size(34, 30);
            shortcutTwoButton.Font = new Font("Segoe Fluent Icons", 11F, FontStyle.Regular);
            shortcutTwoButton.AccessibleName = "Change second global hotkey";
            toolTips.SetToolTip(shortcutTwoButton, "Change Key 2");
            shortcutTwoButton.Click += delegate
            {
                BeginShortcutCapture(2);
            };
            shortcutTwoButton.KeyDown += CaptureShortcut;
            shortcutTwoButton.Visible = false;

            shortcutValueLabel.Click += delegate { BeginShortcutCapture(1); };
            shortcutTwoValueLabel.Click += delegate { BeginShortcutCapture(2); };
            toolTips.SetToolTip(shortcutValueLabel, "Click to change Key 1");
            toolTips.SetToolTip(shortcutTwoValueLabel, "Click to change Key 2");
            KeyDown += CaptureShortcut;

            settingsButton = CreateSmallButton("\uE713", new Point(72, 22));
            settingsButton.Size = new Size(32, 28);
            settingsButton.Font = new Font("Segoe Fluent Icons", 11F, FontStyle.Regular);
            settingsButton.AccessibleName = "Settings";
            settingsButton.Click += delegate { ShowSelectedSettings(); };
            toolTips.SetToolTip(settingsButton, "Settings");

            topButton = CreateSmallButton("\uE718", new Point(108, 286));
            topButton.Size = new Size(34, 28);
            topButton.Font = new Font("Segoe Fluent Icons", 11F, FontStyle.Regular);
            topButton.AccessibleName = "Stay on top";
            toolTips.SetToolTip(topButton, "Stay on top");
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

            refreshButton = CreateSmallButton("\uE72C", new Point(20, 286));
            refreshButton.Size = new Size(40, 28);
            refreshButton.Font = new Font("Segoe Fluent Icons", 11F, FontStyle.Regular);
            refreshButton.AccessibleName = "Refresh microphone state";
            toolTips.SetToolTip(refreshButton, "Refresh microphone state");
            refreshButton.Click += delegate { RefreshState(); };

            hideButton = CreateSmallButton("\uE70D", new Point(223, 286));
            hideButton.Size = new Size(40, 28);
            hideButton.Font = new Font("Segoe Fluent Icons", 11F, FontStyle.Regular);
            hideButton.AccessibleName = "Hide to tray";
            toolTips.SetToolTip(hideButton, "Hide to tray");
            hideButton.Click += delegate { HideToTray(); };

            Controls.Add(titleLabel);
            Controls.Add(statusLabel);
            Controls.Add(deviceLabel);
            Controls.Add(toggleButton);
            Controls.Add(shortcutValueLabel);
            Controls.Add(shortcutTwoValueLabel);
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
            tray.Text = "PulseMute Beta";
            tray.Icon = appIcon;
            tray.ContextMenuStrip = menu;
            tray.Visible = true;
            tray.DoubleClick += delegate { ShowFromTray(); };

            refreshTimer = new Timer();
            refreshTimer.Interval = 350;
            refreshTimer.Tick += delegate { RefreshState(); };
            refreshTimer.Start();

            dualSense.ButtonPressed += DualSenseButtonPressed;

            Load += delegate
            {
                RemoveMatchingLegacyAutoStart();
                LoadShortcut();
                LoadWindowSettings();
                toggleButton.VisualStyle = muteButtonDesign;
                ApplyTheme();
                InstallKeyboardHook();
                InstallMouseHook();
                dualSense.Start();
                RefreshState();
                windowSettingsReady = true;
            };
            FormClosing += delegate
            {
                SaveWindowSettings();
                UninstallKeyboardHook();
                UninstallMouseHook();
                dualSense.Stop();
                notificationSound.Dispose();
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

            ApplyEditionIdentity();
            ApplyResponsiveLayout();
        }

        private void ConfigureEditionWindow()
        {
            if (DesignVariant == 1)
            {
                Width = 520;
                Height = 290;
                MinimumSize = new Size(380, 240);
            }
            else if (DesignVariant == 2)
            {
                Width = 340;
                Height = 500;
                MinimumSize = new Size(250, 380);
            }
            else if (DesignVariant == 3)
            {
                Width = 470;
                Height = 320;
                MinimumSize = new Size(350, 250);
            }
        }

        private void ApplyEditionIdentity()
        {
            if (DesignVariant == 1)
            {
                titleLabel.Text = "PulseMute / Focus";
                toggleButton.ShapeStyle = 0;
            }
            else if (DesignVariant == 2)
            {
                titleLabel.Text = "PulseMute Signal";
                toggleButton.ShapeStyle = 1;
            }
            else if (DesignVariant == 3)
            {
                titleLabel.Text = "PULSEMUTE CONSOLE";
                toggleButton.ShapeStyle = 2;
            }
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

        internal static Bitmap CreateSafeIconBitmap(Icon icon)
        {
            try
            {
                if (icon != null)
                    return icon.ToBitmap();
            }
            catch
            {
            }
            using (Icon fallback = IconFactory.Create(false, false))
                return fallback.ToBitmap();
        }

        internal static Bitmap CreateHighQualityLogoBitmap()
        {
            try
            {
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("PulseMuteProfessional.png"))
                {
                    if (stream != null)
                    {
                        using (Image image = Image.FromStream(stream))
                            return new Bitmap(image);
                    }
                }
            }
            catch
            {
            }

            using (Bitmap fallback = CreateSafeIconBitmap(null))
                return new Bitmap(fallback);
        }

        private void ApplyTheme()
        {
            Color background = ThemeBackgroundColor();
            Color primaryText = ThemePrimaryTextColor();
            Color secondaryText = ThemeSecondaryTextColor();
            Color mutedText = customColorsEnabled ? customSecondaryTextColor : (darkMode ? Color.FromArgb(144, 154, 166) : Color.FromArgb(96, 106, 118));

            BackColor = background;
            ForeColor = primaryText;
            titleLabel.ForeColor = primaryText;
            deviceLabel.ForeColor = secondaryText;
            shortcutLabel.ForeColor = mutedText;
            shortcutValueLabel.ForeColor = primaryText;
            shortcutValueLabel.BackColor = ThemeSurfaceColor();
            shortcutTwoLabel.ForeColor = mutedText;
            shortcutTwoValueLabel.ForeColor = primaryText;
            shortcutTwoValueLabel.BackColor = ThemeSurfaceColor();
            creditLabel.ForeColor = secondaryText;
            creditRuleLeft.BackColor = CreatorLineColor();
            creditRuleRight.BackColor = CreatorLineColor();

            ApplyButtonTheme(shortcutButton);
            ApplyButtonTheme(shortcutTwoButton);
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
            Color darkSurface = DesignVariant == 1 ? Color.FromArgb(24, 34, 38) :
                DesignVariant == 2 ? Color.FromArgb(35, 37, 43) :
                DesignVariant == 3 ? Color.FromArgb(25, 31, 35) : Color.FromArgb(31, 36, 43);
            button.BackColor = customColorsEnabled ? customSurfaceColor : (darkMode ? darkSurface : Color.White);
            button.ForeColor = customColorsEnabled ? customPrimaryTextColor : (darkMode ? Color.White : Color.FromArgb(32, 37, 44));
            button.FlatAppearance.BorderColor = darkMode ? Color.FromArgb(54, 62, 72) : Color.FromArgb(195, 202, 211);
            button.FlatAppearance.MouseOverBackColor = darkMode ? Color.FromArgb(42, 49, 59) : Color.FromArgb(232, 236, 241);
        }

        private Color AccentColor()
        {
            if (customColorsEnabled)
                return customAccentColor;
            if (DesignVariant == 1)
                return Color.FromArgb(35, 166, 173);
            if (DesignVariant == 2)
                return Color.FromArgb(63, 111, 198);
            if (DesignVariant == 3)
                return Color.FromArgb(217, 151, 55);
            return Color.FromArgb(193, 53, 69);
        }

        private Color CreatorLineColor()
        {
            return customColorsEnabled ? customCreatorColor : Color.FromArgb(193, 53, 69);
        }

        private Color ThemeBackgroundColor()
        {
            if (customColorsEnabled)
                return customBackgroundColor;
            if (DesignVariant == 1)
                return darkMode ? Color.FromArgb(12, 20, 23) : Color.FromArgb(243, 248, 248);
            if (DesignVariant == 2)
                return darkMode ? Color.FromArgb(22, 23, 27) : Color.FromArgb(248, 249, 251);
            if (DesignVariant == 3)
                return darkMode ? Color.FromArgb(13, 17, 20) : Color.FromArgb(243, 246, 247);
            return darkMode ? Color.FromArgb(17, 20, 24) : Color.FromArgb(245, 247, 250);
        }

        private Color ThemeSurfaceColor()
        {
            return customColorsEnabled ? customSurfaceColor : (darkMode ? Color.FromArgb(28, 33, 40) : Color.White);
        }

        private Color ThemePrimaryTextColor()
        {
            return customColorsEnabled ? customPrimaryTextColor : (darkMode ? Color.FromArgb(238, 241, 245) : Color.FromArgb(25, 29, 35));
        }

        private Color ThemeSecondaryTextColor()
        {
            return customColorsEnabled ? customSecondaryTextColor : (darkMode ? Color.FromArgb(139, 149, 161) : Color.FromArgb(102, 111, 122));
        }

        private Color SettingsSidebarColor()
        {
            return customColorsEnabled
                ? customSettingsSidebarColor
                : (darkMode ? Color.FromArgb(17, 20, 24) : Color.White);
        }

        private Color SettingsBorderColor()
        {
            return customColorsEnabled
                ? customSettingsBorderColor
                : (darkMode ? Color.FromArgb(54, 62, 72) : Color.FromArgb(205, 211, 219));
        }

        private static Color MixColor(Color from, Color to, int percent)
        {
            int amount = Math.Max(0, Math.Min(100, percent));
            return Color.FromArgb(
                from.R + ((to.R - from.R) * amount / 100),
                from.G + ((to.G - from.G) * amount / 100),
                from.B + ((to.B - from.B) * amount / 100));
        }

        private static void ApplyCaptionTheme(IntPtr windowHandle, bool useDarkMode)
        {
            int enabled = useDarkMode ? 1 : 0;
            DwmSetWindowAttribute(windowHandle, 20, ref enabled, sizeof(int));
            DwmSetWindowAttribute(windowHandle, 19, ref enabled, sizeof(int));

            Color background = useDarkMode ? Color.FromArgb(17, 20, 24) : Color.FromArgb(245, 247, 250);
            if (DesignVariant == 1)
                background = useDarkMode ? Color.FromArgb(12, 20, 23) : Color.FromArgb(243, 248, 248);
            else if (DesignVariant == 2)
                background = useDarkMode ? Color.FromArgb(22, 23, 27) : Color.FromArgb(248, 249, 251);
            else if (DesignVariant == 3)
                background = useDarkMode ? Color.FromArgb(13, 17, 20) : Color.FromArgb(243, 246, 247);
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

        private void ShowSelectedSettings()
        {
            try
            {
                if (legacySettingsEnabled)
                    ShowSettingsV15Legacy();
                else
                    ShowSettingsV15();
            }
            catch (Exception ex)
            {
                LogUiException(ex);
                MessageBox.Show(
                    this,
                    "Settings could not open. PulseMute is still running safely.\n\n" + ex.Message,
                    "PulseMute settings",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private static void LogUiException(Exception exception)
        {
            try
            {
                string directory = Path.GetDirectoryName(ConfigPath());
                Directory.CreateDirectory(directory);
                File.AppendAllText(
                    Path.Combine(directory, "error.log"),
                    DateTime.Now.ToString("u") + Environment.NewLine + exception + Environment.NewLine + Environment.NewLine);
            }
            catch
            {
            }
        }

        private void ShowSettingsV15Legacy()
        {
            using (Form dialog = new Form())
            {
                dialog.Text = "PulseMute Beta settings";
                dialog.ClientSize = new Size(390, 460);
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;
                dialog.ShowInTaskbar = false;
                dialog.TopMost = TopMost;
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.Font = new Font("Segoe UI", 10F);
                dialog.AutoScaleDimensions = new SizeF(96F, 96F);
                dialog.AutoScaleMode = AutoScaleMode.Dpi;
                dialog.HandleCreated += delegate { ApplyCaptionTheme(dialog.Handle, darkMode); };

                Panel scrollPanel = new Panel();
                scrollPanel.SetBounds(0, 0, 390, 404);
                scrollPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                scrollPanel.AutoScroll = true;
                scrollPanel.AutoScrollMinSize = new Size(0, 700);

                Label heading = new Label();
                heading.Text = "Settings";
                heading.Font = new Font("Segoe UI Semibold", 17F, FontStyle.Bold);
                heading.SetBounds(20, 14, 250, 32);

                Button newSettingsButton = CreateSmallButton("New settings", new Point(258, 16));
                newSettingsButton.Size = new Size(110, 28);

                Label versionLabel = new Label();
                versionLabel.Text = EditionVersionText() + "  |  Created by Wrakeeb";
                versionLabel.Font = new Font("Segoe UI Semibold", 7.5F, FontStyle.Bold);
                versionLabel.TextAlign = ContentAlignment.MiddleCenter;
                versionLabel.SetBounds(16, 425, 260, 18);

                Label autoLabel = CreateSettingsLabel("Start with Windows", 20, 62);
                Label autoState = CreateSettingsStateLabel(20, 84);
                ToggleSwitch autoToggle = CreateToggleSwitch(322, 68, IsAutoStartEnabled());

                Panel separatorOne = CreateSeparator(20, 115, 348);

                Label taskbarLabel = CreateSettingsLabel("Hide window from taskbar", 20, 126);
                Label taskbarState = CreateSettingsStateLabel(20, 148);
                ToggleSwitch taskbarToggle = CreateToggleSwitch(322, 132, hideFromTaskbar);

                Panel separatorTwo = CreateSeparator(20, 179, 348);

                Label rememberLabel = CreateSettingsLabel("Remember window placement", 20, 190);
                Label rememberState = CreateSettingsStateLabel(20, 212);
                ToggleSwitch rememberToggle = CreateToggleSwitch(322, 196, rememberWindowPlacement);

                Panel separatorThree = CreateSeparator(20, 243, 348);

                Label dualHotkeyLabel = CreateSettingsLabel("Dual Hotkey", 20, 254);
                Label dualHotkeyState = CreateSettingsStateLabel(20, 276);
                ToggleSwitch dualHotkeyToggle = CreateToggleSwitch(322, 260, dualHotkeyEnabled);

                Panel separatorFour = CreateSeparator(20, 307, 348);

                Label appearanceLabel = CreateSettingsLabel("Appearance", 20, 318);
                Label appearanceState = CreateSettingsStateLabel(20, 340);
                ToggleSwitch appearanceToggle = CreateToggleSwitch(322, 324, darkMode);

                Label customizationLabel = CreateSettingsLabel("Customization", 20, 368);
                customizationLabel.Width = 220;
                Label customizationState = CreateSettingsStateLabel(20, 391);
                Button customizeButton = CreateSmallButton("Customize", new Point(258, 368));
                customizeButton.Size = new Size(110, 28);

                Label controllerLabel = CreateSettingsLabel("PlayStation controller", 20, 422);
                controllerLabel.Width = 220;
                Label controllerState = CreateSettingsStateLabel(20, 445);
                controllerState.SetBounds(20, 445, 230, 18);
                Button controllerScanButton = CreateSmallButton("Rescan", new Point(258, 422));
                controllerScanButton.Size = new Size(110, 28);

                Label microphoneLabel = CreateSettingsLabel("Microphone", 20, 476);
                ThemedComboBox microphoneBox = new ThemedComboBox();
                microphoneBox.DropDownStyle = ComboBoxStyle.DropDownList;
                microphoneBox.FlatStyle = FlatStyle.Flat;
                microphoneBox.SetBounds(20, 500, 348, 30);

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

                Button soundSettingsButton = CreateSmallButton("Sound settings", new Point(20, 542));
                soundSettingsButton.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
                soundSettingsButton.Size = new Size(150, 28);
                soundSettingsButton.Click += delegate { OpenWindowsSoundSettings(dialog); };

                Panel separatorFive = CreateSeparator(20, 584, 348);

                Label soundFeedbackLabel = CreateSettingsLabel("Mute sound feedback", 20, 600);
                Label soundFeedbackState = CreateSettingsStateLabel(20, 622);
                ToggleSwitch soundFeedbackToggle = CreateToggleSwitch(322, 606, soundFeedbackEnabled);

                Label soundPresetLabel = CreateSettingsLabel("Sound style", 20, 654);
                ThemedComboBox soundPresetBox = new ThemedComboBox();
                soundPresetBox.DropDownStyle = ComboBoxStyle.DropDownList;
                soundPresetBox.FlatStyle = FlatStyle.Flat;
                soundPresetBox.SetBounds(20, 678, 348, 30);
                int soundPresetIndex = 0;
                string[] soundPresets = NotificationSoundEngine.Presets;
                for (int i = 0; i < soundPresets.Length; i++)
                {
                    soundPresetBox.Items.Add(new SoundPresetInfo(soundPresets[i]));
                    if (string.Equals(soundPresets[i], soundPreset, StringComparison.Ordinal))
                        soundPresetIndex = i;
                }
                soundPresetBox.SelectedIndex = soundPresetIndex;

                Panel separatorSix = CreateSeparator(20, 720, 348);

                Label developerHeading = CreateSettingsLabel("Developer settings", 20, 736);
                developerHeading.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);

                Label developerModeLabel = CreateSettingsLabel("Developer mode", 20, 776);
                Label developerModeState = CreateSettingsStateLabel(20, 798);
                ToggleSwitch developerModeToggle = CreateToggleSwitch(322, 782, developerModeEnabled);

                Label hotkeyStripLabel = CreateSettingsLabel("Hotkey strip position", 20, 840);
                Label hotkeyStripState = CreateSettingsStateLabel(20, 862);
                ToggleSwitch hotkeyStripToggle = CreateToggleSwitch(322, 846, hotkeyStripAboveCredit);

                Label olderVersionLabel = CreateSettingsLabel("Older version", 20, 904);
                ThemedComboBox olderVersionBox = new ThemedComboBox();
                olderVersionBox.DropDownStyle = ComboBoxStyle.DropDownList;
                olderVersionBox.FlatStyle = FlatStyle.Flat;
                olderVersionBox.SetBounds(20, 928, 238, 30);

                List<ArchivedVersionInfo> archivedVersions = DiscoverArchivedVersions();
                foreach (ArchivedVersionInfo version in archivedVersions)
                    olderVersionBox.Items.Add(version);
                if (olderVersionBox.Items.Count > 0)
                    olderVersionBox.SelectedIndex = 0;

                Button openVersionButton = CreateSmallButton("Open", new Point(268, 928));
                openVersionButton.Size = new Size(100, 30);

                Label versionsInfoLabel = CreateSettingsLabel("Versions info", 20, 972);
                Label versionInfoText = new Label();
                versionInfoText.AutoSize = false;
                versionInfoText.SetBounds(20, 996, 348, 50);
                versionInfoText.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
                versionInfoText.TextAlign = ContentAlignment.MiddleLeft;
                versionInfoText.Padding = new Padding(8, 4, 8, 4);
                versionInfoText.BorderStyle = BorderStyle.FixedSingle;

                Button doneButton = CreateSmallButton("Done", new Point(298, 416));
                doneButton.Size = new Size(74, 28);
                doneButton.Click += delegate { dialog.Close(); };
                dialog.AcceptButton = doneButton;

                Panel footerSeparator = CreateSeparator(16, 404, 358);

                Action applyDialogTheme = delegate
                {
                    Color background = ThemeBackgroundColor();
                    Color foreground = ThemePrimaryTextColor();
                    Color secondary = ThemeSecondaryTextColor();
                    Color surface = ThemeSurfaceColor();
                    Color border = darkMode ? Color.FromArgb(54, 62, 72) : Color.FromArgb(205, 211, 219);
                    Color accent = AccentColor();

                    dialog.BackColor = background;
                    scrollPanel.BackColor = background;
                    dialog.ForeColor = foreground;
                    foreach (Label label in new Label[] { heading, autoLabel, taskbarLabel, rememberLabel, dualHotkeyLabel, appearanceLabel, customizationLabel, controllerLabel, microphoneLabel, soundFeedbackLabel, soundPresetLabel, developerHeading, developerModeLabel, hotkeyStripLabel, olderVersionLabel, versionsInfoLabel })
                        label.ForeColor = foreground;
                    versionLabel.ForeColor = secondary;

                    foreach (Label state in new Label[] { autoState, taskbarState, rememberState, dualHotkeyState, appearanceState, customizationState, controllerState, soundFeedbackState, developerModeState, hotkeyStripState })
                        state.ForeColor = secondary;
                    autoState.Text = autoToggle.Checked ? "Enabled" : "Disabled";
                    taskbarState.Text = taskbarToggle.Checked ? "Enabled" : "Disabled";
                    rememberState.Text = rememberToggle.Checked ? "Enabled" : "Disabled";
                    dualHotkeyState.Text = dualHotkeyToggle.Checked ? "Key 1 and Key 2 active" : "Key 1 only";
                    appearanceState.Text = appearanceToggle.Checked ? "Dark theme" : "White theme";
                    customizationState.Text = customColorsEnabled ? "Custom colors" : "Default colors";
                    controllerState.Text = dualSense.StatusText;
                    soundFeedbackState.Text = soundFeedbackToggle.Checked ? "Enabled" : "Disabled";
                    developerModeState.Text = developerModeToggle.Checked ? "Enabled" : "Disabled";
                    hotkeyStripState.Text = hotkeyStripToggle.Checked ? "Above creator line" : "Bottom toolbar";

                    foreach (ToggleSwitch toggle in new ToggleSwitch[] { autoToggle, taskbarToggle, rememberToggle, dualHotkeyToggle, appearanceToggle, soundFeedbackToggle, developerModeToggle, hotkeyStripToggle })
                    {
                        toggle.OnColor = accent;
                        toggle.OffColor = darkMode ? Color.FromArgb(67, 75, 86) : Color.FromArgb(187, 194, 203);
                        toggle.ThumbColor = Color.White;
                        toggle.Invalidate();
                    }

                    microphoneBox.BackColor = surface;
                    microphoneBox.ForeColor = foreground;
                    microphoneBox.HighlightColor = accent;
                    microphoneBox.Invalidate();
                    soundPresetBox.BackColor = surface;
                    soundPresetBox.ForeColor = foreground;
                    soundPresetBox.HighlightColor = accent;
                    soundPresetBox.Enabled = soundFeedbackToggle.Checked;
                    soundPresetBox.Invalidate();
                    olderVersionBox.BackColor = surface;
                    olderVersionBox.ForeColor = foreground;
                    olderVersionBox.HighlightColor = accent;
                    olderVersionBox.Invalidate();
                    versionInfoText.BackColor = surface;
                    versionInfoText.ForeColor = secondary;
                    olderVersionBox.Enabled = developerModeToggle.Checked && olderVersionBox.Items.Count > 0;
                    openVersionButton.Enabled = olderVersionBox.Enabled;
                    hotkeyStripLabel.Visible = developerModeToggle.Checked;
                    hotkeyStripState.Visible = developerModeToggle.Checked;
                    hotkeyStripToggle.Visible = developerModeToggle.Checked;
                    olderVersionLabel.Visible = developerModeToggle.Checked;
                    olderVersionBox.Visible = developerModeToggle.Checked;
                    openVersionButton.Visible = developerModeToggle.Checked;
                    versionsInfoLabel.Visible = developerModeToggle.Checked;
                    versionInfoText.Visible = developerModeToggle.Checked;
                    int contentBottom = developerModeToggle.Checked ? versionInfoText.Bottom + 14 : developerModeToggle.Bottom + 20;
                    scrollPanel.AutoScrollMinSize = new Size(0, contentBottom);
                    foreach (Panel separator in new Panel[] { separatorOne, separatorTwo, separatorThree, separatorFour, separatorFive, separatorSix, footerSeparator })
                        separator.BackColor = border;
                    foreach (Button button in new Button[] { newSettingsButton, customizeButton, controllerScanButton, soundSettingsButton, openVersionButton, doneButton })
                    {
                        button.BackColor = surface;
                        button.ForeColor = foreground;
                        button.FlatAppearance.BorderColor = border;
                        button.FlatAppearance.MouseOverBackColor = darkMode ? Color.FromArgb(39, 45, 54) : Color.FromArgb(235, 238, 242);
                    }

                    ApplyCaptionTheme(dialog.Handle, darkMode);
                    SetWindowTheme(scrollPanel.Handle, darkMode ? "DarkMode_Explorer" : "Explorer", null);
                    dialog.Invalidate(true);
                };

                bool changingAutoStart = false;
                newSettingsButton.Click += delegate
                {
                    legacySettingsEnabled = false;
                    SaveSettingsFile();
                    dialog.Close();
                    BeginInvoke((MethodInvoker)delegate { ShowSelectedSettings(); });
                };

                autoToggle.CheckedChanged += delegate
                {
                    if (changingAutoStart)
                        return;
                    try
                    {
                        SetAutoStartEnabled(autoToggle.Checked);
                    }
                    catch (Exception ex)
                    {
                        changingAutoStart = true;
                        autoToggle.Checked = !autoToggle.Checked;
                        changingAutoStart = false;
                        MessageBox.Show(dialog, ex.Message, "PulseMute", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    applyDialogTheme();
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

                dualHotkeyToggle.CheckedChanged += delegate
                {
                    dualHotkeyEnabled = dualHotkeyToggle.Checked;
                    shortcutTwoHeld = false;
                    UpdateShortcutDisplay();
                    SaveSettingsFile();
                    applyDialogTheme();
                };

                soundFeedbackToggle.CheckedChanged += delegate
                {
                    soundFeedbackEnabled = soundFeedbackToggle.Checked;
                    SaveSettingsFile();
                    applyDialogTheme();
                };

                soundPresetBox.SelectedIndexChanged += delegate
                {
                    SoundPresetInfo selected = soundPresetBox.SelectedItem as SoundPresetInfo;
                    if (selected == null)
                        return;
                    soundPreset = selected.Name;
                    SaveSettingsFile();
                };

                developerModeToggle.CheckedChanged += delegate
                {
                    developerModeEnabled = developerModeToggle.Checked;
                    SaveSettingsFile();
                    applyDialogTheme();
                };

                hotkeyStripToggle.CheckedChanged += delegate
                {
                    hotkeyStripAboveCredit = hotkeyStripToggle.Checked;
                    ApplyResponsiveLayout();
                    SaveSettingsFile();
                    applyDialogTheme();
                };

                appearanceToggle.CheckedChanged += delegate
                {
                    if (darkMode == appearanceToggle.Checked)
                        return;
                    darkMode = appearanceToggle.Checked;
                    ApplyTheme();
                    SaveSettingsFile();
                    applyDialogTheme();
                };

                customizeButton.Click += delegate
                {
                    ShowColorCustomization(dialog, applyDialogTheme);
                    applyDialogTheme();
                };

                controllerScanButton.Click += delegate
                {
                    dualSense.Rescan();
                    controllerState.Text = "Searching...";
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

                openVersionButton.Click += delegate
                {
                    ArchivedVersionInfo selected = olderVersionBox.SelectedItem as ArchivedVersionInfo;
                    if (selected == null)
                        return;

                    try
                    {
                        Process.Start(new ProcessStartInfo(selected.ExecutablePath) { UseShellExecute = true });
                        dialog.Close();
                        BeginInvoke((MethodInvoker)delegate { Close(); });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(dialog, ex.Message, "PulseMute", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                };

                olderVersionBox.SelectedIndexChanged += delegate
                {
                    ArchivedVersionInfo selected = olderVersionBox.SelectedItem as ArchivedVersionInfo;
                    toolTips.SetToolTip(olderVersionBox, selected == null ? string.Empty : selected.DisplayName);
                    versionInfoText.Text = selected == null ? "No archived versions found." : selected.Details;
                };
                ArchivedVersionInfo initialVersion = olderVersionBox.SelectedItem as ArchivedVersionInfo;
                toolTips.SetToolTip(olderVersionBox, initialVersion == null ? string.Empty : initialVersion.DisplayName);
                versionInfoText.Text = initialVersion == null ? "No archived versions found." : initialVersion.Details;

                foreach (Control control in new Control[]
                {
                    heading, newSettingsButton, autoLabel, autoState, autoToggle, separatorOne,
                    taskbarLabel, taskbarState, taskbarToggle, separatorTwo,
                    rememberLabel, rememberState, rememberToggle, separatorThree,
                    dualHotkeyLabel, dualHotkeyState, dualHotkeyToggle, separatorFour,
                    appearanceLabel, appearanceState, appearanceToggle,
                    customizationLabel, customizationState, customizeButton,
                    controllerLabel, controllerState, controllerScanButton,
                    microphoneLabel, microphoneBox, soundSettingsButton, separatorFive,
                    soundFeedbackLabel, soundFeedbackState, soundFeedbackToggle,
                    soundPresetLabel, soundPresetBox, separatorSix,
                    developerHeading, developerModeLabel, developerModeState, developerModeToggle,
                    hotkeyStripLabel, hotkeyStripState, hotkeyStripToggle,
                    olderVersionLabel, olderVersionBox, openVersionButton,
                    versionsInfoLabel, versionInfoText
                })
                    scrollPanel.Controls.Add(control);

                dialog.Controls.Add(scrollPanel);
                dialog.Controls.Add(versionLabel);
                dialog.Controls.Add(doneButton);
                dialog.Controls.Add(footerSeparator);
                Timer controllerStatusTimer = new Timer();
                controllerStatusTimer.Interval = 500;
                controllerStatusTimer.Tick += delegate { controllerState.Text = dualSense.StatusText; };
                controllerStatusTimer.Start();
                dialog.FormClosed += delegate
                {
                    controllerStatusTimer.Stop();
                    controllerStatusTimer.Dispose();
                };
                applyDialogTheme();
                dialog.ShowDialog(this);
            }
        }

        private void ShowColorCustomization(Form owner, Action refreshSettings)
        {
            using (Form dialog = new Form())
            {
                dialog.Text = "PulseMute customization";
                dialog.ClientSize = new Size(400, 514);
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;
                dialog.ShowInTaskbar = false;
                dialog.TopMost = owner.TopMost;
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.Font = new Font("Segoe UI", 10F);

                Label heading = CreateSettingsLabel("Customize colors", 20, 16);
                heading.Font = new Font("Segoe UI Semibold", 17F, FontStyle.Bold);
                heading.SetBounds(20, 16, 280, 34);

                string[] names =
                {
                    "Accent", "Creator lines", "Background", "Control surface",
                    "Primary text", "Secondary text", "Settings sidebar", "Settings border"
                };
                Label[] labels = new Label[names.Length];
                Button[] swatches = new Button[names.Length];
                for (int i = 0; i < names.Length; i++)
                {
                    int y = 66 + (i * 47);
                    labels[i] = CreateSettingsLabel(names[i], 20, y + 4);
                    labels[i].SetBounds(20, y + 4, 210, 24);
                    swatches[i] = CreateColorSwatchButton(250, y);
                    dialog.Controls.Add(labels[i]);
                    dialog.Controls.Add(swatches[i]);
                }

                Button resetButton = CreateSmallButton("Reset defaults", new Point(20, 448));
                resetButton.Size = new Size(130, 30);
                Button closeButton = CreateSmallButton("Done", new Point(306, 448));
                closeButton.Size = new Size(74, 30);
                closeButton.Click += delegate { dialog.Close(); };
                dialog.AcceptButton = closeButton;

                Label footer = new Label();
                footer.Text = EditionVersionText() + "  |  Created by Wrakeeb";
                footer.Font = new Font("Segoe UI Semibold", 7.5F, FontStyle.Bold);
                footer.TextAlign = ContentAlignment.MiddleCenter;
                footer.SetBounds(20, 488, 360, 18);

                Action refreshDialog = delegate
                {
                    Color background = ThemeBackgroundColor();
                    Color foreground = ThemePrimaryTextColor();
                    Color secondary = ThemeSecondaryTextColor();
                    Color surface = ThemeSurfaceColor();
                    Color border = SettingsBorderColor();
                    Color[] colors =
                    {
                        AccentColor(), CreatorLineColor(), ThemeBackgroundColor(), ThemeSurfaceColor(),
                        ThemePrimaryTextColor(), ThemeSecondaryTextColor(), SettingsSidebarColor(), SettingsBorderColor()
                    };

                    dialog.BackColor = background;
                    heading.ForeColor = foreground;
                    footer.ForeColor = secondary;
                    foreach (Label label in labels)
                        label.ForeColor = foreground;
                    for (int i = 0; i < swatches.Length; i++)
                        StyleColorSwatch(swatches[i], colors[i], border);
                    foreach (Button button in new Button[] { resetButton, closeButton })
                    {
                        button.BackColor = surface;
                        button.ForeColor = foreground;
                        button.FlatAppearance.BorderColor = border;
                    }
                    ApplyCaptionTheme(dialog.Handle, darkMode);
                    dialog.Invalidate(true);
                };

                Action commitColors = delegate
                {
                    ApplyTheme();
                    SaveSettingsFile();
                    refreshSettings();
                    refreshDialog();
                };

                swatches[0].Click += delegate { PickCustomColor(dialog, AccentColor(), delegate(Color color) { customAccentColor = color; }, commitColors); };
                swatches[1].Click += delegate { PickCustomColor(dialog, CreatorLineColor(), delegate(Color color) { customCreatorColor = color; }, commitColors); };
                swatches[2].Click += delegate { PickCustomColor(dialog, ThemeBackgroundColor(), delegate(Color color) { customBackgroundColor = color; }, commitColors); };
                swatches[3].Click += delegate { PickCustomColor(dialog, ThemeSurfaceColor(), delegate(Color color) { customSurfaceColor = color; }, commitColors); };
                swatches[4].Click += delegate { PickCustomColor(dialog, ThemePrimaryTextColor(), delegate(Color color) { customPrimaryTextColor = color; }, commitColors); };
                swatches[5].Click += delegate { PickCustomColor(dialog, ThemeSecondaryTextColor(), delegate(Color color) { customSecondaryTextColor = color; }, commitColors); };
                swatches[6].Click += delegate { PickCustomColor(dialog, SettingsSidebarColor(), delegate(Color color) { customSettingsSidebarColor = color; }, commitColors); };
                swatches[7].Click += delegate { PickCustomColor(dialog, SettingsBorderColor(), delegate(Color color) { customSettingsBorderColor = color; }, commitColors); };

                resetButton.Click += delegate
                {
                    customColorsEnabled = false;
                    commitColors();
                };

                dialog.Controls.Add(heading);
                dialog.Controls.Add(resetButton);
                dialog.Controls.Add(closeButton);
                dialog.Controls.Add(footer);
                refreshDialog();
                dialog.ShowDialog(owner);
            }
        }

        private void PickCustomColor(Form owner, Color initialColor, Action<Color> assignColor, Action commitColors)
        {
            Color selectedColor;
            if (!ShowColorEditor(owner, initialColor, out selectedColor))
                return;

            EnsureCustomColorsInitialized();
            assignColor(selectedColor);
            commitColors();
        }

        private bool ShowColorEditor(Form owner, Color initialColor, out Color selectedColor)
        {
            Color result = initialColor;
            using (Form dialog = new Form())
            {
                dialog.Text = "PulseMute color editor";
                dialog.ClientSize = new Size(470, 430);
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;
                dialog.ShowInTaskbar = false;
                dialog.TopMost = owner.TopMost;
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.Font = SettingsBodyFont(9.5F, FontStyle.Regular);

                Color background = ThemeBackgroundColor();
                Color surface = ThemeSurfaceColor();
                Color foreground = ThemePrimaryTextColor();
                Color secondary = ThemeSecondaryTextColor();
                Color border = SettingsBorderColor();

                Label heading = CreateSettingsLabel("Choose a color", 22, 18);
                heading.Font = SettingsDisplayFont(17F, FontStyle.Bold);
                heading.SetBounds(22, 18, 300, 34);

                Panel preview = new Panel();
                preview.SetBounds(22, 62, 426, 58);
                preview.BorderStyle = BorderStyle.FixedSingle;
                Label previewCode = new Label();
                previewCode.Dock = DockStyle.Fill;
                previewCode.Font = new Font("Consolas", 12F, FontStyle.Bold);
                previewCode.TextAlign = ContentAlignment.MiddleCenter;
                preview.Controls.Add(previewCode);

                Label hexLabel = CreateSettingsLabel("HEX color", 22, 140);
                hexLabel.SetBounds(22, 140, 90, 22);
                TextBox hexBox = CreateColorCodeBox(22, 164, 218);
                Button copyButton = CreateSmallButton("Copy", new Point(250, 163));
                copyButton.Size = new Size(92, 30);
                Button pasteButton = CreateSmallButton("Paste", new Point(352, 163));
                pasteButton.Size = new Size(96, 30);

                Label rgbLabel = CreateSettingsLabel("RGB channels", 22, 208);
                rgbLabel.SetBounds(22, 208, 130, 22);
                Label redLabel = CreateColorChannelLabel("R", 22, 238);
                TextBox redBox = CreateColorCodeBox(48, 234, 86);
                Label greenLabel = CreateColorChannelLabel("G", 158, 238);
                TextBox greenBox = CreateColorCodeBox(184, 234, 86);
                Label blueLabel = CreateColorChannelLabel("B", 294, 238);
                TextBox blueBox = CreateColorCodeBox(320, 234, 86);

                Label presetsLabel = CreateSettingsLabel("Quick colors", 22, 284);
                presetsLabel.SetBounds(22, 284, 130, 22);
                Color[] presets =
                {
                    Color.FromArgb(193, 53, 69), Color.FromArgb(224, 83, 105),
                    Color.FromArgb(34, 139, 111), Color.FromArgb(35, 166, 173),
                    Color.FromArgb(63, 111, 198), Color.FromArgb(139, 92, 246),
                    Color.FromArgb(217, 151, 55), Color.FromArgb(238, 241, 245)
                };
                Button[] presetButtons = new Button[presets.Length];
                for (int i = 0; i < presets.Length; i++)
                {
                    Button preset = new Button();
                    preset.SetBounds(22 + (i * 53), 310, 42, 30);
                    preset.FlatStyle = FlatStyle.Flat;
                    preset.FlatAppearance.BorderColor = border;
                    preset.BackColor = presets[i];
                    preset.Cursor = Cursors.Hand;
                    preset.Tag = presets[i];
                    preset.TabStop = false;
                    presetButtons[i] = preset;
                    dialog.Controls.Add(preset);
                }

                Label validationLabel = CreateSettingsStateLabel(22, 350);
                validationLabel.SetBounds(22, 350, 275, 20);
                Button advancedButton = CreateSmallButton("Advanced picker", new Point(22, 382));
                advancedButton.Size = new Size(142, 30);
                Button cancelButton = CreateSmallButton("Cancel", new Point(286, 382));
                cancelButton.Size = new Size(76, 30);
                Button applyButton = CreateSmallButton("Apply", new Point(372, 382));
                applyButton.Size = new Size(76, 30);

                bool updatingFields = false;
                Action<Color> updateEditor = delegate(Color color)
                {
                    result = color;
                    updatingFields = true;
                    string code = "#" + ColorValue(color);
                    hexBox.Text = code;
                    redBox.Text = color.R.ToString();
                    greenBox.Text = color.G.ToString();
                    blueBox.Text = color.B.ToString();
                    updatingFields = false;
                    preview.BackColor = color;
                    previewCode.BackColor = color;
                    previewCode.ForeColor = ReadableTextColor(color);
                    previewCode.Text = code;
                    validationLabel.Text = "Ready";
                    validationLabel.ForeColor = secondary;
                    applyButton.Enabled = true;
                };

                EventHandler validateHex = delegate
                {
                    if (updatingFields)
                        return;
                    Color parsed;
                    if (TryParseHexColor(hexBox.Text, out parsed))
                        updateEditor(parsed);
                    else
                    {
                        validationLabel.Text = "Enter #RRGGBB or #RGB";
                        validationLabel.ForeColor = Color.FromArgb(224, 83, 105);
                        applyButton.Enabled = false;
                    }
                };
                hexBox.TextChanged += validateHex;

                EventHandler validateRgb = delegate
                {
                    if (updatingFields)
                        return;
                    int red;
                    int green;
                    int blue;
                    if (TryParseColorChannel(redBox.Text, out red) &&
                        TryParseColorChannel(greenBox.Text, out green) &&
                        TryParseColorChannel(blueBox.Text, out blue))
                    {
                        updateEditor(Color.FromArgb(red, green, blue));
                    }
                    else
                    {
                        validationLabel.Text = "RGB values must be 0 to 255";
                        validationLabel.ForeColor = Color.FromArgb(224, 83, 105);
                        applyButton.Enabled = false;
                    }
                };
                redBox.TextChanged += validateRgb;
                greenBox.TextChanged += validateRgb;
                blueBox.TextChanged += validateRgb;

                foreach (Button presetButton in presetButtons)
                {
                    presetButton.Click += delegate(object sender, EventArgs e)
                    {
                        Button clicked = sender as Button;
                        if (clicked != null && clicked.Tag is Color)
                            updateEditor((Color)clicked.Tag);
                    };
                }

                copyButton.Click += delegate
                {
                    try
                    {
                        Clipboard.SetText("#" + ColorValue(result));
                        validationLabel.Text = "Color code copied";
                        validationLabel.ForeColor = secondary;
                    }
                    catch
                    {
                        validationLabel.Text = "Clipboard is unavailable";
                        validationLabel.ForeColor = Color.FromArgb(224, 83, 105);
                    }
                };
                pasteButton.Click += delegate
                {
                    try
                    {
                        Color pasted;
                        if (Clipboard.ContainsText() && TryParseHexColor(Clipboard.GetText(), out pasted))
                            updateEditor(pasted);
                        else
                        {
                            validationLabel.Text = "Clipboard has no valid color code";
                            validationLabel.ForeColor = Color.FromArgb(224, 83, 105);
                        }
                    }
                    catch
                    {
                        validationLabel.Text = "Clipboard is unavailable";
                        validationLabel.ForeColor = Color.FromArgb(224, 83, 105);
                    }
                };
                advancedButton.Click += delegate
                {
                    using (ColorDialog picker = new ColorDialog())
                    {
                        picker.Color = result;
                        picker.FullOpen = true;
                        if (picker.ShowDialog(dialog) == DialogResult.OK)
                            updateEditor(picker.Color);
                    }
                };
                cancelButton.Click += delegate { dialog.DialogResult = DialogResult.Cancel; };
                applyButton.Click += delegate { dialog.DialogResult = DialogResult.OK; };
                dialog.CancelButton = cancelButton;
                dialog.AcceptButton = applyButton;

                foreach (Label label in new Label[] { heading, hexLabel, rgbLabel, redLabel, greenLabel, blueLabel, presetsLabel })
                    label.ForeColor = foreground;
                validationLabel.ForeColor = secondary;
                foreach (TextBox box in new TextBox[] { hexBox, redBox, greenBox, blueBox })
                {
                    box.BackColor = surface;
                    box.ForeColor = foreground;
                    box.BorderStyle = BorderStyle.FixedSingle;
                }
                foreach (Button button in new Button[] { copyButton, pasteButton, advancedButton, cancelButton, applyButton })
                {
                    button.BackColor = surface;
                    button.ForeColor = foreground;
                    button.FlatAppearance.BorderColor = border;
                    button.FlatAppearance.MouseOverBackColor = MixColor(surface, foreground, 10);
                }

                dialog.BackColor = background;
                dialog.Controls.Add(heading);
                dialog.Controls.Add(preview);
                dialog.Controls.Add(hexLabel);
                dialog.Controls.Add(hexBox);
                dialog.Controls.Add(copyButton);
                dialog.Controls.Add(pasteButton);
                dialog.Controls.Add(rgbLabel);
                dialog.Controls.Add(redLabel);
                dialog.Controls.Add(redBox);
                dialog.Controls.Add(greenLabel);
                dialog.Controls.Add(greenBox);
                dialog.Controls.Add(blueLabel);
                dialog.Controls.Add(blueBox);
                dialog.Controls.Add(presetsLabel);
                dialog.Controls.Add(validationLabel);
                dialog.Controls.Add(advancedButton);
                dialog.Controls.Add(cancelButton);
                dialog.Controls.Add(applyButton);
                updateEditor(initialColor);
                ApplyCaptionTheme(dialog.Handle, darkMode);

                if (dialog.ShowDialog(owner) != DialogResult.OK)
                {
                    selectedColor = initialColor;
                    return false;
                }
            }

            selectedColor = result;
            return true;
        }

        private static TextBox CreateColorCodeBox(int x, int y, int width)
        {
            TextBox box = new TextBox();
            box.SetBounds(x, y, width, 30);
            box.Font = new Font("Consolas", 10F, FontStyle.Regular);
            box.TextAlign = HorizontalAlignment.Center;
            return box;
        }

        private static Label CreateColorChannelLabel(string text, int x, int y)
        {
            Label label = new Label();
            label.Text = text;
            label.SetBounds(x, y, 22, 24);
            label.TextAlign = ContentAlignment.MiddleLeft;
            return label;
        }

        internal static bool TryParseHexColor(string value, out Color color)
        {
            color = Color.Empty;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            string code = value.Trim();
            if (code.StartsWith("#", StringComparison.Ordinal))
                code = code.Substring(1);
            else if (code.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                code = code.Substring(2);
            if (code.Length == 3)
                code = new string(new[] { code[0], code[0], code[1], code[1], code[2], code[2] });

            int rgb;
            if (code.Length != 6 || !int.TryParse(code, System.Globalization.NumberStyles.HexNumber, null, out rgb))
                return false;
            color = Color.FromArgb((rgb >> 16) & 255, (rgb >> 8) & 255, rgb & 255);
            return true;
        }

        internal static bool TryParseColorChannel(string value, out int channel)
        {
            return int.TryParse(value, out channel) && channel >= 0 && channel <= 255;
        }

        private void EnsureCustomColorsInitialized()
        {
            if (customColorsEnabled)
                return;

            customAccentColor = AccentColor();
            customCreatorColor = CreatorLineColor();
            customBackgroundColor = ThemeBackgroundColor();
            customSurfaceColor = ThemeSurfaceColor();
            customPrimaryTextColor = ThemePrimaryTextColor();
            customSecondaryTextColor = ThemeSecondaryTextColor();
            customSettingsSidebarColor = SettingsSidebarColor();
            customSettingsBorderColor = SettingsBorderColor();
            customColorsEnabled = true;
        }

        private static Button CreateColorSwatchButton(int x, int y)
        {
            Button button = new Button();
            button.SetBounds(x, y, 130, 32);
            button.FlatStyle = FlatStyle.Flat;
            button.Cursor = Cursors.Hand;
            button.Font = new Font("Consolas", 8.5F, FontStyle.Bold);
            return button;
        }

        private static void StyleColorSwatch(Button button, Color color, Color border)
        {
            button.BackColor = color;
            button.ForeColor = ReadableTextColor(color);
            button.Text = "#" + color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2");
            button.FlatAppearance.BorderColor = border;
        }

        private static Color ReadableTextColor(Color background)
        {
            int brightness = (background.R * 299 + background.G * 587 + background.B * 114) / 1000;
            return brightness >= 145 ? Color.FromArgb(20, 24, 29) : Color.White;
        }

        private static Label CreateSettingsLabel(string text, int x, int y)
        {
            Label label = new Label();
            label.Text = text;
            label.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            label.SetBounds(x, y, 280, 22);
            return label;
        }

        private static string EditionVersionText()
        {
            if (DesignVariant == 1)
                return "PulseMute 1.5.1 Focus";
            if (DesignVariant == 2)
                return "PulseMute 1.5.2 Signal";
            if (DesignVariant == 3)
                return "PulseMute 1.5.3 Console";
            return "PulseMute Beta " + ReleaseVersion;
        }

        private static Label CreateSettingsStateLabel(int x, int y)
        {
            Label label = new Label();
            label.Font = new Font("Segoe UI", 8F, FontStyle.Regular);
            label.SetBounds(x, y, 180, 18);
            return label;
        }

        private static ToggleSwitch CreateToggleSwitch(int x, int y, bool isChecked)
        {
            ToggleSwitch toggle = new ToggleSwitch();
            toggle.SetBounds(x, y, 46, 24);
            toggle.AnimationsEnabled = false;
            toggle.Checked = isChecked;
            toggle.AnimationsEnabled = true;
            toggle.Cursor = Cursors.Hand;
            return toggle;
        }

        private static RadioButton CreateThemeChoice(string text, int x, int y, bool isChecked)
        {
            RadioButton choice = new RadioButton();
            choice.Appearance = Appearance.Button;
            choice.FlatStyle = FlatStyle.Flat;
            choice.Text = text;
            choice.TextAlign = ContentAlignment.MiddleCenter;
            choice.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);
            choice.SetBounds(x, y, 90, 30);
            choice.Checked = isChecked;
            choice.Cursor = Cursors.Hand;
            return choice;
        }

        private static Panel CreateSeparator(int x, int y, int width)
        {
            Panel separator = new Panel();
            separator.SetBounds(x, y, width, 1);
            return separator;
        }

        private void ShowSettings()
        {
            using (Form dialog = new Form())
            {
                dialog.Text = "PulseMute Beta settings";
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
                if (soundFeedbackEnabled)
                    notificationSound.Play(soundPreset, next, soundVolume);
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
                tray.Text = muted ? "PulseMute Beta - muted" : "PulseMute Beta - live";
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
                tray.Text = "PulseMute Beta - microphone unavailable";
            }
        }

        private void HideToTray()
        {
            Hide();
            WindowState = FormWindowState.Normal;
            tray.ShowBalloonTip(900, "PulseMute Beta", "Listening for " + CurrentShortcutText(1) + " or " + CurrentShortcutText(2) + ".", ToolTipIcon.Info);
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
            ApplyTheme();
            SaveWindowSettings();
        }

        private void ApplyResponsiveLayout()
        {
            int width = Math.Max(1, ClientSize.Width);
            int height = Math.Max(1, ClientSize.Height);
            int pad = Clamp(Math.Min(width, height) / 18, 8, 22);
            int gap = Clamp(Math.Min(width, height) / 42, 4, 12);
            float scale = Math.Max(0.72f, Math.Min(1.8f, Math.Min(width / 300f, height / 380f)));
            float fontScale = Math.Max(0.9f, scale);
            shortcutValueLabel.Visible = true;
            shortcutTwoValueLabel.Visible = true;

            if (DesignVariant != 0)
            {
                ApplyEditionLayout(width, height, pad, gap, scale, fontScale);
                return;
            }

            SetControlFont(titleLabel, "Segoe UI Semibold", 13f * fontScale, FontStyle.Bold);
            SetControlFont(statusLabel, "Segoe UI Semibold", 9.5f * fontScale, FontStyle.Bold);
            SetControlFont(deviceLabel, "Segoe UI", 9.5f * fontScale, FontStyle.Regular);
            float hotkeyFontSize = hotkeyStripAboveCredit ? 9f : 8.2f;
            SetControlFont(shortcutValueLabel, "Segoe UI Semibold", hotkeyFontSize * fontScale, FontStyle.Bold);
            SetControlFont(shortcutTwoValueLabel, "Segoe UI Semibold", hotkeyFontSize * fontScale, FontStyle.Bold);
            SetControlFont(settingsButton, "Segoe Fluent Icons", 11f * fontScale, FontStyle.Regular);
            SetControlFont(topButton, "Segoe Fluent Icons", 11f * fontScale, FontStyle.Regular);
            SetControlFont(refreshButton, "Segoe Fluent Icons", 11f * fontScale, FontStyle.Regular);
            SetControlFont(hideButton, "Segoe Fluent Icons", 11f * fontScale, FontStyle.Regular);
            SetControlFont(creditLabel, "Segoe UI Semibold", 7.8f * fontScale, FontStyle.Bold);

            int topRowHeight = Clamp((int)(28 * scale), 22, 42);
            int statusWidth = Clamp((int)(74 * scale), 54, Math.Max(54, width / 3));
            int topWidth = Clamp((int)(34 * scale), 30, 46);
            int settingsWidth = Clamp((int)(34 * scale), 28, 48);
            statusLabel.SetBounds(width - pad - statusWidth, pad, statusWidth, topRowHeight);
            topButton.SetBounds(statusLabel.Left - gap - topWidth, pad, topWidth, topRowHeight);
            settingsButton.SetBounds(topButton.Left - gap - settingsWidth, pad, settingsWidth, topRowHeight);
            int titleRight = Math.Max(pad, settingsButton.Left - gap);
            titleLabel.SetBounds(pad, pad - 2, Math.Max(20, titleRight - pad), topRowHeight + 8);

            int deviceTop = pad + topRowHeight + gap;
            deviceLabel.SetBounds(pad, deviceTop, Math.Max(20, width - (pad * 2)), Clamp((int)(24 * scale), 18, 34));

            int bottomButtonHeight = Clamp((int)(28 * scale), 24, 40);
            int bottomButtonWidth = Clamp((int)(40 * scale), 34, 52);
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

            int shortcutTop;
            if (hotkeyStripAboveCredit)
            {
                int shortcutHeight = Clamp((int)(34 * scale), 28, 42);
                shortcutTop = creditTop - gap - shortcutHeight;
                int availableShortcutWidth = Math.Max(80, width - (pad * 2) - gap);
                int shortcutWidth = availableShortcutWidth / 2;
                shortcutValueLabel.SetBounds(pad, shortcutTop, shortcutWidth, shortcutHeight);
                shortcutTwoValueLabel.SetBounds(pad + shortcutWidth + gap, shortcutTop, availableShortcutWidth - shortcutWidth, shortcutHeight);
            }
            else
            {
                shortcutTop = bottomTop;
                int shortcutLeft = refreshButton.Right + gap;
                int shortcutRight = hideButton.Left - gap;
                int availableShortcutWidth = Math.Max(40, shortcutRight - shortcutLeft - gap);
                int shortcutWidth = availableShortcutWidth / 2;
                shortcutValueLabel.SetBounds(shortcutLeft, bottomTop, shortcutWidth, bottomButtonHeight);
                shortcutTwoValueLabel.SetBounds(shortcutLeft + shortcutWidth + gap, bottomTop, availableShortcutWidth - shortcutWidth, bottomButtonHeight);
            }

            int circleTop = deviceLabel.Bottom + gap;
            int circleBottom = (hotkeyStripAboveCredit ? shortcutTop : creditTop) - gap;
            int maxCircle = Math.Min(width - (pad * 2), circleBottom - circleTop);
            int circleSize = Clamp(maxCircle, 58, Math.Max(58, Math.Min(width - (pad * 2), height - (pad * 2))));
            int circleLeft = (width - circleSize) / 2;
            int adjustedCircleTop = circleTop + Math.Max(0, (circleBottom - circleTop - circleSize) / 2);
            toggleButton.SetBounds(circleLeft, adjustedCircleTop, circleSize, circleSize);
            SetControlFont(toggleButton, "Segoe UI Semibold", Math.Max(8f, circleSize / 9f), FontStyle.Bold);
        }

        private void ApplyEditionLayout(int width, int height, int pad, int gap, float scale, float fontScale)
        {
            SetControlFont(titleLabel, "Segoe UI Semibold", (DesignVariant == 3 ? 13f : 16f) * fontScale, FontStyle.Bold);
            SetControlFont(statusLabel, "Segoe UI Semibold", 9f * fontScale, FontStyle.Bold);
            SetControlFont(deviceLabel, "Segoe UI", 9f * fontScale, FontStyle.Regular);
            SetControlFont(shortcutLabel, "Segoe UI Semibold", 7.5f * fontScale, FontStyle.Bold);
            SetControlFont(shortcutValueLabel, "Segoe UI Semibold", 9f * fontScale, FontStyle.Bold);
            SetControlFont(shortcutButton, "Segoe Fluent Icons", 10.5f * fontScale, FontStyle.Regular);
            SetControlFont(settingsButton, "Segoe Fluent Icons", 10.5f * fontScale, FontStyle.Regular);
            SetControlFont(topButton, "Segoe Fluent Icons", 10.5f * fontScale, FontStyle.Regular);
            SetControlFont(refreshButton, "Segoe Fluent Icons", 10.5f * fontScale, FontStyle.Regular);
            SetControlFont(hideButton, "Segoe Fluent Icons", 10.5f * fontScale, FontStyle.Regular);
            SetControlFont(creditLabel, "Segoe UI Semibold", 7.3f * fontScale, FontStyle.Bold);

            if (DesignVariant == 1)
                ApplyFocusLayout(width, height, pad, gap, scale);
            else if (DesignVariant == 2)
                ApplySignalLayout(width, height, pad, gap, scale);
            else
                ApplyConsoleLayout(width, height, pad, gap, scale);
        }

        private void ApplyFocusLayout(int width, int height, int pad, int gap, float scale)
        {
            int topHeight = Clamp((int)(30 * scale), 24, 38);
            int iconSize = Clamp((int)(34 * scale), 30, 42);
            int statusWidth = Clamp((int)(76 * scale), 62, 92);
            statusLabel.SetBounds(width - pad - statusWidth, pad, statusWidth, topHeight);
            topButton.SetBounds(statusLabel.Left - gap - iconSize, pad, iconSize, topHeight);
            settingsButton.SetBounds(topButton.Left - gap - iconSize, pad, iconSize, topHeight);
            titleLabel.SetBounds(pad, pad - 2, Math.Max(80, settingsButton.Left - pad - gap), topHeight + 6);

            int rightX = Clamp((int)(width * 0.43f), 155, width - pad - 150);
            int deviceTop = pad + topHeight + gap;
            deviceLabel.SetBounds(rightX, deviceTop, Math.Max(120, width - rightX - pad), Clamp((int)(24 * scale), 20, 30));

            int bottomHeight = Clamp((int)(30 * scale), 26, 38);
            int bottomTop = height - pad - bottomHeight;
            refreshButton.SetBounds(rightX, bottomTop, iconSize, bottomHeight);
            hideButton.SetBounds(width - pad - iconSize, bottomTop, iconSize, bottomHeight);

            int creditHeight = Clamp((int)(18 * scale), 16, 24);
            int creditTop = bottomTop - gap - creditHeight;
            creditLabel.SetBounds(rightX + 36, creditTop, Math.Max(80, width - rightX - pad - 72), creditHeight);
            int ruleY = creditTop + creditHeight / 2;
            creditRuleLeft.SetBounds(rightX, ruleY, Math.Max(0, creditLabel.Left - rightX - gap), 1);
            creditRuleRight.SetBounds(creditLabel.Right + gap, ruleY, Math.Max(0, width - pad - creditLabel.Right - gap), 1);

            int shortcutHeight = Clamp((int)(31 * scale), 26, 38);
            int shortcutTop = deviceLabel.Bottom + gap * 2;
            int editWidth = iconSize;
            int keyWidth = Clamp((int)(72 * scale), 58, 92);
            shortcutButton.SetBounds(width - pad - editWidth, shortcutTop, editWidth, shortcutHeight);
            shortcutValueLabel.SetBounds(shortcutButton.Left - gap - keyWidth, shortcutTop, keyWidth, shortcutHeight);
            shortcutLabel.SetBounds(rightX, shortcutTop, Math.Max(35, shortcutValueLabel.Left - rightX - gap), shortcutHeight);

            int circleTop = deviceTop;
            int circleBottom = bottomTop;
            int circleSize = Math.Max(72, Math.Min(rightX - pad - gap, circleBottom - circleTop));
            toggleButton.SetBounds(pad + Math.Max(0, (rightX - pad - circleSize) / 2), circleTop + Math.Max(0, (circleBottom - circleTop - circleSize) / 2), circleSize, circleSize);
            SetControlFont(toggleButton, "Segoe UI Semibold", Math.Max(9f, circleSize / 9f), FontStyle.Bold);
        }

        private void ApplySignalLayout(int width, int height, int pad, int gap, float scale)
        {
            int iconSize = Clamp((int)(34 * scale), 30, 44);
            int topHeight = iconSize;
            settingsButton.SetBounds(pad, pad, iconSize, topHeight);
            topButton.SetBounds(width - pad - iconSize, pad, iconSize, topHeight);
            titleLabel.TextAlign = ContentAlignment.MiddleCenter;
            titleLabel.SetBounds(settingsButton.Right + gap, pad - 2, Math.Max(60, topButton.Left - settingsButton.Right - gap * 2), topHeight + 4);

            int statusHeight = Clamp((int)(27 * scale), 24, 34);
            int statusWidth = Clamp((int)(82 * scale), 66, 100);
            statusLabel.SetBounds((width - statusWidth) / 2, pad + topHeight + gap, statusWidth, statusHeight);
            int deviceTop = statusLabel.Bottom + gap;
            deviceLabel.TextAlign = ContentAlignment.MiddleCenter;
            deviceLabel.SetBounds(pad, deviceTop, Math.Max(80, width - pad * 2), Clamp((int)(24 * scale), 20, 30));

            int bottomHeight = Clamp((int)(31 * scale), 27, 40);
            int bottomTop = height - pad - bottomHeight;
            refreshButton.SetBounds(pad, bottomTop, iconSize, bottomHeight);
            hideButton.SetBounds(width - pad - iconSize, bottomTop, iconSize, bottomHeight);

            int creditHeight = Clamp((int)(18 * scale), 16, 24);
            int creditTop = bottomTop - gap - creditHeight;
            int creditWidth = Clamp((int)(145 * scale), 112, Math.Max(112, width - pad * 2));
            creditLabel.SetBounds((width - creditWidth) / 2, creditTop, creditWidth, creditHeight);
            int ruleY = creditTop + creditHeight / 2;
            creditRuleLeft.SetBounds(pad, ruleY, Math.Max(0, creditLabel.Left - pad - gap), 1);
            creditRuleRight.SetBounds(creditLabel.Right + gap, ruleY, Math.Max(0, width - pad - creditLabel.Right - gap), 1);

            int shortcutHeight = Clamp((int)(32 * scale), 27, 40);
            int shortcutTop = creditTop - gap - shortcutHeight;
            int editWidth = iconSize;
            int keyWidth = Clamp((int)(74 * scale), 58, 96);
            shortcutButton.SetBounds(width - pad - editWidth, shortcutTop, editWidth, shortcutHeight);
            shortcutValueLabel.SetBounds(shortcutButton.Left - gap - keyWidth, shortcutTop, keyWidth, shortcutHeight);
            shortcutLabel.SetBounds(pad, shortcutTop, Math.Max(35, shortcutValueLabel.Left - pad - gap), shortcutHeight);

            int muteTop = deviceLabel.Bottom + gap * 2;
            int muteBottom = shortcutTop - gap * 2;
            int muteHeight = Math.Max(64, Math.Min(112, muteBottom - muteTop));
            toggleButton.SetBounds(pad, muteTop + Math.Max(0, (muteBottom - muteTop - muteHeight) / 2), Math.Max(80, width - pad * 2), muteHeight);
            SetControlFont(toggleButton, "Segoe UI Semibold", Math.Max(12f, muteHeight / 5.5f), FontStyle.Bold);
        }

        private void ApplyConsoleLayout(int width, int height, int pad, int gap, float scale)
        {
            titleLabel.TextAlign = ContentAlignment.MiddleLeft;
            deviceLabel.TextAlign = ContentAlignment.MiddleLeft;
            int rightWidth = Clamp((int)(width * 0.36f), 120, 190);
            int rightX = width - pad - rightWidth;
            int titleHeight = Clamp((int)(28 * scale), 24, 36);
            titleLabel.SetBounds(pad, pad, Math.Max(100, rightX - pad - gap), titleHeight);
            statusLabel.SetBounds(rightX, pad, rightWidth, titleHeight);
            deviceLabel.SetBounds(pad, titleLabel.Bottom + gap, Math.Max(90, rightX - pad - gap), Clamp((int)(24 * scale), 20, 30));

            int toolbarHeight = Clamp((int)(31 * scale), 27, 38);
            int toolbarTop = height - pad - toolbarHeight;
            int iconSize = Clamp((int)(34 * scale), 30, 42);
            settingsButton.SetBounds(pad, toolbarTop, iconSize, toolbarHeight);
            topButton.SetBounds(settingsButton.Right + gap, toolbarTop, iconSize, toolbarHeight);
            refreshButton.SetBounds(topButton.Right + gap, toolbarTop, iconSize, toolbarHeight);
            hideButton.SetBounds(refreshButton.Right + gap, toolbarTop, iconSize, toolbarHeight);

            int creditHeight = Clamp((int)(18 * scale), 16, 24);
            int creditTop = toolbarTop - gap - creditHeight;
            creditLabel.SetBounds(pad, creditTop, Math.Max(90, rightX - pad - gap), creditHeight);
            creditLabel.TextAlign = ContentAlignment.MiddleLeft;
            creditRuleLeft.SetBounds(0, 0, 0, 0);
            creditRuleRight.SetBounds(pad, creditLabel.Bottom, Math.Max(40, rightX - pad - gap), 1);

            int shortcutHeight = Clamp((int)(34 * scale), 28, 42);
            int shortcutTop = deviceLabel.Bottom + gap * 2;
            int editWidth = iconSize;
            int keyWidth = Clamp((int)(72 * scale), 58, 92);
            shortcutButton.SetBounds(rightX - gap - editWidth, shortcutTop, editWidth, shortcutHeight);
            shortcutValueLabel.SetBounds(shortcutButton.Left - gap - keyWidth, shortcutTop, keyWidth, shortcutHeight);
            shortcutLabel.SetBounds(pad, shortcutTop, Math.Max(35, shortcutValueLabel.Left - pad - gap), shortcutHeight);

            int muteTop = statusLabel.Bottom + gap * 2;
            int muteBottom = height - pad;
            int muteSize = Math.Max(82, Math.Min(rightWidth, muteBottom - muteTop));
            toggleButton.SetBounds(rightX + Math.Max(0, (rightWidth - muteSize) / 2), muteTop + Math.Max(0, (muteBottom - muteTop - muteSize) / 2), muteSize, muteSize);
            SetControlFont(toggleButton, "Segoe UI Semibold", Math.Max(9f, muteSize / 8f), FontStyle.Bold);
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

        private void BeginShortcutCapture(int slot)
        {
            if (slot == 2 && !dualHotkeyEnabled)
                return;

            captureShortcutSlot = slot == 2 ? 2 : 1;
            if (captureShortcutSlot == 1)
                shortcutValueLabel.Text = "1  Listening...";
            else
                shortcutTwoValueLabel.Text = "2  Listening...";

            ActiveControl = null;
            Focus();
        }

        private void UpdateShortcutDisplay()
        {
            shortcutValueLabel.Text = "1  " + CurrentShortcutText(1);
            shortcutTwoValueLabel.Text = dualHotkeyEnabled
                ? "2  " + CurrentShortcutText(2)
                : "2  Off";
            shortcutTwoValueLabel.Enabled = dualHotkeyEnabled;
            shortcutTwoValueLabel.Cursor = dualHotkeyEnabled ? Cursors.Hand : Cursors.Default;
        }

        private void CaptureShortcut(object sender, KeyEventArgs e)
        {
            if (captureShortcutSlot == 0)
                return;

            e.SuppressKeyPress = true;

            Keys keyCode = e.KeyCode;
            if (keyCode == Keys.ControlKey || keyCode == Keys.ShiftKey || keyCode == Keys.Menu || keyCode == Keys.LWin || keyCode == Keys.RWin)
                return;

            int slot = captureShortcutSlot == 2 ? 2 : 1;
            captureShortcutSlot = 0;
            ApplyKeyboardShortcut(slot, (uint)keyCode, true);
        }

        private void DualSenseButtonPressed(object sender, DualSenseButtonEventArgs e)
        {
            if (IsDisposed || !IsHandleCreated)
                return;

            try
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    if (captureShortcutSlot != 0)
                    {
                        int slot = captureShortcutSlot;
                        captureShortcutSlot = 0;
                        if (slot == 1)
                        {
                            shortcutSource = ShortcutSource.DualSense;
                            currentControllerButton = e.Button;
                            shortcutOneHeld = false;
                            UpdateShortcutDisplay();
                        }
                        else
                        {
                            secondShortcutSource = ShortcutSource.DualSense;
                            secondControllerButton = e.Button;
                            shortcutTwoHeld = false;
                            UpdateShortcutDisplay();
                        }
                        SaveShortcut();
                        return;
                    }

                    if (ShortcutBindingMatcher.MatchesController(
                        shortcutSource, currentControllerButton,
                        secondShortcutSource, secondControllerButton,
                        e.Button, dualHotkeyEnabled))
                        ToggleMute();
                });
            }
            catch (InvalidOperationException)
            {
            }
        }

        private void AssignMouseShortcut(int slot, MouseHotkey button)
        {
            if (slot == 1)
            {
                shortcutSource = ShortcutSource.Mouse;
                currentMouseButton = button;
                shortcutOneHeld = false;
                UpdateShortcutDisplay();
            }
            else
            {
                secondShortcutSource = ShortcutSource.Mouse;
                secondMouseButton = button;
                shortcutTwoHeld = false;
                UpdateShortcutDisplay();
            }
            SaveShortcut();
        }

        private void ApplyKeyboardShortcut(int slot, uint key, bool persist)
        {
            if (slot == 1)
            {
                currentKey = key;
                shortcutSource = ShortcutSource.Keyboard;
                shortcutOneHeld = false;
                UpdateShortcutDisplay();
            }
            else
            {
                secondKey = key;
                secondShortcutSource = ShortcutSource.Keyboard;
                shortcutTwoHeld = false;
                UpdateShortcutDisplay();
            }
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
                ShortcutSource source = shortcutSource;
                DualSenseButton controllerButton = currentControllerButton;
                MouseHotkey mouseButton = currentMouseButton;
                uint keyTwo = secondKey;
                ShortcutSource sourceTwo = secondShortcutSource;
                DualSenseButton controllerButtonTwo = secondControllerButton;
                MouseHotkey mouseButtonTwo = secondMouseButton;
                bool legacyComboSetting = false;
                foreach (string line in lines)
                {
                    if (line.StartsWith("Modifiers=", StringComparison.OrdinalIgnoreCase))
                        legacyComboSetting = true;
                    if (line.StartsWith("Key=", StringComparison.OrdinalIgnoreCase))
                        uint.TryParse(line.Substring("Key=".Length), out key);
                    if (line.StartsWith("ShortcutSource=", StringComparison.OrdinalIgnoreCase))
                    {
                        ShortcutSource parsedSource;
                        if (Enum.TryParse(line.Substring("ShortcutSource=".Length), true, out parsedSource))
                            source = parsedSource;
                    }
                    if (line.StartsWith("ControllerButton=", StringComparison.OrdinalIgnoreCase))
                    {
                        DualSenseButton parsedButton;
                        if (Enum.TryParse(line.Substring("ControllerButton=".Length), true, out parsedButton))
                            controllerButton = parsedButton;
                    }
                    if (line.StartsWith("MouseButton=", StringComparison.OrdinalIgnoreCase))
                    {
                        MouseHotkey parsedButton;
                        if (Enum.TryParse(line.Substring("MouseButton=".Length), true, out parsedButton))
                            mouseButton = parsedButton;
                    }
                    if (line.StartsWith("Key2=", StringComparison.OrdinalIgnoreCase))
                        uint.TryParse(line.Substring("Key2=".Length), out keyTwo);
                    if (line.StartsWith("ShortcutSource2=", StringComparison.OrdinalIgnoreCase))
                    {
                        ShortcutSource parsedSource;
                        if (Enum.TryParse(line.Substring("ShortcutSource2=".Length), true, out parsedSource))
                            sourceTwo = parsedSource;
                    }
                    if (line.StartsWith("ControllerButton2=", StringComparison.OrdinalIgnoreCase))
                    {
                        DualSenseButton parsedButton;
                        if (Enum.TryParse(line.Substring("ControllerButton2=".Length), true, out parsedButton))
                            controllerButtonTwo = parsedButton;
                    }
                    if (line.StartsWith("MouseButton2=", StringComparison.OrdinalIgnoreCase))
                    {
                        MouseHotkey parsedButton;
                        if (Enum.TryParse(line.Substring("MouseButton2=".Length), true, out parsedButton))
                            mouseButtonTwo = parsedButton;
                    }
                }

                if (legacyComboSetting)
                {
                    currentKey = (uint)Keys.F8;
                    shortcutSource = ShortcutSource.Keyboard;
                    secondKey = (uint)Keys.F9;
                    secondShortcutSource = ShortcutSource.Keyboard;
                    UpdateShortcutDisplay();
                    SaveShortcut();
                }
                else if (key != 0)
                {
                    currentKey = key;
                    shortcutSource = source;
                    currentControllerButton = controllerButton;
                    currentMouseButton = mouseButton;
                    secondKey = keyTwo == 0 ? (uint)Keys.F9 : keyTwo;
                    secondShortcutSource = sourceTwo;
                    secondControllerButton = controllerButtonTwo;
                    secondMouseButton = mouseButtonTwo;
                    UpdateShortcutDisplay();
                }
            }
            catch
            {
                UpdateShortcutDisplay();
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
                File.WriteAllText(path,
                    "Key=" + currentKey + Environment.NewLine +
                    "ShortcutSource=" + shortcutSource + Environment.NewLine +
                    "ControllerButton=" + currentControllerButton + Environment.NewLine +
                    "MouseButton=" + currentMouseButton + Environment.NewLine +
                    "Key2=" + secondKey + Environment.NewLine +
                    "ShortcutSource2=" + secondShortcutSource + Environment.NewLine +
                    "ControllerButton2=" + secondControllerButton + Environment.NewLine +
                    "MouseButton2=" + secondMouseButton + Environment.NewLine +
                    "DualHotkeyEnabled=" + dualHotkeyEnabled + Environment.NewLine +
                    "DeveloperModeEnabled=" + developerModeEnabled + Environment.NewLine +
                    "SoundFeedbackEnabled=" + soundFeedbackEnabled + Environment.NewLine +
                    "SoundPreset=" + soundPreset + Environment.NewLine +
                    "SoundVolume=" + soundVolume + Environment.NewLine +
                    "AnimationsEnabled=" + animationsEnabled + Environment.NewLine +
                    "LegacySettingsEnabled=" + legacySettingsEnabled + Environment.NewLine);
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
                bool hasCustomSettingsSidebar = false;
                bool hasCustomSettingsBorder = false;

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
                    else if (line.StartsWith("DualHotkeyEnabled=", StringComparison.OrdinalIgnoreCase))
                    {
                        bool.TryParse(line.Substring("DualHotkeyEnabled=".Length), out dualHotkeyEnabled);
                    }
                    else if (line.StartsWith("DeveloperModeEnabled=", StringComparison.OrdinalIgnoreCase))
                    {
                        bool.TryParse(line.Substring("DeveloperModeEnabled=".Length), out developerModeEnabled);
                    }
                    else if (line.StartsWith("SoundFeedbackEnabled=", StringComparison.OrdinalIgnoreCase))
                    {
                        bool.TryParse(line.Substring("SoundFeedbackEnabled=".Length), out soundFeedbackEnabled);
                    }
                    else if (line.StartsWith("SoundPreset=", StringComparison.OrdinalIgnoreCase))
                    {
                        soundPreset = line.Substring("SoundPreset=".Length);
                    }
                    else if (line.StartsWith("SoundVolume=", StringComparison.OrdinalIgnoreCase))
                    {
                        int.TryParse(line.Substring("SoundVolume=".Length), out soundVolume);
                    }
                    else if (line.StartsWith("AnimationsEnabled=", StringComparison.OrdinalIgnoreCase))
                    {
                        bool.TryParse(line.Substring("AnimationsEnabled=".Length), out animationsEnabled);
                    }
                    else if (line.StartsWith("HotkeyStripAboveCredit=", StringComparison.OrdinalIgnoreCase))
                    {
                        bool.TryParse(line.Substring("HotkeyStripAboveCredit=".Length), out hotkeyStripAboveCredit);
                    }
                    else if (line.StartsWith("LegacySettingsEnabled=", StringComparison.OrdinalIgnoreCase))
                    {
                        bool.TryParse(line.Substring("LegacySettingsEnabled=".Length), out legacySettingsEnabled);
                    }
                    else if (line.StartsWith("MuteButtonDesign=", StringComparison.OrdinalIgnoreCase))
                    {
                        int.TryParse(line.Substring("MuteButtonDesign=".Length), out muteButtonDesign);
                    }
                    else if (line.StartsWith("Theme=", StringComparison.OrdinalIgnoreCase))
                    {
                        darkMode = !line.Substring("Theme=".Length).Equals("White", StringComparison.OrdinalIgnoreCase);
                    }
                    else if (line.StartsWith("CustomColors=", StringComparison.OrdinalIgnoreCase))
                    {
                        bool.TryParse(line.Substring("CustomColors=".Length), out customColorsEnabled);
                    }
                    else if (line.StartsWith("CustomAccent=", StringComparison.OrdinalIgnoreCase))
                    {
                        TryParseColor(line.Substring("CustomAccent=".Length), ref customAccentColor);
                    }
                    else if (line.StartsWith("CustomCreator=", StringComparison.OrdinalIgnoreCase))
                    {
                        TryParseColor(line.Substring("CustomCreator=".Length), ref customCreatorColor);
                    }
                    else if (line.StartsWith("CustomBackground=", StringComparison.OrdinalIgnoreCase))
                    {
                        TryParseColor(line.Substring("CustomBackground=".Length), ref customBackgroundColor);
                    }
                    else if (line.StartsWith("CustomSurface=", StringComparison.OrdinalIgnoreCase))
                    {
                        TryParseColor(line.Substring("CustomSurface=".Length), ref customSurfaceColor);
                    }
                    else if (line.StartsWith("CustomPrimaryText=", StringComparison.OrdinalIgnoreCase))
                    {
                        TryParseColor(line.Substring("CustomPrimaryText=".Length), ref customPrimaryTextColor);
                    }
                    else if (line.StartsWith("CustomSecondaryText=", StringComparison.OrdinalIgnoreCase))
                    {
                        TryParseColor(line.Substring("CustomSecondaryText=".Length), ref customSecondaryTextColor);
                    }
                    else if (line.StartsWith("CustomSettingsSidebar=", StringComparison.OrdinalIgnoreCase))
                    {
                        TryParseColor(line.Substring("CustomSettingsSidebar=".Length), ref customSettingsSidebarColor);
                        hasCustomSettingsSidebar = true;
                    }
                    else if (line.StartsWith("CustomSettingsBorder=", StringComparison.OrdinalIgnoreCase))
                    {
                        TryParseColor(line.Substring("CustomSettingsBorder=".Length), ref customSettingsBorderColor);
                        hasCustomSettingsBorder = true;
                    }
                }

                if (customColorsEnabled && !hasCustomSettingsSidebar)
                    customSettingsSidebarColor = customSurfaceColor;
                if (customColorsEnabled && !hasCustomSettingsBorder)
                    customSettingsBorderColor = MixColor(customSurfaceColor, customPrimaryTextColor, 18);

                if (!NotificationSoundEngine.IsValidPreset(soundPreset))
                    soundPreset = NotificationSoundEngine.DefaultPreset;
                soundVolume = Math.Max(0, Math.Min(100, soundVolume));
                muteButtonDesign = Math.Max(0, Math.Min(4, muteButtonDesign));

                Rectangle savedBounds = new Rectangle(x, y, Math.Max(MinimumSize.Width, width), Math.Max(MinimumSize.Height, height));
                if (rememberWindowPlacement && hasBounds && IsOnAnyScreen(savedBounds))
                {
                    StartPosition = FormStartPosition.Manual;
                    Bounds = savedBounds;
                }

                stayOnTop = savedTopMost;
                TopMost = stayOnTop;
                ShowInTaskbar = !hideFromTaskbar;
                toggleButton.VisualStyle = muteButtonDesign;
                UpdateShortcutDisplay();
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
                "ShortcutSource=" + shortcutSource + Environment.NewLine +
                "ControllerButton=" + currentControllerButton + Environment.NewLine +
                "MouseButton=" + currentMouseButton + Environment.NewLine +
                "Key2=" + secondKey + Environment.NewLine +
                "ShortcutSource2=" + secondShortcutSource + Environment.NewLine +
                "ControllerButton2=" + secondControllerButton + Environment.NewLine +
                "MouseButton2=" + secondMouseButton + Environment.NewLine +
                "DualHotkeyEnabled=" + dualHotkeyEnabled + Environment.NewLine +
                "DeveloperModeEnabled=" + developerModeEnabled + Environment.NewLine +
                "SoundFeedbackEnabled=" + soundFeedbackEnabled + Environment.NewLine +
                "SoundPreset=" + soundPreset + Environment.NewLine +
                "SoundVolume=" + soundVolume + Environment.NewLine +
                "AnimationsEnabled=" + animationsEnabled + Environment.NewLine +
                "HotkeyStripAboveCredit=" + hotkeyStripAboveCredit + Environment.NewLine +
                "LegacySettingsEnabled=" + legacySettingsEnabled + Environment.NewLine +
                "MuteButtonDesign=" + muteButtonDesign + Environment.NewLine +
                "X=" + Left + Environment.NewLine +
                "Y=" + Top + Environment.NewLine +
                "Width=" + Width + Environment.NewLine +
                "Height=" + Height + Environment.NewLine +
                "TopMost=" + stayOnTop + Environment.NewLine +
                "DeviceId=" + (mic.SelectedDeviceId ?? string.Empty) + Environment.NewLine +
                "HideFromTaskbar=" + hideFromTaskbar + Environment.NewLine +
                "RememberWindowPlacement=" + rememberWindowPlacement + Environment.NewLine +
                "Theme=" + (darkMode ? "Dark" : "White") + Environment.NewLine +
                "CustomColors=" + customColorsEnabled + Environment.NewLine +
                "CustomAccent=" + ColorValue(customAccentColor) + Environment.NewLine +
                "CustomCreator=" + ColorValue(customCreatorColor) + Environment.NewLine +
                "CustomBackground=" + ColorValue(customBackgroundColor) + Environment.NewLine +
                "CustomSurface=" + ColorValue(customSurfaceColor) + Environment.NewLine +
                "CustomPrimaryText=" + ColorValue(customPrimaryTextColor) + Environment.NewLine +
                "CustomSecondaryText=" + ColorValue(customSecondaryTextColor) + Environment.NewLine +
                "CustomSettingsSidebar=" + ColorValue(customSettingsSidebarColor) + Environment.NewLine +
                "CustomSettingsBorder=" + ColorValue(customSettingsBorderColor) + Environment.NewLine);
        }

        private static string ColorValue(Color color)
        {
            return color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2");
        }

        private static void TryParseColor(string value, ref Color target)
        {
            int rgb;
            if (value.Length == 6 && int.TryParse(value, System.Globalization.NumberStyles.HexNumber, null, out rgb))
                target = Color.FromArgb((rgb >> 16) & 255, (rgb >> 8) & 255, rgb & 255);
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
                return key != null && key.GetValue(AutoStartValueNameForExecutable(Application.ExecutablePath)) != null;
            }
        }

        private static void SetAutoStartEnabled(bool enabled)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
            {
                if (key == null)
                    throw new InvalidOperationException("Windows startup settings are unavailable.");

                if (enabled)
                    key.SetValue(AutoStartValueNameForExecutable(Application.ExecutablePath), "\"" + Application.ExecutablePath + "\"");
                else
                    key.DeleteValue(AutoStartValueNameForExecutable(Application.ExecutablePath), false);
            }
        }

        private static string ConfigPath()
        {
            return ConfigPathForExecutable(
                Application.ExecutablePath,
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
        }

        internal static string ConfigPathForExecutable(string executablePath, string appDataRoot)
        {
            return ConfigPathForRelease(executablePath, appDataRoot, ReleaseChannel, ReleaseVersion);
        }

        internal static string ConfigPathForRelease(string executablePath, string appDataRoot, string channel, string version)
        {
            return Path.Combine(
                appDataRoot,
                "PulseMute",
                "Instances",
                InstanceIdentityForExecutable(executablePath),
                SettingsIdentityForRelease(channel, version),
                "settings.txt");
        }

        internal static string AutoStartValueNameForExecutable(string executablePath)
        {
            return "PulseMuteAutoStart-" + InstanceIdentityForExecutable(executablePath) + "-" + ReleaseSettingsIdentity();
        }

        internal static string ReleaseMutexName()
        {
            return "Local\\PulseMute-" + ReleaseChannel + "-" +
                InstanceIdentityForExecutable(Application.ExecutablePath) + "-" + ReleaseSettingsIdentity();
        }

        internal static string ReleaseSettingsIdentity()
        {
            return SettingsIdentityForRelease(ReleaseChannel, ReleaseVersion);
        }

        internal static string SettingsIdentityForRelease(string channel, string version)
        {
            string identity = (channel ?? "PulseMute") + "-" + (version ?? "Unknown");
            foreach (char invalid in Path.GetInvalidFileNameChars())
                identity = identity.Replace(invalid, '_');
            return identity.Replace('.', '_');
        }

        internal static string InstanceIdentityForExecutable(string executablePath)
        {
            string fullPath;
            try { fullPath = Path.GetFullPath(executablePath ?? string.Empty); }
            catch { fullPath = executablePath ?? string.Empty; }
            string normalized = fullPath.Trim().ToUpperInvariant();

            uint hash = 2166136261;
            unchecked
            {
                foreach (char character in normalized)
                {
                    hash ^= character;
                    hash *= 16777619;
                }
            }

            string name = Path.GetFileNameWithoutExtension(fullPath);
            if (string.IsNullOrWhiteSpace(name))
                name = "PulseMute";
            name = name.ToUpperInvariant();
            string safeName = string.Empty;
            foreach (char character in name)
                safeName += char.IsLetterOrDigit(character) ? character : '_';
            if (safeName.Length > 36)
                safeName = safeName.Substring(0, 36);
            return safeName + "-" + hash.ToString("X8");
        }

        private static void RemoveMatchingLegacyAutoStart()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key == null)
                        return;
                    string executable = Application.ExecutablePath.Trim();
                    string currentValueName = AutoStartValueNameForExecutable(executable);
                    foreach (string valueName in key.GetValueNames())
                    {
                        bool pulseMuteEntry = string.Equals(valueName, "PulseMuteBetaAutoStart", StringComparison.OrdinalIgnoreCase) ||
                            valueName.StartsWith("PulseMuteAutoStart-", StringComparison.OrdinalIgnoreCase);
                        if (!pulseMuteEntry || string.Equals(valueName, currentValueName, StringComparison.OrdinalIgnoreCase))
                            continue;

                        string command = Convert.ToString(key.GetValue(valueName)).Trim().Trim('"');
                        if (string.Equals(command, executable, StringComparison.OrdinalIgnoreCase))
                            key.DeleteValue(valueName, false);
                    }
                }
            }
            catch
            {
            }
        }

        private static List<ArchivedVersionInfo> DiscoverArchivedVersions()
        {
            return DiscoverArchivedVersions(Application.StartupPath);
        }

        internal static List<ArchivedVersionInfo> DiscoverArchivedVersions(string startupPath)
        {
            List<ArchivedVersionInfo> versions = new List<ArchivedVersionInfo>();
            HashSet<string> seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string betaArchive = Path.Combine(startupPath, "Main source", "Version Archive");
            AddArchivedVersionFolders(betaArchive, "Beta ", versions, seenPaths);

            string projectRoot = Path.GetFullPath(Path.Combine(startupPath, "..", ".."));
            string legacyArchive = Path.Combine(projectRoot, "PulseMute older Versions");
            AddArchivedVersionFolders(legacyArchive, string.Empty, versions, seenPaths);

            versions.Sort(delegate(ArchivedVersionInfo left, ArchivedVersionInfo right)
            {
                return string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase);
            });
            return versions;
        }

        private static void AddArchivedVersionFolders(
            string archiveRoot,
            string displayPrefix,
            List<ArchivedVersionInfo> versions,
            HashSet<string> seenPaths)
        {
            if (!Directory.Exists(archiveRoot))
                return;

            string[] directories = Directory.GetDirectories(archiveRoot);
            Array.Sort(directories, StringComparer.OrdinalIgnoreCase);
            foreach (string directory in directories)
            {
                string[] executables = Directory.GetFiles(directory, "*.exe", SearchOption.TopDirectoryOnly);
                if (executables.Length != 1)
                    continue;

                string executable = Path.GetFullPath(executables[0]);
                if (!seenPaths.Add(executable))
                    continue;

                string folderName = Path.GetFileName(directory);
                string versionName = displayPrefix + folderName;
                versions.Add(new ArchivedVersionInfo(
                    ArchivedVersionDisplayName(versionName),
                    executable,
                    ArchivedVersionDetails(versionName)));
            }
        }

        private static string ArchivedVersionDisplayName(string name)
        {
            if (name == "PulseMute Test 26.1.0.2-test Source")
                return "PulseMute Test 26.1.0.2-test";
            return name;
        }

        private static string ArchivedVersionDetails(string name)
        {
            if (name == "PulseMute 1.1") return "Original compact release with one global keyboard hotkey.";
            if (name == "PulseMute 1.2") return "Added app settings, auto-start controls, microphone selection, and branding.";
            if (name == "PulseMute 1.3") return "Refined the title bar, creator credit, tray behavior, and taskbar hiding.";
            if (name == "PulseMute 1.4") return "Added dark and white themes, placement memory, sound settings, and DPI fixes.";
            if (name == "PulseMute 1.5") return "Introduced the professional settings and main-interface redesign.";
            if (name == "PulseMute 1.5.1") return "Alternative Focus interface design.";
            if (name == "PulseMute 1.5.2") return "Alternative Signal interface design.";
            if (name == "PulseMute 1.5.3") return "Alternative Console interface design.";
            if (name == "PulseMute 1.6 Stable") return "Added stable custom accent, surface, background, and text colors.";
            if (name == "PulseMute 1.6 Test") return "Experimental custom-color controls for interface testing.";
            if (name == "PulseMute PS 1.0") return "Added full DualSense and DualSense Edge button support.";
            if (name == "PulseMute PS 1.1") return "Added two independent keyboard or PlayStation hotkey slots.";
            if (name == "PulseMute Test 26.1.0.2-test Source") return "Added Mouse 1-5 and vertical or horizontal wheel hotkeys.";
            if (name == "Beta 26.1.0.3-beta") return "Added compact hotkey cards, Dual Hotkey mode, and improved scaling.";
            if (name == "Beta 26.1.0.4-beta") return "Added Developer Mode, archived-version switching, and compact release information.";
            if (name == "Beta 26.1.0.5-beta") return "Added selectable mute and unmute sound feedback with four built-in styles.";
            if (name == "Beta 26.1.0.6-beta") return "Introduced responsive sidebar Settings with focused category pages.";
            if (name == "Beta 26.1.0.7-beta") return "Expanded sound feedback, volume control, animations, and professional logo preview.";
            if (name == "Beta 26.1.0.8-beta") return "Promoted the professional logo, removed rounded-control experiments, and hardened sidebar Settings.";
            if (name == "Beta 26.1.0.9-beta") return "Redesigned native Settings with Silence-inspired navigation, cards, controls, and animation.";
            if (name == "Beta 26.1.0.10-beta") return "Unified front and Settings palettes with customizable Settings sidebar and border colors.";
            if (name == "Beta 26.1.0.11-beta") return "Added a themed HEX and RGB color editor with copy, paste, presets, and live preview.";
            if (name == "Beta 26.1.0.12-beta") return "Fixed custom dropdown menu lifetime and the disposed ContextMenuStrip error.";
            if (name == "Beta 26.1.0.13-beta") return "Moved the hotkey strip below creator credit, added visibility control, and isolated instance settings.";
            if (name == "Beta 26.1.0.14-beta") return "Placed compact Key 1 and Key 2 cards inside the bottom icon toolbar.";
            if (name == "Beta 26.1.0.15-beta") return "Replaced hotkey visibility with a bottom-toolbar or upper-position switch.";
            if (name == "Beta 26.1.0.16-beta") return "Added compact side-opening Settings and the #111418 default sidebar.";
            if (name == "Beta 26.1.0.17-beta") return "Reduced Settings to a minimal shell and changed the default accent to #C13545.";
            if (name == "Beta 26.1.0.18-beta") return "Fixed compact text, added release isolation, sharpened About, and introduced selectable mute visuals.";
            if (name == "Beta 26.1.0.19-beta") return "Rebuilt Developer cards, enlarged Settings, replaced two mute visuals, and sharpened typography.";
            if (name == "Beta 26.1.0.20-beta") return "Fixed Audio scaling, added the volume icon, increased logo resolution, and tested new mute silhouettes.";
            return "Archived PulseMute release.";
        }

        private static string HotkeyText(uint key)
        {
            return ((Keys)key).ToString();
        }

        private string CurrentShortcutText(int slot)
        {
            if (slot == 2)
            {
                if (secondShortcutSource == ShortcutSource.Mouse)
                    return MouseHotkeyProtocol.ButtonName(secondMouseButton);
                return secondShortcutSource == ShortcutSource.DualSense
                    ? "PS: " + DualSenseProtocol.ButtonName(secondControllerButton)
                    : HotkeyText(secondKey);
            }
            if (shortcutSource == ShortcutSource.Mouse)
                return MouseHotkeyProtocol.ButtonName(currentMouseButton);
            return shortcutSource == ShortcutSource.DualSense
                ? "PS: " + DualSenseProtocol.ButtonName(currentControllerButton)
                : HotkeyText(currentKey);
        }

        private void InstallKeyboardHook()
        {
            if (keyboardHook != IntPtr.Zero)
                return;

            keyboardHook = SetWindowsHookEx(WhKeyboardLl, keyboardProc, GetModuleHandle(null), 0);
            UpdateShortcutDisplay();
        }

        private void UninstallKeyboardHook()
        {
            if (keyboardHook == IntPtr.Zero)
                return;

            UnhookWindowsHookEx(keyboardHook);
            keyboardHook = IntPtr.Zero;
        }

        private void InstallMouseHook()
        {
            if (mouseHook != IntPtr.Zero)
                return;
            mouseHook = SetWindowsHookExMouse(WhMouseLl, mouseProc, GetModuleHandle(null), 0);
        }

        private void UninstallMouseHook()
        {
            if (mouseHook == IntPtr.Zero)
                return;
            UnhookWindowsHookEx(mouseHook);
            mouseHook = IntPtr.Zero;
        }

        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && captureShortcutSlot == 0)
            {
                int message = wParam.ToInt32();
                Keys key = (Keys)Marshal.ReadInt32(lParam);

                if (message == WmKeydown || message == WmSyskeydown)
                {
                    bool shouldToggle = false;
                    if (ShortcutBindingMatcher.MatchesKeyboard(shortcutSource, currentKey, key) && !shortcutOneHeld)
                    {
                        shortcutOneHeld = true;
                        shouldToggle = true;
                    }
                    if (ShortcutBindingMatcher.MatchesKeyboardSlot(
                        secondShortcutSource, secondKey, key, dualHotkeyEnabled) && !shortcutTwoHeld)
                    {
                        shortcutTwoHeld = true;
                        shouldToggle = true;
                    }
                    if (shouldToggle)
                        BeginInvoke((MethodInvoker)delegate { ToggleMute(); });
                }
                else if (message == WmKeyup || message == WmSyskeyup)
                {
                    if ((uint)key == currentKey)
                        shortcutOneHeld = false;
                    if ((uint)key == secondKey)
                        shortcutTwoHeld = false;
                }
            }

            return CallNextHookEx(keyboardHook, nCode, wParam, lParam);
        }

        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                MsllHookStruct data = (MsllHookStruct)Marshal.PtrToStructure(lParam, typeof(MsllHookStruct));
                MouseHotkey button;
                if ((data.Flags & LlMhfInjected) == 0 && MouseHotkeyProtocol.TryParseMessage(wParam.ToInt32(), data.MouseData, out button))
                {
                    if (captureShortcutSlot != 0)
                    {
                        int slot = captureShortcutSlot;
                        captureShortcutSlot = 0;
                        if (InvokeRequired)
                            BeginInvoke((MethodInvoker)delegate { AssignMouseShortcut(slot, button); });
                        else
                            AssignMouseShortcut(slot, button);
                        return new IntPtr(1);
                    }

                    if (!IsPulseMuteWindow(data.Point) && ShortcutBindingMatcher.MatchesMouse(
                        shortcutSource, currentMouseButton,
                        secondShortcutSource, secondMouseButton,
                        button, dualHotkeyEnabled))
                    {
                        BeginInvoke((MethodInvoker)delegate { ToggleMute(); });
                    }
                }
            }
            return CallNextHookEx(mouseHook, nCode, wParam, lParam);
        }

        private bool IsPulseMuteWindow(NativePoint point)
        {
            IntPtr window = WindowFromPoint(point);
            for (int depth = 0; window != IntPtr.Zero && depth < 8; depth++)
            {
                IntPtr root = GetAncestor(window, GaRoot);
                if (root == Handle)
                    return true;
                window = GetWindow(root, GwOwner);
            }
            return false;
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct NativePoint
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MsllHookStruct
        {
            public NativePoint Point;
            public uint MouseData;
            public uint Flags;
            public uint Time;
            public IntPtr ExtraInfo;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", EntryPoint = "SetWindowsHookEx", SetLastError = true)]
        private static extern IntPtr SetWindowsHookExMouse(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(NativePoint point);

        [DllImport("user32.dll")]
        private static extern IntPtr GetAncestor(IntPtr hwnd, int flags);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr hwnd, uint command);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int valueSize);

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hwnd, string subAppName, string subIdList);
    }

    internal static class ShortcutBindingMatcher
    {
        public static bool MatchesKeyboard(ShortcutSource source, uint assignedKey, Keys pressedKey)
        {
            return source == ShortcutSource.Keyboard && assignedKey == (uint)pressedKey;
        }

        public static bool MatchesKeyboardSlot(ShortcutSource source, uint assignedKey, Keys pressedKey, bool enabled)
        {
            return enabled && MatchesKeyboard(source, assignedKey, pressedKey);
        }

        public static bool MatchesController(
            ShortcutSource sourceOne, DualSenseButton buttonOne,
            ShortcutSource sourceTwo, DualSenseButton buttonTwo,
            DualSenseButton pressedButton)
        {
            return MatchesController(sourceOne, buttonOne, sourceTwo, buttonTwo, pressedButton, true);
        }

        public static bool MatchesController(
            ShortcutSource sourceOne, DualSenseButton buttonOne,
            ShortcutSource sourceTwo, DualSenseButton buttonTwo,
            DualSenseButton pressedButton, bool secondEnabled)
        {
            return (sourceOne == ShortcutSource.DualSense && buttonOne == pressedButton) ||
                (secondEnabled && sourceTwo == ShortcutSource.DualSense && buttonTwo == pressedButton);
        }

        public static bool MatchesMouse(
            ShortcutSource sourceOne, MouseHotkey buttonOne,
            ShortcutSource sourceTwo, MouseHotkey buttonTwo,
            MouseHotkey pressedButton)
        {
            return MatchesMouse(sourceOne, buttonOne, sourceTwo, buttonTwo, pressedButton, true);
        }

        public static bool MatchesMouse(
            ShortcutSource sourceOne, MouseHotkey buttonOne,
            ShortcutSource sourceTwo, MouseHotkey buttonTwo,
            MouseHotkey pressedButton, bool secondEnabled)
        {
            return (sourceOne == ShortcutSource.Mouse && buttonOne == pressedButton) ||
                (secondEnabled && sourceTwo == ShortcutSource.Mouse && buttonTwo == pressedButton);
        }
    }

    internal sealed class NotificationSoundEngine : IDisposable
    {
        public const string DefaultPreset = "Soft Chime";
        private static readonly string[] PresetNames =
        {
            DefaultPreset, "Digital", "Click", "Pulse", "8-Bit", "Arcade", "Radio", "Glass", "Signal"
        };
        private SoundPlayer player;
        private MemoryStream stream;

        public static string[] Presets
        {
            get { return (string[])PresetNames.Clone(); }
        }

        public static bool IsValidPreset(string preset)
        {
            foreach (string name in PresetNames)
            {
                if (string.Equals(name, preset, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        public void Play(string preset, bool muted, int volumePercent)
        {
            try
            {
                DisposePlayer();
                stream = new MemoryStream(CreateWaveData(preset, muted, volumePercent), false);
                player = new SoundPlayer(stream);
                player.Load();
                player.Play();
            }
            catch
            {
                DisposePlayer();
            }
        }

        internal static byte[] CreateWaveData(string preset, bool muted)
        {
            return CreateWaveData(preset, muted, 70);
        }

        internal static byte[] CreateWaveData(string preset, bool muted, int volumePercent)
        {
            if (!IsValidPreset(preset))
                preset = DefaultPreset;

            const int sampleRate = 22050;
            double duration = preset == "Pulse" || preset == "Glass" ? 0.28 :
                preset == "Soft Chime" || preset == "Arcade" ? 0.24 :
                preset == "Signal" ? 0.22 :
                preset == "Digital" || preset == "8-Bit" ? 0.18 :
                preset == "Radio" ? 0.16 : 0.11;
            int sampleCount = (int)(sampleRate * duration);
            short[] samples = new short[sampleCount];
            double volumeScale = Math.Max(0, Math.Min(100, volumePercent)) / 100.0;

            for (int i = 0; i < sampleCount; i++)
            {
                double time = i / (double)sampleRate;
                double progress = i / (double)Math.Max(1, sampleCount - 1);
                double fadeIn = Math.Min(1.0, i / (sampleRate * 0.006));
                double fadeOut = Math.Min(1.0, (sampleCount - 1 - i) / (sampleRate * 0.025));
                double envelope = Math.Max(0.0, Math.Min(fadeIn, fadeOut));
                double signal;

                if (preset == "Digital")
                {
                    double frequency = muted
                        ? (progress < 0.5 ? 620.0 : 390.0)
                        : (progress < 0.5 ? 390.0 : 680.0);
                    signal = Math.Sin(2.0 * Math.PI * frequency * time) +
                        0.18 * Math.Sin(2.0 * Math.PI * frequency * 2.0 * time);
                }
                else if (preset == "Click")
                {
                    double frequency = muted ? 320.0 : 760.0;
                    envelope *= Math.Exp(-18.0 * progress);
                    signal = Math.Sin(2.0 * Math.PI * frequency * time);
                }
                else if (preset == "Pulse")
                {
                    double pulsePosition = (progress * 2.0) % 1.0;
                    double pulseEnvelope = Math.Sin(Math.PI * Math.Min(1.0, pulsePosition / 0.72));
                    if (pulsePosition > 0.72)
                        pulseEnvelope = 0.0;
                    envelope *= Math.Max(0.0, pulseEnvelope);
                    double frequency = muted ? 410.0 : 690.0;
                    signal = Math.Sin(2.0 * Math.PI * frequency * time);
                }
                else if (preset == "8-Bit")
                {
                    int note = Math.Min(2, (int)(progress * 3.0));
                    double[] mutedNotes = { 660.0, 520.0, 390.0 };
                    double[] liveNotes = { 390.0, 520.0, 740.0 };
                    double frequency = muted ? mutedNotes[note] : liveNotes[note];
                    signal = Math.Sin(2.0 * Math.PI * frequency * time) >= 0 ? 0.78 : -0.78;
                }
                else if (preset == "Arcade")
                {
                    int note = Math.Min(3, (int)(progress * 4.0));
                    double[] mutedNotes = { 820.0, 660.0, 520.0, 390.0 };
                    double[] liveNotes = { 390.0, 520.0, 660.0, 880.0 };
                    double frequency = muted ? mutedNotes[note] : liveNotes[note];
                    signal = Math.Sin(2.0 * Math.PI * frequency * time) +
                        0.12 * Math.Sin(2.0 * Math.PI * frequency * 2.0 * time);
                }
                else if (preset == "Radio")
                {
                    double chirp = muted ? 760.0 - (340.0 * progress) : 420.0 + (420.0 * progress);
                    double gate = ((int)(progress * 6.0) % 2) == 0 ? 1.0 : 0.42;
                    envelope *= gate;
                    signal = Math.Sin(2.0 * Math.PI * chirp * time) +
                        0.1 * Math.Sin(2.0 * Math.PI * 120.0 * time);
                }
                else if (preset == "Glass")
                {
                    double frequency = muted ? 720.0 : 980.0;
                    envelope *= Math.Exp(-4.5 * progress);
                    signal = Math.Sin(2.0 * Math.PI * frequency * time) +
                        0.34 * Math.Sin(2.0 * Math.PI * frequency * 2.01 * time);
                }
                else if (preset == "Signal")
                {
                    double pulsePosition = (progress * 3.0) % 1.0;
                    envelope *= pulsePosition < 0.58 ? Math.Sin(Math.PI * pulsePosition / 0.58) : 0.0;
                    double frequency = muted ? 440.0 : 760.0;
                    signal = Math.Sin(2.0 * Math.PI * frequency * time);
                }
                else
                {
                    double startFrequency = muted ? 680.0 : 420.0;
                    double endFrequency = muted ? 410.0 : 720.0;
                    double frequency = startFrequency + (endFrequency - startFrequency) * progress;
                    signal = Math.Sin(2.0 * Math.PI * frequency * time) +
                        0.22 * Math.Sin(2.0 * Math.PI * frequency * 1.5 * time);
                }

                double volume = (preset == "Click" ? 0.22 : preset == "8-Bit" ? 0.15 : 0.18) * volumeScale;
                samples[i] = (short)(short.MaxValue * volume * envelope * Math.Max(-1.0, Math.Min(1.0, signal)));
            }

            using (MemoryStream output = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(output))
            {
                int dataSize = samples.Length * sizeof(short);
                writer.Write(new byte[] { 82, 73, 70, 70 });
                writer.Write(36 + dataSize);
                writer.Write(new byte[] { 87, 65, 86, 69 });
                writer.Write(new byte[] { 102, 109, 116, 32 });
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)1);
                writer.Write(sampleRate);
                writer.Write(sampleRate * sizeof(short));
                writer.Write((short)sizeof(short));
                writer.Write((short)16);
                writer.Write(new byte[] { 100, 97, 116, 97 });
                writer.Write(dataSize);
                foreach (short sample in samples)
                    writer.Write(sample);
                writer.Flush();
                return output.ToArray();
            }
        }

        public void Dispose()
        {
            DisposePlayer();
        }

        private void DisposePlayer()
        {
            if (player != null)
            {
                try { player.Stop(); }
                catch { }
                player.Dispose();
                player = null;
            }
            if (stream != null)
            {
                stream.Dispose();
                stream = null;
            }
        }
    }

    internal sealed class SoundPresetInfo
    {
        public string Name { get; private set; }

        public SoundPresetInfo(string name)
        {
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    internal sealed class ArchivedVersionInfo
    {
        public string DisplayName { get; private set; }
        public string ExecutablePath { get; private set; }
        public string Details { get; private set; }

        public ArchivedVersionInfo(string displayName, string executablePath, string details)
        {
            DisplayName = displayName;
            ExecutablePath = executablePath;
            Details = details;
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }

    internal enum ShortcutSource
    {
        Keyboard,
        DualSense,
        Mouse
    }

    internal enum MouseHotkey
    {
        Left,
        Right,
        Middle,
        XButton1,
        XButton2,
        WheelUp,
        WheelDown,
        WheelLeft,
        WheelRight
    }

    internal static class MouseHotkeyProtocol
    {
        private const int WmLButtonDown = 0x0201;
        private const int WmRButtonDown = 0x0204;
        private const int WmMButtonDown = 0x0207;
        private const int WmMouseWheel = 0x020A;
        private const int WmXButtonDown = 0x020B;
        private const int WmMouseHWheel = 0x020E;

        public static bool TryParseMessage(int message, uint mouseData, out MouseHotkey button)
        {
            button = MouseHotkey.Left;
            switch (message)
            {
                case WmLButtonDown: button = MouseHotkey.Left; return true;
                case WmRButtonDown: button = MouseHotkey.Right; return true;
                case WmMButtonDown: button = MouseHotkey.Middle; return true;
                case WmXButtonDown:
                    int xButton = (int)((mouseData >> 16) & 0xFFFF);
                    if (xButton == 1) { button = MouseHotkey.XButton1; return true; }
                    if (xButton == 2) { button = MouseHotkey.XButton2; return true; }
                    return false;
                case WmMouseWheel:
                    short wheelDelta = unchecked((short)((mouseData >> 16) & 0xFFFF));
                    if (wheelDelta == 0) return false;
                    button = wheelDelta > 0 ? MouseHotkey.WheelUp : MouseHotkey.WheelDown;
                    return true;
                case WmMouseHWheel:
                    short horizontalDelta = unchecked((short)((mouseData >> 16) & 0xFFFF));
                    if (horizontalDelta == 0) return false;
                    button = horizontalDelta > 0 ? MouseHotkey.WheelRight : MouseHotkey.WheelLeft;
                    return true;
                default:
                    return false;
            }
        }

        public static string ButtonName(MouseHotkey button)
        {
            switch (button)
            {
                case MouseHotkey.Left: return "Mouse 1";
                case MouseHotkey.Right: return "Mouse 2";
                case MouseHotkey.Middle: return "Mouse 3";
                case MouseHotkey.XButton1: return "Mouse 4";
                case MouseHotkey.XButton2: return "Mouse 5";
                case MouseHotkey.WheelUp: return "Wheel Up";
                case MouseHotkey.WheelDown: return "Wheel Down";
                case MouseHotkey.WheelLeft: return "Wheel Left";
                case MouseHotkey.WheelRight: return "Wheel Right";
                default: return "Mouse";
            }
        }
    }

    [Flags]
    internal enum DualSenseButton : ulong
    {
        None = 0,
        DPadUp = 1UL << 0,
        DPadRight = 1UL << 1,
        DPadDown = 1UL << 2,
        DPadLeft = 1UL << 3,
        Square = 1UL << 4,
        Cross = 1UL << 5,
        Circle = 1UL << 6,
        Triangle = 1UL << 7,
        L1 = 1UL << 8,
        R1 = 1UL << 9,
        L2 = 1UL << 10,
        R2 = 1UL << 11,
        Create = 1UL << 12,
        Options = 1UL << 13,
        L3 = 1UL << 14,
        R3 = 1UL << 15,
        PS = 1UL << 16,
        Touchpad = 1UL << 17,
        MicrophoneMute = 1UL << 18,
        EdgeFnLeft = 1UL << 19,
        EdgeFnRight = 1UL << 20,
        EdgeLeftPaddle = 1UL << 21,
        EdgeRightPaddle = 1UL << 22
    }

    internal sealed class DualSenseButtonEventArgs : EventArgs
    {
        public DualSenseButton Button { get; private set; }

        public DualSenseButtonEventArgs(DualSenseButton button)
        {
            Button = button;
        }
    }

    internal static class DualSenseProtocol
    {
        private static readonly DualSenseButton[] AllButtons =
        {
            DualSenseButton.DPadUp, DualSenseButton.DPadRight, DualSenseButton.DPadDown, DualSenseButton.DPadLeft,
            DualSenseButton.Square, DualSenseButton.Cross, DualSenseButton.Circle, DualSenseButton.Triangle,
            DualSenseButton.L1, DualSenseButton.R1, DualSenseButton.L2, DualSenseButton.R2,
            DualSenseButton.Create, DualSenseButton.Options, DualSenseButton.L3, DualSenseButton.R3,
            DualSenseButton.PS, DualSenseButton.Touchpad, DualSenseButton.MicrophoneMute,
            DualSenseButton.EdgeFnLeft, DualSenseButton.EdgeFnRight,
            DualSenseButton.EdgeLeftPaddle, DualSenseButton.EdgeRightPaddle
        };

        public static DualSenseButton[] Buttons
        {
            get { return (DualSenseButton[])AllButtons.Clone(); }
        }

        public static bool TryParseReport(byte[] report, int count, out DualSenseButton buttons)
        {
            buttons = DualSenseButton.None;
            if (report == null || count <= 0 || count > report.Length)
                return false;

            int commonOffset;
            if (report[0] == 0x01)
            {
                commonOffset = 1;
            }
            else if (report[0] == 0x31)
            {
                commonOffset = 2;
            }
            else
            {
                return false;
            }

            int buttonOffset = commonOffset + 7;
            if (count <= buttonOffset + 1)
                return false;

            byte buttons0 = report[buttonOffset];
            byte buttons1 = report[buttonOffset + 1];
            byte buttons2 = count > buttonOffset + 2 ? report[buttonOffset + 2] : (byte)0;
            int hat = buttons0 & 0x0F;

            if (hat == 0 || hat == 1 || hat == 7) buttons |= DualSenseButton.DPadUp;
            if (hat == 1 || hat == 2 || hat == 3) buttons |= DualSenseButton.DPadRight;
            if (hat == 3 || hat == 4 || hat == 5) buttons |= DualSenseButton.DPadDown;
            if (hat == 5 || hat == 6 || hat == 7) buttons |= DualSenseButton.DPadLeft;

            if ((buttons0 & 0x10) != 0) buttons |= DualSenseButton.Square;
            if ((buttons0 & 0x20) != 0) buttons |= DualSenseButton.Cross;
            if ((buttons0 & 0x40) != 0) buttons |= DualSenseButton.Circle;
            if ((buttons0 & 0x80) != 0) buttons |= DualSenseButton.Triangle;
            if ((buttons1 & 0x01) != 0) buttons |= DualSenseButton.L1;
            if ((buttons1 & 0x02) != 0) buttons |= DualSenseButton.R1;
            if ((buttons1 & 0x04) != 0) buttons |= DualSenseButton.L2;
            if ((buttons1 & 0x08) != 0) buttons |= DualSenseButton.R2;
            if ((buttons1 & 0x10) != 0) buttons |= DualSenseButton.Create;
            if ((buttons1 & 0x20) != 0) buttons |= DualSenseButton.Options;
            if ((buttons1 & 0x40) != 0) buttons |= DualSenseButton.L3;
            if ((buttons1 & 0x80) != 0) buttons |= DualSenseButton.R3;
            if ((buttons2 & 0x01) != 0) buttons |= DualSenseButton.PS;
            if ((buttons2 & 0x02) != 0) buttons |= DualSenseButton.Touchpad;
            if ((buttons2 & 0x04) != 0) buttons |= DualSenseButton.MicrophoneMute;
            if ((buttons2 & 0x10) != 0) buttons |= DualSenseButton.EdgeFnLeft;
            if ((buttons2 & 0x20) != 0) buttons |= DualSenseButton.EdgeFnRight;
            if ((buttons2 & 0x40) != 0) buttons |= DualSenseButton.EdgeLeftPaddle;
            if ((buttons2 & 0x80) != 0) buttons |= DualSenseButton.EdgeRightPaddle;
            return true;
        }

        public static string ButtonName(DualSenseButton button)
        {
            switch (button)
            {
                case DualSenseButton.DPadUp: return "D-pad Up";
                case DualSenseButton.DPadRight: return "D-pad Right";
                case DualSenseButton.DPadDown: return "D-pad Down";
                case DualSenseButton.DPadLeft: return "D-pad Left";
                case DualSenseButton.Square: return "Square";
                case DualSenseButton.Cross: return "Cross";
                case DualSenseButton.Circle: return "Circle";
                case DualSenseButton.Triangle: return "Triangle";
                case DualSenseButton.L1: return "L1";
                case DualSenseButton.R1: return "R1";
                case DualSenseButton.L2: return "L2";
                case DualSenseButton.R2: return "R2";
                case DualSenseButton.Create: return "Create";
                case DualSenseButton.Options: return "Options";
                case DualSenseButton.L3: return "L3";
                case DualSenseButton.R3: return "R3";
                case DualSenseButton.PS: return "PS";
                case DualSenseButton.Touchpad: return "Touchpad";
                case DualSenseButton.MicrophoneMute: return "Mute";
                case DualSenseButton.EdgeFnLeft: return "Fn Left";
                case DualSenseButton.EdgeFnRight: return "Fn Right";
                case DualSenseButton.EdgeLeftPaddle: return "Left Paddle";
                case DualSenseButton.EdgeRightPaddle: return "Right Paddle";
                default: return "Unknown";
            }
        }

        public static byte[] CreateBluetoothEnhancedModeReport()
        {
            byte[] report = new byte[78];
            report[0] = 0x31;
            report[1] = 0x00;
            report[2] = 0x10;
            uint crc = ComputeBluetoothCrc(report, 74);
            report[74] = (byte)(crc & 0xFF);
            report[75] = (byte)((crc >> 8) & 0xFF);
            report[76] = (byte)((crc >> 16) & 0xFF);
            report[77] = (byte)((crc >> 24) & 0xFF);
            return report;
        }

        public static uint ComputeBluetoothCrc(byte[] data, int count)
        {
            uint crc = 0xFFFFFFFF;
            crc = UpdateCrc(crc, 0xA2);
            for (int i = 0; i < count; i++)
                crc = UpdateCrc(crc, data[i]);
            return ~crc;
        }

        private static uint UpdateCrc(uint crc, byte value)
        {
            crc ^= value;
            for (int bit = 0; bit < 8; bit++)
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;
            return crc;
        }
    }

    internal sealed class DualSenseControllerService : IDisposable
    {
        private const int SonyVendorId = 0x054C;
        private const int DualSenseProductId = 0x0CE6;
        private const int DualSenseEdgeProductId = 0x0DF2;
        private readonly object streamLock = new object();
        private volatile bool running;
        private volatile bool forceRescan;
        private volatile string statusText = "Not connected";
        private System.Threading.Thread worker;
        private HidStream activeStream;

        public event EventHandler<DualSenseButtonEventArgs> ButtonPressed;

        public string StatusText
        {
            get { return statusText; }
        }

        public void Start()
        {
            if (running)
                return;
            running = true;
            worker = new System.Threading.Thread(ReadLoop);
            worker.IsBackground = true;
            worker.Name = "PulseMute DualSense input";
            worker.Start();
        }

        public void Rescan()
        {
            forceRescan = true;
            statusText = "Searching...";
            CloseActiveStream();
        }

        public void Stop()
        {
            running = false;
            forceRescan = true;
            CloseActiveStream();
            if (worker != null && worker.IsAlive)
                worker.Join(1500);
            worker = null;
            statusText = "Not connected";
        }

        public void Dispose()
        {
            Stop();
        }

        private void ReadLoop()
        {
            while (running)
            {
                forceRescan = false;
                HidDevice device = FindController();
                if (device == null)
                {
                    statusText = "Not connected";
                    WaitForRescan(1000);
                    continue;
                }

                HidStream stream = null;
                try
                {
                    OpenConfiguration configuration = new OpenConfiguration();
                    configuration.SetOption(OpenOption.Exclusive, false);
                    device.TryOpen(configuration, out stream);
                }
                catch
                {
                }
                if (stream == null)
                {
                    statusText = "Controller is busy";
                    WaitForRescan(1000);
                    continue;
                }

                lock (streamLock)
                    activeStream = stream;

                try
                {
                    stream.ReadTimeout = 500;
                    stream.WriteTimeout = 500;
                    TryEnableBluetoothEnhancedMode(device, stream);
                    statusText = ControllerName(device) + " connected";
                    int reportLength = 78;
                    try { reportLength = Math.Max(78, device.GetMaxInputReportLength()); }
                    catch { }
                    byte[] report = new byte[reportLength];
                    DualSenseButton previousButtons = DualSenseButton.None;
                    bool hasPreviousReport = false;

                    while (running && !forceRescan)
                    {
                        try
                        {
                            int count = stream.Read(report, 0, report.Length);
                            DualSenseButton buttons;
                            if (!DualSenseProtocol.TryParseReport(report, count, out buttons))
                                continue;

                            if (!hasPreviousReport)
                            {
                                previousButtons = buttons;
                                hasPreviousReport = true;
                                continue;
                            }

                            DualSenseButton pressed = buttons & ~previousButtons;
                            previousButtons = buttons;
                            RaisePressedButtons(pressed);
                        }
                        catch (TimeoutException)
                        {
                        }
                        catch (IOException)
                        {
                            break;
                        }
                        catch (ObjectDisposedException)
                        {
                            break;
                        }
                        catch
                        {
                            break;
                        }
                    }
                }
                finally
                {
                    lock (streamLock)
                    {
                        if (ReferenceEquals(activeStream, stream))
                            activeStream = null;
                    }
                    try { stream.Dispose(); }
                    catch { }
                }

                if (running)
                {
                    statusText = "Reconnecting...";
                    WaitForRescan(500);
                }
            }
        }

        private static HidDevice FindController()
        {
            HidDevice best = null;
            int bestLength = 0;
            try
            {
                foreach (HidDevice device in DeviceList.Local.GetHidDevices(SonyVendorId))
                {
                    if (device.ProductID != DualSenseProductId && device.ProductID != DualSenseEdgeProductId)
                        continue;
                    try
                    {
                        int length = device.GetMaxInputReportLength();
                        if (length >= 10 && length > bestLength)
                        {
                            best = device;
                            bestLength = length;
                        }
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }
            return best;
        }

        private static string ControllerName(HidDevice device)
        {
            if (device.ProductID == DualSenseEdgeProductId)
                return "DualSense Edge";
            return "DualSense";
        }

        private static void TryEnableBluetoothEnhancedMode(HidDevice device, HidStream stream)
        {
            try
            {
                if (device.GetMaxInputReportLength() >= 78 && device.GetMaxOutputReportLength() >= 78)
                    stream.Write(DualSenseProtocol.CreateBluetoothEnhancedModeReport());
            }
            catch
            {
                // Basic reports still provide the standard controls if enhanced mode is unavailable.
            }
        }

        private void RaisePressedButtons(DualSenseButton pressed)
        {
            if (pressed == DualSenseButton.None)
                return;
            EventHandler<DualSenseButtonEventArgs> handler = ButtonPressed;
            if (handler == null)
                return;
            foreach (DualSenseButton button in DualSenseProtocol.Buttons)
            {
                if ((pressed & button) != 0)
                    handler(this, new DualSenseButtonEventArgs(button));
            }
        }

        private void CloseActiveStream()
        {
            lock (streamLock)
            {
                if (activeStream != null)
                {
                    try { activeStream.Dispose(); }
                    catch { }
                    activeStream = null;
                }
            }
        }

        private void WaitForRescan(int milliseconds)
        {
            int waited = 0;
            while (running && !forceRescan && waited < milliseconds)
            {
                System.Threading.Thread.Sleep(100);
                waited += 100;
            }
        }
    }

    internal sealed class ThemedComboBox : ComboBox
    {
        public Color HighlightColor = Color.FromArgb(34, 139, 111);

        public ThemedComboBox()
        {
            DrawMode = DrawMode.OwnerDrawFixed;
            ItemHeight = 22;
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            int itemIndex = e.Index < 0 ? SelectedIndex : e.Index;
            if (itemIndex < 0 || itemIndex >= Items.Count)
                return;

            bool highlighted = DroppedDown && (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            Color background = highlighted ? HighlightColor : BackColor;
            Color foreground = highlighted ? Color.White : ForeColor;

            using (SolidBrush brush = new SolidBrush(background))
                e.Graphics.FillRectangle(brush, e.Bounds);

            Rectangle paintBounds = e.Bounds;
            if (!DroppedDown)
            {
                int arrowWidth = SystemInformation.VerticalScrollBarWidth;
                paintBounds = new Rectangle(
                    e.Bounds.Left,
                    e.Bounds.Top,
                    Math.Max(1, e.Bounds.Width - arrowWidth),
                    e.Bounds.Height);
                using (SolidBrush brush = new SolidBrush(BackColor))
                    e.Graphics.FillRectangle(brush, paintBounds);
            }

            Rectangle textBounds = new Rectangle(paintBounds.Left + 5, paintBounds.Top, Math.Max(1, paintBounds.Width - 8), paintBounds.Height);
            TextRenderer.DrawText(
                e.Graphics,
                Convert.ToString(Items[itemIndex]),
                Font,
                textBounds,
                foreground,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            if (highlighted)
                e.DrawFocusRectangle();
        }
    }

    internal sealed class ToggleSwitch : CheckBox
    {
        public Color OnColor = Color.FromArgb(34, 139, 111);
        public Color OffColor = Color.FromArgb(67, 75, 86);
        public Color ThumbColor = Color.White;
        private readonly Timer animationTimer;
        private float animationProgress;
        private float animationTarget;
        private bool animationsEnabled = true;

        public bool AnimationsEnabled
        {
            get { return animationsEnabled; }
            set
            {
                animationsEnabled = value;
                if (!animationsEnabled)
                {
                    animationTimer.Stop();
                    animationProgress = Checked ? 1F : 0F;
                    animationTarget = animationProgress;
                    Invalidate();
                }
            }
        }

        public ToggleSwitch()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            TabStop = true;
            animationTimer = new Timer();
            animationTimer.Interval = 16;
            animationTimer.Tick += delegate
            {
                animationProgress += (animationTarget - animationProgress) * 0.38F;
                if (Math.Abs(animationTarget - animationProgress) < 0.015F)
                {
                    animationProgress = animationTarget;
                    animationTimer.Stop();
                }
                Invalidate();
            };
        }

        protected override void OnCheckedChanged(EventArgs e)
        {
            base.OnCheckedChanged(e);
            animationTarget = Checked ? 1F : 0F;
            if (animationsEnabled && IsHandleCreated)
                animationTimer.Start();
            else
                animationProgress = animationTarget;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics graphics = e.Graphics;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(Parent == null ? Color.Transparent : Parent.BackColor);

            Rectangle track = new Rectangle(1, 3, Math.Max(1, Width - 2), Math.Max(1, Height - 6));
            int radius = track.Height / 2;
            Color animatedTrackColor = InterpolateColor(OffColor, OnColor, animationProgress);
            using (GraphicsPath path = RoundedPath(track, radius))
            using (SolidBrush trackBrush = new SolidBrush(animatedTrackColor))
            using (SolidBrush thumbBrush = new SolidBrush(ThumbColor))
            {
                graphics.FillPath(trackBrush, path);
                int thumbSize = Math.Max(8, Height - 10);
                int thumbStart = 5;
                int thumbEnd = Width - thumbSize - 5;
                int thumbX = thumbStart + (int)Math.Round((thumbEnd - thumbStart) * animationProgress);
                graphics.FillEllipse(thumbBrush, thumbX, (Height - thumbSize) / 2, thumbSize, thumbSize);
            }

            if (Focused)
                ControlPaint.DrawFocusRectangle(graphics, ClientRectangle);
        }

        private static GraphicsPath RoundedPath(Rectangle bounds, int radius)
        {
            int diameter = Math.Max(2, radius * 2);
            GraphicsPath path = new GraphicsPath();
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 90, 180);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 180);
            path.CloseFigure();
            return path;
        }

        private static Color InterpolateColor(Color from, Color to, float amount)
        {
            amount = Math.Max(0F, Math.Min(1F, amount));
            return Color.FromArgb(
                from.A + (int)((to.A - from.A) * amount),
                from.R + (int)((to.R - from.R) * amount),
                from.G + (int)((to.G - from.G) * amount),
                from.B + (int)((to.B - from.B) * amount));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                animationTimer.Dispose();
            base.Dispose(disposing);
        }
    }

    internal sealed class RoundButton : Button
    {
        public Color PrimaryColor = Color.FromArgb(24, 151, 104);
        public Color SecondaryColor = Color.FromArgb(21, 91, 83);
        public int ShapeStyle;
        public int VisualStyle;

        protected override void OnPaint(PaintEventArgs pevent)
        {
            Graphics g = pevent.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.Clear(Parent == null ? Color.Black : Parent.BackColor);

            Rectangle face = new Rectangle(2, 2, Math.Max(1, Width - 5), Math.Max(1, Height - 5));
            Rectangle shadow = new Rectangle(6, 8, Math.Max(1, Width - 12), Math.Max(1, Height - 14));
            using (GraphicsPath path = CreateShapePath(face))
            using (GraphicsPath shadowPath = CreateShapePath(shadow))
            using (LinearGradientBrush brush = new LinearGradientBrush(ClientRectangle, PrimaryColor, SecondaryColor, 45f))
            using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(55, Color.Black)))
            using (Pen pen = new Pen(Color.FromArgb(80, Color.White), 2))
            {
                g.FillPath(shadowBrush, shadowPath);
                g.FillPath(brush, path);
                g.DrawPath(pen, path);
            }

            if (VisualStyle == 0)
            {
                TextRenderer.DrawText(
                    g,
                    Text,
                    Font,
                    ClientRectangle,
                    ForeColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
            else
            {
                DrawVisualGlyph(g, VisualStyle, string.Equals(Text, "Unmute", StringComparison.OrdinalIgnoreCase));
                Rectangle textBounds = new Rectangle(8, (int)(Height * 0.65F), Math.Max(1, Width - 16), Math.Max(1, (int)(Height * 0.25F)));
                TextRenderer.DrawText(
                    g,
                    Text,
                    Font,
                    textBounds,
                    ForeColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            }
        }

        private GraphicsPath CreateShapePath(Rectangle bounds)
        {
            GraphicsPath path = new GraphicsPath();
            if (VisualStyle == 0 && ShapeStyle == 0)
            {
                path.AddEllipse(bounds);
                return path;
            }

            int baseSize = Math.Min(bounds.Width, bounds.Height);
            int radius = VisualStyle == 1
                ? Math.Max(10, baseSize / 5)
                : VisualStyle == 3
                    ? Math.Max(10, baseSize / 4)
                    : VisualStyle == 4
                        ? Math.Max(10, baseSize / 3)
                        : ShapeStyle == 1 ? Math.Max(8, bounds.Height / 2) : Math.Max(8, baseSize / 7);
            int diameter = Math.Min(Math.Min(bounds.Width, bounds.Height), radius * 2);
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void DrawVisualGlyph(Graphics graphics, int style, bool muted)
        {
            float unit = Math.Max(0.55F, Math.Min(Width, Height) / 126F);
            float centerX = Width / 2F;
            float centerY = Height * 0.38F;
            using (Pen pen = new Pen(ForeColor, Math.Max(1.6F, 2.3F * unit)))
            using (SolidBrush brush = new SolidBrush(ForeColor))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;

                if (style == 2)
                {
                    float[] heights = { 10F, 20F, 30F, 20F, 10F };
                    for (int i = 0; i < heights.Length; i++)
                    {
                        float x = centerX + (i - 2) * 9F * unit;
                        float half = heights[i] * unit / 2F;
                        graphics.DrawLine(pen, x, centerY - half, x, centerY + half);
                    }
                }
                else if (style == 3)
                {
                    graphics.DrawEllipse(pen, centerX - 22F * unit, centerY - 22F * unit, 44F * unit, 44F * unit);
                    graphics.FillEllipse(brush, centerX - 9F * unit, centerY - 9F * unit, 18F * unit, 18F * unit);
                }
                else if (style == 4)
                {
                    graphics.DrawArc(pen, centerX - 23F * unit, centerY - 18F * unit, 46F * unit, 46F * unit, -48, 276);
                    graphics.DrawLine(pen, centerX, centerY - 27F * unit, centerX, centerY + 1F * unit);
                }
                else
                {
                    RectangleF capsule = new RectangleF(centerX - 9F * unit, centerY - 21F * unit, 18F * unit, 32F * unit);
                    using (GraphicsPath micPath = RoundedRectanglePath(capsule, 9F * unit))
                        graphics.FillPath(brush, micPath);
                    graphics.DrawArc(pen, centerX - 17F * unit, centerY - 5F * unit, 34F * unit, 28F * unit, 0, 180);
                    graphics.DrawLine(pen, centerX, centerY + 23F * unit, centerX, centerY + 29F * unit);
                    graphics.DrawLine(pen, centerX - 9F * unit, centerY + 29F * unit, centerX + 9F * unit, centerY + 29F * unit);
                }

                if (muted)
                    graphics.DrawLine(pen, centerX - 24F * unit, centerY - 23F * unit, centerX + 24F * unit, centerY + 25F * unit);
            }
        }

        private static GraphicsPath RoundedRectanglePath(RectangleF bounds, float radius)
        {
            float diameter = Math.Max(2F, radius * 2F);
            GraphicsPath path = new GraphicsPath();
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
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
