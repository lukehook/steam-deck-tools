using CommonHelpers;
using ExternalHelpers;
using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;

namespace SteamController
{
    internal class Controller : IDisposable
    {
        public const String Title = "Steam Controller";
        public static readonly String TitleWithVersion = Title + " v" + Application.ProductVersion.ToString();

        public const int ControllerDelayAfterResumeMs = 1000;

        public static readonly Dictionary<String, Profiles.Profile> PreconfiguredUserProfiles = new Dictionary<String, Profiles.Profile>()
        {
            { "*.desktop.cs", new Profiles.Predefined.DesktopProfile() { Name = "Desktop" } },
            { "*.x360.cs", new Profiles.Predefined.X360HapticProfile() { Name = "X360" } }
        };

        Container components = new Container();
        NotifyIcon notifyIcon;
        StartupManager startupManager = new StartupManager(Title);

        Context context = new Context()
        {
            Profiles = {
                new Profiles.Predefined.DesktopProfile() { Name = "Desktop" },
                new Profiles.Predefined.SteamProfile() { Name = "Steam", Visible = false },
                new Profiles.Predefined.SteamWithShorcutsProfile() { Name = "Steam with Shortcuts", Visible = false },
                new Profiles.Predefined.X360HapticProfile() { Name = "X360" }
            },
            Managers = {
                new Managers.ProcessManager(),
                new Managers.SteamManager(),
                new Managers.RTSSManager(),
                new Managers.ProfileSwitcher(),
                new Managers.SteamConfigsManager(),
                new Managers.SharedDataManager(),
                new Managers.SASManager()
            }
        };

        static Controller()
        {
            Dependencies.ValidateHidapi(TitleWithVersion);
        }

