# ATassistant

[![License](https://img.shields.io/badge/license-GPL-blue.svg)](LICENSE)
[![Release](https://img.shields.io/github/v/release/QS-July/ATassistant?include_prereleases)](https://github.com/QS-July/ATassistant/releases)
[![Downloads](https://img.shields.io/github/downloads/QS-July/ATassistant/total)](https://github.com/QS-July/ATassistant/releases)

> A general FPS curved fire distance measurement tool, originally designed for **Hell Let Loose (HLL)** Anti-Tank (AT) gameplay.

---

## 📖 Introduction

- In HLL, judging the firing angle for AT is challenging for anyone, especially newcomers.
- Even experienced players often rely on intuition rather than fixed scales. 
- This tool calculates the distance based on the amount of mouse lift (DPI movement), making it easier to estimate firing angles without memorizing reference points.

---

## ✨ Features

- **Multi-point calibration**: Input multiple known distances and measure corresponding mouse lifts to fit a curve.
- **Real-time overlay**: Displays current distance and angle (θ) on a transparent overlay window.
- **Hotkey support**: Customizable keys for start/stop measurement, toggle overlay, and start multi-calibration.
- **Adjustable parameters**: Sensitivity, smoothing factor, and fixed maximum range can be tuned.
- **Save/Load calibration data**: JSON format for sharing and reusing calibration profiles.
- **Language support**: Chinese (default) and English UI, switchable via tray icon.
- **Acrylic-style UI** (optional) with dark theme for better visibility.

---

## 🚀 How to Start

### Prerequisites
- Windows OS (tested on Windows 10/11)
- [.NET Framework 4.7.2](https://dotnet.microsoft.com/download/dotnet-framework/net472) or higher (usually pre-installed)

### Installation
1. Go to the [Releases](https://github.com/yourusername/ATassistant/releases) page.
2. Download the latest `ATassistant.exe` (or the zip archive).

### First-time Setup
1. Enter at least two distances (in meters) into the multiline text box, one per line.
2. Click **"Start Multi Calibration"** to enter calibration mode.
3. Press **Middle Mouse Button** to begin measuring, then move the mouse upward (simulating the aiming lift).
4. Press **F2** (or your configured hotkey) to end the measurement. The delta value is recorded for that distance.
5. Repeat for each distance entered. After the last point, the tool automatically fits the curve and switches to **Measuring Mode**.
6. Now, in Measuring Mode, press **Middle Mouse Button** to start, move the mouse, and the overlay will show the estimated distance and angle.

> **Note**: You can also load a previously saved calibration file via the **"Load Calibration"** button.

---

## ⚙️ Configuration

All settings can be adjusted via the main UI. The following parameters are available:

| Parameter | Description | Default |
|-----------|-------------|---------|
| **Max Range** | Fixed maximum range (used when "Use fixed max range" is checked) | 1000 m |
| **Smooth Factor** | Exponential smoothing factor for displayed delta (0.01–1.0) | 0.20 |
| **Sensitivity** | Multiplier for mouse movement sensitivity | 1.00 |

The **"Refresh"** button recalculates the calibration using the current fixed max range (only valid when the checkbox is checked).

---

## ⌨️ Hotkeys

Default hotkeys (can be changed in the **Hotkey Settings** window):

| Action | Default Key |
|--------|-------------|
| Start Measurement (B1) | `F1`&`MMB` |
| End Measurement (B2) | `F2` |
| Toggle Main Window Visibility | `Ctrl+Shift+H` |
| Toggle Overlay Visibility | `Ctrl+Shift+T`(Maybe) |
| Start Multi Calibration | Not Set |

> **Important**: Hotkeys are global and may conflict with other applications. Change them if needed.

---

## 🗂️ File Format (Calibration JSON)

The calibration data is stored in JSON format. Example:

```json
{
  "Points": [
    {
      "Delta": 6,
      "Distance": 50.0
    },
    {
      "Delta": 13,
      "Distance": 100.0
    },
    {
      "Delta": 19,
      "Distance": 150.0
    }
  ],
  "K": 0.003945013637999941,
  "C": 1000.0
}
