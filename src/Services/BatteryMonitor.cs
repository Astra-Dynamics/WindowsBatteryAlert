using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Microsoft.Win32;
using BatteryAlert.Models;
using BatteryAlert.Native;

namespace BatteryAlert.Services
{
    public class BatteryMonitor : IDisposable
    {
        private readonly ConfigService _configService;
        private readonly NotificationService _notificationService;
        private readonly Timer _pollTimer;
        private readonly HashSet<int> _firedThresholds;

        private bool _wasPluggedIn;
        private int _lastPercent;
        private bool _isFirstCheck;
        private bool _isDisposed;

        // Simulation support
        private bool _isSimulating;
        private int _simPercent = 85;
        private bool _simPluggedIn = true;
        private Timer _simTimer;

        public event Action<PowerSnapshot> PowerStateChanged;
        public event Action<int> ThresholdFired;

        public bool IsSimulating
        {
            get { return _isSimulating; }
        }

        public BatteryMonitor(ConfigService configService, NotificationService notificationService, bool simulate = false)
        {
            _configService = configService;
            _notificationService = notificationService;
            _firedThresholds = new HashSet<int>();
            _isFirstCheck = true;
            _lastPercent = -1;
            _wasPluggedIn = false;
            _isSimulating = simulate;

            // System power mode event
            SystemEvents.PowerModeChanged += OnSystemPowerModeChanged;

            // Fallback poll timer: checks every 30 seconds
            _pollTimer = new Timer();
            _pollTimer.Interval = 30000;
            _pollTimer.Tick += (s, e) => CheckStatus();
            _pollTimer.Start();

            if (_isSimulating)
            {
                InitSimulation();
            }
        }

        public PowerSnapshot GetCurrentSnapshot()
        {
            if (_isSimulating)
            {
                return new PowerSnapshot(_simPluggedIn, _simPluggedIn, _simPercent, true);
            }

            NativeMethods.SYSTEM_POWER_STATUS status;
            if (NativeMethods.GetSystemPowerStatus(out status))
            {
                bool isPluggedIn = status.ACLineStatus == 1;
                bool isCharging = (status.BatteryFlag & 8) != 0 || isPluggedIn;
                int percent = status.BatteryLifePercent;
                bool hasBattery = (status.BatteryFlag & 128) == 0 && percent <= 100;

                // Fallback to WinForms PowerStatus if percent is invalid (255)
                if (percent > 100)
                {
                    percent = (int)(SystemInformation.PowerStatus.BatteryLifePercent * 100);
                    isPluggedIn = SystemInformation.PowerStatus.PowerLineStatus == PowerLineStatus.Online;
                    isCharging = isPluggedIn;
                }

                return new PowerSnapshot(isPluggedIn, isCharging, percent, hasBattery);
            }

            // Fallback
            int winPercent = (int)(SystemInformation.PowerStatus.BatteryLifePercent * 100);
            bool winPlugged = SystemInformation.PowerStatus.PowerLineStatus == PowerLineStatus.Online;
            return new PowerSnapshot(winPlugged, winPlugged, winPercent, true);
        }

