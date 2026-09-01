using System;
using HidSharp;

namespace PulseMute
{
    internal static class ControllerProtocolTests
    {
        private static int assertions;

        [STAThread]
        private static void Main()
        {
            DualSenseButton[] buttons = DualSenseProtocol.Buttons;
            Assert(buttons.Length == 23, "Expected 23 assignable controller buttons.");

            DualSenseButton unique = DualSenseButton.None;
            foreach (DualSenseButton button in buttons)
            {
                Assert((unique & button) == 0, "Duplicate button flag: " + button);
                unique |= button;
                Assert(DualSenseProtocol.ButtonName(button) != "Unknown", "Missing display name: " + button);
                TestSingleButton(0x01, 64, 8, button, "USB");
                TestSingleButton(0x31, 78, 9, button, "Bluetooth");
            }

            byte[] diagonal = CreateNeutralReport(0x01, 64, 8);
            diagonal[8] = 1;
            DualSenseButton parsed;
            Assert(DualSenseProtocol.TryParseReport(diagonal, diagonal.Length, out parsed), "Diagonal report rejected.");
            Assert(parsed == (DualSenseButton.DPadUp | DualSenseButton.DPadRight), "Diagonal D-pad parsing failed.");

            byte[] basicBluetooth = CreateNeutralReport(0x01, 10, 8);
            basicBluetooth[8] |= 0x20;
            basicBluetooth[9] |= 0x02;
            Assert(DualSenseProtocol.TryParseReport(basicBluetooth, basicBluetooth.Length, out parsed), "Basic Bluetooth report rejected.");
            Assert(parsed == (DualSenseButton.Cross | DualSenseButton.R1), "Basic Bluetooth button parsing failed.");

            byte[] enhanced = DualSenseProtocol.CreateBluetoothEnhancedModeReport();
            Assert(enhanced.Length == 78 && enhanced[0] == 0x31 && enhanced[2] == 0x10, "Enhanced-mode report header failed.");
            uint storedCrc = (uint)(enhanced[74] | (enhanced[75] << 8) | (enhanced[76] << 16) | (enhanced[77] << 24));
            Assert(storedCrc == DualSenseProtocol.ComputeBluetoothCrc(enhanced, 74), "Enhanced-mode CRC failed.");

            TestTwoShortcutBindings();
            TestMouseHotkeys();
            TestResponsiveHotkeyLayout();
            TestInstanceIsolation();
            TestNativeSettingsDesign();
            TestSettingsDropDownLifetime();
            TestColorCodeEditor();
            TestNotificationSounds();
            TestProfessionalLogoResource();
            TestArchivedVersionDiscovery();
            TestMouseHookInstallation();
            TestConnectedController();

            Console.WriteLine("PASS: " + assertions + " input assertions");
        }

        private static void TestTwoShortcutBindings()
        {
            Assert(ShortcutBindingMatcher.MatchesKeyboard(ShortcutSource.Keyboard, (uint)System.Windows.Forms.Keys.F8, System.Windows.Forms.Keys.F8), "Key 1 keyboard match failed.");
            Assert(!ShortcutBindingMatcher.MatchesKeyboard(ShortcutSource.DualSense, (uint)System.Windows.Forms.Keys.F8, System.Windows.Forms.Keys.F8), "Controller assignment matched keyboard input.");
            Assert(ShortcutBindingMatcher.MatchesKeyboard(ShortcutSource.Keyboard, (uint)System.Windows.Forms.Keys.F9, System.Windows.Forms.Keys.F9), "Key 2 keyboard match failed.");
            Assert(!ShortcutBindingMatcher.MatchesKeyboardSlot(ShortcutSource.Keyboard, (uint)System.Windows.Forms.Keys.F9, System.Windows.Forms.Keys.F9, false), "Disabled Key 2 matched keyboard input.");
            Assert(ShortcutBindingMatcher.MatchesController(
                ShortcutSource.Keyboard, DualSenseButton.MicrophoneMute,
                ShortcutSource.DualSense, DualSenseButton.Touchpad,
                DualSenseButton.Touchpad), "Mixed keyboard/controller slots failed.");
            Assert(ShortcutBindingMatcher.MatchesController(
                ShortcutSource.DualSense, DualSenseButton.MicrophoneMute,
                ShortcutSource.DualSense, DualSenseButton.MicrophoneMute,
                DualSenseButton.MicrophoneMute), "Duplicate controller assignment failed.");
            Assert(!ShortcutBindingMatcher.MatchesController(
                ShortcutSource.DualSense, DualSenseButton.PS,
                ShortcutSource.DualSense, DualSenseButton.Touchpad,
                DualSenseButton.MicrophoneMute), "Unassigned controller button matched.");
            Assert(!ShortcutBindingMatcher.MatchesController(
                ShortcutSource.Keyboard, DualSenseButton.PS,
                ShortcutSource.DualSense, DualSenseButton.Touchpad,
                DualSenseButton.Touchpad, false), "Disabled Key 2 matched controller input.");
            Assert(ShortcutBindingMatcher.MatchesController(
                ShortcutSource.DualSense, DualSenseButton.PS,
                ShortcutSource.DualSense, DualSenseButton.Touchpad,
                DualSenseButton.PS, false), "Key 1 controller input was disabled with Dual Hotkey.");
        }

