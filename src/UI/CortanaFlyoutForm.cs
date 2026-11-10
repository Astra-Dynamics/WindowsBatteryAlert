using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using BatteryAlert.Models;
using BatteryAlert.Native;
using BatteryAlert.Services;

namespace BatteryAlert.UI
{
    public class CortanaFlyoutForm : Form
    {
        private readonly ConfigService _configService;
        private readonly BatteryMonitor _batteryMonitor;
        private readonly uint _showWindowMessage;

        // UI Controls
        private Label _lblAppTitle;
        private Label _lblSimBadge;
        private Label _btnClose;
        private CortanaCircleControl _circleControl;

        private PillButton _btnMasterToggle;
        private Label _lblStatus;
        private Label _btnResumeNow;

        private Label _lblPauseHeader;
        private PillButton _btnPause1h;
        private PillButton _btnPause2h;
        private PillButton _btnPauseTomorrow;
        private PillButton _btnPauseCustom;

        private Label _lblThresholdsHeader;
        private FlowLayoutPanel _pnlThresholds;
        private TextBox _txtAddThreshold;
        private PillButton _btnAddThreshold;
        private Label _lblInputError;

        public CortanaFlyoutForm(ConfigService configService, BatteryMonitor batteryMonitor, uint showWindowMessage)
        {
            _configService = configService;
            _batteryMonitor = batteryMonitor;
            _showWindowMessage = showWindowMessage;

            InitializeComponent();
            ApplyStyling();

            // Subscribe to model and monitor updates
            _configService.ConfigChanged += OnConfigChanged;
            _batteryMonitor.PowerStateChanged += OnPowerStateChanged;

            // Initial refresh
            RefreshAll();
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Size = new Size(330, 485);
            BackColor = Color.FromArgb(17, 17, 19);
            ShowInTaskbar = false;
            TopMost = true;
            KeyPreview = true;

            // Top Header
            _lblAppTitle = new Label
            {
                Text = "BatteryAlert",
                Font = new Font("Segoe UI Semibold", 9.5f),
                ForeColor = Color.FromArgb(235, 235, 235),
                Location = new Point(16, 12),
                AutoSize = true
            };

            _lblSimBadge = new Label
            {
                Text = "[SIMULATION - Click to Restart 85%]",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 185, 0),
                Location = new Point(96, 14),
                AutoSize = true,
                Cursor = Cursors.Hand,
                Visible = _batteryMonitor.IsSimulating
            };
            _lblSimBadge.Click += (s, e) =>
            {
                if (_batteryMonitor.IsSimulating)
                {
                    _batteryMonitor.ResetSimulation(85);
                }
            };

            _btnClose = new Label
            {
                Text = "✕",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(140, 140, 145),
                Location = new Point(300, 11),
                Size = new Size(20, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            _btnClose.MouseEnter += (s, e) => _btnClose.ForeColor = Color.White;
            _btnClose.MouseLeave += (s, e) => _btnClose.ForeColor = Color.FromArgb(140, 140, 145);
            _btnClose.Click += (s, e) => HideFlyout();

            // Centerpiece: Cortana Circle Arc
            _circleControl = new CortanaCircleControl
            {
                Location = new Point(85, 38),
                Size = new Size(160, 160)
            };

            // Master Toggle Button
            _btnMasterToggle = new PillButton
            {
                Text = "NOTIFICATIONS: ON",
                Font = new Font("Segoe UI Semibold", 8.5f),
                Location = new Point(55, 200),
                Size = new Size(220, 30),
                IsActive = true
            };
            _btnMasterToggle.Click += OnMasterToggleClicked;

            // Status label & Resume now button
            _lblStatus = new Label
            {
                Text = "● Active - Monitoring charge level",
                Font = new Font("Segoe UI", 8f),
                ForeColor = Color.FromArgb(140, 140, 145),
                Location = new Point(16, 234),
                Size = new Size(210, 18),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _btnResumeNow = new Label
            {
                Text = "Resume now",
                Font = new Font("Segoe UI", 8f, FontStyle.Underline),
                ForeColor = Color.FromArgb(0, 216, 246),
                Location = new Point(230, 234),
                Size = new Size(84, 18),
                TextAlign = ContentAlignment.MiddleRight,
                Cursor = Cursors.Hand,
                Visible = false
            };
            _btnResumeNow.Click += (s, e) => _configService.ResumeNow();

            // Pause section
            _lblPauseHeader = new Label
            {
                Text = "PAUSE UNTIL",
                Font = new Font("Segoe UI Semibold", 7.5f),
                ForeColor = Color.FromArgb(110, 110, 120),
                Location = new Point(16, 258),
                AutoSize = true
            };

            _btnPause1h = new PillButton
            {
                Text = "1 h",
                Font = new Font("Segoe UI", 8f),
                Location = new Point(16, 276),
                Size = new Size(54, 26)
            };
            _btnPause1h.Click += (s, e) => _configService.SetPauseUntil(DateTime.Now.AddHours(1));

            _btnPause2h = new PillButton
            {
                Text = "2 h",
                Font = new Font("Segoe UI", 8f),
                Location = new Point(76, 276),
                Size = new Size(54, 26)
            };
            _btnPause2h.Click += (s, e) => _configService.SetPauseUntil(DateTime.Now.AddHours(2));

            _btnPauseTomorrow = new PillButton
            {
                Text = "Tom 8 AM",
                Font = new Font("Segoe UI", 8f),
                Location = new Point(136, 276),
                Size = new Size(86, 26)
            };
            _btnPauseTomorrow.Click += (s, e) =>
            {
                DateTime tom8am = DateTime.Today.AddDays(1).AddHours(8);
                if (tom8am <= DateTime.Now) tom8am = tom8am.AddDays(1);
                _configService.SetPauseUntil(tom8am);
            };

            _btnPauseCustom = new PillButton
            {
                Text = "Custom...",
                Font = new Font("Segoe UI", 8f),
                Location = new Point(228, 276),
                Size = new Size(86, 26)
            };
            _btnPauseCustom.Click += OnPauseCustomClicked;

            // Thresholds section
            _lblThresholdsHeader = new Label
            {
                Text = "ALERT THRESHOLDS",
                Font = new Font("Segoe UI Semibold", 7.5f),
                ForeColor = Color.FromArgb(110, 110, 120),
                Location = new Point(16, 314),
                AutoSize = true
            };

            _pnlThresholds = new FlowLayoutPanel
            {
                Location = new Point(16, 332),
                Size = new Size(298, 74),
                AutoScroll = true,
                BackColor = Color.FromArgb(20, 20, 24),
                Padding = new Padding(4)
            };

            // Add custom threshold controls
            _txtAddThreshold = new TextBox
            {
                Location = new Point(16, 416),
                Size = new Size(110, 24),
                BackColor = Color.FromArgb(28, 28, 34),
                ForeColor = Color.FromArgb(240, 240, 240),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9f)
            };
            _txtAddThreshold.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    TryAddThreshold();
                }
            };

