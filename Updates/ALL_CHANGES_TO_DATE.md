# PulseMute Main - all changes to date

Current version: `26.1.0.9-stable`
Channel: Stable daily-use build

## Before

- Began as a basic Windows microphone mute utility with one keyboard shortcut.

## Added over time

- Silent microphone mute and unmute with selectable microphones.
- Compact, resizable interface with responsive controls and clear DPI rendering.
- Stay on top, tray operation, taskbar hiding, auto-start, and remembered placement.
- Dark and White themes, Windows Sound settings access, and custom interface colors.
- Professional settings controls, app branding, icon, and creator credit.
- DualSense and DualSense Edge support over USB and Bluetooth.
- Every DualSense button, including Mute, PS, touchpad, Fn buttons, and paddles.
- Two independent keyboard or PlayStation shortcut slots.
- Mouse 1-5 and vertical or horizontal wheel hotkeys in either shortcut slot.
- Compact side-by-side clickable hotkey cards without separate edit buttons.
- Optional Dual Hotkey mode for enabling or disabling Key 2.
- Smaller scrollable settings with improved dark-mode scrollbar styling.
- Sharper responsive typography and tested compact, medium, and large layouts.
- Responsive category Settings with General, Hotkeys, Audio, Controller, Customization, Developer, and About pages.
- Optional legacy Settings interface with the new sidebar interface as default.
- Nine selectable mute/unmute sounds and remembered feedback-volume control.
- Professional microphone logo across the executable, window, tray, and About page.
- Buffered page transitions with an optional animation switch.
- Safe icon rendering and guarded Settings startup for the reported array-range error.
- Isolated Stable settings, startup registration, source, executable, and update history.
- Full keyboard, mouse, DualSense, and DualSense Edge input support across two assignable hotkey slots.
- Finalized compact Audio controls, feedback-volume scaling, and preferred mute-control designs.
- Opt-in Developer Mode with animations control, hotkey-strip placement, archived-version launching, and version information.
- Release-specific settings, startup entries, and instance identity for Stable, Beta, and archived builds.
- Four persistent app-logo choices with immediate window, tray, Settings, and About updates.
- Embedded vector-derived logo assets with multi-resolution Windows icons.
- A supplied red/green state-aware microphone control with corrected `#A02A39` muted color.
- Rounded square branding as the default app and executable icon.
- Red/green mic as the default mute-control design.
- Preserved complete Stable `26.1.0.5-stable` source, executable, and update history.
- Preserved complete Stable `26.1.0.6-stable` source, executable, and update history.
- Replaced the primary identity with the supplied `#FB453D` circular muted-microphone SVG.
- Applied the supplied identity to the executable, window, taskbar, tray, Settings, and About surfaces.
- Preserved the complete `26.1.0.7-stable` source, executable, and update history.
- Corrected the primary identity to the supplied `#A02A39` circular muted-microphone SVG.
- Updated the executable, window, taskbar, tray, Settings, and About artwork to the corrected identity.
- Finalized active source ownership, added VS Code project files and a plain-language file guide, and passed 575 regression assertions.

## Now

- PulseMute uses the correctly named `PulseMute Muted.svg` and `PulseMute Unmute.svg` master artwork.
- Preserved `26.1.0.8-stable` as a complete rollback release.
- Replaced raw missing-device exception dialogs with a quiet `No microphone detected` state.
- Added automatic fallback to the Windows default input when a saved microphone is disconnected.
- Mute clicks and hotkeys remain silent while no capture device exists, then recover automatically when one appears.
- Published `26.1.0.9-stable` as `PulseMute.exe` with complete app and website source.
- Connected GitHub `main` to automatic Vercel production deployment.
- Kept the local PulseMute Main channel separate from public-release branding.
