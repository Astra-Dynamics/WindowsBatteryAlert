using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using BatteryAlert.Models;
using BatteryAlert.Native;
using BatteryAlert.Services;

namespace BatteryAlert.UI
{
    public class TrayIconController : IDisposable
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly ConfigService _configService;
        private BatteryMonitor _batteryMonitor;
        private readonly Action _toggleFlyoutAction;

        private ContextMenuStrip _contextMenu;
        private ToolStripMenuItem _itemHeader;
        private ToolStripMenuItem _itemOpen;
        private ToolStripMenuItem _itemNotificationsToggle;
        private ToolStripMenuItem _itemPauseMenu;
        private ToolStripMenuItem _itemPause1h;
        private ToolStripMenuItem _itemPause2h;
        private ToolStripMenuItem _itemPauseTomorrow;
        private ToolStripMenuItem _itemResumeNow;
        private ToolStripMenuItem _itemStartupToggle;
        private ToolStripMenuItem _itemQuit;

        private IntPtr _lastHIcon = IntPtr.Zero;
        private bool _isDisposed;

        public NotifyIcon NotifyIcon
        {
            get { return _notifyIcon; }
        }

        public TrayIconController(ConfigService configService, Action toggleFlyoutAction)
        {
            _configService = configService;
            _toggleFlyoutAction = toggleFlyoutAction;

            _notifyIcon = new NotifyIcon();
            BuildContextMenu();

            _notifyIcon.ContextMenuStrip = _contextMenu;
            _notifyIcon.Visible = true;
            _notifyIcon.MouseClick += OnTrayIconMouseClick;

            _configService.ConfigChanged += UpdateTrayState;
        }

        public void SetBatteryMonitor(BatteryMonitor monitor)
        {
            _batteryMonitor = monitor;
            if (_batteryMonitor != null)
            {
                _batteryMonitor.PowerStateChanged += (snapshot) => UpdateTrayState();
            }
            UpdateTrayState();
        }

        private void BuildContextMenu()
        {
            _contextMenu = new ContextMenuStrip();
            _contextMenu.RenderMode = ToolStripRenderMode.System;

            _itemHeader = new ToolStripMenuItem("BatteryAlert");
            _itemHeader.Enabled = false;

            _itemOpen = new ToolStripMenuItem("Open BatteryAlert");
            _itemOpen.Font = new Font(_contextMenu.Font, FontStyle.Bold);
            _itemOpen.Click += (s, e) => _toggleFlyoutAction();

            _itemNotificationsToggle = new ToolStripMenuItem("Notifications Enabled");
            _itemNotificationsToggle.Click += (s, e) =>
            {
                _configService.SetNotificationsEnabled(!_configService.Config.NotificationsEnabled);
            };

            // Pause submenu
            _itemPauseMenu = new ToolStripMenuItem("Pause Notifications");
            _itemPause1h = new ToolStripMenuItem("Pause for 1 hour", null, (s, e) => _configService.SetPauseUntil(DateTime.Now.AddHours(1)));
            _itemPause2h = new ToolStripMenuItem("Pause for 2 hours", null, (s, e) => _configService.SetPauseUntil(DateTime.Now.AddHours(2)));
            _itemPauseTomorrow = new ToolStripMenuItem("Pause until tomorrow 8:00 AM", null, (s, e) =>
            {
                DateTime tom8am = DateTime.Today.AddDays(1).AddHours(8);
                if (tom8am <= DateTime.Now) tom8am = tom8am.AddDays(1);
                _configService.SetPauseUntil(tom8am);
            });
            _itemResumeNow = new ToolStripMenuItem("Resume now", null, (s, e) => _configService.ResumeNow());

            _itemPauseMenu.DropDownItems.Add(_itemPause1h);
            _itemPauseMenu.DropDownItems.Add(_itemPause2h);
            _itemPauseMenu.DropDownItems.Add(_itemPauseTomorrow);
            _itemPauseMenu.DropDownItems.Add(new ToolStripSeparator());
            _itemPauseMenu.DropDownItems.Add(_itemResumeNow);

            _itemStartupToggle = new ToolStripMenuItem("Start with Windows");
            _itemStartupToggle.Click += (s, e) =>
            {
                bool next = !_configService.Config.StartWithWindows;
                _configService.SetStartWithWindows(next);
                StartupService.SetStartup(next);
            };

            _itemQuit = new ToolStripMenuItem("Quit");
            _itemQuit.Click += (s, e) => Application.Exit();

            _contextMenu.Items.Add(_itemHeader);
            _contextMenu.Items.Add(new ToolStripSeparator());
            _contextMenu.Items.Add(_itemOpen);
            _contextMenu.Items.Add(_itemNotificationsToggle);
            _contextMenu.Items.Add(_itemPauseMenu);
            _contextMenu.Items.Add(_itemStartupToggle);
            _contextMenu.Items.Add(new ToolStripSeparator());
            _contextMenu.Items.Add(_itemQuit);
        }

