# BatteryAlert ⚡

> An utterly lightweight, Cortana-styled Windows 10/11 system tray application that sends clean toast notifications while your laptop is plugged in and charging.

[![Platform: Windows 10 / 11](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-0078D7.svg)](#)
[![Memory: < 25 MB](https://img.shields.io/badge/Memory-%3C%2025%20MB%20RAM-00D8F6.svg)](#)
[![CPU: ~0% Idle](https://img.shields.io/badge/CPU-~0%25%20Idle-brightgreen.svg)](#)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

---

## Highlights

- **Zero Runtime Dependencies**: Single standalone `BatteryAlert.exe` (~82 KB) compiled natively against the built-in Windows .NET Framework 4.5/4.8. No installers, no Electron, no WebView2, and no external runtime downloads.
- **Ultra-Lightweight**: Consumes **under 25 MB RAM** and **~0.0% idle CPU**. Working set memory is automatically trimmed whenever the flyout is dismissed.
- **Event-Driven Power Monitoring**: Hooks into `SystemEvents.PowerModeChanged` and Win32 `WM_POWERBROADCAST` messages with a slow 30-second fallback timer.
- **Smart Charge Session Logic**: Fires notifications when crossing each threshold using `>=` logic. Automatically resets fired thresholds when unplugged or when battery percentage drops below a threshold.
- **No Retroactive Spam**: If the app starts while your laptop is already at 96%, it automatically suppresses alerts for 90% and 95%, firing only for thresholds above 96%.
- **Cortana-Inspired Dark UI**: Compact fixed-size flyout (`330 × 485` px) with deep obsidian canvas (`#111111`), Cortana cyan accent (`#00D8F6`), anti-aliased circular percentage ring, and pill-style controls.
- **Notification Controls**: Master ON/OFF toggle, plus a "Pause until..." feature with quick presets (1h, 2h, tomorrow 8:00 AM, custom picker) and an instant "Resume now" button.
- **Dynamic Tray Icon**: Live icon updates in the Windows notification area to visibly reflect active, paused, or silenced states.
- **Fault-Tolerant Persistence**: Configuration stored at `%APPDATA%\BatteryAlert\config.json`, safely falling back to defaults if missing or corrupted.

---

## UI Preview

```
+---------------------------------------------------+
| BatteryAlert                              [✕]     |
|                                                   |
|                      .-''''-.                     |
|                    .'   95%  '.                   |
|                   /  ⚡CHARGING \                 |
|                   |             |  (Cortana Arc)  |
|                    \           /                  |
|                     '.       .'                   |
|                       '-...-'                     |
|                                                   |
|           [  NOTIFICATIONS: ON  ]                 |
|       ● Active - Monitoring charge level          |
|                                                   |
|   PAUSE UNTIL                                     |
|   [ 1 h ]  [ 2 h ]  [ Tom 8 AM ]  [ Custom... ]   |
|                                                   |
|   ALERT THRESHOLDS                                |
|   [90% 🔒]  [95% 🔒]  [98% 🔒]  [100% 🔒]  [80% ×]|
|                                                   |
|   [ Add % (1-100) ]  [ + Add ]                    |
+---------------------------------------------------+
```
*(Screenshot placeholder: `docs/screenshots/flyout.png`)*

---

## Core Behavior & Notifications

### Default Thresholds
- **90%, 95%, 98%, 100%** are permanently built-in, immutable, and locked (marked with a lock icon `🔒`).
- Custom thresholds (integers 1–100%) can be added or deleted at any time.

### Toast Messages
- Standard thresholds: Title: `Battery at 95%` | Body: `Charger still connected.`
- 100% threshold: Title: `Battery at 100%` | Body: `Fully charged - you can unplug.`

### Charge Session Lifecycle
1. **Unplugging AC Power**: Resets all fired thresholds.
2. **Reconnecting Charger**: Begins a new charge session. All thresholds $\le \text{currentPercent}$ are marked as fired to prevent backwards notification spam.
3. **Threshold Jump**: If percentage jumps from 89% to 91% between checks, the 90% threshold fires properly using `>=` logic.
4. **Battery Drop**: If battery drops below a previously fired threshold under heavy load (e.g., from 95% down to 93%), the 95% threshold is reset and will fire again when charge reaches 95%.

---

## Notification Controls

### Master Toggle (ON / OFF)
- Click the master pill toggle in the flyout window or right-click the tray icon to switch between **ON** and **OFF**.
- When turned **OFF**, all notifications are silenced indefinitely.
- The toggle state persists across app restarts and system reboots.

### "Pause until..." Presets
- Silences alerts temporarily until a specified timestamp:
  - **1 hour**: Pauses for 60 minutes.
  - **2 hours**: Pauses for 120 minutes.
  - **Tom 8 AM**: Pauses until 8:00 AM tomorrow morning.
  - **Custom...**: Opens a minimal date/time picker dialog.
- Flyout displays a status indicator: `⏸ Paused until 6:30 PM`.
- Click **Resume now** at any time to immediately cancel the pause.
- Automatically resumes once the target time is reached. Checked during normal battery cycles (zero extra polling threads).

### Tray Icon States
| State | Tray Icon Appearance | Tooltip Status |
| :--- | :--- | :--- |
| **Active** | Vibrant Cyan circular arc with electric bolt | `BatteryAlert - 95% (Charging)` |
| **Paused** | Amber circular arc with vertical pause bars | `BatteryAlert - 95% (Charging) [Paused]` |
| **Silenced** | Muted dark-grey ring with diagonal slash | `BatteryAlert - 95% (Charging) [OFF]` |

---

## Installation & Building

### Pre-requisites
- Windows 10 or Windows 11.
- Built-in .NET Framework 4.5 or higher (pre-installed on all modern Windows machines).

### Build from Source
Run the included PowerShell build script:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build.ps1
```

The script compiles the source code using the native Windows `MSBuild.exe` or `csc.exe` and outputs a clean standalone binary:
```
dist/BatteryAlert.exe (~82 KB)
```

To run the automated verification test suite:
```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\test-all.ps1
```

---

## Usage

1. **Launch**: Run `BatteryAlert.exe`. It starts silently in the system tray without showing a window.
2. **Open Flyout**: Left-click or double-click the tray icon to open the flyout above the taskbar.
3. **Dismiss Flyout**: Click anywhere outside the window or press `Esc`.
4. **Tray Menu**: Right-click the tray icon for quick access to:
   - Open BatteryAlert
   - Notifications Enabled (Toggle)
   - Pause Notifications (1h, 2h, Tomorrow 8 AM, Resume now)
   - Start with Windows (Toggle)
   - Quit

---

## Testing with `--simulate`

BatteryAlert includes a hidden CLI flag designed for instant testing without waiting for hardware charging:

```cmd
BatteryAlert.exe --simulate
```

### What `--simulate` Does:
1. Simulates AC charger connection.
2. Initializes battery level at **85%**.
3. Steps battery percentage upward every 1.8 seconds:
   `85% -> 88% -> 90% (Toast) -> 93% -> 95% (Toast) -> 97% -> 98% (Toast) -> 99% -> 100% (Toast: "Fully charged - you can unplug.")`.
4. Displays a yellow `[SIMULATION - Click to Restart 85%]` badge in the flyout header. Click this badge at any time to reset back to 85% and replay the sequence.
5. Allows real-time verification of the Master Toggle, Pause Until, and Custom Thresholds.

---

## Configuration

Settings are saved in clean JSON format at:
```
%APPDATA%\BatteryAlert\config.json
```

Example `config.json`:
```json
{
  "notificationsEnabled": true,
  "pauseUntil": null,
  "customThresholds": [80],
  "startWithWindows": true
}
```

*Note: Default thresholds (`90, 95, 98, 100`) are hardcoded into the application and are never stored as deletable items in the JSON configuration.*

---

## Troubleshooting & Notification Notes

> [!NOTE]
> **Windows Focus Assist / Do Not Disturb**:
> Windows 10 and Windows 11 include a feature called **Focus Assist** (Windows 10) or **Do Not Disturb** (Windows 11).
> - When Focus Assist or Do Not Disturb is turned ON (or during full-screen gaming, presentation, or display duplication), Windows automatically silences notification banners.
> - In this mode, toasts are routed silently directly into the **Windows Action Center / Notification Center** without showing a popup toast on your screen.
> - **How to verify**: Open Windows **Settings > System > Focus Assist** (or **Notifications > Do Not Disturb**) and verify that notifications are set to **Off** (allowing notifications) or add BatteryAlert to your **Priority list**.

---

## Architecture

See [ARCHITECTURE.md](docs/ARCHITECTURE.md) for a comprehensive overview of the application architecture, Win32 hooks, memory management, and message routing.

---

## Author & Organization

- **Owner & Builder**: Anant Jain
- **Organization**: Astra Dynamics

---

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
