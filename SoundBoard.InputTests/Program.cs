using System;
using System.Linq;
using SoundBoard;
#if WINDOWS
using System.Windows.Input;
#endif

internal static class Program
{
    private static int passed;
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
    private static void Test(string name, Action test)
    {
        test();
        ++passed;
        Console.WriteLine("PASS " + name);
    }
    private static HotkeyBinding<string> Binding(string target = "sound", int input = 6, int modifiers = 0, bool global = true, bool hold = true)
    {
        return new HotkeyBinding<string> { Target = target, Input = input, Modifiers = modifiers, Global = global, Hold = hold };
    }
    private static HotkeyActivation<string> Down(HotkeyInputState<string> state, HotkeyBinding<string>[] bindings,
        int input = 6, int modifiers = 0, bool focused = false, bool suppressed = false)
    {
        return state.Press(input, modifiers, bindings, focused, suppressed);
    }
    private static void Up(HotkeyInputState<string> state, int input, string expectedStop, bool focused = false, bool suppressed = false)
    {
        string stop;
        state.Release(input, focused, suppressed, out stop);
        Check(stop == expectedStop, "Unexpected stop target: " + stop);
    }

    private static int Main()
    {
        try
        {
            Test("Mouse4 and Mouse5 independent", () =>
            {
                var s = new HotkeyInputState<string>();
                var b = new[] { Binding("four", 5), Binding("five", 6) };
                Check(Down(s, b, 5).Target == "four", "Mouse4");
                Check(Down(s, b, 6).Target == "five", "Mouse5");
                Up(s, 5, "four"); Up(s, 6, "five");
            });
            Test("Hold repeats do not retrigger; next physical press rearms", () =>
            {
                var s = new HotkeyInputState<string>(); var b = new[] { Binding() };
                var first = Down(s, b);
                for (int i = 0; i < 100; ++i) Check(Down(s, b) == null, "Repeat triggered");
                Up(s, 6, "sound"); Check(!first.IsValid, "Release must cancel the load");
                Check(Down(s, b) != null, "New physical press must start");
            });
            Test("Keyboard hold uses the same edge/repeat semantics", () =>
            {
                var s = new HotkeyInputState<string>(); var b = new[] { Binding(input: 0x41) };
                Check(Down(s, b, 0x41) != null, "A down");
                Check(Down(s, b, 0x41) == null, "A repeat"); Up(s, 0x41, "sound");
            });
            Test("Modifier-first release and focus loss still stop", () =>
            {
                var s = new HotkeyInputState<string>(); var b = new[] { Binding(modifiers: 2) };
                Down(s, b, 0x11, 2); var start = Down(s, b, modifiers: 2, focused: true);
                Check(start != null, "Modified down"); Up(s, 0x11, null); Up(s, 6, "sound", false);
            });
            Test("Modifier matching is exact; changing modifiers while held does not arm", () =>
            {
                var s = new HotkeyInputState<string>(); var b = new[] { Binding(modifiers: 2) };
                Check(Down(s, b, modifiers: 0) == null, "Missing Ctrl");
                Check(Down(s, b, modifiers: 2) == null, "Mid-hold Ctrl must not arm"); Up(s, 6, null);
                Check(Down(s, b, modifiers: 6) == null, "Extra Shift must not match"); Up(s, 6, null);
                Check(Down(s, b, modifiers: 2) != null, "Exact match");
            });
            Test("Editor suppression survives close until physical release", () =>
            {
                var s = new HotkeyInputState<string>(); var b = new[] { Binding() };
                Check(Down(s, b, suppressed: true) == null, "Editor capture played");
                Check(Down(s, b) == null, "Editor-close repeat played"); Up(s, 6, null);
                Check(Down(s, b) != null, "New press should work");
                Check(s.Cancel().Single() == "sound", "Editor opening must stop hold");
                Up(s, 6, null, suppressed: true);
            });
            Test("Release is processed even while editor is open", () =>
            {
                var s = new HotkeyInputState<string>(); var b = new[] { Binding() };
                var start = Down(s, b); Up(s, 6, "sound", suppressed: true);
                Check(!start.IsValid, "Suppressed release ignored");
            });
            Test("Release before dispatch cancels queued hold start", () =>
            {
                var s = new HotkeyInputState<string>(); var start = Down(s, new[] { Binding() });
                Up(s, 6, "sound"); Check(!start.IsValid, "Queued start survived release"); s.Dispatched(start);
            });
            Test("Stop-all cancels holds and normal queued starts; no held-repeat restart", () =>
            {
                var s = new HotkeyInputState<string>();
                var b = new[] { Binding(), Binding("normal", 5, hold: false) };
                var hold = Down(s, b); var normal = Down(s, b, 5); Up(s, 5, null);
                Check(s.Cancel().Single() == "sound", "Stop list");
                Check(!hold.IsValid && !normal.IsValid, "Queued work survived stop-all");
                Check(Down(s, b) == null, "Held repeat restarted"); Up(s, 6, null);
                Check(Down(s, b) != null, "Repress after stop-all");
            });
            Test("Target cancellation leaves independent sounds alone", () =>
            {
                var s = new HotkeyInputState<string>(); var b = new[] { Binding(), Binding("other", 5) };
                var a = Down(s, b); var other = Down(s, b, 5);
                Check(s.Cancel("sound").Single() == "sound", "Cancel target");
                Check(!a.IsValid && other.IsValid, "Other sound canceled");
                Up(s, 5, "other"); Up(s, 6, null);
            });
            Test("Duplicate bindings choose first global sound, not multiple sounds", () =>
            {
                var s = new HotkeyInputState<string>();
                var b = new[] { Binding("local", global: false), Binding("first"), Binding("second") };
                Check(Down(s, b, focused: true).Target == "first", "Duplicate winner"); Up(s, 6, "first");
            });
            Test("Two hold bindings on one sound share playback until both release", () =>
            {
                var s = new HotkeyInputState<string>(); var b = new[] { Binding(), Binding(input: 5) };
                var start = Down(s, b); Check(Down(s, b, 5) == null, "Second binding restarted");
                Up(s, 6, null); Check(start.IsValid, "First release stopped shared playback");
                Up(s, 5, "sound"); Check(!start.IsValid, "Last release must stop");
            });
            Test("Normal global playback starts on down once, release does not stop", () =>
            {
                var s = new HotkeyInputState<string>(); var b = new[] { Binding(hold: false) };
                var start = Down(s, b); Check(start != null && !start.Hold, "Normal down");
                Check(Down(s, b) == null, "Normal repeat"); Up(s, 6, null);
                Check(start.IsValid, "Normal release canceled playback");
            });
            Test("Normal local playback retains key-up behavior and focus gating", () =>
            {
                var s = new HotkeyInputState<string>(); var b = new[] { Binding(global: false, hold: false) };
                Check(Down(s, b) == null, "Local while unfocused"); Up(s, 6, null);
                Check(Down(s, b, focused: true) == null, "Local must wait for key-up");
                string stop; var start = s.Release(6, true, false, out stop);
                Check(start != null && stop == null, "Local key-up");
                Down(s, b, focused: true); Check(s.Release(6, false, false, out stop) == null, "Focus loss local start");
                Down(s, b, focused: true); Check(s.Release(6, true, true, out stop) == null, "Editor local start");
            });
            Test("Local hold starts on down; release works outside window", () =>
            {
                var s = new HotkeyInputState<string>(); var b = new[] { Binding(global: false) };
                Check(Down(s, b, focused: true) != null, "Local hold down"); Up(s, 6, "sound", false);
            });
            Test("Unmatched and duplicate releases are harmless", () =>
            {
                var s = new HotkeyInputState<string>(); Up(s, 6, null); Up(s, 6, null);
                Check(!s.PressedInputs.Any(), "Unmatched state leaked");
            });
            Test("Asynchronous load and stale completion cannot restart after stop", () =>
            {
                var gate = new PlaybackGate(); long oldLoad = gate.Begin();
                gate.Cancel(); Check(!gate.IsCurrent(oldLoad), "Stopped load still current");
                long newLoad = gate.Begin(); Check(gate.IsCurrent(newLoad), "New load not current");
                Check(!gate.IsCurrent(oldLoad), "Old completion can affect new playback");
                long stop = gate.Cancel(); Check(gate.IsCurrent(stop), "Queued stop version");
                gate.Begin(); Check(!gate.IsCurrent(stop), "Old queued stop can stop new playback");
            });
            Test("Release during async load invalidates both input and playback ownership", () =>
            {
                var state = new HotkeyInputState<string>(); var gate = new PlaybackGate();
                var start = Down(state, new[] { Binding() }); state.Dispatched(start);
                long load = gate.Begin(); Up(state, 6, "sound"); gate.Cancel();
                Check(!(start.IsValid && gate.IsCurrent(load)), "Canceled load may play");
            });
#if WINDOWS
            Test("All ten bare top-row digits remain distinct from numpad and round-trip", () =>
            {
                for (int digit = 0; digit < 10; digit++)
                {
                    var key = new Hotkey((Key)((int)Key.D0 + digit), ModifierKeys.None);
                    var num = new Hotkey((Key)((int)Key.NumPad0 + digit), ModifierKeys.None);
                    Check(key.DisplayText == digit.ToString(), "Top-row label");
                    Check(num.DisplayText == "Num " + digit, "Numpad label");
                    Check(key.VirtualKey == 0x30 + digit && key.VirtualKey != num.VirtualKey, "Wrong physical key");
                    Check(Hotkey.FromString(key.ToString()).VirtualKey == key.VirtualKey, "Persistence");
                    var state = new HotkeyInputState<string>();
                    var bindings = new[] { Binding(input: key.VirtualKey) };
                    Check(Down(state, bindings, input: num.VirtualKey) == null, "Numpad fired top-row binding");
                    Check(Down(state, bindings, input: key.VirtualKey) != null, "Bare digit did not fire");
                    Up(state, key.VirtualKey, "sound");
                }
            });
            Test("Readable labels preserve modifiers and canonical storage", () =>
            {
                var key = new Hotkey(Key.D2, ModifierKeys.Control);
                Check(key.DisplayText == "Ctrl + 2" && key.ToString() == "Ctrl + D2", "Modifier display/storage");
                Check(new Hotkey(MouseButton.XButton1, ModifierKeys.None).DisplayText == "Mouse 4", "Mouse label");
            });
            Test("Keyboard and mouse config strings round-trip with all modifier combinations", () =>
            {
                foreach (int mask in Enumerable.Range(0, 16))
                {
                    foreach (var key in new[] { new Hotkey(MouseButton.XButton1, (ModifierKeys)mask),
                        new Hotkey(MouseButton.XButton2, (ModifierKeys)mask), new Hotkey(Key.F8, (ModifierKeys)mask) })
                    {
                        var read = Hotkey.FromString(key.ToString());
                        Check(read != null && read.ToString() == key.ToString() && read.VirtualKey == key.VirtualKey, "Round-trip " + key);
                    }
                }
                Check(Hotkey.FromString("Ctrl + XButton1").VirtualKey == 5, "XButton1 alias");
                Check(Hotkey.FromString("XButton2").VirtualKey == 6, "XButton2 alias");
            });
            Test("Malformed and empty config hotkeys are safely ignored", () =>
            {
                foreach (var text in new[] { null, "", " ", "None", "Ctrl +", "Bogus + Mouse5", "Ctrl + NoSuchKey", "12" })
                    Check(Hotkey.FromString(text) == null, "Accepted malformed: " + text);
            });
#endif
            Console.WriteLine("PASS: " + passed + " tests");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine("FAIL: " + ex); return 1; }
    }
}
