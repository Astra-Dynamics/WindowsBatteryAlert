using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;
using BatteryAlert.Models;

namespace BatteryAlert.Services
{
    public class ConfigService
    {
        private readonly string _configDirectory;
        private readonly string _configPath;
        private readonly JavaScriptSerializer _serializer;
        private AppConfig _config;

        public event Action ConfigChanged;

        public AppConfig Config
        {
            get { return _config; }
        }

        public ConfigService()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _configDirectory = Path.Combine(appData, "BatteryAlert");
            _configPath = Path.Combine(_configDirectory, "config.json");
            _serializer = new JavaScriptSerializer();
            _config = Load();
        }

        public AppConfig Load()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    string json = File.ReadAllText(_configPath);
                    if (!string.IsNullOrEmpty(json.Trim()))
                    {
                        var dict = _serializer.Deserialize<Dictionary<string, object>>(json);
                        if (dict != null)
                        {
                            AppConfig loaded = new AppConfig();

                            if (dict.ContainsKey("notificationsEnabled") && dict["notificationsEnabled"] is bool)
                            {
                                loaded.NotificationsEnabled = (bool)dict["notificationsEnabled"];
                            }

                            if (dict.ContainsKey("pauseUntil") && dict["pauseUntil"] != null)
                            {
                                string pauseStr = dict["pauseUntil"].ToString();
                                DateTime dt;
                                if (DateTime.TryParse(pauseStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out dt))
                                {
                                    DateTime utc = dt.ToUniversalTime();
                                    if (utc > DateTime.UtcNow)
                                    {
                                        loaded.PauseUntilUtc = utc;
                                    }
                                }
                            }

                            if (dict.ContainsKey("customThresholds") && dict["customThresholds"] is System.Collections.ArrayList)
                            {
                                var list = (System.Collections.ArrayList)dict["customThresholds"];
                                loaded.CustomThresholds = new List<int>();
                                foreach (object item in list)
                                {
                                    int val;
                                    if (int.TryParse(item.ToString(), out val))
                                    {
                                        if (val >= 1 && val <= 100 && !AppConfig.IsDefault(val) && !loaded.CustomThresholds.Contains(val))
                                        {
                                            loaded.CustomThresholds.Add(val);
                                        }
                                    }
                                }
                                loaded.CustomThresholds.Sort();
                            }

                            if (dict.ContainsKey("startWithWindows") && dict["startWithWindows"] is bool)
                            {
                                loaded.StartWithWindows = (bool)dict["startWithWindows"];
                            }

                            return loaded;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Silently fallback to defaults without crashing
                System.Diagnostics.Debug.WriteLine("Config load failed, using defaults: " + ex.Message);
            }

            // Defaults
            AppConfig defaultConfig = new AppConfig();
            Save(defaultConfig);
            return defaultConfig;
        }

        public void Save()
        {
            Save(_config);
        }

        private void Save(AppConfig config)
        {
            try
            {
                if (!Directory.Exists(_configDirectory))
                {
                    Directory.CreateDirectory(_configDirectory);
                }

                Dictionary<string, object> dict = new Dictionary<string, object>();
                dict["notificationsEnabled"] = config.NotificationsEnabled;
                dict["pauseUntil"] = config.PauseUntilUtc.HasValue ? config.PauseUntilUtc.Value.ToString("o") : null;
                dict["customThresholds"] = config.CustomThresholds ?? new List<int>();
                dict["startWithWindows"] = config.StartWithWindows;

                string json = _serializer.Serialize(dict);
                
                // Formatted JSON for cleanliness and readability
                json = json.Replace("{\"", "{\r\n  \"")
                           .Replace(",\"", ",\r\n  \"")
                           .Replace("}", "\r\n}");

                string tempFile = _configPath + ".tmp";
                File.WriteAllText(tempFile, json);
                if (File.Exists(_configPath))
                {
                    File.Delete(_configPath);
                }
                File.Move(tempFile, _configPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Config save failed: " + ex.Message);
            }
        }

        public void SetNotificationsEnabled(bool enabled)
        {
            if (_config.NotificationsEnabled != enabled)
            {
                _config.NotificationsEnabled = enabled;
                Save();
                NotifyChanged();
            }
        }

        public void SetPauseUntil(DateTime? pauseUntil)
        {
            if (pauseUntil.HasValue)
            {
                _config.PauseUntilUtc = pauseUntil.Value.ToUniversalTime();
            }
            else
            {
                _config.PauseUntilUtc = null;
            }
            Save();
            NotifyChanged();
        }

        public void ResumeNow()
        {
            if (_config.PauseUntilUtc.HasValue)
            {
                _config.PauseUntilUtc = null;
                Save();
                NotifyChanged();
            }
        }

        public bool AddCustomThreshold(int threshold)
        {
            if (_config.AddCustomThreshold(threshold))
            {
                Save();
                NotifyChanged();
                return true;
            }
            return false;
        }

        public bool RemoveCustomThreshold(int threshold)
        {
            if (_config.RemoveCustomThreshold(threshold))
            {
                Save();
                NotifyChanged();
                return true;
            }
            return false;
        }

        public void SetStartWithWindows(bool enable)
        {
            _config.StartWithWindows = enable;
            Save();
            NotifyChanged();
        }

        public bool CheckPauseExpiry()
        {
            if (_config.PauseUntilUtc.HasValue && DateTime.UtcNow >= _config.PauseUntilUtc.Value)
            {
                _config.PauseUntilUtc = null;
                Save();
                NotifyChanged();
                return true;
            }
            return false;
        }

        private void NotifyChanged()
        {
            Action handler = ConfigChanged;
            if (handler != null)
            {
                handler();
            }
        }
    }
}
