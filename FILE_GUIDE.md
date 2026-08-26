# PulseMute File Guide

## Start here

1. Open `PulseMute.code-workspace` in VS Code.
2. Open `Main source/Program.cs` to view the main application logic.
3. Open `Main source/SidebarSettings.cs` to edit the modern Settings window.
4. Press `Ctrl+Shift+B` to build the app.

The finished app is `PulseMute Main.exe` in this folder.

## Active files

| File | What it contains |
| --- | --- |
| `Main source/Program.cs` | App startup, main window, microphone control, hotkeys, mouse and DualSense support, themes, tray behavior, settings storage, and audio feedback. |
| `Main source/SidebarSettings.cs` | Settings window pages, navigation, custom controls, layout, and animations. |
| `Main source/PulseMute.csproj` | Project name, Windows target, NuGet dependency, icons, and embedded images. |
| `Main source/AssemblyInfo.cs` | App title, owner, and version shown by Windows. |
| `Main source/ControllerProtocolTests.cs` | Automated tests for hotkeys, controller reports, UI resources, release isolation, and archived versions. |
| `Main source/build.ps1` | Builds `PulseMute Main.exe` with the Windows C# compiler. |
| `Main source/test-controller.ps1` | Runs the automated test suite and checks a connected DualSense when available. |
| `Main source/Dependencies` | HidSharp files used for PlayStation controller communication. |
| `Main source/PulseMute-*.png/.svg/.ico` | App logos, Windows icons, and mute/unmute artwork. |
| `Updates` | Stable release notes and the complete change summary. |
| `Main source/Version Archive` | Read-only rollback copies. Do not edit these when changing the current app. |
| `DETAILS.md` | Compact technical summary of the current Stable release. |
| `README.md` | Full feature, build, and usage documentation. |
| `LICENSE` | Project license. |

## Safe editing routine

1. Edit `Program.cs` or `SidebarSettings.cs`.
2. Run the `Build PulseMute Main` task.
3. Run the `Test PulseMute Main` task.
4. Open the rebuilt `PulseMute Main.exe` only after both commands succeed.

`RecoverAllMics.cs` and `UnmuteMic.cs` are recovery utilities, not part of the normal app interface.
