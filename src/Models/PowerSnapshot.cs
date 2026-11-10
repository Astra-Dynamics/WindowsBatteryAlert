using System;

namespace BatteryAlert.Models
{
    public class PowerSnapshot
    {
        public bool IsPluggedIn { get; private set; }
        public bool IsCharging { get; private set; }
        public int Percent { get; private set; }
        public bool HasBattery { get; private set; }
        public DateTime Timestamp { get; private set; }

        public PowerSnapshot(bool isPluggedIn, bool isCharging, int percent, bool hasBattery)
        {
            IsPluggedIn = isPluggedIn;
            IsCharging = isCharging;
            Percent = Math.Max(0, Math.Min(100, percent));
            HasBattery = hasBattery;
            Timestamp = DateTime.Now;
        }

        public override string ToString()
        {
            return string.Format("{0}% ({1})", Percent, IsPluggedIn ? (IsCharging ? "Charging" : "Plugged in") : "On Battery");
        }
    }
}
