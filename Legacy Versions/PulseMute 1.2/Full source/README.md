# PulseMute

PulseMute is a compact Windows microphone mute utility made by **Wrakeeb**.

Owner: [w-rakeeb](https://github.com/w-rakeeb) / Ra Kib

Version 1.2 adds a settings panel for managing Windows auto-start and selecting which microphone PulseMute controls.

## Features

- Toggle the default Windows communication microphone.
- One-key global shortcut, defaulting to `F8`.
- In-app shortcut picker.
- Silent mute/unmute behavior.
- Resizable interface for compact, mid-size, and larger windows.
- Controls and main mute button scale with the window size.
- Stay-on-top toggle.
- Auto-start on/off setting.
- Microphone selection dropdown with a Windows-default option.
- Remembers the selected microphone.
- Branded application, taskbar, and tray icon.
- Remembers window size, position, and stay-on-top state.
- System tray support.
- Credit line: `made by Wrakeeb`.

## Files

- `Program.cs` contains the full Windows Forms application source.
- `PulseMute.csproj` is the modern .NET project file.
- `build.ps1` compiles the app on Windows using the built-in .NET Framework C# compiler.
- `LICENSE` contains the project license.

## Build

### Option 1: Build with the included script

Open PowerShell in the repository folder and run:

```powershell
.\build.ps1
```

The compiled app will be written beside the `Full source` folder:

```text
..\PulseMute 1.2.exe
```

### Option 2: Build with the .NET SDK

If you have the .NET SDK installed:

```powershell
dotnet publish .\PulseMute.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o .\dist
```

## Usage

1. Open `PulseMute.exe`.
2. Click the large button to mute or unmute.
3. Press the configured key from anywhere to toggle.
4. Click `Change`, then press one key to set a new shortcut.
5. Open the settings button to manage auto-start or select a microphone.
6. Click `Top` to keep the window above other windows.
7. Click `Hide` to keep it running from the system tray.

## Notes

PulseMute uses Windows Core Audio APIs to control the default communication capture device. It was built from scratch and does not reuse MicMute's original code.

Window and microphone settings are saved under the current user's app data folder. When enabled, auto-start is registered under the current user's Windows startup registry entry, so it does not require administrator rights.