        private static void TestMouseHotkeys()
        {
            AssertMouse(0x0201, 0, MouseHotkey.Left);
            AssertMouse(0x0204, 0, MouseHotkey.Right);
            AssertMouse(0x0207, 0, MouseHotkey.Middle);
            AssertMouse(0x020B, 1u << 16, MouseHotkey.XButton1);
            AssertMouse(0x020B, 2u << 16, MouseHotkey.XButton2);
            AssertMouse(0x020A, 120u << 16, MouseHotkey.WheelUp);
            AssertMouse(0x020A, unchecked((uint)(-120 << 16)), MouseHotkey.WheelDown);
            AssertMouse(0x020E, 120u << 16, MouseHotkey.WheelRight);
            AssertMouse(0x020E, unchecked((uint)(-120 << 16)), MouseHotkey.WheelLeft);

            MouseHotkey ignored;
            Assert(!MouseHotkeyProtocol.TryParseMessage(0x020B, 0, out ignored), "Invalid X button was accepted.");
            Assert(!MouseHotkeyProtocol.TryParseMessage(0x020A, 0, out ignored), "Zero wheel delta was accepted.");
            Assert(!MouseHotkeyProtocol.TryParseMessage(0x0200, 0, out ignored), "Mouse movement was accepted as a hotkey.");
            Assert(ShortcutBindingMatcher.MatchesMouse(
                ShortcutSource.Keyboard, MouseHotkey.Left,
                ShortcutSource.Mouse, MouseHotkey.XButton1,
                MouseHotkey.XButton1), "Mixed keyboard/mouse slots failed.");
            Assert(ShortcutBindingMatcher.MatchesMouse(
                ShortcutSource.Mouse, MouseHotkey.Middle,
                ShortcutSource.Mouse, MouseHotkey.Middle,
                MouseHotkey.Middle), "Duplicate mouse assignment failed.");
            Assert(!ShortcutBindingMatcher.MatchesMouse(
                ShortcutSource.Mouse, MouseHotkey.Left,
                ShortcutSource.Mouse, MouseHotkey.Right,
                MouseHotkey.XButton2), "Unassigned mouse button matched.");
            Assert(!ShortcutBindingMatcher.MatchesMouse(
                ShortcutSource.Keyboard, MouseHotkey.Left,
                ShortcutSource.Mouse, MouseHotkey.Right,
                MouseHotkey.Right, false), "Disabled Key 2 matched mouse input.");
            Assert(ShortcutBindingMatcher.MatchesMouse(
                ShortcutSource.Mouse, MouseHotkey.Left,
                ShortcutSource.Mouse, MouseHotkey.Right,
                MouseHotkey.Left, false), "Key 1 mouse input was disabled with Dual Hotkey.");
        }

        private static void AssertMouse(int message, uint mouseData, MouseHotkey expected)
        {
            MouseHotkey parsed;
            Assert(MouseHotkeyProtocol.TryParseMessage(message, mouseData, out parsed), "Mouse message was rejected for " + expected + ".");
            Assert(parsed == expected, "Mouse message parsed " + parsed + " instead of " + expected + ".");
            Assert(MouseHotkeyProtocol.ButtonName(parsed) != "Mouse", "Mouse display name missing for " + expected + ".");
        }

