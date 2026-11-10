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
        private const string EventName = "BatteryAlert_ShowEvent_9A8F";

        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            bool simulate = false;
            bool isAutoStart = false;

            if (args != null)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (string.Equals(args[i], "--simulate", StringComparison.OrdinalIgnoreCase))
                    {
                        simulate = true;
                    }
                    if (string.Equals(args[i], "--autostart", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(args[i], "--minimized", StringComparison.OrdinalIgnoreCase))
                    {
                        isAutoStart = true;
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
                    try
                    {
                        using (EventWaitHandle existingEvent = EventWaitHandle.OpenExisting(EventName))
                        {
                            existingEvent.Set();
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Failed to signal existing instance: " + ex.Message);
                    }
                    return;
                }

                // Create the named event for inter-process activation
                using (EventWaitHandle showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, EventName))
                {
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

                        // Create flyout form
                        flyoutForm = new CortanaFlyoutForm(configService, batteryMonitor, 0);

                        // Register kernel wait handle: when another instance launches, show the flyout!
                        RegisteredWaitHandle waitHandle = ThreadPool.RegisterWaitForSingleObject(showEvent, (state, timedOut) =>
                        {
                            if (!timedOut && flyoutForm != null && !flyoutForm.IsDisposed)
                            {
                                flyoutForm.BeginInvoke(new Action(() => flyoutForm.ShowFlyout()));
                            }
                        }, null, -1, false);

                        // Initial battery check
                        batteryMonitor.CheckStatus();

                        // If user launched manually (not at Windows startup), open the flyout window immediately!
                        if (!isAutoStart)
                        {
                            flyoutForm.ShowFlyout();
                        }

                        // Aggressively trim working set so startup footprint is ultra-lightweight
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        NativeMethods.TrimWorkingSet();

                        // Run application message loop
                        Application.Run();

                        // Cleanup wait handle
                        if (waitHandle != null)
                        {
                            waitHandle.Unregister(null);
                        }
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
}
