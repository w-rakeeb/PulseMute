using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace PulseMute
{
    internal sealed partial class MainForm
    {
        private void ShowSettingsV15()
        {
            using (Form dialog = new BufferedSettingsForm())
            {
                dialog.Text = "PulseMute Main settings";
                dialog.Icon = appIcon;
                dialog.ClientSize = new Size(760, 500);
                dialog.MinimumSize = new Size(660, 440);
                dialog.FormBorderStyle = FormBorderStyle.Sizable;
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;
                dialog.ShowInTaskbar = false;
                dialog.TopMost = TopMost;
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.Font = new Font("Segoe UI", 10F);
                dialog.AutoScaleDimensions = new SizeF(96F, 96F);
                dialog.AutoScaleMode = AutoScaleMode.Dpi;
                dialog.HandleCreated += delegate { ApplyCaptionTheme(dialog.Handle, darkMode); };

                Panel sidebar = new BufferedPanel();
                sidebar.SetBounds(0, 0, 176, 500);
                sidebar.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;

                Label brandLabel = new Label();
                brandLabel.Text = "PulseMute";
                brandLabel.Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold);
                brandLabel.SetBounds(20, 18, 140, 30);

                Panel contentHost = new BufferedPanel();
                contentHost.SetBounds(176, 0, 584, 450);
                contentHost.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

                Panel footer = new BufferedPanel();
                footer.SetBounds(176, 450, 584, 50);
                footer.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

                Label versionLabel = new Label();
                versionLabel.Text = EditionVersionText() + "  |  Created by Wrakeeb";
                versionLabel.Font = new Font("Segoe UI Semibold", 7.5F, FontStyle.Bold);
                versionLabel.TextAlign = ContentAlignment.MiddleLeft;
                versionLabel.SetBounds(22, 15, 370, 20);
                versionLabel.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;

                Button doneButton = CreateSmallButton("Done", new Point(486, 10));
                doneButton.Size = new Size(78, 30);
                doneButton.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
                doneButton.Click += delegate { dialog.Close(); };
                dialog.AcceptButton = doneButton;
                dialog.CancelButton = doneButton;

                Panel footerSeparator = CreateSeparator(0, 0, 584);
                footerSeparator.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

                footer.Controls.Add(versionLabel);
                footer.Controls.Add(doneButton);
                footer.Controls.Add(footerSeparator);

                Dictionary<string, Panel> pages = new Dictionary<string, Panel>(StringComparer.OrdinalIgnoreCase);
                List<Label> primaryLabels = new List<Label>();
                List<Label> stateLabels = new List<Label>();
                List<ToggleSwitch> toggles = new List<ToggleSwitch>();
                List<ThemedComboBox> comboBoxes = new List<ThemedComboBox>();
                List<Button> actionButtons = new List<Button>();
                List<Panel> separators = new List<Panel>();
                List<Button> navigationButtons = new List<Button>();

                Panel generalPage = CreateSidebarSettingsPage("General", primaryLabels);
                pages.Add("General", generalPage);

                Label autoLabel = AddPageLabel(generalPage, primaryLabels, "Start with Windows", 28, 88);
                Label autoState = AddPageState(generalPage, stateLabels, 28, 110);
                ToggleSwitch autoToggle = AddPageToggle(generalPage, toggles, 510, 94, IsAutoStartEnabled());
                Panel generalSeparatorOne = AddPageSeparator(generalPage, separators, 142);

                Label taskbarLabel = AddPageLabel(generalPage, primaryLabels, "Hide window from taskbar", 28, 158);
                Label taskbarState = AddPageState(generalPage, stateLabels, 28, 180);
                ToggleSwitch taskbarToggle = AddPageToggle(generalPage, toggles, 510, 164, hideFromTaskbar);
                Panel generalSeparatorTwo = AddPageSeparator(generalPage, separators, 212);

                Label rememberLabel = AddPageLabel(generalPage, primaryLabels, "Remember window placement", 28, 228);
                Label rememberState = AddPageState(generalPage, stateLabels, 28, 250);
                ToggleSwitch rememberToggle = AddPageToggle(generalPage, toggles, 510, 234, rememberWindowPlacement);
                Panel generalSeparatorThree = AddPageSeparator(generalPage, separators, 282);

                Label legacySettingsLabel = AddPageLabel(generalPage, primaryLabels, "Legacy settings interface", 28, 298);
                Label legacySettingsState = AddPageState(generalPage, stateLabels, 28, 320);
                ToggleSwitch legacySettingsToggle = AddPageToggle(generalPage, toggles, 510, 304, legacySettingsEnabled);

                Panel hotkeysPage = CreateSidebarSettingsPage("Hotkeys", primaryLabels);
                pages.Add("Hotkeys", hotkeysPage);

                Label dualHotkeyLabel = AddPageLabel(hotkeysPage, primaryLabels, "Dual Hotkey", 28, 88);
                Label dualHotkeyState = AddPageState(hotkeysPage, stateLabels, 28, 110);
                ToggleSwitch dualHotkeyToggle = AddPageToggle(hotkeysPage, toggles, 510, 94, dualHotkeyEnabled);
                Panel hotkeySeparatorOne = AddPageSeparator(hotkeysPage, separators, 142);

                Label keyOneLabel = AddPageLabel(hotkeysPage, primaryLabels, "Key 1", 28, 166);
                Label keyOneState = AddPageState(hotkeysPage, stateLabels, 28, 188);
                keyOneState.Text = "Primary assignment";
                Button keyOneButton = CreateSmallButton(CurrentShortcutText(1), new Point(330, 164));
                keyOneButton.Size = new Size(208, 34);
                keyOneButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                hotkeysPage.Controls.Add(keyOneButton);
                actionButtons.Add(keyOneButton);
                Panel hotkeySeparatorTwo = AddPageSeparator(hotkeysPage, separators, 218);

                Label keyTwoLabel = AddPageLabel(hotkeysPage, primaryLabels, "Key 2", 28, 242);
                Label keyTwoState = AddPageState(hotkeysPage, stateLabels, 28, 264);
                keyTwoState.Text = "Secondary assignment";
                Button keyTwoButton = CreateSmallButton(CurrentShortcutText(2), new Point(330, 240));
                keyTwoButton.Size = new Size(208, 34);
                keyTwoButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                hotkeysPage.Controls.Add(keyTwoButton);
                actionButtons.Add(keyTwoButton);

                Panel audioPage = CreateSidebarSettingsPage("Audio", primaryLabels);
                pages.Add("Audio", audioPage);

                Label microphoneLabel = AddPageLabel(audioPage, primaryLabels, "Microphone", 28, 88);
                ThemedComboBox microphoneBox = CreatePageComboBox(28, 116, 510);
                audioPage.Controls.Add(microphoneBox);
                comboBoxes.Add(microphoneBox);

                List<MicDeviceInfo> devices = mic.GetCaptureDevices();
                devices.Insert(0, new MicDeviceInfo(null, "Windows default microphone"));
                int selectedMicrophoneIndex = 0;
                for (int i = 0; i < devices.Count; i++)
                {
                    microphoneBox.Items.Add(devices[i]);
                    if (!string.IsNullOrEmpty(mic.SelectedDeviceId) &&
                        string.Equals(devices[i].Id, mic.SelectedDeviceId, StringComparison.OrdinalIgnoreCase))
                        selectedMicrophoneIndex = i;
                }
                microphoneBox.SelectedIndex = selectedMicrophoneIndex;
                Panel audioSeparatorOne = AddPageSeparator(audioPage, separators, 164);

                Label soundFeedbackLabel = AddPageLabel(audioPage, primaryLabels, "Mute sound feedback", 28, 184);
                Label soundFeedbackState = AddPageState(audioPage, stateLabels, 28, 206);
                ToggleSwitch soundFeedbackToggle = AddPageToggle(audioPage, toggles, 510, 190, soundFeedbackEnabled);

                Label soundPresetLabel = AddPageLabel(audioPage, primaryLabels, "Sound style", 28, 250);
                ThemedComboBox soundPresetBox = CreatePageComboBox(28, 278, 510);
                audioPage.Controls.Add(soundPresetBox);
                comboBoxes.Add(soundPresetBox);
                int soundPresetIndex = 0;
                string[] soundPresets = NotificationSoundEngine.Presets;
                for (int i = 0; i < soundPresets.Length; i++)
                {
                    soundPresetBox.Items.Add(new SoundPresetInfo(soundPresets[i]));
                    if (string.Equals(soundPresets[i], soundPreset, StringComparison.Ordinal))
                        soundPresetIndex = i;
                }
                soundPresetBox.SelectedIndex = soundPresetIndex;

                Label soundVolumeLabel = AddPageLabel(audioPage, primaryLabels, "Feedback volume", 28, 330);
                Label soundVolumeState = AddPageState(audioPage, stateLabels, 28, 352);
                TrackBar soundVolumeSlider = new TrackBar();
                soundVolumeSlider.Minimum = 0;
                soundVolumeSlider.Maximum = 100;
                soundVolumeSlider.Value = Math.Max(0, Math.Min(100, soundVolume));
                soundVolumeSlider.TickStyle = TickStyle.None;
                soundVolumeSlider.SetBounds(236, 326, 302, 40);
                soundVolumeSlider.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                audioPage.Controls.Add(soundVolumeSlider);

                Button soundSettingsButton = CreateSmallButton("Windows sound settings", new Point(28, 388));
                soundSettingsButton.Size = new Size(190, 32);
                audioPage.Controls.Add(soundSettingsButton);
                actionButtons.Add(soundSettingsButton);

                Panel controllerPage = CreateSidebarSettingsPage("Controller", primaryLabels);
                pages.Add("Controller", controllerPage);

                Label controllerLabel = AddPageLabel(controllerPage, primaryLabels, "PlayStation controller", 28, 88);
                Label controllerState = AddPageState(controllerPage, stateLabels, 28, 110);
                controllerState.SetBounds(28, 110, 320, 18);
                Button controllerScanButton = CreateSmallButton("Rescan", new Point(412, 88));
                controllerScanButton.Size = new Size(126, 32);
                controllerScanButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                controllerPage.Controls.Add(controllerScanButton);
                actionButtons.Add(controllerScanButton);
                Panel controllerSeparator = AddPageSeparator(controllerPage, separators, 142);

                Label controllerSupportLabel = AddPageLabel(controllerPage, primaryLabels, "Supported input", 28, 166);
                Label controllerSupportState = AddPageState(controllerPage, stateLabels, 28, 188);
                controllerSupportState.SetBounds(28, 188, 430, 36);
                controllerSupportState.Text = "DualSense and DualSense Edge | USB and Bluetooth";

                Panel customizationPage = CreateSidebarSettingsPage("Customization", primaryLabels);
                pages.Add("Customization", customizationPage);

                Label appearanceLabel = AddPageLabel(customizationPage, primaryLabels, "Appearance", 28, 88);
                Label appearanceState = AddPageState(customizationPage, stateLabels, 28, 110);
                ToggleSwitch appearanceToggle = AddPageToggle(customizationPage, toggles, 510, 94, darkMode);
                Panel customizationSeparator = AddPageSeparator(customizationPage, separators, 142);

                Label customizationLabel = AddPageLabel(customizationPage, primaryLabels, "Interface colors", 28, 166);
                Label customizationState = AddPageState(customizationPage, stateLabels, 28, 188);
                Button customizeButton = CreateSmallButton("Customize", new Point(398, 164));
                customizeButton.Size = new Size(140, 34);
                customizeButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                customizationPage.Controls.Add(customizeButton);
                actionButtons.Add(customizeButton);

                Panel developerPage = CreateSidebarSettingsPage("Developer", primaryLabels);
                pages.Add("Developer", developerPage);

                Label developerModeLabel = AddPageLabel(developerPage, primaryLabels, "Developer mode", 28, 88);
                Label developerModeState = AddPageState(developerPage, stateLabels, 28, 110);
                ToggleSwitch developerModeToggle = AddPageToggle(developerPage, toggles, 510, 94, developerModeEnabled);
                Panel developerSeparator = AddPageSeparator(developerPage, separators, 142);

                Panel developerContent = new BufferedPanel();
                developerContent.SetBounds(28, 166, 510, 286);
                developerContent.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

                Label animationsLabel = AddPageLabel(developerContent, primaryLabels, "Interface animations", 0, 0);
                Label animationsState = AddPageState(developerContent, stateLabels, 0, 22);
                ToggleSwitch animationsToggle = AddPageToggle(developerContent, toggles, 464, 6, animationsEnabled);

                Panel developerOptionsSeparator = CreateSeparator(0, 64, 510);
                developerOptionsSeparator.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                developerContent.Controls.Add(developerOptionsSeparator);
                separators.Add(developerOptionsSeparator);

                Label olderVersionLabel = AddPageLabel(developerContent, primaryLabels, "Older version", 0, 86);
                ThemedComboBox olderVersionBox = CreatePageComboBox(0, 114, 382);
                developerContent.Controls.Add(olderVersionBox);
                comboBoxes.Add(olderVersionBox);
                List<ArchivedVersionInfo> archivedVersions = DiscoverArchivedVersions();
                foreach (ArchivedVersionInfo version in archivedVersions)
                    olderVersionBox.Items.Add(version);
                if (olderVersionBox.Items.Count > 0)
                    olderVersionBox.SelectedIndex = 0;

                Button openVersionButton = CreateSmallButton("Open", new Point(392, 114));
                openVersionButton.Size = new Size(118, 30);
                openVersionButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                developerContent.Controls.Add(openVersionButton);
                actionButtons.Add(openVersionButton);

                Label versionsInfoLabel = AddPageLabel(developerContent, primaryLabels, "Versions info", 0, 166);
                Label versionInfoText = new Label();
                versionInfoText.AutoSize = false;
                versionInfoText.SetBounds(0, 194, 510, 74);
                versionInfoText.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                versionInfoText.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
                versionInfoText.TextAlign = ContentAlignment.MiddleLeft;
                versionInfoText.Padding = new Padding(10, 6, 10, 6);
                versionInfoText.BorderStyle = BorderStyle.FixedSingle;
                developerContent.Controls.Add(versionInfoText);
                developerPage.Controls.Add(developerContent);
                developerPage.AutoScrollMinSize = new Size(0, 470);

                Panel aboutPage = CreateSidebarSettingsPage("About", primaryLabels);
                pages.Add("About", aboutPage);

                PictureBox appLogo = new PictureBox();
                appLogo.Image = CreateSafeIconBitmap(appIcon);
                appLogo.SizeMode = PictureBoxSizeMode.StretchImage;
                appLogo.SetBounds(28, 88, 64, 64);
                aboutPage.Controls.Add(appLogo);

                Label aboutTitle = AddPageLabel(aboutPage, primaryLabels, "PulseMute Main", 112, 88);
                aboutTitle.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold);
                aboutTitle.SetBounds(112, 88, 380, 34);
                Label aboutVersion = AddPageState(aboutPage, stateLabels, 112, 126);
                aboutVersion.SetBounds(112, 126, 380, 20);
                aboutVersion.Text = "Version 26.1.0.4-stable";
                Panel aboutSeparator = AddPageSeparator(aboutPage, separators, 178);
                Label creatorLabel = AddPageLabel(aboutPage, primaryLabels, "Created by Wrakeeb", 28, 204);
                Label ownerLabel = AddPageState(aboutPage, stateLabels, 28, 230);
                ownerLabel.SetBounds(28, 230, 450, 20);
                ownerLabel.Text = "Owner: w-rakeeb | Stable daily-use channel";

                string[] navigationNames = { "General", "Hotkeys", "Audio", "Controller", "Customization", "Developer", "About" };
                for (int i = 0; i < navigationNames.Length; i++)
                {
                    Button navigationButton = CreateSidebarNavigationButton(navigationNames[i], 64 + (i * 44));
                    sidebar.Controls.Add(navigationButton);
                    navigationButtons.Add(navigationButton);
                }

                Label sidebarVersion = new Label();
                sidebarVersion.Text = "26.1.0.4-stable";
                sidebarVersion.Font = new Font("Segoe UI", 7.5F, FontStyle.Regular);
                sidebarVersion.TextAlign = ContentAlignment.MiddleLeft;
                sidebarVersion.SetBounds(20, 466, 140, 18);
                sidebarVersion.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;

                sidebar.Controls.Add(brandLabel);
                sidebar.Controls.Add(sidebarVersion);

                foreach (Panel page in pages.Values)
                {
                    page.Visible = false;
                    contentHost.Controls.Add(page);
                }

                string activePage = "General";
                Panel animatedPage = null;
                Timer pageAnimationTimer = new Timer();
                pageAnimationTimer.Interval = 16;
                pageAnimationTimer.Tick += delegate
                {
                    if (animatedPage == null || animatedPage.IsDisposed)
                    {
                        pageAnimationTimer.Stop();
                        return;
                    }
                    animatedPage.Left = Math.Max(0, animatedPage.Left - 3);
                    if (animatedPage.Left == 0)
                    {
                        pageAnimationTimer.Stop();
                        animatedPage = null;
                    }
                };
                Action applyDialogTheme = null;
                Action<string> showPage = delegate(string pageName)
                {
                    if (!pages.ContainsKey(pageName))
                        return;
                    pageAnimationTimer.Stop();
                    activePage = pageName;
                    foreach (KeyValuePair<string, Panel> page in pages)
                        page.Value.Visible = false;
                    Panel selectedPage = pages[activePage];
                    selectedPage.SetBounds(
                        animationsEnabled && dialog.Visible ? 12 : 0,
                        0,
                        contentHost.ClientSize.Width,
                        contentHost.ClientSize.Height);
                    selectedPage.Visible = true;
                    selectedPage.BringToFront();
                    if (animationsEnabled && dialog.Visible)
                    {
                        animatedPage = selectedPage;
                        pageAnimationTimer.Start();
                    }
                    if (applyDialogTheme != null)
                        applyDialogTheme();
                };

                foreach (Button navigationButton in navigationButtons)
                {
                    navigationButton.Click += delegate(object sender, EventArgs e)
                    {
                        Button selectedButton = sender as Button;
                        if (selectedButton != null)
                            showPage(Convert.ToString(selectedButton.Tag));
                    };
                }

                applyDialogTheme = delegate
                {
                    Color background = ThemeBackgroundColor();
                    Color foreground = ThemePrimaryTextColor();
                    Color secondary = ThemeSecondaryTextColor();
                    Color surface = ThemeSurfaceColor();
                    Color border = darkMode ? Color.FromArgb(54, 62, 72) : Color.FromArgb(205, 211, 219);
                    Color sidebarColor = darkMode ? Color.FromArgb(21, 24, 29) : Color.FromArgb(240, 243, 247);
                    Color accent = AccentColor();

                    dialog.BackColor = background;
                    contentHost.BackColor = background;
                    footer.BackColor = background;
                    sidebar.BackColor = sidebarColor;
                    brandLabel.ForeColor = foreground;
                    versionLabel.ForeColor = secondary;
                    sidebarVersion.ForeColor = secondary;

                    foreach (Panel page in pages.Values)
                    {
                        page.BackColor = background;
                        SetWindowTheme(page.Handle, darkMode ? "DarkMode_Explorer" : "Explorer", null);
                    }
                    foreach (Label label in primaryLabels)
                        label.ForeColor = foreground;
                    foreach (Label label in stateLabels)
                        label.ForeColor = secondary;

                    autoState.Text = autoToggle.Checked ? "Enabled" : "Disabled";
                    taskbarState.Text = taskbarToggle.Checked ? "Enabled" : "Disabled";
                    rememberState.Text = rememberToggle.Checked ? "Enabled" : "Disabled";
                    legacySettingsState.Text = legacySettingsToggle.Checked ? "Legacy opens next time" : "New interface (default)";
                    dualHotkeyState.Text = dualHotkeyToggle.Checked ? "Key 1 and Key 2 active" : "Key 1 only";
                    soundFeedbackState.Text = soundFeedbackToggle.Checked ? "Enabled" : "Disabled";
                    soundVolumeState.Text = soundVolumeSlider.Value + "%";
                    controllerState.Text = dualSense.StatusText;
                    appearanceState.Text = appearanceToggle.Checked ? "Dark theme" : "White theme";
                    customizationState.Text = customColorsEnabled ? "Custom colors" : "Default colors";
                    developerModeState.Text = developerModeToggle.Checked ? "Enabled" : "Disabled";
                    animationsState.Text = animationsToggle.Checked ? "Enabled" : "Disabled";
                    keyOneButton.Text = CurrentShortcutText(1);
                    keyTwoButton.Text = CurrentShortcutText(2);
                    keyTwoButton.Enabled = dualHotkeyToggle.Checked;
                    developerContent.Visible = developerModeToggle.Checked;
                    developerPage.AutoScrollMinSize = developerModeToggle.Checked ? new Size(0, 470) : new Size(0, 410);
                    olderVersionBox.Enabled = developerModeToggle.Checked && olderVersionBox.Items.Count > 0;
                    openVersionButton.Enabled = olderVersionBox.Enabled;
                    soundPresetBox.Enabled = soundFeedbackToggle.Checked;

                    foreach (ToggleSwitch toggle in toggles)
                    {
                        toggle.OnColor = accent;
                        toggle.OffColor = darkMode ? Color.FromArgb(67, 75, 86) : Color.FromArgb(187, 194, 203);
                        toggle.ThumbColor = Color.White;
                        toggle.AnimationsEnabled = animationsEnabled;
                        toggle.Invalidate();
                    }
                    foreach (ThemedComboBox comboBox in comboBoxes)
                    {
                        comboBox.BackColor = surface;
                        comboBox.ForeColor = foreground;
                        comboBox.HighlightColor = accent;
                        comboBox.Invalidate();
                    }
                    versionInfoText.BackColor = surface;
                    versionInfoText.ForeColor = secondary;
                    developerContent.BackColor = background;
                    soundVolumeSlider.BackColor = background;
                    soundVolumeSlider.ForeColor = accent;

                    foreach (Panel separator in separators)
                        separator.BackColor = border;
                    footerSeparator.BackColor = border;
                    foreach (Button button in actionButtons)
                        StyleSidebarActionButton(button, surface, foreground, border, darkMode);
                    StyleSidebarActionButton(doneButton, surface, foreground, border, darkMode);

                    foreach (Button navigationButton in navigationButtons)
                    {
                        bool selected = string.Equals(Convert.ToString(navigationButton.Tag), activePage, StringComparison.OrdinalIgnoreCase);
                        navigationButton.BackColor = selected ? surface : sidebarColor;
                        navigationButton.ForeColor = selected ? accent : secondary;
                        navigationButton.FlatAppearance.BorderSize = selected ? 1 : 0;
                        navigationButton.FlatAppearance.BorderColor = selected ? accent : sidebarColor;
                        navigationButton.FlatAppearance.MouseOverBackColor = surface;
                    }

                    dialog.Icon = appIcon;
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

                legacySettingsToggle.CheckedChanged += delegate
                {
                    legacySettingsEnabled = legacySettingsToggle.Checked;
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

                keyOneButton.Click += delegate
                {
                    dialog.Close();
                    BeginInvoke((MethodInvoker)delegate { BeginShortcutCapture(1); });
                };
                keyTwoButton.Click += delegate
                {
                    if (!dualHotkeyEnabled)
                        return;
                    dialog.Close();
                    BeginInvoke((MethodInvoker)delegate { BeginShortcutCapture(2); });
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

                soundVolumeSlider.ValueChanged += delegate
                {
                    soundVolume = soundVolumeSlider.Value;
                    soundVolumeState.Text = soundVolume + "%";
                    SaveSettingsFile();
                };

                soundSettingsButton.Click += delegate { OpenWindowsSoundSettings(dialog); };

                controllerScanButton.Click += delegate
                {
                    dualSense.Rescan();
                    controllerState.Text = "Searching...";
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

                developerModeToggle.CheckedChanged += delegate
                {
                    developerModeEnabled = developerModeToggle.Checked;
                    SaveSettingsFile();
                    applyDialogTheme();
                };

                animationsToggle.CheckedChanged += delegate
                {
                    animationsEnabled = animationsToggle.Checked;
                    pageAnimationTimer.Stop();
                    foreach (ToggleSwitch toggle in toggles)
                        toggle.AnimationsEnabled = animationsEnabled;
                    SaveSettingsFile();
                    applyDialogTheme();
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

                Timer controllerStatusTimer = new Timer();
                controllerStatusTimer.Interval = 500;
                controllerStatusTimer.Tick += delegate { controllerState.Text = dualSense.StatusText; };
                controllerStatusTimer.Start();
                dialog.FormClosed += delegate
                {
                    controllerStatusTimer.Stop();
                    controllerStatusTimer.Dispose();
                    pageAnimationTimer.Stop();
                    pageAnimationTimer.Dispose();
                    if (appLogo.Image != null)
                    {
                        appLogo.Image.Dispose();
                        appLogo.Image = null;
                    }
                };
                dialog.ResizeEnd += delegate { applyDialogTheme(); };

                dialog.Controls.Add(contentHost);
                dialog.Controls.Add(footer);
                dialog.Controls.Add(sidebar);
                showPage("General");
                applyDialogTheme();
                dialog.ShowDialog(this);
            }
        }

        private static Panel CreateSidebarSettingsPage(string title, List<Label> primaryLabels)
        {
            Panel page = new BufferedPanel();
            page.Size = new Size(584, 450);
            page.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            page.AutoScroll = true;
            page.AutoScrollMinSize = new Size(0, 410);

            Label heading = new Label();
            heading.Text = title;
            heading.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold);
            heading.SetBounds(28, 22, 500, 38);
            heading.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            page.Controls.Add(heading);
            primaryLabels.Add(heading);
            return page;
        }

        private static Label AddPageLabel(Panel page, List<Label> labels, string text, int x, int y)
        {
            Label label = CreateSettingsLabel(text, x, y);
            label.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            page.Controls.Add(label);
            labels.Add(label);
            return label;
        }

        private static Label AddPageState(Panel page, List<Label> labels, int x, int y)
        {
            Label label = CreateSettingsStateLabel(x, y);
            label.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            page.Controls.Add(label);
            labels.Add(label);
            return label;
        }

        private static ToggleSwitch AddPageToggle(Panel page, List<ToggleSwitch> toggles, int x, int y, bool isChecked)
        {
            ToggleSwitch toggle = CreateToggleSwitch(x, y, isChecked);
            toggle.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            page.Controls.Add(toggle);
            toggles.Add(toggle);
            return toggle;
        }

        private static Panel AddPageSeparator(Panel page, List<Panel> separators, int y)
        {
            Panel separator = CreateSeparator(28, y, 510);
            separator.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            page.Controls.Add(separator);
            separators.Add(separator);
            return separator;
        }

        private static ThemedComboBox CreatePageComboBox(int x, int y, int width)
        {
            ThemedComboBox comboBox = new ThemedComboBox();
            comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox.FlatStyle = FlatStyle.Flat;
            comboBox.SetBounds(x, y, width, 30);
            comboBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            return comboBox;
        }

        private static Button CreateSidebarNavigationButton(string text, int y)
        {
            Button button = new Button();
            button.Text = text;
            button.Tag = text;
            button.SetBounds(12, y, 152, 36);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            button.TextAlign = ContentAlignment.MiddleLeft;
            button.Padding = new Padding(12, 0, 0, 0);
            button.Cursor = Cursors.Hand;
            return button;
        }

        private static void StyleSidebarActionButton(Button button, Color surface, Color foreground, Color border, bool darkTheme)
        {
            button.BackColor = surface;
            button.ForeColor = foreground;
            button.FlatAppearance.BorderColor = border;
            button.FlatAppearance.MouseOverBackColor = darkTheme
                ? Color.FromArgb(39, 45, 54)
                : Color.FromArgb(235, 238, 242);
        }

    }

    internal sealed class BufferedSettingsForm : Form
    {
        public BufferedSettingsForm()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            DoubleBuffered = true;
            ResizeRedraw = true;
        }
    }

    internal sealed class BufferedPanel : Panel
    {
        public BufferedPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            DoubleBuffered = true;
            ResizeRedraw = true;
        }
    }
}
