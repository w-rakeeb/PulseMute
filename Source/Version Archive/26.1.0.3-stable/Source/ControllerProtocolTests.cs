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

            try
            {
                System.Drawing.Size[] sizes =
                {
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
                    Assert(keyOne.Height >= 28 && keyTwo.Height >= 28, "Hotkey cards became too small at " + size + ".");
                }

                Assert(!form.Controls.Contains(editOne) && !form.Controls.Contains(editTwo), "Legacy pen buttons are still visible.");
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