        public Controller()
        {
            Instance.OnUninstall(() =>
            {
                Helpers.SteamConfiguration.KillSteam();
                Helpers.SteamConfiguration.WaitForSteamClose(5000);
                Helpers.SteamConfiguration.BackupSteamConfig();

                var steamControllerUpdate = Helpers.SteamConfiguration.UpdateControllerBlacklist(
                    Devices.SteamController.VendorID, Devices.SteamController.ProductID, false
                );
                var x360ControllerUpdate = Helpers.SteamConfiguration.UpdateControllerBlacklist(
                    Devices.Xbox360Controller.VendorID, Devices.Xbox360Controller.ProductID, false
                );
                Settings.Default.EnableSteamDetection = false;
                startupManager.Startup = false;
            });

            Instance.RunOnce(TitleWithVersion, "Global\\SteamController");
            Instance.RunUpdater(TitleWithVersion);

            if (Instance.WantsRunOnStartup)
                startupManager.Startup = true;

            notifyIcon = new NotifyIcon(components);
            notifyIcon.Icon = WindowsDarkMode.IsDarkModeEnabled ? Resources.microsoft_xbox_controller_off_white : Resources.microsoft_xbox_controller_off;
            notifyIcon.Text = TitleWithVersion;
            notifyIcon.Visible = true;

#if DEBUG
            foreach (var profile in Profiles.Dynamic.RoslynDynamicProfile.GetUserProfiles(PreconfiguredUserProfiles))
            {
                profile.ErrorsChanged += (errors) =>
                {
                    notifyIcon.ShowBalloonTip(
                        3000, profile.Name,
                        String.Join("\n", errors),
                        ToolTipIcon.Error
                    );
                };
                profile.Compile();
                profile.Watch();
                context.Profiles.Add(profile);
            }
#endif

            // Set available profiles
            ProfilesSettings.Helpers.ProfileStringConverter.Profiles = context.Profiles.
                Where((profile) => profile.Visible).
                Select((profile) => profile.Name).ToArray();

            var contextMenu = new ContextMenuStrip(components);

            var enabledItem = new ToolStripMenuItem("&Enabled");
            enabledItem.Click += delegate { context.RequestEnable = !context.RequestEnable; };
            contextMenu.Opening += delegate { enabledItem.Checked = context.RequestEnable; };
            contextMenu.Items.Add(enabledItem);
            contextMenu.Items.Add(new ToolStripSeparator());

            foreach (var profile in context.Profiles)
            {
                if (profile.Name == "" || !profile.Visible)
                    continue;

                var profileItem = new ToolStripMenuItem(profile.Name);
                profileItem.Click += delegate { context.SelectProfile(profile.Name); };
                contextMenu.Opening += delegate
                {
                    profileItem.Checked = context.CurrentProfile == profile;
                    profileItem.ToolTipText = String.Join("\n", profile.Errors ?? new string[0]);
                    profileItem.Enabled = profile.Errors is null;
                };
                contextMenu.Items.Add(profileItem);
            }

            contextMenu.Items.Add(new ToolStripSeparator());

            var setupSteamItem = new ToolStripMenuItem("Setup &Steam");
            setupSteamItem.Click += delegate { SetupSteam(true); };
            contextMenu.Items.Add(setupSteamItem);
            contextMenu.Items.Add(new ToolStripSeparator());

            var settingsItem = contextMenu.Items.Add("&Settings");
            settingsItem.Click += Settings_Click;

            var shortcutsItem = contextMenu.Items.Add("&Shortcuts");
            shortcutsItem.Click += delegate { Process.Start("explorer.exe", "https://steam-deck-tools.ayufan.dev/shortcuts.html"); };

            contextMenu.Items.Add(new ToolStripSeparator());

            if (startupManager.IsAvailable)
            {
                var startupItem = new ToolStripMenuItem("Run On Startup");
                startupItem.Checked = startupManager.Startup;
                startupItem.Click += delegate { startupItem.Checked = startupManager.Startup = !startupManager.Startup; };
                contextMenu.Items.Add(startupItem);
            }

            var checkForUpdatesItem = contextMenu.Items.Add("&Check for Updates");
            checkForUpdatesItem.Click += delegate { Instance.RunUpdater(TitleWithVersion, true); };

            var helpItem = contextMenu.Items.Add("&Help");
            helpItem.Click += delegate { Process.Start("explorer.exe", "https://steam-deck-tools.ayufan.dev"); };

            contextMenu.Items.Add(new ToolStripSeparator());

            var exitItem = contextMenu.Items.Add("&Exit");
            exitItem.Click += delegate { Application.Exit(); };

            notifyIcon.ContextMenuStrip = contextMenu;

            var contextStateUpdate = new System.Windows.Forms.Timer(components);
            contextStateUpdate.Interval = 250;
            contextStateUpdate.Enabled = true;
            contextStateUpdate.Tick += ContextStateUpdate_Tick;

            context.SelectDefault = () =>
            {
                if (!context.SelectProfile(Settings.Default.DefaultProfile, true))
                    context.SelectProfile(context.Profiles.First().Name, true);
            };
            context.BackToDefault();

            context.ProfileChanged += (profile) =>
            {
#if false
                notifyIcon.ShowBalloonTip(
                    1000,
                    TitleWithVersion,
                    String.Format("Selected profile: {0}", profile.Name),
                    ToolTipIcon.Info
                );
#endif
            };

            SetupSteam(false);

            context.Start();

            Microsoft.Win32.SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
        }

        private void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            Log.TraceLine("SystemEvents_PowerModeChanged: {0}", e.Mode);

            switch (e.Mode)
            {
                case PowerModes.Suspend:
                    context.Stop();
                    break;

                case PowerModes.Resume:
                    context.Start(ControllerDelayAfterResumeMs);
                    break;
            }
        }

