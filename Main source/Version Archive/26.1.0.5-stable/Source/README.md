# PulseMute Main

PulseMute is a compact Windows microphone mute utility made by **Wrakeeb**.

Owner: [w-rakeeb](https://github.com/w-rakeeb) / Ra Kib

Current Stable release: `26.1.0.5-stable`

## Features

- Toggle a selected Windows microphone or the default communication microphone.
- Two assignable global hotkeys supporting keyboard keys, Mouse 1-5, mouse wheels, DualSense, and DualSense Edge controls.
- Optional Dual Hotkey mode; disabling it leaves only Key 1 active.
- Full DualSense input support over USB and Bluetooth, including Mute, PS, touchpad, triggers, D-pad, Edge Fn buttons, and rear paddles.
- Nine selectable mute/unmute feedback sounds with adjustable volume.
- Compact responsive main window, stay-on-top mode, tray operation, optional taskbar hiding, and remembered placement.
- Optional per-user Windows auto-start, disabled by default.
- Dark and White appearances plus customizable accent, creator line, background, surface, text, sidebar, and border colors.
- Five selectable mute-control designs.
- Compact sidebar Settings with General, Hotkeys, Audio, Controller, Customization, Developer, and About pages.
- Optional legacy Settings interface.
- Opt-in Developer Mode with animations control, hotkey-strip placement, preserved-version launching, and compact version information.
- Per-release settings, startup registration, and duplicate-instance identity so Stable, Beta, and archived builds remain separate.
- Per-monitor DPI awareness, buffered rendering, and high-resolution application artwork.

## Files

- `Program.cs` contains the Windows Forms application.
- `SidebarSettings.cs` contains the compact sidebar Settings interface.
- `PulseMute.csproj` contains project metadata.
- `build.ps1` builds `PulseMute Main.exe` with the supported Windows compiler path.
- `test-controller.ps1` runs synthetic input tests and a read-only controller smoke test when hardware is connected.
- `Dependencies` contains HidSharp 2.6.4 and its Apache 2.0 license.
- `Version Archive` contains preserved releases exposed through Developer Mode.

## Build

Open PowerShell in `Main source` and run:

```powershell
.\build.ps1
```

The executable is written to:

```text
..\PulseMute Main.exe
```

## Test

```powershell
.\test-controller.ps1
```

The test suite validates keyboard, mouse, controller, release-isolation, archive, and Settings contracts. A connected supported controller is checked read-only.

## Notes

PulseMute uses Windows Core Audio APIs to control microphone mute state. Settings are stored under the current user's app-data directory using a release-specific identity. Auto-start, when enabled, uses a release-specific value under the current user's Windows startup registry entry and does not require administrator rights.

Official Sony DualSense (`054C:0CE6`) and DualSense Edge (`054C:0DF2`) controllers are supported over USB and Bluetooth. Controller initialization does not intentionally change LEDs, audio, or haptics.
