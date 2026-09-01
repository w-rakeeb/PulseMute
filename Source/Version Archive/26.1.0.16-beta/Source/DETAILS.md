# PulseMute Beta 26.1.0.16-beta

Owner: Wrakeeb (`w-rakeeb`)

## Beta baseline

- Based on PulseMute PS 1.1.
- Validation channel for promoted Test features.
- Supports keyboard, PlayStation controller, and mouse hotkeys.
- Includes compact clickable hotkey cards and optional Dual Hotkey mode.
- Includes opt-in Developer Settings with an archived-version launcher.
- Includes optional mute/unmute sound feedback with nine selectable styles and volume control.
- Uses a Silence-inspired frameless Settings shell with compact icon navigation.
- Uses Inter typography, neutral surfaces, card rows, and PulseMute red accents.
- Includes eased page transitions and animated toggles with an optional animation disable switch.
- Uses custom-painted dropdowns, sliders, navigation, cards, and command buttons.
- Shares one background, surface, text, and accent palette between the front page and Settings.
- Allows separate customization of the Settings sidebar and border colors.
- Includes a themed color editor with live preview, HEX/RGB input, presets, and copy/paste.
- Keeps custom dropdown menus alive through WinForms click and close processing, then disposes them with their owner.
- Places two compact hotkey cards inside the bottom toolbar between its icon buttons.
- Can move both hotkey cards back above the creator line from Developer Settings without hiding them.
- Uses `#111418` as the default dark Settings sidebar color.
- Opens a compact Settings window beside the main interface when monitor space allows.
- Isolates settings and Windows auto-start registration by executable location.
- Uses the professional microphone mark as the main application logo.
- Opens the new sidebar Settings by default with an optional legacy interface.
- Uses guarded icon rendering and Settings error handling.
- Uses isolated settings and auto-start registration.

## Layout

- `PulseMute Beta.exe` is the standalone main app.
- Editable and supporting files are stored in `Main source`.
- Includes repeatable controller protocol and hardware smoke tests.