            _btnAddThreshold = new PillButton
            {
                Text = "+ Add",
                Font = new Font("Segoe UI Semibold", 8.5f),
                Location = new Point(134, 415),
                Size = new Size(68, 26),
                IsActive = true
            };
            _btnAddThreshold.Click += (s, e) => TryAddThreshold();

            _lblInputError = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 7.5f),
                ForeColor = Color.FromArgb(255, 110, 110),
                Location = new Point(16, 446),
                Size = new Size(298, 18)
            };

            // Add controls
            Controls.Add(_lblAppTitle);
            Controls.Add(_lblSimBadge);
            Controls.Add(_btnClose);
            Controls.Add(_circleControl);
            Controls.Add(_btnMasterToggle);
            Controls.Add(_lblStatus);
            Controls.Add(_btnResumeNow);
            Controls.Add(_lblPauseHeader);
            Controls.Add(_btnPause1h);
            Controls.Add(_btnPause2h);
            Controls.Add(_btnPauseTomorrow);
            Controls.Add(_btnPauseCustom);
            Controls.Add(_lblThresholdsHeader);
            Controls.Add(_pnlThresholds);
            Controls.Add(_txtAddThreshold);
            Controls.Add(_btnAddThreshold);
            Controls.Add(_lblInputError);

            ResumeLayout(false);
            PerformLayout();
        }

        private void ApplyStyling()
        {
            // Apply Windows 10/11 dark mode & rounded corners
            try
            {
                int darkMode = 1;
                NativeMethods.DwmSetWindowAttribute(Handle, NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));
                int roundCorner = NativeMethods.DWMWCP_ROUND;
                NativeMethods.DwmSetWindowAttribute(Handle, NativeMethods.DWMWA_WINDOW_CORNER_PREFERENCE, ref roundCorner, sizeof(int));
            }
            catch { }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Escape)
            {
                HideFlyout();
            }
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            // Hide flyout when user clicks away
            HideFlyout();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                HideFlyout();
            }
            else
            {
                base.OnFormClosing(e);
            }
        }

        public void ToggleVisibility()
        {
            if (Visible)
            {
                HideFlyout();
            }
            else
            {
                ShowFlyout();
            }
        }

        public void ShowFlyout()
        {
            PositionNearTray();
            RefreshAll();
            if (WindowState == FormWindowState.Minimized)
            {
                WindowState = FormWindowState.Normal;
            }
            Show();
            Activate();
            BringToFront();
            NativeMethods.SetForegroundWindow(Handle);
        }

        public void HideFlyout()
        {
            if (Visible)
            {
                Hide();
                // Free working set when hidden to minimize RAM
                NativeMethods.TrimWorkingSet();
            }
        }

        private void PositionNearTray()
        {
            Rectangle workingArea = Screen.PrimaryScreen.WorkingArea;
            int margin = 12;
            int x = workingArea.Right - Width - margin;
            int y = workingArea.Bottom - Height - margin;
            Location = new Point(x, y);
        }

        private void OnMasterToggleClicked(object sender, EventArgs e)
        {
            bool current = _configService.Config.NotificationsEnabled;
            _configService.SetNotificationsEnabled(!current);
        }

        private void OnPauseCustomClicked(object sender, EventArgs e)
        {
            using (CustomTimeDialog dlg = new CustomTimeDialog())
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _configService.SetPauseUntil(dlg.SelectedTime);
                }
            }
        }

        private void TryAddThreshold()
        {
            _lblInputError.Text = "";
            string text = _txtAddThreshold.Text.Trim();
            int val;
            if (!int.TryParse(text, out val))
            {
                _lblInputError.Text = "Enter a whole number between 1 and 100.";
                return;
            }

            if (val < 1 || val > 100)
            {
                _lblInputError.Text = "Threshold must be between 1% and 100%.";
                return;
            }

            if (AppConfig.IsDefault(val))
            {
                _lblInputError.Text = string.Format("{0}% is already a default locked threshold.", val);
                return;
            }

            if (_configService.Config.CustomThresholds != null && _configService.Config.CustomThresholds.Contains(val))
            {
                _lblInputError.Text = string.Format("{0}% is already in your thresholds list.", val);
                return;
            }

            if (_configService.AddCustomThreshold(val))
            {
                _txtAddThreshold.Text = "";
                _lblInputError.Text = "";
            }
        }

        private void OnConfigChanged()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(RefreshUI));
            }
            else
            {
                RefreshUI();
            }
        }

        private void OnPowerStateChanged(PowerSnapshot snapshot)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdatePowerDisplay(snapshot)));
            }
            else
            {
                UpdatePowerDisplay(snapshot);
            }
        }

        private void RefreshAll()
        {
            PowerSnapshot snapshot = _batteryMonitor.GetCurrentSnapshot();
            UpdatePowerDisplay(snapshot);
            RefreshUI();
        }

        private void UpdatePowerDisplay(PowerSnapshot snapshot)
        {
            _circleControl.Percent = snapshot.Percent;
            _circleControl.IsPluggedIn = snapshot.IsPluggedIn;
            _circleControl.IsCharging = snapshot.IsCharging;
        }

        private void RefreshUI()
        {
            AppConfig config = _configService.Config;

            // Master Toggle Button
            if (config.NotificationsEnabled)
            {
                _btnMasterToggle.Text = "NOTIFICATIONS: ON";
                _btnMasterToggle.IsActive = true;
                _btnMasterToggle.AccentColor = Color.FromArgb(0, 216, 246);
            }
            else
            {
                _btnMasterToggle.Text = "NOTIFICATIONS: OFF";
                _btnMasterToggle.IsActive = false;
            }

            // Status label & Resume now
            if (!config.NotificationsEnabled)
            {
                _lblStatus.Text = "✕ Silenced indefinitely";
                _lblStatus.ForeColor = Color.FromArgb(120, 120, 125);
                _btnResumeNow.Visible = false;
            }
            else if (config.IsPaused)
            {
                DateTime localPause = config.PauseUntilUtc.Value.ToLocalTime();
                string timeStr = localPause.Date == DateTime.Today
                    ? localPause.ToString("h:mm tt")
                    : localPause.ToString("ddd h:mm tt");

                _lblStatus.Text = "⏸ Paused until " + timeStr;
                _lblStatus.ForeColor = Color.FromArgb(255, 185, 0);
                _btnResumeNow.Visible = true;
            }
            else
            {
                _lblStatus.Text = "● Active - Monitoring charge level";
                _lblStatus.ForeColor = Color.FromArgb(0, 216, 246);
                _btnResumeNow.Visible = false;
            }

            // Rebuild Threshold Chips
            _pnlThresholds.SuspendLayout();
            _pnlThresholds.Controls.Clear();

            List<int> effective = config.GetEffectiveThresholds();
            foreach (int t in effective)
            {
                bool isLocked = AppConfig.IsDefault(t);
                ThresholdChipControl chip = new ThresholdChipControl(t, isLocked);
                if (!isLocked)
                {
                    chip.DeleteRequested += (val) => _configService.RemoveCustomThreshold(val);
                }
                _pnlThresholds.Controls.Add(chip);
            }

            _pnlThresholds.ResumeLayout();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            // Draw subtle outer border
            using (Pen borderPen = new Pen(Color.FromArgb(40, 40, 48), 1.2f))
            {
                e.Graphics.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == (int)_showWindowMessage && _showWindowMessage != 0)
            {
                // Second instance requested window open
                ShowFlyout();
                return;
            }

            if (m.Msg == NativeMethods.WM_POWERBROADCAST)
            {
                _batteryMonitor.CheckStatus();
            }

            base.WndProc(ref m);
        }
    }
}
