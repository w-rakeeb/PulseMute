# PulseMute Beta file guide

## Start here

- Open `PulseMute.code-workspace` in VS Code to view the complete project.
- `Main source/Program.cs` is the main application source and microphone/hotkey logic.
- `Main source/SidebarSettings.cs` contains the Settings window and custom controls.
- `Main source/PulseMute.csproj` defines the C# project, version, icon, and embedded assets.

## Build and test

- Run `Main source/build.ps1` to create `PulseMute Beta.exe`.
- Run `Main source/test-controller.ps1` for automated behavior and UI checks.

## Supporting files

- `Main source/ControllerProtocolTests.cs` contains automated tests.
- `Main source/Dependencies` contains the controller input dependency and license.
- `Main source/*.png`, `*.svg`, and `*.ico` are app and mute-control artwork.
- `Updates` contains the release notes.
- `Legacy Versions` contains portable older releases used by Developer Mode.
- `Main source/Version Archive` contains frozen rollback versions; do not edit them.