        private static void TestMouseHookInstallation()
        {
            MainForm form = new MainForm();
            Type type = typeof(MainForm);
            System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            try
            {
                type.GetMethod("InstallMouseHook", flags).Invoke(form, null);
                IntPtr hook = (IntPtr)type.GetField("mouseHook", flags).GetValue(form);
                Assert(hook != IntPtr.Zero, "Windows did not install the PulseMute mouse hook.");
                type.GetMethod("UninstallMouseHook", flags).Invoke(form, null);
            }
            finally
            {
                System.Windows.Forms.Timer timer = (System.Windows.Forms.Timer)type.GetField("refreshTimer", flags).GetValue(form);
                timer.Stop();
                System.Windows.Forms.NotifyIcon tray = (System.Windows.Forms.NotifyIcon)type.GetField("tray", flags).GetValue(form);
                tray.Visible = false;
                tray.Dispose();
                form.Dispose();
            }
        }

        private static void TestResponsiveHotkeyLayout()
        {
            MainForm form = new MainForm();
            Type type = typeof(MainForm);
            System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            System.Reflection.MethodInfo applyLayout = type.GetMethod("ApplyResponsiveLayout", flags);
            System.Windows.Forms.Control keyOne = (System.Windows.Forms.Control)type.GetField("shortcutValueLabel", flags).GetValue(form);
            System.Windows.Forms.Control keyTwo = (System.Windows.Forms.Control)type.GetField("shortcutTwoValueLabel", flags).GetValue(form);
            System.Windows.Forms.Control editOne = (System.Windows.Forms.Control)type.GetField("shortcutButton", flags).GetValue(form);
            System.Windows.Forms.Control editTwo = (System.Windows.Forms.Control)type.GetField("shortcutTwoButton", flags).GetValue(form);
            System.Windows.Forms.Control credit = (System.Windows.Forms.Control)type.GetField("creditLabel", flags).GetValue(form);
            System.Windows.Forms.Control refresh = (System.Windows.Forms.Control)type.GetField("refreshButton", flags).GetValue(form);
            System.Windows.Forms.Control hide = (System.Windows.Forms.Control)type.GetField("hideButton", flags).GetValue(form);
            System.Reflection.FieldInfo showHotkeyStrip = type.GetField("showHotkeyStrip", flags);

            try
            {
                System.Drawing.Size[] sizes =
                {
                    new System.Drawing.Size(184, 211),
                    new System.Drawing.Size(204, 261),
                    new System.Drawing.Size(284, 341),
                    new System.Drawing.Size(544, 501)
                };

                foreach (System.Drawing.Size size in sizes)
                {
                    form.ClientSize = size;
                    applyLayout.Invoke(form, null);
                    Assert(keyOne.Top == keyTwo.Top && keyOne.Height == keyTwo.Height, "Hotkey cards were not aligned at " + size + ".");
                    Assert(keyOne.Right < keyTwo.Left, "Hotkey cards overlapped at " + size + ".");
                    Assert(keyOne.Left >= 0 && keyTwo.Right <= form.ClientSize.Width, "Hotkey cards escaped the window at " + size + ".");
                    Assert(keyOne.Height >= 24 && keyTwo.Height >= 24, "Hotkey cards became too small at " + size + ".");
                    Assert(keyOne.Region == null && keyTwo.Region == null, "Removed corner regions returned at " + size + ".");
                    Assert(keyOne.Top > credit.Bottom, "Hotkey strip was not placed below the creator line at " + size + ".");
                    Assert(keyOne.Top == refresh.Top && keyOne.Bottom == refresh.Bottom, "Hotkey strip was not aligned with the bottom toolbar at " + size + ".");
                    Assert(keyOne.Left > refresh.Right, "Key 1 overlapped the left toolbar icon at " + size + ".");
                    Assert(keyTwo.Right < hide.Left, "Key 2 overlapped the right toolbar icon at " + size + ".");
                }

                form.ClientSize = new System.Drawing.Size(284, 341);
                applyLayout.Invoke(form, null);
                showHotkeyStrip.SetValue(form, false);
                applyLayout.Invoke(form, null);
                Assert(!keyOne.Visible && !keyTwo.Visible, "Developer option did not hide both hotkey cards.");

                Assert(!form.Controls.Contains(editOne) && !form.Controls.Contains(editTwo), "Legacy pen buttons are still visible.");
                Assert(!(bool)type.GetField("legacySettingsEnabled", flags).GetValue(form), "Legacy Settings was enabled by default.");
                Assert(!(bool)type.GetField("hideFromTaskbar", flags).GetValue(form), "Hide-from-taskbar was enabled by default.");
            }
            finally
            {
                System.Windows.Forms.Timer timer = (System.Windows.Forms.Timer)type.GetField("refreshTimer", flags).GetValue(form);
                timer.Stop();
                System.Windows.Forms.NotifyIcon tray = (System.Windows.Forms.NotifyIcon)type.GetField("tray", flags).GetValue(form);
                tray.Visible = false;
                tray.Dispose();
                form.Dispose();
            }
        }

