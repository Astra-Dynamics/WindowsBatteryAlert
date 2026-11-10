using System;
using System.Collections.Generic;

namespace BatteryAlert.Models
{
    public class AppConfig
    {
        public static readonly int[] DefaultThresholds = new int[] { 90, 95, 98, 100 };

        public bool NotificationsEnabled { get; set; }
        public DateTime? PauseUntilUtc { get; set; }
        public List<int> CustomThresholds { get; set; }
        public bool StartWithWindows { get; set; }

        public AppConfig()
        {
            NotificationsEnabled = true;
            PauseUntilUtc = null;
            CustomThresholds = new List<int>();
            StartWithWindows = true;
        }

        public bool IsPaused
        {
            get
            {
                if (!PauseUntilUtc.HasValue) return false;
                return DateTime.UtcNow < PauseUntilUtc.Value;
            }
        }

        public bool IsNotificationsActive
        {
            get
            {
                return NotificationsEnabled && !IsPaused;
            }
        }

        public static bool IsDefault(int threshold)
        {
            for (int i = 0; i < DefaultThresholds.Length; i++)
            {
                if (DefaultThresholds[i] == threshold) return true;
            }
            return false;
        }

        public List<int> GetEffectiveThresholds()
        {
            SortedSet<int> set = new SortedSet<int>();
            for (int i = 0; i < DefaultThresholds.Length; i++)
            {
                set.Add(DefaultThresholds[i]);
            }
            if (CustomThresholds != null)
            {
                for (int i = 0; i < CustomThresholds.Count; i++)
                {
                    int val = CustomThresholds[i];
                    if (val >= 1 && val <= 100)
                    {
                        set.Add(val);
                    }
                }
            }
            return new List<int>(set);
        }

        public bool AddCustomThreshold(int threshold)
        {
            if (threshold < 1 || threshold > 100) return false;
            if (IsDefault(threshold)) return false;
            if (CustomThresholds == null) CustomThresholds = new List<int>();
            if (CustomThresholds.Contains(threshold)) return false;

            CustomThresholds.Add(threshold);
            CustomThresholds.Sort();
            return true;
        }

        public bool RemoveCustomThreshold(int threshold)
        {
            if (IsDefault(threshold)) return false; // Default cannot be removed
            if (CustomThresholds == null) return false;
            return CustomThresholds.Remove(threshold);
        }
    }
}