        public void CheckStatus()
        {
            // Check if pause expired
            _configService.CheckPauseExpiry();

            PowerSnapshot snapshot = GetCurrentSnapshot();
            bool isPluggedIn = snapshot.IsPluggedIn;
            int percent = snapshot.Percent;

            if (!isPluggedIn)
            {
                // Charger unplugged -> Reset fired flags
                if (_wasPluggedIn)
                {
                    _firedThresholds.Clear();
                }
                _wasPluggedIn = false;
                _lastPercent = percent;
                _isFirstCheck = false;
                NotifyPowerState(snapshot);
                return;
            }

            // Plugged in!
            if (!_wasPluggedIn)
            {
                // Fresh charge session started
                _firedThresholds.Clear();
                
                // Only fire thresholds ABOVE current level (don't spam backwards)
                List<int> thresholds = _configService.Config.GetEffectiveThresholds();
                for (int i = 0; i < thresholds.Count; i++)
                {
                    int t = thresholds[i];
                    if (t <= percent)
                    {
                        _firedThresholds.Add(t);
                    }
                }

                _wasPluggedIn = true;
                _lastPercent = percent;
                _isFirstCheck = false;
                NotifyPowerState(snapshot);
                return;
            }

            // Startup check while already plugged in
            if (_isFirstCheck)
            {
                _firedThresholds.Clear();
                List<int> thresholds = _configService.Config.GetEffectiveThresholds();
                for (int i = 0; i < thresholds.Count; i++)
                {
                    int t = thresholds[i];
                    if (t <= percent)
                    {
                        _firedThresholds.Add(t);
                    }
                }
                _isFirstCheck = false;
            }

            // Reset any threshold the battery has dropped below
            List<int> toReset = new List<int>();
            foreach (int t in _firedThresholds)
            {
                if (percent < t)
                {
                    toReset.Add(t);
                }
            }
            for (int i = 0; i < toReset.Count; i++)
            {
                _firedThresholds.Remove(toReset[i]);
            }

            // Evaluate thresholds using >= logic
            List<int> activeThresholds = _configService.Config.GetEffectiveThresholds();
            for (int i = 0; i < activeThresholds.Count; i++)
            {
                int t = activeThresholds[i];
                if (percent >= t && !_firedThresholds.Contains(t))
                {
                    // Threshold reached!
                    _firedThresholds.Add(t);

                    // If notifications are active (enabled and not paused), send toast
                    if (_configService.Config.IsNotificationsActive)
                    {
                        _notificationService.SendBatteryNotification(t);

                        Action<int> handler = ThresholdFired;
                        if (handler != null)
                        {
                            handler(t);
                        }
                    }
                }
            }

            _lastPercent = percent;
            NotifyPowerState(snapshot);
        }

        private void OnSystemPowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            // Immediate check on power plug / unplug / resume
            CheckStatus();
        }

        private void NotifyPowerState(PowerSnapshot snapshot)
        {
            Action<PowerSnapshot> handler = PowerStateChanged;
            if (handler != null)
            {
                handler(snapshot);
            }
        }

        #region Simulation

        private void InitSimulation()
        {
            _simPercent = 85;
            _simPluggedIn = true;

            _simTimer = new Timer();
            _simTimer.Interval = 1800; // Step every 1.8 seconds for smooth testing
            _simTimer.Tick += (s, e) =>
            {
                if (_simPercent < 100)
                {
                    // Realistic jumps: 85 -> 88 -> 90 -> 93 -> 95 -> 97 -> 98 -> 99 -> 100
                    if (_simPercent == 85) _simPercent = 88;
                    else if (_simPercent == 88) _simPercent = 90;
                    else if (_simPercent == 90) _simPercent = 93;
                    else if (_simPercent == 93) _simPercent = 95;
                    else if (_simPercent == 95) _simPercent = 97;
                    else if (_simPercent == 97) _simPercent = 98;
                    else if (_simPercent == 98) _simPercent = 99;
                    else if (_simPercent == 99) _simPercent = 100;
                    else _simPercent += 1;
                }
                CheckStatus();
            };
            _simTimer.Start();
        }

        public void StepSimulation(int percent, bool pluggedIn)
        {
            _simPercent = percent;
            _simPluggedIn = pluggedIn;
            CheckStatus();
        }

        public void ResetSimulation(int startPercent)
        {
            _simPercent = startPercent;
            _simPluggedIn = true;
            _firedThresholds.Clear();
            _wasPluggedIn = false;
            _isFirstCheck = true;
            CheckStatus();
        }

        #endregion

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                SystemEvents.PowerModeChanged -= OnSystemPowerModeChanged;
                if (_pollTimer != null)
                {
                    _pollTimer.Stop();
                    _pollTimer.Dispose();
                }
                if (_simTimer != null)
                {
                    _simTimer.Stop();
                    _simTimer.Dispose();
                }
            }
        }
    }
}