        private static void TestInstanceIsolation()
        {
            string appData = @"C:\Users\Test\AppData\Roaming";
            string betaPath = @"D:\PulseMute\Beta\PulseMute Beta.exe";
            string stablePath = @"D:\PulseMute\Stable\PulseMute Main.exe";
            string archivedPath = @"D:\PulseMute\Beta\Archive\26.1.0.12\PulseMute Beta.exe";

            string betaConfig = MainForm.ConfigPathForExecutable(betaPath, appData);
            string stableConfig = MainForm.ConfigPathForExecutable(stablePath, appData);
            string archivedConfig = MainForm.ConfigPathForExecutable(archivedPath, appData);
            Assert(!string.Equals(betaConfig, stableConfig, StringComparison.OrdinalIgnoreCase), "Beta and Stable share one settings file.");
            Assert(!string.Equals(betaConfig, archivedConfig, StringComparison.OrdinalIgnoreCase), "Current and archived Beta share one settings file.");
            Assert(betaConfig.IndexOf(System.IO.Path.Combine("PulseMute", "Instances"), StringComparison.OrdinalIgnoreCase) >= 0, "Instance settings are outside the isolated directory.");
            Assert(betaConfig.EndsWith("settings.txt", StringComparison.OrdinalIgnoreCase), "Instance settings filename is invalid.");
            Assert(MainForm.InstanceIdentityForExecutable(betaPath) == MainForm.InstanceIdentityForExecutable(betaPath.ToLowerInvariant()), "Instance identity is not path-case stable.");
            Assert(MainForm.AutoStartValueNameForExecutable(betaPath) != MainForm.AutoStartValueNameForExecutable(stablePath), "Beta and Stable share one auto-start value.");
            Assert(MainForm.AutoStartValueNameForExecutable(betaPath) != MainForm.AutoStartValueNameForExecutable(archivedPath), "Current and archived Beta share one auto-start value.");
        }

        private static void TestConnectedController()
        {
            HidDevice controller = null;
            foreach (HidDevice device in DeviceList.Local.GetHidDevices(0x054C))
            {
                if (device.ProductID == 0x0CE6 || device.ProductID == 0x0DF2)
                {
                    controller = device;
                    break;
                }
            }

            if (controller == null)
            {
                Console.WriteLine("SKIP: no physical DualSense connected");
                return;
            }

            OpenConfiguration configuration = new OpenConfiguration();
            configuration.SetOption(OpenOption.Exclusive, false);
            HidStream stream;
            Assert(controller.TryOpen(configuration, out stream), "Connected DualSense could not be opened in shared mode.");
            using (stream)
            {
                stream.ReadTimeout = 1200;
                byte[] report = new byte[Math.Max(78, controller.GetMaxInputReportLength())];
                int validReports = 0;
                int attempts = 0;
                while (validReports < 5 && attempts < 10)
                {
                    attempts++;
                    try
                    {
                        int count = stream.Read(report, 0, report.Length);
                        DualSenseButton buttons;
                        if (DualSenseProtocol.TryParseReport(report, count, out buttons))
                            validReports++;
                    }
                    catch (TimeoutException)
                    {
                    }
                }
                Assert(validReports == 5, "Did not receive five valid reports from the connected DualSense.");
                Console.WriteLine("HARDWARE PASS: " + controller.GetProductName() + " supplied " + validReports + " valid reports");
            }
        }

