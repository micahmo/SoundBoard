# Keyboard and mouse hotkeys

## Assign a shortcut

1. Right-click a sound and choose **Set Hotkeys**. If it already has a shortcut, click the **large hotkey badge** on its tile instead. Clicking the badge edits the shortcut; clicking the sound name plays the sound normally.
2. Choose **Global Hotkey** to activate the sound from another application, or **Local Hotkey** to activate it only while SoundBoard is focused.
3. Focus the capture field and press a keyboard key or a side mouse button. Alternatively, use the explicit **Mouse4**, **Mouse5**, or **Top row — no modifiers** buttons.
4. Enable **Hold to play (hotkeys)** if desired, then click **OK**. **Cancel** leaves the binding unchanged.

Mouse4 means XButton1 and Mouse5 means XButton2. Ctrl, Shift, Alt and Win can be combined with keyboard or mouse bindings. The explicit mouse buttons allow editing without generating a browser-back/forward or push-to-talk input. Escape, Delete or Backspace without modifiers clears the focused capture field; the **Clear** button does the same.

The editor rejects duplicate bindings across sounds and identical local/global bindings on one sound. Legacy/imported duplicates have deterministic ownership: global wins over local, then the first sound in tab/grid order wins within that scope.

## Number-row keys and compact keyboards

A numpad is not required. The **1 2 3 4 5 6 7 8 9 0** selector assigns the corresponding top-row key **alone**, clearing modifier checkboxes. You can also press a number-row key in the capture field. Add modifiers afterward if needed.

Top-row and numpad keys remain different inputs:

| Binding | Display | Stored value |
| --- | --- | --- |
| Top-row 1 | `1` | `D1` |
| Ctrl + top-row 1 | `Ctrl + 1` | `Ctrl + D1` |
| Numpad 1 | `Num 1` | `NumPad1` |
| Second side mouse button | `Mouse 5` | `Mouse5` |

The underlying keyboard capture already accepts modifier-free numbers; this change makes that capability explicit and accessible in the picker, and distinguishes it from numpad input in labels and tests.

## Hold versus normal playback

| Mode | Press | Release |
| --- | --- | --- |
| Normal global shortcut | Starts playback once | Does not stop playback |
| Normal local shortcut | Arms while focused | Starts playback if still focused |
| Hold shortcut | Starts once from the beginning | Stops playback, even if modifiers were released first or focus changed |

- **Loop** remains a separate per-sound option. Enable Loop + Hold to repeat while held; without Loop, the clip ends naturally and will not restart until a fresh press.
- Key-repeat does not repeatedly restart the sound.
- If two distinct held bindings target the same sound, it plays once and stops after the last binding is released.
- Held playback does not follow **Next Sound**. Normal playback retains sound chaining.
- Clicking a sound tile or its transport controls remains ordinary playback; Hold applies to configured hotkeys, not dragging or left-clicking the tile.
- **Stop All** cancels queued starts, file preparation and active playback. Continuing to hold the input will not restart a canceled sound; release and press again.
- Opening a hotkey/dialog editor cancels active holds. Input captured while editing cannot start playback when the editor closes until the input is released and pressed again.
- Session lock/disconnect, suspend, configuration reload and app close cancel held or pending playback. A recovery timer checks for missed releases.

## Main-grid badges

Every assigned sound displays a large, high-contrast shortcut badge. It shows the global binding when present, otherwise the local binding. Its smaller footer identifies **GLOBAL** or **LOCAL**, **HOLD** when enabled, the secondary local shortcut if both scopes exist, and any volume offset. The sound name remains separate above the badge, with transport/menu controls below it.

Click the badge to open **Set Hotkeys** without activating the sound. Badges refresh after editing, clearing, undoing, copying/swapping or loading a configuration. Unassigned tiles do not show an empty badge.

## Input reaches other applications

Sound shortcuts now use the existing MouseKeyHook package's non-suppressing global keyboard/mouse events rather than exclusive keyboard registrations. SoundBoard does **not** consume or remap foreground input, inject into games, or change their bindings.

Consequently, a number key can activate both a sound and a game action; a side button can also trigger browser navigation or push-to-talk. Avoid conflicting shortcuts. Registered operating-system shortcuts and protected/elevated applications can still impose limits. SoundBoard's own quick-search handler yields to matching sound shortcuts when SoundBoard is focused.

## Drag-and-drop and administrator mode

Run SoundBoard normally when dragging audio files from File Explorer. Windows blocks ordinary Explorer drag-and-drop into an elevated process. SoundBoard's **ADMIN** warning indicates that situation when UAC is enabled.

If drops stop working after a relaunch:

1. Close SoundBoard normally so settings are saved.
2. Open it from Explorer or a normal desktop shortcut, **not** with **Run as administrator** or from an elevated launcher.
3. Confirm the ADMIN indicator is gone and retry the drop.

Do not disable UAC or elevate Explorer as a workaround. This is an existing Windows integrity-level restriction, not a new file-format restriction. During local validation, an elevated automation launcher caused this symptom; restarting unelevated removed the mismatch. A physical file-drop retest remains separate from verifying the warning disappeared.

## Configuration compatibility

Existing `localHotkey` and `globalHotkey` attributes keep their canonical strings. Mouse keys serialize as `Mouse4` / `Mouse5`, optionally with modifiers; the parser also accepts `XButton1` / `XButton2` aliases. New `holdToPlay="True|False"` defaults to **False** when absent or invalid, so older configurations retain normal playback.

Example sound attributes (not a complete configuration):

```xml
globalHotkey="Ctrl + Mouse5" localHotkey="" holdToPlay="True" loop="True"
```

Save/load, import/export, undo and sound copy/swap carry the new field. Binding/mode changes save immediately. Output-device identifiers, sound paths and microphone routing are not migrated or rewritten by this feature. Back up configurations before opening them in older releases, which do not understand mouse shortcuts or hold mode.

## Implementation and verification

See [development and testing](hotkey-development.md) for the input/playback design, build commands, executed checks, remaining manual checks and the distinction between the local custom build and this upstream contribution.
