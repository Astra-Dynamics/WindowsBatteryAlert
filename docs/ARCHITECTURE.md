# Architecture Overview - BatteryAlert

## High-Level Design

BatteryAlert is engineered as an ultra-lightweight, zero-dependency Windows desktop utility designed for minimal RAM footprint (< 25 MB), near-zero idle CPU usage (~0.0%), and native Windows integration.

```
                          +------------------------+
                          |   Windows OS Events    |
                          | (WM_POWERBROADCAST &   |
                          | PowerModeChanged)      |
                          +-----------+------------+
                                      |
                                      v
                          +-----------+------------+
                          |     BatteryMonitor     |
                          |   (State & Thresholds) |
                          +-----+------------+-----+
                                |            |
         +----------------------+            +-----------------------+
         |                                                           |
         v                                                           v
+--------+---------------+                                  +--------+---------------+
|  NotificationService   |                                  |   CortanaFlyoutForm    |
| (NotifyIcon & Toasts)  |                                  | (Flyout UI & Ring Arc) |
+--------+---------------+                                  +--------+---------------+
         |                                                           |
         v                                                           v
+--------+---------------+                                  +--------+---------------+
|  Windows Action Center |                                  | Double-Buffered GDI+   |
|   Toast Notification   |                                  | Direct Rendering       |
+------------------------+                                  +------------------------+
```

---

## Component Breakdown

### 1. Battery & Power Engine (`src/Services/BatteryMonitor.cs`)
- **Event-Driven**: Subscribes to `Microsoft.Win32.SystemEvents.PowerModeChanged` and intercepts Win32 `WM_POWERBROADCAST` (with `PBT_APMPOWERSTATUSCHANGE`) messages in the main window loop.
- **Slow Fallback Poll**: Runs a single lightweight 30-second timer to detect edge cases where hardware drivers fail to dispatch power broadcast messages.
- **Session Logic**:
  - Maintains `HashSet<int> _firedThresholds`.
  - When AC power is disconnected, `_firedThresholds.Clear()`.
  - When AC power is re-connected or the application starts, all thresholds `<= currentPercent` are marked as fired to prevent stale backward spam.
  - If battery level drops below a previously fired threshold $T$ during a session (e.g., from 95% down to 93%), $T$ is reset from `_firedThresholds` so that it will fire again upon climbing back to $T$.

### 2. Configuration & Persistence (`src/Services/ConfigService.cs`)
- **Path**: `%APPDATA%\BatteryAlert\config.json`.
- **Fault-Tolerant**: Corrupted or empty files trigger an automatic fallback to safe hardcoded defaults (`90%, 95%, 98%, 100%`) without throwing unhandled exceptions.
- **Safe Persistence**: Writes to a `.tmp` file and atomically replaces the active configuration file to protect against power outages or system restarts during write operations.
- **Pause Expiry**: Checked lazily during regular battery ticks and UI refresh cycles—eliminates the need for background polling timers.

### 3. Cortana-Style Flyout (`src/UI/CortanaFlyoutForm.cs`)
- **Geometry**: Compact fixed-size `330 x 485` px borderless window.
- **Taskbar Positioning**: Calculated dynamically from `Screen.PrimaryScreen.WorkingArea` to dock just above the Windows notification tray area.
- **Dismissal**: Closes automatically when losing focus (`OnDeactivate`) or when the user presses `Esc`.
- **Idle Resource Optimization**:
  - Rendering is completely suspended when the window is hidden (`Visible = false`).
  - Calls `NativeMethods.TrimWorkingSet()` whenever the window is hidden, aggressively reducing the process working set down to ~20 MB.

### 4. Custom GDI+ Controls
- **`CortanaCircleControl`**: Vector circular arc gauge with double buffering (`OptimizedDoubleBuffer`). Sweeps from $-90^\circ$ through $360^\circ \times (\text{Percent} / 100)$, displaying a glowing aura and electric bolt glyph when AC power is active.
- **`PillButton`**: Anti-aliased pill-shaped toggle and action buttons styled with Cortana cyan highlights.
- **`ThresholdChipControl`**: Compact badge displaying the threshold level, a locked padlock indicator for immutable defaults, and a hover-responsive `✕` delete button for custom user thresholds.

### 5. Single-Instance Mutex & Inter-Process Messaging
- **Named Mutex**: `BatteryAlert_SingleInstance_Mutex_9A8F` ensures only one instance runs at any time.
- **Inter-Process Activation**: A second instance registers a custom Win32 message `BatteryAlert_ShowWindow_9A8F` using `RegisterWindowMessage`, broadcasts it to `HWND_BROADCAST`, and exits immediately. The running instance intercepts this message in `WndProc` and opens the flyout.

### 6. Dynamic System Tray Controller (`src/UI/TrayIconController.cs`)
- Renders high-DPI 32×32 bitmaps directly in memory and converts them into Windows icons via `Icon.FromHandle(hIcon)`.
- Disposes previous Win32 icon handles using `NativeMethods.DestroyIcon` to prevent GDI resource leaks.
- Dynamically reflects three distinct visual states:
  - **Normal Active**: Vibrant Cyan ring with central lightning bolt.
  - **Paused**: Amber ring with vertical pause bars.
  - **Silenced (OFF)**: Dim grey ring with diagonal slash.
