# PulseMute

PulseMute is a compact Windows microphone mute utility made by **Wrakeeb**.

Owner: [w-rakeeb](https://github.com/w-rakeeb) / Ra Kib

PulseMute Beta 26.1.0.26-beta is the active development and validation channel.

## Features

- Toggle the default Windows communication microphone.
- One-key global shortcut, defaulting to `F8`.
- Two independent shortcut slots, defaulting to `F8` and `F9`.
- Optional Dual Hotkey mode; when disabled, only Key 1 is active.
- Compact side-by-side hotkey cards that are clicked directly to change assignments.
- Assign Mouse 1-5 or vertical and horizontal wheel directions to either shortcut slot.
- Assign any DualSense button, including Mute, PS, touchpad click, triggers, and D-pad.
- Assign DualSense Edge Fn buttons and rear paddles.
- USB and Bluetooth controller input with reconnect and rescan support.
- In-app shortcut picker.
- Optional mute/unmute sound feedback with distinct confirmation tones.
- Nine built-in sound styles: Soft Chime, Digital, Click, Pulse, 8-Bit, Arcade, Radio, Glass, and Signal.
- Adjustable feedback volume from silent to full volume.
- Sound feedback, volume, and the selected style are remembered between launches.
- Resizable interface for compact, mid-size, and larger windows.
- Controls and main mute button scale with the window size.
- Stay-on-top toggle.
- Auto-start on/off setting.
- Microphone selection dropdown with a Windows-default option.
- Remembers the selected microphone.
- Matching red muted-microphone application, taskbar, tray, title-bar, and About branding.
- Four saved logo choices in Customization with immediate window, tray, Settings, and About updates.
- Rounded square is the default application and executable logo.
- Dark title bar matching the main interface.
- Optional hide-from-taskbar mode that keeps the window visible and tray app running.
- Remember-size-and-position on/off setting.
- Dark and White appearance modes.
- Matching front-page and Settings palettes with customizable Settings sidebar and border colors.
- Themed color editor with live preview, HEX/RGB entry, quick presets, and color-code copy/paste.
- Compact hotkey cards inside the bottom toolbar with a Developer option to restore their previous upper position.
- Per-release settings, auto-start, and duplicate-instance identity to keep Main, Beta, and archived builds separate.
- Every new release starts with its own clean default settings.
- Balanced `660 x 500` Settings window that opens beside the main interface.
- Default dark Settings sidebar color of `#111418`.
- Default interface accent color of `#C13545`.
- Custom accent, creator-line, background, surface, and text colors.
- Six selectable mute controls, including a supplied red/green microphone state design.
- Red/green mic is the default mute control.
- Direct Windows Sound settings button.
- Per-monitor DPI awareness and clearer small-window text.
- Professional toggle switches with clear state text.
- Minimal Fluent action icons and redesigned global hotkey control.
- Settings stays above the main window when Always on Top is enabled.
- Silence-inspired frameless Settings window with compact icon navigation and responsive content.
- Inter typography, dark neutral cards, PulseMute red accents, custom dropdowns, and a custom volume slider.
- Eased animated page changes and toggle transitions, with an option to disable animations.
- Buffered drawing and faster state refresh for smoother interface updates.
- Optional legacy Settings interface; the sidebar interface is the default.
- Direct hotkey reassignment from the Hotkeys settings page.
- Fixed version footer and compact page-specific scrolling.
- Full-width compact card text with a sharp high-resolution About logo.
- Scroll-contained Developer cards and sharper Segoe UI Variable typography.
- Fully padded Audio cards with a Fluent volume icon and correctly bounded slider.
- Sharp source artwork and a multi-resolution Windows icon for clear rendering at compact sizes.
- Opt-in Developer Mode with a picker for preserved Beta and legacy releases.
- Older-version controls remain hidden until Developer Mode is enabled.
- A compact Versions info field shows the selected release details below the picker.
- Opening an archived release closes the current Beta to prevent duplicate global hotkeys.
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
..\PulseMute Beta.exe
```

### Option 2: Build with the .NET SDK

If you have the .NET SDK installed:

```powershell
dotnet publish .\PulseMute.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o .\dist
```

## Usage

1. Open `PulseMute Beta.exe`.
2. Click the large button to mute or unmute.
3. Click either hotkey card to assign Key 1 or Key 2.
4. Press one keyboard key or one DualSense button for each slot.
5. Either assigned input can toggle the microphone from anywhere.
6. Open Settings to choose audio feedback, view the controller connection, or manually rescan it.
7. Click `Top` to keep the window above other windows.
8. Click `Hide` to keep it running from the system tray.

## Notes

PulseMute uses Windows Core Audio APIs to control the default communication capture device. It was built from scratch and does not reuse MicMute's original code.

Window and microphone settings are saved under the current user's app data folder. When enabled, auto-start is registered under the current user's Windows startup registry entry, so it does not require administrator rights.

Official Sony DualSense (`054C:0CE6`) and DualSense Edge (`054C:0DF2`) controllers are supported over USB and Bluetooth. Bluetooth uses a neutral, CRC-protected initialization report to expose the complete button report without changing controller LEDs, audio, or haptics.