        private void ContextStateUpdate_Tick(object? sender, EventArgs e)
        {
            context.Tick();

            var isDesktop = context.CurrentProfile?.IsDesktop ?? false;
            var monitorOffIco = WindowsDarkMode.IsDarkModeEnabled ? Resources.monitor_off_white : Resources.monitor_off;
            var monitorOnIco = WindowsDarkMode.IsDarkModeEnabled ? Resources.monitor_white : Resources.monitor;
            var controllerOffIco = WindowsDarkMode.IsDarkModeEnabled ?
                Resources.microsoft_xbox_controller_off_white :
                Resources.microsoft_xbox_controller_off;
            var controllerOnIco = WindowsDarkMode.IsDarkModeEnabled ?
                Resources.microsoft_xbox_controller_white :
                Resources.microsoft_xbox_controller;

            if (!context.KeyboardMouseValid)
            {
                notifyIcon.Text = TitleWithVersion + ". Cannot send input.";
                notifyIcon.Icon = Resources.microsoft_xbox_controller_off_red;
            }
            else if (!context.X360.Valid)
            {
                notifyIcon.Text = TitleWithVersion + ". Missing ViGEm?";
                notifyIcon.Icon = Resources.microsoft_xbox_controller_red;
            }
            else if (context.Enabled)
            {
                if (context.State.SteamUsesSteamInput)
                {
                    notifyIcon.Icon = isDesktop ? monitorOffIco : controllerOffIco;
                    notifyIcon.Text = TitleWithVersion + ". Steam uses Steam Input";
                }
                else
                {
                    notifyIcon.Icon = isDesktop ? monitorOnIco : controllerOnIco;
                    notifyIcon.Text = TitleWithVersion;
                }

                var profile = context.CurrentProfile;
                if (profile is not null)
                    notifyIcon.Text = TitleWithVersion + ". Profile: " + profile.Name;
            }
            else
            {
                notifyIcon.Icon = isDesktop ? monitorOffIco : controllerOffIco;
                notifyIcon.Text = TitleWithVersion + ". Disabled";
            }

            notifyIcon.Text += String.Format(". Updates: {0}/s", context.UpdatesPerSec);
        }

        public void Dispose()
        {
            Microsoft.Win32.SystemEvents.PowerModeChanged -= SystemEvents_PowerModeChanged;
            notifyIcon.Visible = false;
            context.Stop();
            using (context) { }
        }

        public void SetupSteam(bool always)
        {
            var blacklistedSteamController = Helpers.SteamConfiguration.IsControllerBlacklisted(
                Devices.SteamController.VendorID,
                Devices.SteamController.ProductID
            );
            var blacklistedX360Controller = Helpers.SteamConfiguration.IsControllerBlacklisted(
                Devices.Xbox360Controller.VendorID,
                Devices.Xbox360Controller.ProductID
            );

            if (blacklistedSteamController is null || blacklistedX360Controller is null)
            {
                // Appears that Steam is not installed
                if (always)
                {
                    MessageBox.Show("Steam appears not to be installed.", TitleWithVersion, MessageBoxButtons.OK);
                }
                return;
            }

            Application.DoEvents();

            var page = new TaskDialogPage();
            page.Caption = TitleWithVersion;
            page.AllowCancel = true;

            var useX360Controller = page.RadioButtons.Add("Use &X360 Controller with Steam (preferred)");
            useX360Controller.Text += "\n- Will always use X360 controller.";
            useX360Controller.Checked = Settings.Default.EnableSteamDetection == true &&
                blacklistedSteamController == true &&
                blacklistedX360Controller == false;

            var useSteamInput = page.RadioButtons.Add("Use &Steam Input with Steam (requires configuration)");
            useSteamInput.Text += "\n- Will try to use Steam controls.";
            useSteamInput.Text += "\n- Does REQUIRE disabling DESKTOP MODE shortcuts in Steam.";
            useSteamInput.Text += "\n- Click Help for more information.";
            useSteamInput.Checked = Settings.Default.EnableSteamDetection == true &&
                blacklistedSteamController == false &&
                blacklistedX360Controller == true;

            var ignoreSteam = page.RadioButtons.Add("&Ignore Steam (only if you know why you need it)");
            ignoreSteam.Text += "\n- Will revert all previously made changes.";
            ignoreSteam.Checked = Settings.Default.EnableSteamDetection == false;

            bool valid = ignoreSteam.Checked || useX360Controller.Checked || useSteamInput.Checked;

            // If everything is OK, on subsequent runs nothing to configure
            if (valid && !always)
                return;

            if (valid || Settings.Default.EnableSteamDetection == null)
            {
                page.Heading = "Steam Controller Setup";
                page.Text = "To use Steam Controller with Steam you need to configure it first.";
                page.Icon = TaskDialogIcon.ShieldBlueBar;
            }
            else
            {
                page.Heading = "Steam Controller Setup - Configuration Lost";
                page.Text = "Configure your Steam Controller again.";
                page.Icon = TaskDialogIcon.ShieldWarningYellowBar;
            }

            var continueButton = new TaskDialogButton("Continue") { ShowShieldIcon = true };

            page.Buttons.Add(continueButton);
            page.Buttons.Add(TaskDialogButton.Cancel);
            page.Buttons.Add(TaskDialogButton.Help);

            page.Footnote = new TaskDialogFootnote();
            page.Footnote.Text = "This will change Steam configuration. ";
            page.Footnote.Text += "Close Steam before confirming as otherwise Steam will be forcefully closed.";
            page.Footnote.Icon = TaskDialogIcon.Warning;

            page.HelpRequest += delegate
            {
                try { System.Diagnostics.Process.Start("explorer.exe", "https://steam-deck-tools.ayufan.dev/steam-controller"); }
                catch { }
            };

            var result = TaskDialog.ShowDialog(new Form { TopMost = true }, page, TaskDialogStartupLocation.CenterScreen);
            if (result != continueButton)
                return;

            Helpers.SteamConfiguration.KillSteam();
            Helpers.SteamConfiguration.WaitForSteamClose(5000);
            Helpers.SteamConfiguration.BackupSteamConfig();

            var steamControllerUpdate = Helpers.SteamConfiguration.UpdateControllerBlacklist(
                Devices.SteamController.VendorID,
                Devices.SteamController.ProductID,
                useX360Controller.Checked
            );
            var x360ControllerUpdate = Helpers.SteamConfiguration.UpdateControllerBlacklist(
                Devices.Xbox360Controller.VendorID,
                Devices.Xbox360Controller.ProductID,
                useSteamInput.Checked
            );
            Settings.Default.EnableSteamDetection = useSteamInput.Checked || useX360Controller.Checked;

            if (steamControllerUpdate && x360ControllerUpdate)
            {
                notifyIcon.ShowBalloonTip(
                    3000, TitleWithVersion,
                    "Steam Configuration changed. You can start Steam now.",
                    ToolTipIcon.Info
                );
            }
            else
            {
                notifyIcon.ShowBalloonTip(
                    3000, TitleWithVersion,
                    "Steam Configuration was not updated. Maybe Steam is open?",
                    ToolTipIcon.Warning
                );
            }
        }

