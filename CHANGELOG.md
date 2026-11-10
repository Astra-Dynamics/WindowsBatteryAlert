# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-11-10

### Added
- **Core Battery Engine**: Event-driven battery monitoring using `SystemEvents.PowerModeChanged`, Win32 `WM_POWERBROADCAST`, and fallback 30-second polling.
- **Charge Threshold Tracking**: Built-in default thresholds at 90%, 95%, 98%, and 100% using `>=` logic (once per charge session).
- **Session Management**: Automatic reset of fired thresholds upon unplugging AC power or when the battery percentage drops below any previously fired threshold.
- **Smart Startup**: Automatically marks thresholds `<= currentPercent` as fired on startup to prevent retroactive notification spam.
- **Master Notification Switch**: Complete ON/OFF master toggle that persists across reboots.
- **Pause Until Controls**: Silences notifications with presets (1 hour, 2 hours, tomorrow 8:00 AM, and custom date/time picker) with automated resumption and instant "Resume now" capability.
- **Cortana-Inspired Flyout UI**: Compact 330×485 borderless window with obsidian dark background (`#111111`), Cortana cyan accent (`#00D8F6`), anti-aliased circular percentage ring arc with charging bolt/glow indicator, and pill-style controls.
- **Custom Threshold Management**: Ability to add unique integer thresholds (1–100%) with locked defaults and removable custom items.
- **Dynamic Tray Icon**: High-DPI icon that dynamically reflects system state (active cyan with bolt, paused amber with pause bars, off grey with slash).
- **Single Instance Support**: Win32 Mutex and broadcast window message to automatically surface the existing flyout window if a second instance is launched.
- **Zero-Privilege Windows Autostart**: Optional "Start with Windows" toggle stored under HKCU without requiring administrative privileges.
- **Hidden CLI Simulation Mode**: `--simulate` flag faking a battery rising from 85% to 100% for rapid end-to-end testing without waiting for hardware charging.
