using System;
using System.Runtime.InteropServices;

namespace BatteryAlert.Native
{
    internal static class NativeMethods
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEM_POWER_STATUS
        {
            public byte ACLineStatus;         // 0 = Offline, 1 = Online, 255 = Unknown
            public byte BatteryFlag;          // 1 = High, 2 = Low, 4 = Critical, 8 = Charging, 128 = No battery, 255 = Unknown
            public byte BatteryLifePercent;   // 0 - 100, 255 = Unknown
            public byte SystemStatusFlag;
            public int BatteryLifeTime;
            public int BatteryFullLifeTime;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS lpSystemPowerStatus);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetProcessWorkingSetSize(IntPtr hProcess, IntPtr dwMinimumWorkingSetSize, IntPtr dwMaximumWorkingSetSize);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern uint RegisterWindowMessage(string lpString);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DestroyIcon(IntPtr hIcon);

        public static readonly IntPtr HWND_BROADCAST = new IntPtr(0xffff);
        public const int WM_POWERBROADCAST = 0x0218;
        public const int PBT_APMPOWERSTATUSCHANGE = 0x000A;

        // Windows 11 rounded corners and dark theme
        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        public const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        public const int DWMWCP_ROUND = 2;

        public static void TrimWorkingSet()
        {
            try
            {
                IntPtr currentProc = System.Diagnostics.Process.GetCurrentProcess().Handle;
                SetProcessWorkingSetSize(currentProc, new IntPtr(-1), new IntPtr(-1));
            }
            catch
            {
                // Ignore if permission denied
            }
        }
    }
}