        private static void TestArchivedVersionDiscovery()
        {
            string appDirectory = Environment.GetEnvironmentVariable("PULSEMUTE_TEST_APP_DIR");
            Assert(!string.IsNullOrEmpty(appDirectory), "Version archive test directory was not provided.");

            System.Collections.Generic.List<ArchivedVersionInfo> versions = MainForm.DiscoverArchivedVersions(appDirectory);
            Assert(versions.Count >= 23, "Expected the Beta snapshots and legacy releases in the version picker.");

            bool foundBetaSnapshot = false;
            bool foundCurrentBackup = false;
            bool foundSoundBackup = false;
            bool foundSidebarBackup = false;
            bool foundPolishBackup = false;
            bool foundPreRedesignBackup = false;
            bool foundSilenceDesignBackup = false;
            bool foundUnifiedPaletteBackup = false;
            bool foundColorEditorBackup = false;
            bool foundDropdownFixBackup = false;
            bool foundHotkeyLayoutBackup = false;
            System.Collections.Generic.HashSet<string> uniquePaths = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (ArchivedVersionInfo version in versions)
            {
                Assert(System.IO.File.Exists(version.ExecutablePath), "Archived executable is missing: " + version.DisplayName);
                Assert(uniquePaths.Add(version.ExecutablePath), "Archived executable was listed twice: " + version.DisplayName);
                Assert(version.ExecutablePath.IndexOf("RecoverAllMics", StringComparison.OrdinalIgnoreCase) < 0, "Recovery helper appeared in the version picker.");
                Assert(version.ExecutablePath.IndexOf("UnmuteMic", StringComparison.OrdinalIgnoreCase) < 0, "Microphone helper appeared in the version picker.");
                Assert(!string.IsNullOrEmpty(version.Details), "Version details are missing: " + version.DisplayName);
                Assert(version.DisplayName.IndexOf("Major Update", StringComparison.OrdinalIgnoreCase) < 0, "Major Update text remained in the picker.");
                if (version.DisplayName == "Beta 26.1.0.3-beta")
                    foundBetaSnapshot = true;
                if (version.DisplayName == "Beta 26.1.0.4-beta")
                    foundCurrentBackup = version.Details.IndexOf("Developer Mode", StringComparison.OrdinalIgnoreCase) >= 0;
                if (version.DisplayName == "Beta 26.1.0.5-beta")
                    foundSoundBackup = version.Details.IndexOf("sound", StringComparison.OrdinalIgnoreCase) >= 0;
                if (version.DisplayName == "Beta 26.1.0.6-beta")
                    foundSidebarBackup = version.Details.IndexOf("sidebar", StringComparison.OrdinalIgnoreCase) >= 0;
                if (version.DisplayName == "Beta 26.1.0.7-beta")
                    foundPolishBackup = version.Details.IndexOf("sound", StringComparison.OrdinalIgnoreCase) >= 0;
                if (version.DisplayName == "Beta 26.1.0.8-beta")
                    foundPreRedesignBackup = version.Details.IndexOf("logo", StringComparison.OrdinalIgnoreCase) >= 0;
                if (version.DisplayName == "Beta 26.1.0.9-beta")
                    foundSilenceDesignBackup = version.Details.IndexOf("Silence", StringComparison.OrdinalIgnoreCase) >= 0;
                if (version.DisplayName == "Beta 26.1.0.10-beta")
                    foundUnifiedPaletteBackup = version.Details.IndexOf("palette", StringComparison.OrdinalIgnoreCase) >= 0;
                if (version.DisplayName == "Beta 26.1.0.11-beta")
                    foundColorEditorBackup = version.Details.IndexOf("color", StringComparison.OrdinalIgnoreCase) >= 0;
                if (version.DisplayName == "Beta 26.1.0.12-beta")
                    foundDropdownFixBackup = version.Details.IndexOf("dropdown", StringComparison.OrdinalIgnoreCase) >= 0;
                if (version.DisplayName == "Beta 26.1.0.13-beta")
                    foundHotkeyLayoutBackup = version.Details.IndexOf("hotkey", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            Assert(foundBetaSnapshot, "Beta 26.1.0.3 snapshot was not discovered.");
            Assert(foundCurrentBackup, "Beta 26.1.0.4 backup or its version details were not discovered.");
            Assert(foundSoundBackup, "Beta 26.1.0.5 backup or its version details were not discovered.");
            Assert(foundSidebarBackup, "Beta 26.1.0.6 backup or its version details were not discovered.");
            Assert(foundPolishBackup, "Beta 26.1.0.7 backup or its version details were not discovered.");
            Assert(foundPreRedesignBackup, "Beta 26.1.0.8 pre-redesign backup or its version details were not discovered.");
            Assert(foundSilenceDesignBackup, "Beta 26.1.0.9 design backup or its version details were not discovered.");
            Assert(foundUnifiedPaletteBackup, "Beta 26.1.0.10 palette backup or its version details were not discovered.");
            Assert(foundColorEditorBackup, "Beta 26.1.0.11 color-editor backup or its version details were not discovered.");
            Assert(foundDropdownFixBackup, "Beta 26.1.0.12 dropdown-fix backup or its version details were not discovered.");
            Assert(foundHotkeyLayoutBackup, "Beta 26.1.0.13 hotkey-layout backup or its version details were not discovered.");
            Assert(versions.Exists(delegate(ArchivedVersionInfo version)
            {
                return version.DisplayName == "PulseMute 1.5" && version.Details.IndexOf("redesign", StringComparison.OrdinalIgnoreCase) >= 0;
            }), "Interface redesign version info is missing.");
            Assert(versions.Exists(delegate(ArchivedVersionInfo version)
            {
                return version.DisplayName == "PulseMute 1.6 Stable" && version.Details.IndexOf("custom", StringComparison.OrdinalIgnoreCase) >= 0;
            }), "Color customization version info is missing.");
            Assert(versions.Exists(delegate(ArchivedVersionInfo version)
            {
                return version.DisplayName == "PulseMute PS 1.0" && version.Details.IndexOf("DualSense", StringComparison.OrdinalIgnoreCase) >= 0;
            }), "PlayStation version info is missing.");
        }

        private static void TestColorCodeEditor()
        {
            System.Drawing.Color color;
            Assert(MainForm.TryParseHexColor("#C13545", out color), "Full HEX color was rejected.");
            Assert(color.R == 193 && color.G == 53 && color.B == 69, "Full HEX color parsed incorrectly.");
            Assert(MainForm.TryParseHexColor("0x228B6F", out color), "0x color was rejected.");
            Assert(color.R == 34 && color.G == 139 && color.B == 111, "0x color parsed incorrectly.");
            Assert(MainForm.TryParseHexColor("#ABC", out color), "Short HEX color was rejected.");
            Assert(color.R == 170 && color.G == 187 && color.B == 204, "Short HEX color parsed incorrectly.");
            Assert(!MainForm.TryParseHexColor("#12XX56", out color), "Invalid HEX color was accepted.");
            Assert(!MainForm.TryParseHexColor("12345", out color), "Wrong-length HEX color was accepted.");

            int channel;
            Assert(MainForm.TryParseColorChannel("0", out channel) && channel == 0, "RGB zero was rejected.");
            Assert(MainForm.TryParseColorChannel("255", out channel) && channel == 255, "RGB 255 was rejected.");
            Assert(!MainForm.TryParseColorChannel("-1", out channel), "Negative RGB value was accepted.");
            Assert(!MainForm.TryParseColorChannel("256", out channel), "Oversized RGB value was accepted.");
        }

        private static void TestSettingsDropDownLifetime()
        {
            SilenceComboBox comboBox = new SilenceComboBox();
            comboBox.Items.Add("First");
            comboBox.Items.Add("Second");

            System.Reflection.MethodInfo createMenu = typeof(SilenceComboBox).GetMethod(
                "CreateDropDownMenu",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert(createMenu != null, "Settings dropdown menu factory is missing.");
            System.Windows.Forms.ContextMenuStrip menu = (System.Windows.Forms.ContextMenuStrip)createMenu.Invoke(comboBox, null);
            Assert(menu != null && !menu.IsDisposed, "Settings dropdown was disposed during creation.");
            Assert(menu.Items.Count == 2, "Settings dropdown did not create every item.");

            ((System.Windows.Forms.ToolStripMenuItem)menu.Items[1]).PerformClick();
            Assert(comboBox.SelectedIndex == 1, "Settings dropdown selection did not update.");
            Assert(!menu.IsDisposed, "Settings dropdown disposed itself during item selection.");
            menu.Close();
            Assert(!menu.IsDisposed, "Settings dropdown disposed itself during close notification.");

            comboBox.Dispose();
            Assert(menu.IsDisposed, "Settings dropdown was not disposed with its owning control.");
        }

        private static void TestNativeSettingsDesign()
        {
            string appDirectory = Environment.GetEnvironmentVariable("PULSEMUTE_TEST_APP_DIR");
            Assert(!string.IsNullOrEmpty(appDirectory), "Settings design test directory was not provided.");

            string sourcePath = System.IO.Path.Combine(appDirectory, "Main source", "SidebarSettings.cs");
            string source = System.IO.File.ReadAllText(sourcePath);
            string programSource = System.IO.File.ReadAllText(System.IO.Path.Combine(appDirectory, "Main source", "Program.cs"));
            Assert(source.IndexOf("FormBorderStyle.None", StringComparison.Ordinal) >= 0, "Native Settings shell is not borderless.");
            Assert(source.IndexOf("SilenceNavigationButton", StringComparison.Ordinal) >= 0, "Silence-style navigation control is missing.");
            Assert(source.IndexOf("SilenceCardPanel", StringComparison.Ordinal) >= 0, "Silence-style settings cards are missing.");
            Assert(source.IndexOf("SilenceComboBox", StringComparison.Ordinal) >= 0, "Custom settings dropdown is missing.");
            Assert(source.IndexOf("SilenceSlider", StringComparison.Ordinal) >= 0, "Custom settings slider is missing.");
            Assert(source.IndexOf("SettingsBodyFont", StringComparison.Ordinal) >= 0, "Settings typography helper is missing.");
            Assert(source.IndexOf("26.1.0.14-beta", StringComparison.Ordinal) >= 0, "Settings version label is stale.");
            Assert(source.IndexOf("Color sidebarColor = SettingsSidebarColor();", StringComparison.Ordinal) >= 0, "Settings sidebar does not use the shared palette.");
            Assert(source.IndexOf("Color border = SettingsBorderColor();", StringComparison.Ordinal) >= 0, "Settings border does not use the shared palette.");
            Assert(programSource.IndexOf("CustomSettingsSidebar=", StringComparison.Ordinal) >= 0, "Settings sidebar color is not persisted.");
            Assert(programSource.IndexOf("CustomSettingsBorder=", StringComparison.Ordinal) >= 0, "Settings border color is not persisted.");
            Assert(programSource.IndexOf("\"Settings sidebar\"", StringComparison.Ordinal) >= 0, "Settings sidebar customization is missing.");
            Assert(programSource.IndexOf("\"Settings border\"", StringComparison.Ordinal) >= 0, "Settings border customization is missing.");
            Assert(programSource.IndexOf("customSettingsSidebarColor = customSurfaceColor", StringComparison.Ordinal) >= 0, "Older custom palettes do not migrate the Settings sidebar.");
            Assert(programSource.IndexOf("MixColor(customSurfaceColor, customPrimaryTextColor, 18)", StringComparison.Ordinal) >= 0, "Older custom palettes do not derive a matching Settings border.");
            Assert(programSource.IndexOf("Clipboard.SetText", StringComparison.Ordinal) >= 0, "Color-code copy support is missing.");
            Assert(programSource.IndexOf("Clipboard.GetText", StringComparison.Ordinal) >= 0, "Color-code paste support is missing.");
            Assert(programSource.IndexOf("Advanced picker", StringComparison.Ordinal) >= 0, "Advanced color picker fallback is missing.");
            Assert(source.IndexOf("Show hotkey strip", StringComparison.Ordinal) >= 0, "Developer hotkey-strip option is missing.");
            Assert(programSource.IndexOf("ShowHotkeyStrip=", StringComparison.Ordinal) >= 0, "Hotkey-strip preference is not persisted.");
            Assert(programSource.IndexOf("InstanceIdentityForExecutable", StringComparison.Ordinal) >= 0, "Per-executable instance isolation is missing.");
        }

        private static void TestNotificationSounds()
        {
            Assert(NotificationSoundEngine.Presets.Length == 9, "Expected nine notification sound styles.");
            Assert(!NotificationSoundEngine.IsValidPreset("Not a sound"), "Invalid sound style was accepted.");

            foreach (string preset in NotificationSoundEngine.Presets)
            {
                Assert(NotificationSoundEngine.IsValidPreset(preset), "Sound style was not recognized: " + preset);
                byte[] muted = NotificationSoundEngine.CreateWaveData(preset, true);
                byte[] live = NotificationSoundEngine.CreateWaveData(preset, false);
                byte[] silent = NotificationSoundEngine.CreateWaveData(preset, true, 0);
                byte[] quiet = NotificationSoundEngine.CreateWaveData(preset, true, 20);
                byte[] loud = NotificationSoundEngine.CreateWaveData(preset, true, 100);
                Assert(IsWaveFile(muted), "Muted WAV is invalid: " + preset);
                Assert(IsWaveFile(live), "Live WAV is invalid: " + preset);
                Assert(!ByteArraysEqual(muted, live), "Mute and live sounds are identical: " + preset);
                Assert(PeakPcm(silent) == 0, "Zero-volume sound was not silent: " + preset);
                Assert(PeakPcm(loud) > PeakPcm(quiet), "Volume scaling failed: " + preset);

                using (System.IO.MemoryStream stream = new System.IO.MemoryStream(muted, false))
                using (System.Media.SoundPlayer player = new System.Media.SoundPlayer(stream))
                    player.Load();
                assertions++;
            }
        }

        private static void TestProfessionalLogoResource()
        {
            System.Drawing.Icon detachedIcon;
            using (System.IO.Stream stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("PulseMuteProfessional.ico"))
            {
                Assert(stream != null && stream.Length > 1000, "Professional logo resource is missing.");
                using (System.Drawing.Icon icon = new System.Drawing.Icon(stream))
                {
                    Assert(icon.Width == 256 && icon.Height == 256, "Professional logo dimensions are invalid.");
                    detachedIcon = (System.Drawing.Icon)icon.Clone();
                }
            }

            using (detachedIcon)
            {
                for (int i = 0; i < 25; i++)
                {
                    using (System.Drawing.Bitmap bitmap = MainForm.CreateSafeIconBitmap(detachedIcon))
                    {
                        Assert(bitmap.Width > 0 && bitmap.Height > 0, "Professional logo failed after its resource stream closed.");
                    }
                }
            }
        }

        private static int PeakPcm(byte[] data)
        {
            int peak = 0;
            for (int i = 44; i + 1 < data.Length; i += 2)
            {
                short sample = (short)(data[i] | (data[i + 1] << 8));
                int magnitude = sample == short.MinValue ? short.MaxValue : Math.Abs((int)sample);
                peak = Math.Max(peak, magnitude);
            }
            return peak;
        }

        private static bool IsWaveFile(byte[] data)
        {
            return data != null && data.Length > 44 &&
                data[0] == 82 && data[1] == 73 && data[2] == 70 && data[3] == 70 &&
                data[8] == 87 && data[9] == 65 && data[10] == 86 && data[11] == 69;
        }

        private static bool ByteArraysEqual(byte[] left, byte[] right)
        {
            if (left.Length != right.Length)
                return false;
            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i])
                    return false;
            }
            return true;
        }

