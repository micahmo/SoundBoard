using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Threading;
using Gma.System.MouseKeyHook;
using Microsoft.Win32;
using Forms = System.Windows.Forms;

namespace SoundBoard
{
    // Ordinary non-suppressing Windows low-level input. Never sets Handled/SuppressKeyPress.
    // Hook callbacks only update small input state and enqueue audio work, keeping hooks responsive.
    internal sealed class SoundHotkeyService : IDisposable
    {
        private readonly MainWindow window;
        private readonly IKeyboardMouseEvents hook;
        private readonly HotkeyInputState<SoundButton> state = new HotkeyInputState<SoundButton>();
        private readonly DispatcherTimer releaseRecovery;
        private bool disposed;
        private bool wasSuppressed;
        private bool sessionSuspended;

        internal SoundHotkeyService(MainWindow window)
        {
            this.window = window;
            hook = Hook.GlobalEvents();
            try
            {
                hook.KeyDown += KeyDown;
                hook.KeyUp += KeyUp;
                hook.MouseDown += MouseDown;
                hook.MouseUp += MouseUp;
            }
            catch
            {
                hook.Dispose();
                throw;
            }
            // Recover lost releases after desktop switches; also stop holds when an editor opens.
            releaseRecovery = new DispatcherTimer(TimeSpan.FromMilliseconds(50), DispatcherPriority.Input,
                RecoverReleases, window.Dispatcher);
            window.PreviewKeyDown += LocalPreviewKey;
            window.PreviewKeyUp += LocalPreviewKey;
            SystemEvents.SessionSwitch += SessionSwitch;
            SystemEvents.PowerModeChanged += PowerModeChanged;
        }

        private bool Suppressed { get { return sessionSuspended || window.IsHotkeyPickerOpen || window.IsAnyDialogOpen || Utilities.AreAnyDialogsVisible(); } }

        private IEnumerable<HotkeyBinding<SoundButton>> Bindings()
        {
            foreach (var sound in window.GetSoundButtons().Where(s => s.HasValidSound))
            {
                if (sound.GlobalHotkey != null)
                    yield return Binding(sound, sound.GlobalHotkey, true);
                if (sound.LocalHotkey != null)
                    yield return Binding(sound, sound.LocalHotkey, false);
            }
        }

        private static HotkeyBinding<SoundButton> Binding(SoundButton sound, Hotkey key, bool global)
        {
            return new HotkeyBinding<SoundButton> { Target = sound, Input = key.VirtualKey,
                Modifiers = (int)key.Modifiers, Global = global, Hold = sound.HoldToPlay };
        }

        internal bool IsSoundKey(KeyEventArgs e)
        {
            if (Suppressed) return false;
            int input = KeyInterop.VirtualKeyFromKey(e.Key == Key.System ? e.SystemKey : e.Key);
            return state.HasActivation(input) || Bindings().Any(b => b.Input == input && b.Modifiers == (int)Keyboard.Modifiers);
        }

        private void LocalPreviewKey(object sender, KeyEventArgs e)
        {
            if (IsSoundKey(e)) e.Handled = true; // Only this WPF window: avoid search/Space-button double activation.
        }

        private void KeyDown(object sender, Forms.KeyEventArgs e) { Down((int)e.KeyCode); }
        private void KeyUp(object sender, Forms.KeyEventArgs e) { Up((int)e.KeyCode); }
        private void MouseDown(object sender, Forms.MouseEventArgs e)
        {
            if (e.Button == Forms.MouseButtons.XButton1) Down(5);
            else if (e.Button == Forms.MouseButtons.XButton2) Down(6);
        }
        private void MouseUp(object sender, Forms.MouseEventArgs e)
        {
            if (e.Button == Forms.MouseButtons.XButton1) Up(5);
            else if (e.Button == Forms.MouseButtons.XButton2) Up(6);
        }

        private void Down(int input)
        {
            if (disposed) return;
            bool suppressed = Suppressed;
            if (suppressed && !wasSuppressed) CancelHolds();
            wasSuppressed = suppressed;
            QueueStart(state.Press(input, (int)CurrentModifiers(), Bindings(), window.IsActive, suppressed));
        }

        private void Up(int input, bool recovered = false)
        {
            if (disposed) return;
            SoundButton stop;
            QueueStart(state.Release(input, !recovered && window.IsActive, recovered || Suppressed, out stop));
            if (stop != null) QueueStop(stop);
        }

        private void QueueStart(HotkeyActivation<SoundButton> activation)
        {
            if (activation == null) return;
            window.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                state.Dispatched(activation);
                if (disposed || !activation.IsValid || Suppressed ||
                    !window.GetSoundButtons().Contains(activation.Target))
                {
                    activation.IsValid = false;
                    return;
                }
                // Do not steal focus from a game or other foreground application.
                if (window.IsActive) activation.Target.ParentTab.Focus();
                activation.Target.StartSound(() => !disposed && activation.IsValid && !Suppressed, activation.Hold);
            }));
        }

        private void QueueStop(SoundButton sound)
        {
            long version = sound.CancelPendingPlayback();
            window.Dispatcher.BeginInvoke(DispatcherPriority.Input,
                new Action(() => sound.StopCanceledPlayback(version)));
        }

        internal void CancelHolds()
        {
            foreach (var sound in state.Cancel()) QueueStop(sound);
        }

        internal void CancelSound(SoundButton sound)
        {
            foreach (var heldSound in state.Cancel(sound)) QueueStop(heldSound);
        }

        private void RecoverReleases(object sender, EventArgs e)
        {
            bool suppressed = Suppressed;
            if (suppressed && !wasSuppressed) CancelHolds();
            wasSuppressed = suppressed;
            if (sessionSuspended) return; // GetAsyncKeyState is not meaningful on a secure desktop.
            foreach (int input in state.PressedInputs)
                if (!IsDown(input)) Up(input, true);
        }

        private void SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            window.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (disposed) return;
                sessionSuspended = e.Reason == SessionSwitchReason.SessionLock ||
                    e.Reason == SessionSwitchReason.SessionLogoff || e.Reason == SessionSwitchReason.RemoteDisconnect ||
                    e.Reason == SessionSwitchReason.ConsoleDisconnect;
                if (sessionSuspended) CancelHolds();
            }));
        }
        private void PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            window.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (disposed) return;
                if (e.Mode == PowerModes.Suspend) { sessionSuspended = true; CancelHolds(); }
                else if (e.Mode == PowerModes.Resume) sessionSuspended = false;
            }));
        }

        internal static ModifierKeys CurrentModifiers()
        {
            ModifierKeys result = ModifierKeys.None;
            if (IsDown(0x11)) result |= ModifierKeys.Control;
            if (IsDown(0x10)) result |= ModifierKeys.Shift;
            if (IsDown(0x12)) result |= ModifierKeys.Alt;
            if (IsDown(0x5B) || IsDown(0x5C)) result |= ModifierKeys.Windows;
            return result;
        }

        private static bool IsDown(int key) { return (GetAsyncKeyState(key) & 0x8000) != 0; }
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int virtualKey);

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            releaseRecovery.Stop();
            window.PreviewKeyDown -= LocalPreviewKey;
            window.PreviewKeyUp -= LocalPreviewKey;
            SystemEvents.SessionSwitch -= SessionSwitch;
            SystemEvents.PowerModeChanged -= PowerModeChanged;
            state.Cancel();
            hook.KeyDown -= KeyDown;
            hook.KeyUp -= KeyUp;
            hook.MouseDown -= MouseDown;
            hook.MouseUp -= MouseUp;
            hook.Dispose();
        }
    }
}
