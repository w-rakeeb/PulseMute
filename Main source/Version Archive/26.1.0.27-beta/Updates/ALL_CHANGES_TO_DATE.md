# PulseMute Beta - all changes to date

Current version: `26.1.0.27-beta`
Channel: Beta validation build

## Before

- Began as a basic Windows microphone mute utility with one keyboard shortcut.

## Added over time

- Microphone mute and unmute with selectable microphones.
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
- Opt-in Developer Settings with a verified archived-version picker.
- Developer version controls stay hidden until Developer Mode is enabled.
- A compact Versions info field describes every archived release below the picker.
- Developer content uses its actual visible height to avoid excess scroll space across DPI scales.
- Preserved Beta `26.1.0.3-beta` as the first selectable Beta snapshot.
- Preserved Beta `26.1.0.4-beta` as a complete source and executable backup.
- Optional sound feedback with distinct mute and unmute tones.
- Four built-in sound styles with a remembered Settings selection.
- Preserved Beta `26.1.0.5-beta` with its executable, source, and update history.
- Rebuilt Settings with a responsive category sidebar.
- Added dedicated General, Hotkeys, Audio, Controller, Customization, Developer, and About pages.
- Added direct Key 1 and Key 2 reassignment controls to Settings.
- Expanded feedback audio to nine styles, including 8-Bit, Arcade, Radio, Glass, and Signal.
- Added remembered feedback-volume control.
- Added subtle toggle and page-change animations with a Developer disable switch.
- Added slightly rounded controls with a Developer square-corner switch.
- Added buffered rendering and faster microphone-state refresh for smoother updates.
- Added a professional alternate logo as an opt-in Developer preview; the current logo remains default.
- Promoted the professional microphone mark to the main Beta logo.
- Removed custom corner rounding and its Developer option.
- Added a new/legacy Settings interface switch with the sidebar interface as default.
- Added safe icon rendering and guarded Settings startup to prevent the reported array-range crash.
- Isolated Beta settings, startup registration, source, executable, and update history.
- Preserved the complete `26.1.0.8-beta` executable and source snapshot.
- Rebuilt native Settings around the Silence visual language without using the ceased hybrid runtime.
- Added a frameless resizable shell, icon sidebar, Inter typography, neutral cards, and red accents.
- Added custom-painted dropdowns, slider, navigation, cards, title controls, and command buttons.
- Replaced the linear page slide with a short eased transition while retaining the animation toggle.
- Preserved the complete `26.1.0.9-beta` executable, source, and update history.
- Unified the native Settings palette with the front-page background, surface, text, and accent colors.
- Added persisted Settings sidebar and border color controls to Customization.
- Preserved the complete `26.1.0.10-beta` executable, source, and update history.
- Replaced the basic customization picker with a themed color editor.
- Added live preview, HEX/RGB editing, quick presets, advanced selection, and color-code copy/paste.
- Preserved the complete `26.1.0.11-beta` executable, source, and update history.
- Fixed the custom Settings dropdown disposing its menu during WinForms item-click processing.
- Added dropdown lifetime regression coverage and audited Settings-owned timers, images, menus, and events.
- Preserved the complete `26.1.0.12-beta` executable, source, and update history.
- Moved both main-window hotkey cards below the creator line.
- Added a Developer option to show or hide the hotkey strip.
- Reduced the standard main-window minimum size to `200 x 250`.
- Isolated settings files and Windows Run values by executable path.
- New instance defaults keep auto-start and hide-from-taskbar disabled.
- Preserved the complete `26.1.0.13-beta` executable, source, and update history.
- Moved Key 1 and Key 2 into the bottom toolbar between the left and right icon buttons.
- Reduced the hotkey-card height and typography to match the toolbar without overlap.
- Preserved the complete `26.1.0.14-beta` executable, source, and update history.
- Removed the Developer behavior that could hide both hotkeys completely.
- Replaced it with a position switch between the bottom toolbar and the previous upper layout.
- Removed the old `ShowHotkeyStrip` config key so hidden cards cannot carry into this version.
- Preserved the complete `26.1.0.15-beta` executable, source, and update history.
- Changed the default dark Settings sidebar color to `#111418`.
- Reduced Settings from `760 x 590` to `680 x 520` with a `620 x 460` minimum.
- Narrowed and tightened the sidebar, title bar, footer, navigation, and content area.
- Settings now opens beside the main window, falling back to the opposite side and staying inside the monitor work area.
- Preserved the complete `26.1.0.16-beta` executable, source, and update history.
- Reduced Settings again to `600 x 460` with a `560 x 420` minimum.
- Narrowed the sidebar to `164 px` and tightened navigation, typography, cards, margins, title bar, and footer.
- Changed the default accent to `#C13545` across every shared accent-driven control.
- Preserved the complete `26.1.0.17-beta` executable, source, and update history.
- Fixed compact Settings titles and descriptions collapsing into partial words or single letters.
- Removed the redundant version label from the sidebar and sharpened the About logo rendering.
- Added five selectable mute-control visuals, including four new vector icon designs.
- Added release-specific settings, auto-start, and duplicate-instance identities.
- New updates now start with clean default settings without reusing another PulseMute release's state.
- Preserved the complete `26.1.0.18-beta` executable, source, and update history.
- Rebuilt Developer options as scroll-contained cards that stay clear of the footer.
- Increased Settings to a balanced `660 x 500` layout with a `180 px` sidebar.
- Replaced Shield mic and Signal hex with Record button and Power ring visuals.
- Changed Settings typography to Segoe UI Variable with clearer, roomier text sizing.
- Preserved the complete `26.1.0.19-beta` executable, source, and update history.
- Added a proper Fluent speaker icon and bounded Feedback volume card.
- Fixed Sound style dropdown bottom clipping and Audio-page scaling.
- Rebuilt the five mute choices with circle, vertical capsule, horizontal pill, diamond, and speech-bubble silhouettes.
- Embedded the full `1254 x 1254` professional logo master for sharper rendering.
- Preserved the complete `26.1.0.20-beta` executable, source, and update history.
- Restored the preferred `26.1.0.19-beta` mute-control designs.
- Kept the `26.1.0.20-beta` Audio scaling, volume icon, and full-resolution logo improvements.
- Preserved the finalized complete `26.1.0.21-beta` executable, source, and update history.
- Replaced the Beta logo with the supplied red muted-microphone artwork.
- Generated matching multi-resolution executable, window, taskbar, tray, title-bar, Settings, and About assets.
- Preserved the complete `26.1.0.22-beta` executable, source, and update history.
- Added three supplied SVG logo designs as embedded selectable Beta artwork.
- Added a persistent App logo selector to Customization with immediate window, tray, Settings, and About refresh.
- Preserved the complete `26.1.0.23-beta` executable, source, and update history.
- Added the supplied red and green microphone SVGs as one state-aware mute-control design.
- Added Red/green mic to Customization, using red while muted and green while live.
- Preserved the complete `26.1.0.24-beta` executable, source, and update history.
- Replaced the muted-state artwork with the corrected supplied `#A02A39` SVG.
- Preserved the complete `26.1.0.25-beta` executable, source, and update history.
- Made Rounded square the default runtime, Settings, About, tray, taskbar, and executable logo.
- Made Red/green mic the default mute control.
- Preserved the complete `26.1.0.26-beta` executable, source, and update history.
- Replaced the primary identity with the supplied `#FB453D` circular muted-microphone SVG.
- Applied the supplied identity to the executable, window, taskbar, tray, Settings, and About surfaces.

## Now

- PulseMute Beta now uses the supplied circular muted-microphone identity everywhere and retains the red/green state-aware microphone control.
