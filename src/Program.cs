using System;
using System.Threading;
using System.Windows.Forms;
using BatteryAlert.Native;
using BatteryAlert.Services;
using BatteryAlert.UI;

namespace BatteryAlert
{
    static class Program
    {
        private const string MutexName = "BatteryAlert_SingleInstance_Mutex_9A8F";
        private const string WindowMessageName = "BatteryAlert_ShowWindow_9A8F";

        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            bool simulate = false;
            if (args != null)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (string.Equals(args[i], "--simulate", StringComparison.OrdinalIgnoreCase))
                    {
                        simulate = true;
                    }
                }
            }

            // Single Instance Protection
            bool createdNew;
            using (Mutex mutex = new Mutex(true, MutexName, out createdNew))
            {
                if (!createdNew)
                {
                    // An instance is already running; signal it to open its flyout window
                    uint msg = NativeMethods.RegisterWindowMessage(WindowMessageName);
                    NativeMethods.PostMessage(NativeMethods.HWND_BROADCAST, msg, IntPtr.Zero, IntPtr.Zero);
                    return;
                }

                // Register inter-process window activation message
                uint showWindowMessage = NativeMethods.RegisterWindowMessage(WindowMessageName);

                // Initialize services
                ConfigService configService = new ConfigService();
                
                // Keep Windows startup registration in sync with saved setting
                if (configService.Config.StartWithWindows && !StartupService.IsRegistered())
                {
                    StartupService.SetStartup(true);
                }

                CortanaFlyoutForm flyoutForm = null;
                TrayIconController trayController = null;
                BatteryMonitor batteryMonitor = null;

                try
                {
                    // Create Tray Controller
                    trayController = new TrayIconController(configService, () =>
                    {
                        if (flyoutForm != null)
                        {
                            flyoutForm.ToggleVisibility();
                        }
                    });

                    // Notification Service bound to the tray icon
                    NotificationService notificationService = new NotificationService(trayController.NotifyIcon);

                    // Battery Monitor bound to config and notifications
                    batteryMonitor = new BatteryMonitor(configService, notificationService, simulate);

                    // Connect monitor to tray icon
                    trayController.SetBatteryMonitor(batteryMonitor);

                    // Create flyout form (kept hidden silently on startup)
                    flyoutForm = new CortanaFlyoutForm(configService, batteryMonitor, showWindowMessage);

                    // Initial battery check
                    batteryMonitor.CheckStatus();

                    // Aggressively trim working set so startup footprint is ultra-lightweight
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    NativeMethods.TrimWorkingSet();

                    // Run application message loop silently in system tray
                    Application.Run();
                }
                finally
                {
                    if (batteryMonitor != null)
                    {
                        batteryMonitor.Dispose();
                    }
                    if (trayController != null)
                    {
                        trayController.Dispose();
                    }
                    if (flyoutForm != null)
                    {
                        flyoutForm.Dispose();
                    }
                }
            }
        }
    }
}
