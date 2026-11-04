# Mouse Hold Custom build

Local modification of micahmo/SoundBoard 1.10.2 (.NET Framework 4.8/WPF). The window title and About box identify **Mouse Hold Custom**, with informational version `1.10.2-mouse-hold.3`. Automatic and manual upstream update checks are disabled so an official binary cannot silently replace this feature build. The assembly/config directory names remain unchanged for compatibility.

## Controls and behavior

- Main-grid tiles show a large high-contrast hotkey badge plus GLOBAL/LOCAL and HOLD status. Top-row digits read `1`–`0` (not `D1`); numpad keys read `Num 1` etc. Volume offsets share the badge footer to avoid overlap. Labels refresh when bindings/mode are changed or restored. Click the large badge to open Set Hotkeys; its click is handled separately from sound playback.
- The picker includes a **Top row — no modifiers** 1–0 button row. Click a digit to select the ordinary number-row key alone, clearing modifiers, or focus the capture field and press the key. Existing bindings are not changed automatically. Keys still reach the foreground game/app.

- Right-click a sound → **Set Hotkeys**. Focus a picker and press a keyboard key or side mouse button, or select the explicit **Mouse4** / **Mouse5** buttons. Modifier checkboxes permit mouse selection without generating a side-button press. Mouse4 is XButton1; Mouse5 is XButton2.
- No bindings are assigned automatically. In particular, existing Mouse4/PTT mappings are not changed.
- **Hold to play (hotkeys)** is available in that dialog and alongside Loop in the sound's context menu. Off preserves normal playback: global bindings start on down, local bindings on up. On starts from the beginning once per physical press and stops on release, including after releasing modifiers first or switching to another window. A second binding for the same held sound does not restart it; the last held binding's release stops it.
- Existing **Loop** determines whether the sound repeats while held. A non-looping sound ends naturally and does not restart until a fresh press. Held playback does not chain to Next Sound; normal clicked/hotkey playback retains Next Sound behavior.
- Clicking sound tiles and their transport controls remains ordinary playback; the hold option applies to configured hotkeys, not left-click dragging/selecting.
- Both global keyboard and mouse bindings use the shared MouseKeyHook monitor. No exclusive RegisterHotKey reservation, game injection, or global input suppression is used. The foreground application still receives the input, including its browser-back/PTT actions. Choose a non-conflicting binding; the explicit picker buttons avoid those side effects while editing.
- The editor rejects duplicate assignments. Duplicates already in imported/legacy configs are deterministic: a matching global binding wins over local; the first sound in tab/grid order wins within a scope.
- Opening an editor cancels holds, and input entered while it is open cannot rearm until released. Stop All cancels queued and preparing sounds as well as active players. A canceled load cannot restart playback. Hook callbacks enqueue audio work; file/codec preparation runs asynchronously.
- Lock/suspend cancels holds, release recovery checks for lost key-up events, and closing the app disposes the hooks and audio resources.

## Configuration compatibility

Existing `localHotkey` and `globalHotkey` strings are retained (e.g. `Ctrl + Alt + F8`). Mouse bindings serialize as `Mouse4`, `Mouse5`, or e.g. `Ctrl + Mouse5`; the parser also accepts `XButton1` / `XButton2`. Each sound adds `holdToPlay="True|False"`; absent/invalid means **False**. Copy/swap, undo, normal save/load, and config import/export carry these fields. Hotkey/mode changes are undoable and saved immediately. No user configuration, output-device GUID, audio routing, or sound file is changed by the source patch.

## Build and tests (Windows, no SDK/test packages required)

From the repository in PowerShell, with Visual Studio's .NET desktop tools and .NET 4.8 targeting pack:

```powershell
$msbuild = 'C:\Program Files\Microsoft Visual Studio\18\Enterprise\MSBuild\Current\Bin\MSBuild.exe'
& $msbuild .\SoundBoard.sln /restore /p:RestorePackagesConfig=true /p:Configuration=Release /v:minimal /nologo
if ($LASTEXITCODE -ne 0) { throw 'App build failed' }
& $msbuild .\SoundBoard.InputTests\SoundBoard.InputTests.csproj /p:Configuration=Release /v:minimal /nologo
if ($LASTEXITCODE -ne 0) { throw 'Test build failed' }
& .\SoundBoard.InputTests\bin\Release\SoundBoard.InputTests.exe
if ($LASTEXITCODE -ne 0) { throw 'Input tests failed' }
```

The standalone classic console project links the **production** input-state and hotkey model sources. It has no NuGet/test-framework dependencies. It tests physical edges, repeats, modifier order, focus changes, suppression, queued cancellation, duplicate resolution, overlapping holds, normal/local semantics, playback request invalidation, and all modifier combinations in config-string round trips. It does not install hooks or play audio.

The platform-independent subset can also be exercised with Mono:

```sh
mcs -warnaserror -out:SoundBoard.InputTests.exe SoundBoard/Hotkeys/HotkeyInputState.cs SoundBoard.InputTests/Program.cs
mono SoundBoard.InputTests.exe
```

## Windows acceptance checks

Back up the existing app/config before deployment; keep Wave Link settings and library intact. Test outside an active match, without PTT transmission:

1. Build app and console tests; start the labeled executable. Confirm saved output devices and existing sounds remain intact.
2. Pick Mouse5 using the explicit UI button, enable Hold, save. With a safe test sound, verify down starts once, repeat does not restart, and release stops. Repeat with Ctrl+Mouse5, releasing Ctrl first, and with SoundBoard unfocused. Check keyboard holds too.
3. Turn Loop on/off and verify held looping versus single completion. Test ordinary keyboard shortcuts, local key-up behavior, tile clicks, pause/resume, and normal Next Sound chains.
4. Tap/release rapidly during a large file's preparation, then Stop All while loading/held. There must be no late playback or repeat restart. Stop All and manual Stop must not trigger Next Sound.
5. Open a picker while holding, change/clear a binding, close with the button still down, and check that nothing restarts until a fresh press. Verify keys/buttons still reach the foreground app.
6. Save/restart, copy/swap/undo, and export/import a disposable configuration. Check Mouse5/modifiers/hold/loop survive; importing an older config defaults to normal mode. Test duplicate imported bindings once and verify only the documented winner plays.
7. Check lock/unlock and app close stop held playback. Wave Link routing/audio and physical mouse/game behavior require real Windows verification; pure unit tests do not prove them.
