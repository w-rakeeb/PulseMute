# PulseMute Beta 26.1.0.21-beta

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
- Uses `#C13545` as the default accent across toggles, selection, sliders, and focus states.
- Uses a balanced `660 x 500` Settings shell with a `180 px` sidebar.
- Keeps full card titles and descriptions readable across compact and DPI-scaled layouts.
- Uses a sharp full-resolution logo in About and removes the redundant sidebar version.
- Offers Classic circle, Microphone tile, Wave badge, Record button, and Power ring mute controls.
- Isolates settings, duplicate instances, and Windows auto-start by executable and release.
- Starts every updated release with clean default settings.
- Contains Developer options in clean scrollable cards above the fixed footer.
- Uses Segoe UI Variable typography when available for clearer DPI-scaled text.
- Restores the preferred `26.1.0.19-beta` mute-control designs.
- Keeps Audio dropdowns, volume controls, and actions inside fully padded cards.
- Uses the full `1254 x 1254` master logo for sharp Settings rendering.
- Uses the professional microphone mark as the main application logo.
- Opens the new sidebar Settings by default with an optional legacy interface.
- Uses guarded icon rendering and Settings error handling.
- Uses isolated settings and auto-start registration.

## Layout

- `PulseMute Beta.exe` is the standalone main app.
- Editable and supporting files are stored in `Main source`.
- Includes repeatable controller protocol and hardware smoke tests.