        private void Settings_Click(object? sender, EventArgs e)
        {
            var form = new Form()
            {
                Text = TitleWithVersion + " Settings",
                StartPosition = FormStartPosition.CenterScreen,
                Size = new Size(400, 500),
                AutoScaleMode = AutoScaleMode.Font,
                AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F)
            };

            var propertyGrid = new PropertyGrid()
            {
                Dock = DockStyle.Fill,
                ToolbarVisible = false,
                LargeButtons = true,
                SelectedObject = new
                {
                    Desktop = ProfilesSettings.DesktopPanelSettings.Default,
                    X360 = ProfilesSettings.X360BackPanelSettings.Default,
                    X360Haptic = ProfilesSettings.X360HapticSettings.Default,
                    Application = Settings.Default,
                    DEBUG = SettingsDebug.Default
                }
            };

            var helpLabel = new Label()
            {
                Cursor = Cursors.Hand,
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold | FontStyle.Underline),
                ForeColor = SystemColors.HotTrack,
                Text = "https://steam-deck-tools.ayufan.dev",
                TextAlign = ContentAlignment.MiddleCenter
            };

            var donateLabel = new Label()
            {
                Cursor = Cursors.Hand,
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Text = String.Join("\n",
                    "This project is provided free of charge, but development of it is not free:",
                    "- Consider donating to keep this project alive.",
                    "- Donating also helps to fund new features."
                ),
                TextAlign = ContentAlignment.MiddleLeft,
                Height = 100
            };

            helpLabel.Click += delegate { Process.Start("explorer.exe", "https://steam-deck-tools.ayufan.dev"); };
            donateLabel.Click += delegate { Process.Start("explorer.exe", "https://steam-deck-tools.ayufan.dev/#help-this-project"); };
            propertyGrid.ExpandAllGridItems();

            form.Controls.Add(propertyGrid);
            form.Controls.Add(donateLabel);
            form.Controls.Add(helpLabel);
            form.ShowDialog();
        }
    }
}
