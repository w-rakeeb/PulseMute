using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PulseMute
{
    internal sealed partial class MainForm
    {
        private void ShowSettingsV15()
        {
            using (Form dialog = new BufferedSettingsForm())
            {
                dialog.Text = "PulseMute Beta settings";
                dialog.Icon = appIcon;
                dialog.ClientSize = new Size(760, 590);
                dialog.MinimumSize = new Size(700, 520);
                dialog.FormBorderStyle = FormBorderStyle.None;
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;
                dialog.ShowInTaskbar = false;
                dialog.TopMost = TopMost;
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.Font = SettingsBodyFont(10F, FontStyle.Regular);
                dialog.AutoScaleDimensions = new SizeF(96F, 96F);
                dialog.AutoScaleMode = AutoScaleMode.Dpi;
                dialog.HandleCreated += delegate { ApplyCaptionTheme(dialog.Handle, darkMode); };

                Panel titleBar = new BufferedPanel();
                titleBar.SetBounds(0, 0, 760, 44);
                titleBar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

                PictureBox titleLogo = new PictureBox();
                titleLogo.Image = CreateSafeIconBitmap(appIcon);
                titleLogo.SizeMode = PictureBoxSizeMode.Zoom;
                titleLogo.SetBounds(18, 10, 24, 24);

                Label titleBrand = new Label();
                titleBrand.Text = "PulseMute";
                titleBrand.Font = SettingsBodyFont(10F, FontStyle.Bold);
                titleBrand.TextAlign = ContentAlignment.MiddleLeft;
                titleBrand.SetBounds(50, 7, 180, 30);

                Button closeWindowButton = new SilenceActionButton();
                closeWindowButton.Text = "\uE711";
                closeWindowButton.Font = new Font("Segoe Fluent Icons", 10F, FontStyle.Regular);
                closeWindowButton.SetBounds(716, 7, 34, 30);
                closeWindowButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                closeWindowButton.AccessibleName = "Close settings";
                closeWindowButton.Click += delegate { dialog.Close(); };

                Point dragStart = Point.Empty;
                bool draggingWindow = false;
                MouseEventHandler beginDrag = delegate(object sender, MouseEventArgs e)
                {
                    if (e.Button != MouseButtons.Left) return;
                    draggingWindow = true;
                    dragStart = e.Location;
                };
                MouseEventHandler moveDrag = delegate(object sender, MouseEventArgs e)
                {
                    if (!draggingWindow || e.Button != MouseButtons.Left) return;
                    Point screen = ((Control)sender).PointToScreen(e.Location);
                    dialog.Location = new Point(screen.X - dragStart.X, screen.Y - dragStart.Y);
                };
                MouseEventHandler endDrag = delegate { draggingWindow = false; };
                titleBar.MouseDown += beginDrag;
                titleBar.MouseMove += moveDrag;
                titleBar.MouseUp += endDrag;
                titleBrand.MouseDown += beginDrag;
                titleBrand.MouseMove += moveDrag;
                titleBrand.MouseUp += endDrag;
                titleLogo.MouseDown += beginDrag;
                titleLogo.MouseMove += moveDrag;
                titleLogo.MouseUp += endDrag;
                titleBar.Controls.Add(titleLogo);
                titleBar.Controls.Add(titleBrand);
                titleBar.Controls.Add(closeWindowButton);

                Panel sidebar = new BufferedPanel();
                sidebar.SetBounds(0, 44, 240, 546);
                sidebar.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;

                Label brandLabel = new Label();
                brandLabel.Text = "PulseMute";
                brandLabel.Font = SettingsDisplayFont(9F, FontStyle.Bold);
                brandLabel.SetBounds(20, 14, 196, 24);
                brandLabel.Text = "SETTINGS";

                Panel contentHost = new BufferedPanel();
                contentHost.SetBounds(240, 44, 520, 496);
                contentHost.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

                Panel footer = new BufferedPanel();
                footer.SetBounds(240, 540, 520, 50);
                footer.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

                Label versionLabel = new Label();
                versionLabel.Text = EditionVersionText() + "  |  Created by Wrakeeb";
                versionLabel.Font = SettingsBodyFont(7.5F, FontStyle.Regular);
                versionLabel.TextAlign = ContentAlignment.MiddleLeft;
                versionLabel.SetBounds(22, 15, 350, 20);
                versionLabel.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;

                Button doneButton = CreateSilenceActionButton("Done", new Point(420, 10));
                doneButton.Size = new Size(78, 30);
                doneButton.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
                doneButton.Click += delegate { dialog.Close(); };
                dialog.AcceptButton = doneButton;
                dialog.CancelButton = doneButton;

                Panel footerSeparator = CreateSeparator(0, 0, 520);
                footerSeparator.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

                footer.Controls.Add(versionLabel);
                footer.Controls.Add(doneButton);
                footer.Controls.Add(footerSeparator);

                Dictionary<string, Panel> pages = new Dictionary<string, Panel>(StringComparer.OrdinalIgnoreCase);
                List<Label> primaryLabels = new List<Label>();
                List<Label> stateLabels = new List<Label>();
                List<ToggleSwitch> toggles = new List<ToggleSwitch>();
                List<SilenceComboBox> comboBoxes = new List<SilenceComboBox>();
                List<Button> actionButtons = new List<Button>();
                List<Panel> separators = new List<Panel>();
                List<Button> navigationButtons = new List<Button>();
                List<SilenceCardPanel> cards = new List<SilenceCardPanel>();

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
                Button keyOneButton = CreateSilenceActionButton(CurrentShortcutText(1), new Point(330, 164));
                keyOneButton.Size = new Size(208, 34);
                keyOneButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                hotkeysPage.Controls.Add(keyOneButton);
                actionButtons.Add(keyOneButton);
                Panel hotkeySeparatorTwo = AddPageSeparator(hotkeysPage, separators, 218);

                Label keyTwoLabel = AddPageLabel(hotkeysPage, primaryLabels, "Key 2", 28, 242);
                Label keyTwoState = AddPageState(hotkeysPage, stateLabels, 28, 264);
                keyTwoState.Text = "Secondary assignment";
                Button keyTwoButton = CreateSilenceActionButton(CurrentShortcutText(2), new Point(330, 240));
                keyTwoButton.Size = new Size(208, 34);
                keyTwoButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                hotkeysPage.Controls.Add(keyTwoButton);
                actionButtons.Add(keyTwoButton);

                Panel audioPage = CreateSidebarSettingsPage("Audio", primaryLabels);
                pages.Add("Audio", audioPage);

                Label microphoneLabel = AddPageLabel(audioPage, primaryLabels, "Microphone", 28, 88);
                SilenceComboBox microphoneBox = CreatePageComboBox(28, 116, 510);
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
                SilenceComboBox soundPresetBox = CreatePageComboBox(28, 278, 510);
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
                SilenceSlider soundVolumeSlider = new SilenceSlider();
                soundVolumeSlider.Minimum = 0;
                soundVolumeSlider.Maximum = 100;
                soundVolumeSlider.Value = Math.Max(0, Math.Min(100, soundVolume));
                soundVolumeSlider.SetBounds(236, 326, 302, 32);
                soundVolumeSlider.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                audioPage.Controls.Add(soundVolumeSlider);

                Button soundSettingsButton = CreateSilenceActionButton("Windows sound settings", new Point(28, 388));
                soundSettingsButton.Size = new Size(190, 32);
                audioPage.Controls.Add(soundSettingsButton);
                actionButtons.Add(soundSettingsButton);

                Panel controllerPage = CreateSidebarSettingsPage("Controller", primaryLabels);
                pages.Add("Controller", controllerPage);

                Label controllerLabel = AddPageLabel(controllerPage, primaryLabels, "PlayStation controller", 28, 88);
                Label controllerState = AddPageState(controllerPage, stateLabels, 28, 110);
                controllerState.SetBounds(28, 110, 320, 18);
                Button controllerScanButton = CreateSilenceActionButton("Rescan", new Point(412, 88));
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
                Button customizeButton = CreateSilenceActionButton("Customize", new Point(398, 164));
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
                SilenceComboBox olderVersionBox = CreatePageComboBox(0, 114, 382);
                developerContent.Controls.Add(olderVersionBox);
                comboBoxes.Add(olderVersionBox);
                List<ArchivedVersionInfo> archivedVersions = DiscoverArchivedVersions();
                foreach (ArchivedVersionInfo version in archivedVersions)
                    olderVersionBox.Items.Add(version);
                if (olderVersionBox.Items.Count > 0)
                    olderVersionBox.SelectedIndex = 0;

                Button openVersionButton = CreateSilenceActionButton("Open", new Point(392, 114));
                openVersionButton.Size = new Size(118, 30);
                openVersionButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                developerContent.Controls.Add(openVersionButton);
                actionButtons.Add(openVersionButton);

                Label versionsInfoLabel = AddPageLabel(developerContent, primaryLabels, "Versions info", 0, 166);
                Label versionInfoText = new Label();
                versionInfoText.AutoSize = false;
                versionInfoText.SetBounds(0, 194, 510, 74);
                versionInfoText.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                versionInfoText.Font = SettingsBodyFont(8.5F, FontStyle.Regular);
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

                Label aboutTitle = AddPageLabel(aboutPage, primaryLabels, "PulseMute Beta", 112, 88);
                aboutTitle.Font = SettingsDisplayFont(18F, FontStyle.Bold);
                aboutTitle.SetBounds(112, 88, 380, 34);
                Label aboutVersion = AddPageState(aboutPage, stateLabels, 112, 126);
                aboutVersion.SetBounds(112, 126, 380, 20);
                aboutVersion.Text = "Version 26.1.0.11-beta";
                Panel aboutSeparator = AddPageSeparator(aboutPage, separators, 178);
                Label creatorLabel = AddPageLabel(aboutPage, primaryLabels, "Created by Wrakeeb", 28, 204);
                Label ownerLabel = AddPageState(aboutPage, stateLabels, 28, 230);
                ownerLabel.SetBounds(28, 230, 450, 20);
                ownerLabel.Text = "Owner: w-rakeeb | Beta development channel";

                AddSettingsCard(generalPage, cards, 76, 58, autoLabel, autoState, autoToggle);
                AddSettingsCard(generalPage, cards, 146, 58, taskbarLabel, taskbarState, taskbarToggle);
                AddSettingsCard(generalPage, cards, 216, 58, rememberLabel, rememberState, rememberToggle);
                AddSettingsCard(generalPage, cards, 286, 58, legacySettingsLabel, legacySettingsState, legacySettingsToggle);
                generalSeparatorOne.Visible = false;
                generalSeparatorTwo.Visible = false;
                generalSeparatorThree.Visible = false;

                AddSettingsCard(hotkeysPage, cards, 76, 58, dualHotkeyLabel, dualHotkeyState, dualHotkeyToggle);
                AddSettingsCard(hotkeysPage, cards, 146, 66, keyOneLabel, keyOneState, keyOneButton);
                AddSettingsCard(hotkeysPage, cards, 224, 66, keyTwoLabel, keyTwoState, keyTwoButton);
                hotkeySeparatorOne.Visible = false;
                hotkeySeparatorTwo.Visible = false;

                AddSettingsCard(audioPage, cards, 76, 82, microphoneLabel, microphoneBox);
                AddSettingsCard(audioPage, cards, 170, 58, soundFeedbackLabel, soundFeedbackState, soundFeedbackToggle);
                AddSettingsCard(audioPage, cards, 240, 72, soundPresetLabel, soundPresetBox);
                audioSeparatorOne.Visible = false;

                AddSettingsCard(controllerPage, cards, 76, 62, controllerLabel, controllerState, controllerScanButton);
                AddSettingsCard(controllerPage, cards, 150, 76, controllerSupportLabel, controllerSupportState);
                controllerSeparator.Visible = false;

                AddSettingsCard(customizationPage, cards, 76, 58, appearanceLabel, appearanceState, appearanceToggle);
                AddSettingsCard(customizationPage, cards, 146, 66, customizationLabel, customizationState, customizeButton);
                customizationSeparator.Visible = false;

                AddSettingsCard(developerPage, cards, 76, 58, developerModeLabel, developerModeState, developerModeToggle);
                developerSeparator.Visible = false;

                AddSettingsCard(aboutPage, cards, 76, 92, appLogo, aboutTitle, aboutVersion);
                AddSettingsCard(aboutPage, cards, 190, 68, creatorLabel, ownerLabel);
                aboutSeparator.Visible = false;

                string[] navigationNames = { "General", "Hotkeys", "Audio", "Controller", "Customization", "Developer", "About" };
                string[] navigationIcons = { "\uE713", "\uE765", "\uE8D6", "\uE7FC", "\uE790", "\uE943", "\uE946" };
                for (int i = 0; i < navigationNames.Length; i++)
                {
                    Button navigationButton = CreateSidebarNavigationButton(navigationNames[i], navigationIcons[i], 50 + (i * 44));
                    sidebar.Controls.Add(navigationButton);
                    navigationButtons.Add(navigationButton);
                }

                Label sidebarVersion = new Label();
                sidebarVersion.Text = "26.1.0.11-beta";
                sidebarVersion.Font = SettingsBodyFont(7.5F, FontStyle.Regular);
                sidebarVersion.TextAlign = ContentAlignment.MiddleLeft;
                sidebarVersion.SetBounds(20, 510, 200, 18);
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
                float pageAnimationProgress = 1F;
                Timer pageAnimationTimer = new Timer();
                pageAnimationTimer.Interval = 16;
                pageAnimationTimer.Tick += delegate
                {
                    if (animatedPage == null || animatedPage.IsDisposed)
                    {
                        pageAnimationTimer.Stop();
                        return;
                    }
                    pageAnimationProgress = Math.Min(1F, pageAnimationProgress + 0.14F);
                    float remaining = 1F - pageAnimationProgress;
                    float eased = remaining * remaining * remaining;
                    animatedPage.Left = (int)Math.Round(16F * eased);
                    if (pageAnimationProgress >= 1F)
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
                        animationsEnabled && dialog.Visible ? 16 : 0,
                        0,
                        contentHost.ClientSize.Width,
                        contentHost.ClientSize.Height);
                    selectedPage.Visible = true;
                    selectedPage.BringToFront();
                    if (animationsEnabled && dialog.Visible)
                    {
                        animatedPage = selectedPage;
                        pageAnimationProgress = 0F;
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
                    Color border = SettingsBorderColor();
                    Color sidebarColor = SettingsSidebarColor();
                    Color accent = AccentColor();

                    dialog.BackColor = background;
                    contentHost.BackColor = background;
                    footer.BackColor = background;
                    sidebar.BackColor = sidebarColor;
                    titleBar.BackColor = sidebarColor;
                    titleBrand.ForeColor = foreground;
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
                    foreach (SilenceComboBox comboBox in comboBoxes)
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
                    soundVolumeSlider.TrackColor = border;
                    soundVolumeSlider.FillColor = accent;
                    soundVolumeSlider.ThumbColor = accent;
                    soundVolumeSlider.Invalidate();

                    foreach (Panel separator in separators)
                        separator.BackColor = border;
                    foreach (SilenceCardPanel card in cards)
                    {
                        card.BackColor = surface;
                        card.SurfaceColor = surface;
                        card.BorderColor = border;
                        card.Invalidate();
                    }
                    footerSeparator.BackColor = border;
                    foreach (Button button in actionButtons)
                        StyleSidebarActionButton(button, surface, foreground, border, darkMode);
                    StyleSidebarActionButton(doneButton, surface, foreground, border, darkMode);
                    StyleSidebarActionButton(closeWindowButton, sidebarColor, secondary, border, darkMode);

                    foreach (Button navigationButton in navigationButtons)
                    {
                        bool selected = string.Equals(Convert.ToString(navigationButton.Tag), activePage, StringComparison.OrdinalIgnoreCase);
                        SilenceNavigationButton silenceNavigation = navigationButton as SilenceNavigationButton;
                        if (silenceNavigation != null)
                        {
                            silenceNavigation.Selected = selected;
                            silenceNavigation.AccentColor = accent;
                            silenceNavigation.SurfaceColor = surface;
                            silenceNavigation.NormalTextColor = secondary;
                            silenceNavigation.Invalidate();
                        }
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
                    if (titleLogo.Image != null)
                    {
                        titleLogo.Image.Dispose();
                        titleLogo.Image = null;
                    }
                };
                dialog.ResizeEnd += delegate { applyDialogTheme(); };

                dialog.Controls.Add(contentHost);
                dialog.Controls.Add(footer);
                dialog.Controls.Add(sidebar);
                dialog.Controls.Add(titleBar);
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
            heading.Font = SettingsDisplayFont(20F, FontStyle.Bold);
            heading.SetBounds(28, 22, 500, 38);
            heading.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            page.Controls.Add(heading);
            primaryLabels.Add(heading);
            return page;
        }

        private static Label AddPageLabel(Panel page, List<Label> labels, string text, int x, int y)
        {
            Label label = CreateSettingsLabel(text, x, y);
            label.Font = SettingsBodyFont(9.5F, FontStyle.Bold);
            label.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            page.Controls.Add(label);
            labels.Add(label);
            return label;
        }

        private static Label AddPageState(Panel page, List<Label> labels, int x, int y)
        {
            Label label = CreateSettingsStateLabel(x, y);
            label.Font = SettingsBodyFont(8F, FontStyle.Regular);
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

        private static SilenceComboBox CreatePageComboBox(int x, int y, int width)
        {
            SilenceComboBox comboBox = new SilenceComboBox();
            comboBox.SetBounds(x, y, width, 32);
            comboBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            return comboBox;
        }

        private static SilenceCardPanel AddSettingsCard(
            Panel page,
            List<SilenceCardPanel> cards,
            int y,
            int height,
            params Control[] controls)
        {
            SilenceCardPanel card = new SilenceCardPanel();
            card.SetBounds(28, y, 510, height);
            card.BackColor = Color.FromArgb(39, 36, 38);
            card.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            foreach (Control control in controls)
            {
                int relativeY = control.Top - y;
                int relativeX = Math.Max(16, control.Left - 28);
                bool rightOnly = (control.Anchor & AnchorStyles.Right) != 0
                    && (control.Anchor & AnchorStyles.Left) == 0;
                if (rightOnly)
                    relativeX = card.Width - control.Width - 16;
                if (control is SilenceComboBox || control is SilenceSlider)
                    control.Width = Math.Max(60, card.Width - relativeX - 16);
                page.Controls.Remove(control);
                control.Location = new Point(relativeX, relativeY);
                card.Controls.Add(control);
            }
            foreach (Control control in controls)
            {
                if (control is Button || control is CheckBox || control is SilenceComboBox || control is SilenceSlider)
                    control.BringToFront();
            }
            page.Controls.Add(card);
            card.BringToFront();
            cards.Add(card);
            return card;
        }

        private static Button CreateSidebarNavigationButton(string text, string icon, int y)
        {
            SilenceNavigationButton button = new SilenceNavigationButton();
            button.Text = text;
            button.IconGlyph = icon;
            button.Tag = text;
            button.SetBounds(12, y, 216, 38);
            button.Font = SettingsBodyFont(9.5F, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
            return button;
        }

        private static Button CreateSilenceActionButton(string text, Point location)
        {
            SilenceActionButton button = new SilenceActionButton();
            button.Text = text;
            button.Location = location;
            button.Size = new Size(96, 30);
            button.Font = SettingsBodyFont(9F, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
            return button;
        }

        private static Font SettingsBodyFont(float size, FontStyle style)
        {
            return new Font(FontFamilyExists("Inter") ? "Inter" : "Segoe UI", size, style, GraphicsUnit.Point);
        }

        private static Font SettingsDisplayFont(float size, FontStyle style)
        {
            return new Font(FontFamilyExists("Inter") ? "Inter" : "Segoe UI Semibold", size, style, GraphicsUnit.Point);
        }

        private static bool FontFamilyExists(string family)
        {
            try
            {
                using (Font test = new Font(family, 8F))
                    return string.Equals(test.FontFamily.Name, family, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static void StyleSidebarActionButton(Button button, Color surface, Color foreground, Color border, bool darkTheme)
        {
            SilenceActionButton silenceButton = button as SilenceActionButton;
            if (silenceButton != null)
            {
                silenceButton.SurfaceColor = surface;
                silenceButton.TextColor = foreground;
                silenceButton.BorderColor = border;
                silenceButton.HoverColor = darkTheme ? Color.FromArgb(54, 49, 52) : Color.FromArgb(234, 232, 234);
                silenceButton.Invalidate();
                return;
            }
            button.BackColor = surface;
            button.ForeColor = foreground;
            button.FlatAppearance.BorderColor = border;
            button.FlatAppearance.MouseOverBackColor = darkTheme
                ? Color.FromArgb(39, 45, 54)
                : Color.FromArgb(235, 238, 242);
        }

    }

    internal sealed class SilenceComboBox : Control
    {
        private readonly List<object> items = new List<object>();
        private int selectedIndex = -1;
        public Color HighlightColor = Color.FromArgb(193, 53, 69);
        public IList<object> Items { get { return items; } }
        public event EventHandler SelectedIndexChanged;

        public int SelectedIndex
        {
            get { return selectedIndex; }
            set
            {
                int next = value < -1 || value >= items.Count ? -1 : value;
                if (selectedIndex == next) return;
                selectedIndex = next;
                Invalidate();
                EventHandler handler = SelectedIndexChanged;
                if (handler != null) handler(this, EventArgs.Empty);
            }
        }

        public object SelectedItem
        {
            get { return selectedIndex >= 0 && selectedIndex < items.Count ? items[selectedIndex] : null; }
        }

        public SilenceComboBox()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Cursor = Cursors.Hand;
            TabStop = true;
            AccessibleRole = AccessibleRole.ComboBox;
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            if (!Enabled || items.Count == 0) return;
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.ShowImageMargin = false;
            menu.BackColor = BackColor;
            menu.ForeColor = ForeColor;
            menu.Renderer = new SilenceMenuRenderer(BackColor, ForeColor, HighlightColor);
            for (int index = 0; index < items.Count; index++)
            {
                int itemIndex = index;
                ToolStripMenuItem item = new ToolStripMenuItem(Convert.ToString(items[index]));
                item.AutoSize = false;
                item.Size = new Size(Math.Max(120, Width - 2), 30);
                item.Checked = index == selectedIndex;
                item.Click += delegate
                {
                    SelectedIndex = itemIndex;
                    menu.Close();
                    menu.Dispose();
                };
                menu.Items.Add(item);
            }
            menu.Closed += delegate { if (!menu.IsDisposed) menu.Dispose(); Invalidate(); };
            menu.Show(this, new Point(0, Height + 2));
            Invalidate();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down && selectedIndex < items.Count - 1)
            {
                SelectedIndex++;
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Up && selectedIndex > 0)
            {
                SelectedIndex--;
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
            {
                OnClick(EventArgs.Empty);
                e.Handled = true;
            }
            base.OnKeyDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(Parent == null ? BackColor : Parent.BackColor);
            Rectangle bounds = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = SilenceDrawing.RoundedPath(bounds, 5))
            using (SolidBrush brush = new SolidBrush(BackColor))
            using (Pen pen = new Pen(Focused ? HighlightColor : Color.FromArgb(78, 73, 77)))
            {
                e.Graphics.FillPath(brush, path);
                e.Graphics.DrawPath(pen, path);
            }
            string text = Convert.ToString(SelectedItem);
            TextRenderer.DrawText(e.Graphics, text, Font, new Rectangle(12, 0, Math.Max(10, Width - 44), Height),
                Enabled ? ForeColor : Color.FromArgb(115, 111, 114),
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            Point center = new Point(Width - 18, Height / 2 + 1);
            using (Pen pen = new Pen(Enabled ? ForeColor : Color.FromArgb(115, 111, 114), 1.5F))
            {
                e.Graphics.DrawLine(pen, center.X - 4, center.Y - 2, center.X, center.Y + 2);
                e.Graphics.DrawLine(pen, center.X, center.Y + 2, center.X + 4, center.Y - 2);
            }
        }
    }

    internal sealed class SilenceMenuRenderer : ToolStripProfessionalRenderer
    {
        public SilenceMenuRenderer(Color background, Color foreground, Color accent)
            : base(new SilenceColorTable(background, foreground, accent)) { }
    }

    internal sealed class SilenceColorTable : ProfessionalColorTable
    {
        private readonly Color background;
        private readonly Color accent;
        public SilenceColorTable(Color background, Color foreground, Color accent)
        {
            this.background = background;
            this.accent = accent;
            UseSystemColors = false;
        }
        public override Color ToolStripDropDownBackground { get { return background; } }
        public override Color MenuItemSelected { get { return accent; } }
        public override Color MenuItemBorder { get { return accent; } }
        public override Color ImageMarginGradientBegin { get { return background; } }
        public override Color ImageMarginGradientMiddle { get { return background; } }
        public override Color ImageMarginGradientEnd { get { return background; } }
    }

    internal sealed class SilenceSlider : Control
    {
        private int minimum;
        private int maximum = 100;
        private int currentValue;
        public Color TrackColor = Color.FromArgb(61, 57, 60);
        public Color FillColor = Color.FromArgb(193, 53, 69);
        public Color ThumbColor = Color.FromArgb(193, 53, 69);
        public event EventHandler ValueChanged;
        public int Minimum { get { return minimum; } set { minimum = value; Value = currentValue; } }
        public int Maximum { get { return maximum; } set { maximum = Math.Max(minimum + 1, value); Value = currentValue; } }
        public int Value
        {
            get { return currentValue; }
            set
            {
                int next = Math.Max(minimum, Math.Min(maximum, value));
                if (currentValue == next) return;
                currentValue = next;
                Invalidate();
                EventHandler handler = ValueChanged;
                if (handler != null) handler(this, EventArgs.Empty);
            }
        }

        public SilenceSlider()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Cursor = Cursors.Hand;
            TabStop = true;
        }

        private void SetValueFromX(int x)
        {
            int usable = Math.Max(1, Width - 16);
            float ratio = Math.Max(0F, Math.Min(1F, (x - 8F) / usable));
            Value = minimum + (int)Math.Round((maximum - minimum) * ratio);
        }

        protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) SetValueFromX(e.X); base.OnMouseDown(e); }
        protected override void OnMouseMove(MouseEventArgs e) { if (e.Button == MouseButtons.Left) SetValueFromX(e.X); base.OnMouseMove(e); }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Down) { Value--; e.Handled = true; }
            if (e.KeyCode == Keys.Right || e.KeyCode == Keys.Up) { Value++; e.Handled = true; }
            base.OnKeyDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(Parent == null ? BackColor : Parent.BackColor);
            int left = 8;
            int right = Math.Max(left + 1, Width - 8);
            int center = Height / 2;
            float ratio = (currentValue - minimum) / (float)Math.Max(1, maximum - minimum);
            int thumbX = left + (int)Math.Round((right - left) * ratio);
            using (Pen track = new Pen(TrackColor, 4F))
            using (Pen fill = new Pen(FillColor, 4F))
            using (SolidBrush thumb = new SolidBrush(ThumbColor))
            {
                track.StartCap = track.EndCap = LineCap.Round;
                fill.StartCap = fill.EndCap = LineCap.Round;
                e.Graphics.DrawLine(track, left, center, right, center);
                e.Graphics.DrawLine(fill, left, center, thumbX, center);
                e.Graphics.FillEllipse(thumb, thumbX - 6, center - 6, 12, 12);
            }
            if (Focused) ControlPaint.DrawFocusRectangle(e.Graphics, ClientRectangle);
        }
    }

    internal sealed class SilenceNavigationButton : Button
    {
        public string IconGlyph = string.Empty;
        public bool Selected;
        public Color AccentColor = Color.FromArgb(193, 53, 69);
        public Color SurfaceColor = Color.FromArgb(48, 43, 46);
        public Color NormalTextColor = Color.FromArgb(190, 187, 191);
        private bool hovered;

        public SilenceNavigationButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnMouseEnter(EventArgs e) { hovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hovered = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(Parent == null ? BackColor : Parent.BackColor);
            Rectangle bounds = new Rectangle(0, 0, Width - 1, Height - 1);
            if (Selected || hovered)
            {
                using (GraphicsPath path = SilenceDrawing.RoundedPath(bounds, 6))
                using (SolidBrush brush = new SolidBrush(SurfaceColor))
                    e.Graphics.FillPath(brush, path);
            }
            Color color = Selected ? AccentColor : NormalTextColor;
            using (Font iconFont = new Font("Segoe Fluent Icons", 10F, FontStyle.Regular))
                TextRenderer.DrawText(e.Graphics, IconGlyph, iconFont, new Rectangle(14, 0, 26, Height), color,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(48, 0, Width - 56, Height), color,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            if (Focused)
                ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(bounds, -3, -3));
        }
    }

    internal sealed class SilenceCardPanel : Panel
    {
        public Color SurfaceColor = Color.FromArgb(39, 36, 38);
        public Color BorderColor = Color.FromArgb(61, 57, 60);

        public SilenceCardPanel()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Padding = new Padding(1);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(Parent == null ? SurfaceColor : Parent.BackColor);
            Rectangle bounds = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = SilenceDrawing.RoundedPath(bounds, 7))
            using (SolidBrush brush = new SolidBrush(SurfaceColor))
            using (Pen pen = new Pen(BorderColor))
            {
                e.Graphics.FillPath(brush, path);
                e.Graphics.DrawPath(pen, path);
            }
            base.OnPaint(e);
        }
    }

    internal sealed class SilenceActionButton : Button
    {
        public Color SurfaceColor = Color.FromArgb(43, 39, 41);
        public Color HoverColor = Color.FromArgb(54, 49, 52);
        public Color TextColor = Color.White;
        public Color BorderColor = Color.FromArgb(61, 57, 60);
        private bool hovered;

        public SilenceActionButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnMouseEnter(EventArgs e) { hovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hovered = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(Parent == null ? SurfaceColor : Parent.BackColor);
            Rectangle bounds = new Rectangle(0, 0, Width - 1, Height - 1);
            Color fill = Enabled ? (hovered ? HoverColor : SurfaceColor) : Color.FromArgb(35, 33, 34);
            using (GraphicsPath path = SilenceDrawing.RoundedPath(bounds, 6))
            using (SolidBrush brush = new SolidBrush(fill))
            using (Pen pen = new Pen(BorderColor))
            {
                e.Graphics.FillPath(brush, path);
                e.Graphics.DrawPath(pen, path);
            }
            TextRenderer.DrawText(e.Graphics, Text, Font, bounds, Enabled ? TextColor : Color.FromArgb(115, 111, 114),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            if (Focused)
                ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(bounds, -3, -3));
        }
    }

    internal static class SilenceDrawing
    {
        public static GraphicsPath RoundedPath(Rectangle bounds, int radius)
        {
            int diameter = Math.Max(2, radius * 2);
            GraphicsPath path = new GraphicsPath();
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
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

        protected override void WndProc(ref Message m)
        {
            const int WmNcHitTest = 0x0084;
            const int HtClient = 1;
            if (m.Msg == WmNcHitTest)
            {
                base.WndProc(ref m);
                if ((int)m.Result == HtClient)
                {
                    Point point = PointToClient(new Point((short)((long)m.LParam & 0xFFFF), (short)(((long)m.LParam >> 16) & 0xFFFF)));
                    const int grip = 7;
                    bool left = point.X < grip;
                    bool right = point.X >= ClientSize.Width - grip;
                    bool top = point.Y < grip;
                    bool bottom = point.Y >= ClientSize.Height - grip;
                    if (left && top) m.Result = (IntPtr)13;
                    else if (right && top) m.Result = (IntPtr)14;
                    else if (left && bottom) m.Result = (IntPtr)16;
                    else if (right && bottom) m.Result = (IntPtr)17;
                    else if (left) m.Result = (IntPtr)10;
                    else if (right) m.Result = (IntPtr)11;
                    else if (top) m.Result = (IntPtr)12;
                    else if (bottom) m.Result = (IntPtr)15;
                }
                return;
            }
            base.WndProc(ref m);
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