        private static void TestSingleButton(byte reportId, int length, int buttonOffset, DualSenseButton button, string transport)
        {
            byte[] report = CreateNeutralReport(reportId, length, buttonOffset);
            SetButton(report, buttonOffset, button);
            DualSenseButton parsed;
            Assert(DualSenseProtocol.TryParseReport(report, report.Length, out parsed), transport + " report rejected for " + button);
            Assert(parsed == button, transport + " parsed " + parsed + " instead of " + button);
        }

        private static byte[] CreateNeutralReport(byte reportId, int length, int buttonOffset)
        {
            byte[] report = new byte[length];
            report[0] = reportId;
            report[buttonOffset] = 8;
            return report;
        }

        private static void SetButton(byte[] report, int offset, DualSenseButton button)
        {
            switch (button)
            {
                case DualSenseButton.DPadUp: report[offset] = 0; break;
                case DualSenseButton.DPadRight: report[offset] = 2; break;
                case DualSenseButton.DPadDown: report[offset] = 4; break;
                case DualSenseButton.DPadLeft: report[offset] = 6; break;
                case DualSenseButton.Square: report[offset] |= 0x10; break;
                case DualSenseButton.Cross: report[offset] |= 0x20; break;
                case DualSenseButton.Circle: report[offset] |= 0x40; break;
                case DualSenseButton.Triangle: report[offset] |= 0x80; break;
                case DualSenseButton.L1: report[offset + 1] |= 0x01; break;
                case DualSenseButton.R1: report[offset + 1] |= 0x02; break;
                case DualSenseButton.L2: report[offset + 1] |= 0x04; break;
                case DualSenseButton.R2: report[offset + 1] |= 0x08; break;
                case DualSenseButton.Create: report[offset + 1] |= 0x10; break;
                case DualSenseButton.Options: report[offset + 1] |= 0x20; break;
                case DualSenseButton.L3: report[offset + 1] |= 0x40; break;
                case DualSenseButton.R3: report[offset + 1] |= 0x80; break;
                case DualSenseButton.PS: report[offset + 2] |= 0x01; break;
                case DualSenseButton.Touchpad: report[offset + 2] |= 0x02; break;
                case DualSenseButton.MicrophoneMute: report[offset + 2] |= 0x04; break;
                case DualSenseButton.EdgeFnLeft: report[offset + 2] |= 0x10; break;
                case DualSenseButton.EdgeFnRight: report[offset + 2] |= 0x20; break;
                case DualSenseButton.EdgeLeftPaddle: report[offset + 2] |= 0x40; break;
                case DualSenseButton.EdgeRightPaddle: report[offset + 2] |= 0x80; break;
                default: throw new InvalidOperationException("Unhandled button: " + button);
            }
        }

        private static void Assert(bool condition, string message)
        {
            assertions++;
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
