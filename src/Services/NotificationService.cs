using System;
using System.Windows.Forms;

namespace BatteryAlert.Services
{
    public class NotificationService
    {
        private readonly NotifyIcon _notifyIcon;

        public event Action<int, string, string> NotificationSent;

        public NotificationService(NotifyIcon notifyIcon)
        {
            _notifyIcon = notifyIcon;
        }

        public void SendBatteryNotification(int threshold)
        {
            string title = string.Format("Battery at {0}%", threshold);
            string body;

            if (threshold >= 100)
            {
                body = "Fully charged - you can unplug.";
            }
            else
            {
                body = "Charger still connected.";
            }

            try
            {
                if (_notifyIcon != null && _notifyIcon.Visible)
                {
                    _notifyIcon.ShowBalloonTip(5000, title, body, ToolTipIcon.Info);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Failed to show toast notification: " + ex.Message);
            }

            Action<int, string, string> handler = NotificationSent;
            if (handler != null)
            {
                handler(threshold, title, body);
            }
        }
    }
}
