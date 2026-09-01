# PulseMute file guide

## Start here

- Open `PulseMute.code-workspace` in VS Code to view the complete project.
- Open `PulseMute.sln` in Visual Studio.
- Run `PulseMute.exe` to use the portable release.

## Main app code

- `Source/Program.cs` contains the main window, microphone, hotkey, tray, and controller logic.
- `Source/SidebarSettings.cs` contains the Settings pages and custom interface controls.
- `Source/PulseMute.csproj` contains project metadata, version, icon, and embedded assets.
- `Source/AssemblyInfo.cs` contains Windows executable metadata.

## Build and test

- Run `Source/build.ps1` to build `PulseMute.exe` in the repository root.
- Run `Source/test-controller.ps1` for automated app and controller checks.
- `Source/ControllerProtocolTests.cs` contains those test cases.

## Website

- `src/App.jsx` contains the website interface and content.
- `src/styles.css` contains website styling.
- `public` contains screenshots, artwork, and the downloadable `PulseMute.exe`.
- `package.json` contains website commands and dependencies.
- `vercel.json` contains download-response headers for Vercel.

## Supporting files

- `Logo/PulseMute Muted.svg` and `Logo/PulseMute Unmute.svg` are the correctly named master vector logos.
- `Source/Dependencies` contains HidSharp and its license.
- `Source/*.png`, `*.svg`, and `*.ico` contain app artwork.
- `Updates` contains Stable release notes.
- `Legacy Versions` and `Source/Version Archive` contain preserved releases used by Developer Mode; do not edit them.
- `Screenshots` contains the images displayed on GitHub.
