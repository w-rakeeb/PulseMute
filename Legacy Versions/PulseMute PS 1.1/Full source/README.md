# PulseMute

PulseMute is a compact Windows microphone mute utility made by **Wrakeeb**.

Owner: [w-rakeeb](https://github.com/w-rakeeb) / Ra Kib

PulseMute PS 1.1 adds two independent one-button shortcut slots for keyboard and PlayStation controller inputs.

## Features

- Toggle the default Windows communication microphone.
- One-key global shortcut, defaulting to `F8`.
- Two independent shortcut slots, defaulting to `F8` and `F9`.
- Assign any DualSense button, including Mute, PS, touchpad click, triggers, and D-pad.
- Assign DualSense Edge Fn buttons and rear paddles.
- USB and Bluetooth controller input with reconnect and rescan support.
- In-app shortcut picker.
- Silent mute/unmute behavior.
- Resizable interface for compact, mid-size, and larger windows.
- Controls and main mute button scale with the window size.
- Stay-on-top toggle.
- Auto-start on/off setting.
- Microphone selection dropdown with a Windows-default option.
- Remembers the selected microphone.
- Branded application, taskbar, and tray icon.
- Dark title bar matching the main interface.
- Optional hide-from-taskbar mode that keeps the window visible and tray app running.
- Remember-size-and-position on/off setting.
- Dark and White appearance modes.
- Custom accent, creator-line, background, surface, and text colors.
- Direct Windows Sound settings button.
- Per-monitor DPI awareness and clearer small-window text.
- Professional toggle switches with clear state text.
- Minimal Fluent action icons and redesigned global hotkey control.
- Settings stays above the main window when Always on Top is enabled.
- Remembers window size, position, and stay-on-top state.
- System tray support.
- Credit line: `made by Wrakeeb`.

## Files

- `Program.cs` contains the full Windows Forms application source.
- `PulseMute.csproj` is the modern .NET project file.
- `build.ps1` compiles the app on Windows using the built-in .NET Framework C# compiler.
- `test-controller.ps1` runs synthetic protocol tests and a read-only hardware check when a controller is connected.
- `Dependencies` contains the HidSharp 2.6.4 package and its Apache 2.0 license.
- `LICENSE` contains the project license.

## Build

### Option 1: Build with the included script

Open PowerShell in the repository folder and run:

```powershell
.\build.ps1
```

The compiled app will be written beside the `Full source` folder:

```text
..\PulseMute PS 1.1.exe
```

### Option 2: Build with the .NET SDK

If you have the .NET SDK installed:

```powershell
dotnet publish .\PulseMute.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o .\dist
```

## Usage

1. Open `PulseMute PS 1.1.exe`.
2. Click the large button to mute or unmute.
3. Use either edit icon to assign Key 1 or Key 2.
4. Press one keyboard key or one DualSense button for each slot.
5. Either assigned input can toggle the microphone from anywhere.
6. Open Settings to view the controller connection or manually rescan it.
7. Click `Top` to keep the window above other windows.
8. Click `Hide` to keep it running from the system tray.

## Notes

PulseMute uses Windows Core Audio APIs to control the default communication capture device. It was built from scratch and does not reuse MicMute's original code.

Window and microphone settings are saved under the current user's app data folder. When enabled, auto-start is registered under the current user's Windows startup registry entry, so it does not require administrator rights.

Official Sony DualSense (`054C:0CE6`) and DualSense Edge (`054C:0DF2`) controllers are supported over USB and Bluetooth. Bluetooth uses a neutral, CRC-protected initialization report to expose the complete button report without changing controller LEDs, audio, or haptics.