        private void OnTrayIconMouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _toggleFlyoutAction();
            }
        }

        public void UpdateTrayState()
        {
            try
            {
                AppConfig config = _configService.Config;
                int percent = 100;
                bool pluggedIn = false;
                bool charging = false;

                if (_batteryMonitor != null)
                {
                    PowerSnapshot snapshot = _batteryMonitor.GetCurrentSnapshot();
                    percent = snapshot.Percent;
                    pluggedIn = snapshot.IsPluggedIn;
                    charging = snapshot.IsCharging;
                }

                // Update text
                string statusText = string.Format("BatteryAlert - {0}% ({1})",
                    percent,
                    pluggedIn ? (charging ? "Charging" : "Plugged in") : "On Battery");

                if (!config.NotificationsEnabled)
                {
                    statusText += " [OFF]";
                }
                else if (config.IsPaused)
                {
                    statusText += " [Paused]";
                }

                // Truncate tooltip if needed (max 63 chars on Windows NotifyIcon)
                if (statusText.Length > 63)
                {
                    statusText = statusText.Substring(0, 60) + "...";
                }
                _notifyIcon.Text = statusText;

                // Update Menu checks
                _itemHeader.Text = string.Format("BatteryAlert: {0}% {1}", percent, pluggedIn ? "⚡" : "");
                _itemNotificationsToggle.Checked = config.NotificationsEnabled;
                _itemStartupToggle.Checked = config.StartWithWindows;
                _itemResumeNow.Enabled = config.IsPaused;

                // Draw icon reflecting state
                Icon newIcon = GenerateTrayIcon(percent, pluggedIn, config.NotificationsEnabled, config.IsPaused);
                _notifyIcon.Icon = newIcon;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error updating tray icon: " + ex.Message);
            }
        }

        private Icon GenerateTrayIcon(int percent, bool pluggedIn, bool enabled, bool paused)
        {
            using (Bitmap bmp = new Bitmap(32, 32))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.Clear(Color.Transparent);

                Rectangle rect = new Rectangle(2, 2, 27, 27);

                // Track ring
                using (Pen trackPen = new Pen(Color.FromArgb(50, 50, 56), 3.5f))
                {
                    g.DrawEllipse(trackPen, rect);
                }

                if (!enabled)
                {
                    // State: OFF -> Muted grey ring with slash
                    using (Pen offPen = new Pen(Color.FromArgb(140, 140, 145), 3.5f))
                    {
                        g.DrawEllipse(offPen, rect);
                        g.DrawLine(offPen, 7, 7, 24, 24);
                    }
                }
                else if (paused)
                {
                    // State: Paused -> Amber ring + pause bars
                    using (Pen pausePen = new Pen(Color.FromArgb(255, 185, 0), 3.5f))
                    {
                        float sweep = (percent / 100f) * 360f;
                        g.DrawArc(pausePen, rect, -90f, Math.Max(10f, sweep));
                    }
                    using (SolidBrush barBrush = new SolidBrush(Color.FromArgb(255, 185, 0)))
                    {
                        g.FillRectangle(barBrush, 11, 10, 3, 11);
                        g.FillRectangle(barBrush, 17, 10, 3, 11);
                    }
                }
                else
                {
                    // State: Normal / Active -> Cyan ring + center bolt if plugged in
                    using (Pen activePen = new Pen(Color.FromArgb(0, 216, 246), 3.5f))
                    {
                        float sweep = (percent / 100f) * 360f;
                        g.DrawArc(activePen, rect, -90f, Math.Max(10f, sweep));
                    }

                    if (pluggedIn)
                    {
                        PointF[] bolt = new PointF[]
                        {
                            new PointF(17, 7),
                            new PointF(11, 16),
                            new PointF(15, 16),
                            new PointF(13, 24),
                            new PointF(20, 14),
                            new PointF(16, 14)
                        };
                        using (SolidBrush boltBrush = new SolidBrush(Color.White))
                        {
                            g.FillPolygon(boltBrush, bolt);
                        }
                    }
                }

                IntPtr hIcon = bmp.GetHicon();
                Icon icon = Icon.FromHandle(hIcon);

                if (_lastHIcon != IntPtr.Zero)
                {
                    NativeMethods.DestroyIcon(_lastHIcon);
                }
                _lastHIcon = hIcon;

                return icon;
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                if (_notifyIcon != null)
                {
                    _notifyIcon.Visible = false;
                    _notifyIcon.Dispose();
                }
                if (_contextMenu != null)
                {
                    _contextMenu.Dispose();
                }
                if (_lastHIcon != IntPtr.Zero)
                {
                    NativeMethods.DestroyIcon(_lastHIcon);
                    _lastHIcon = IntPtr.Zero;
                }
            }
        }
    }
}
