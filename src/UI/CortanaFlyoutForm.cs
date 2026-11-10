using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
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

        private Panel _pnlAddInput;
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
            Size = new Size(340, 500);
            BackColor = Color.FromArgb(17, 17, 20);
            ShowInTaskbar = false;
            TopMost = true;
            KeyPreview = true;

            // Top Header
            _lblAppTitle = new Label
            {
                Text = "⚡ BatteryAlert",
                Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(240, 240, 245),
                Location = new Point(16, 12),
                AutoSize = true
            };

            _lblSimBadge = new Label
            {
                Text = "[SIMULATION - Restart 85%]",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(255, 190, 20),
                Location = new Point(125, 14),
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
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(145, 145, 155),
                Location = new Point(306, 10),
                Size = new Size(22, 22),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            _btnClose.MouseEnter += (s, e) => _btnClose.ForeColor = Color.White;
            _btnClose.MouseLeave += (s, e) => _btnClose.ForeColor = Color.FromArgb(145, 145, 155);
            _btnClose.Click += (s, e) => HideFlyout();

            // Centerpiece: Cortana Circle Arc
            _circleControl = new CortanaCircleControl
            {
                Location = new Point(85, 36),
                Size = new Size(170, 170)
            };

            // Master Toggle Button (Cortana Outline Style)
            _btnMasterToggle = new PillButton
            {
                Text = "NOTIFICATIONS: ON",
                Font = new Font("Segoe UI Semibold", 9f, FontStyle.Regular, GraphicsUnit.Point),
                Location = new Point(50, 212),
                Size = new Size(240, 32),
                IsOutlineStyle = true,
                IsActive = true
            };
            _btnMasterToggle.Click += OnMasterToggleClicked;

            // Status label & Resume now button
            _lblStatus = new Label
            {
                Text = "● Active - Monitoring charge level",
                Font = new Font("Segoe UI", 8.2f, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(145, 145, 155),
                Location = new Point(16, 250),
                Size = new Size(215, 18),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _btnResumeNow = new Label
            {
                Text = "Resume now",
                Font = new Font("Segoe UI", 8.2f, FontStyle.Underline, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(0, 216, 246),
                Location = new Point(234, 250),
                Size = new Size(90, 18),
                TextAlign = ContentAlignment.MiddleRight,
                Cursor = Cursors.Hand,
                Visible = false
            };
            _btnResumeNow.Click += (s, e) => _configService.ResumeNow();

            // Pause section
            _lblPauseHeader = new Label
            {
                Text = "PAUSE UNTIL",
                Font = new Font("Segoe UI Semibold", 7.5f, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(115, 115, 130),
                Location = new Point(16, 274),
                AutoSize = true
            };

            _btnPause1h = new PillButton
            {
                Text = "1 h",
                Font = new Font("Segoe UI", 8.2f, FontStyle.Regular, GraphicsUnit.Point),
                Location = new Point(16, 292),
                Size = new Size(62, 28)
            };
            _btnPause1h.Click += (s, e) => _configService.SetPauseUntil(DateTime.Now.AddHours(1));

            _btnPause2h = new PillButton
            {
                Text = "2 h",
                Font = new Font("Segoe UI", 8.2f, FontStyle.Regular, GraphicsUnit.Point),
                Location = new Point(84, 292),
                Size = new Size(62, 28)
            };
            _btnPause2h.Click += (s, e) => _configService.SetPauseUntil(DateTime.Now.AddHours(2));

            _btnPauseTomorrow = new PillButton
            {
                Text = "Tom 8 AM",
                Font = new Font("Segoe UI", 8.2f, FontStyle.Regular, GraphicsUnit.Point),
                Location = new Point(152, 292),
                Size = new Size(92, 28)
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
                Font = new Font("Segoe UI", 8.2f, FontStyle.Regular, GraphicsUnit.Point),
                Location = new Point(250, 292),
                Size = new Size(74, 28)
            };
            _btnPauseCustom.Click += OnPauseCustomClicked;

            // Thresholds section
            _lblThresholdsHeader = new Label
            {
                Text = "ALERT THRESHOLDS",
                Font = new Font("Segoe UI Semibold", 7.5f, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(115, 115, 130),
                Location = new Point(16, 330),
                AutoSize = true
            };

            _pnlThresholds = new FlowLayoutPanel
            {
                Location = new Point(14, 348),
                Size = new Size(312, 70),
                AutoScroll = true,
                BackColor = Color.Transparent,
                Padding = new Padding(0)
            };

            // Unified composite Add Threshold bar
            _pnlAddInput = new Panel
            {
                Location = new Point(16, 424),
                Size = new Size(308, 34),
                BackColor = Color.FromArgb(24, 25, 30)
            };
            _pnlAddInput.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
                Rectangle rect = new Rectangle(0, 0, _pnlAddInput.Width - 1, _pnlAddInput.Height - 1);
                using (GraphicsPath path = GetRoundedRectangle(rect, 8))
                using (Pen borderPen = new Pen(Color.FromArgb(48, 50, 60), 1.1f))
                {
                    e.Graphics.DrawPath(borderPen, path);
                }
            };

            _txtAddThreshold = new TextBox
            {
                Location = new Point(12, 8),
                Size = new Size(225, 20),
                BackColor = Color.FromArgb(24, 25, 30),
                ForeColor = Color.FromArgb(240, 240, 245),
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point)
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
                Font = new Font("Segoe UI Semibold", 8.2f, FontStyle.Regular, GraphicsUnit.Point),
                Location = new Point(245, 4),
                Size = new Size(58, 26),
                IsActive = true
            };
            _btnAddThreshold.Click += (s, e) => TryAddThreshold();

            _pnlAddInput.Controls.Add(_txtAddThreshold);
            _pnlAddInput.Controls.Add(_btnAddThreshold);

            _lblInputError = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(255, 110, 110),
                Location = new Point(16, 464),
                Size = new Size(308, 18)
            };

            // Add controls to form
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
            Controls.Add(_pnlAddInput);
            Controls.Add(_lblInputError);

            ResumeLayout(false);
            PerformLayout();
        }

        private void ApplyStyling()
        {
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
                _lblStatus.ForeColor = Color.FromArgb(125, 125, 135);
                _btnResumeNow.Visible = false;
            }
            else if (config.IsPaused)
            {
                DateTime localPause = config.PauseUntilUtc.Value.ToLocalTime();
                string timeStr = localPause.Date == DateTime.Today
                    ? localPause.ToString("h:mm tt")
                    : localPause.ToString("ddd h:mm tt");

                _lblStatus.Text = "⏸ Paused until " + timeStr;
                _lblStatus.ForeColor = Color.FromArgb(255, 190, 20);
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
                chip.Margin = new Padding(2, 2, 4, 4);
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
            e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
            using (Pen borderPen = new Pen(Color.FromArgb(42, 44, 52), 1.2f))
            {
                e.Graphics.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
            }
        }

        private static GraphicsPath GetRoundedRectangle(Rectangle bounds, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;
            Rectangle arc = new Rectangle(bounds.Location, new Size(diameter, diameter));

            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
