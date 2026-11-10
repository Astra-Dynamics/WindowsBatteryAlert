using System;
using System.Windows.Forms;
using Microsoft.Win32;

namespace BatteryAlert.Services
{
    public static class StartupService
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "BatteryAlert";

        public static bool IsRegistered()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false))
                {
                    if (key != null)
                    {
                        object val = key.GetValue(AppName);
                        return val != null;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Failed to read startup registry key: " + ex.Message);
            }
            return false;
        }

        public static void SetStartup(bool enable)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true))
                {
                    if (key != null)
                    {
                        if (enable)
                        {
                            string exePath = Application.ExecutablePath;
                            key.SetValue(AppName, "\"" + exePath + "\" --autostart");
                        }
                        else
                        {
                            if (key.GetValue(AppName) != null)
                            {
                                key.DeleteValue(AppName, false);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Failed to update startup registry key: " + ex.Message);
            }
        }
    }
}
