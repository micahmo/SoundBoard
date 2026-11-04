using System;
using System.Collections.Generic;
using System.Linq;

namespace SoundBoard
{
    // Pure input state: no WPF, hooks, audio or clock dependencies. All calls on the UI thread.
    internal sealed class HotkeyBinding<T> where T : class
    {
        internal T Target;
        internal int Input;
        internal int Modifiers;
        internal bool Global;
        internal bool Hold;
    }

    internal sealed class HotkeyActivation<T> where T : class
    {
        internal T Target;
        internal bool Hold;
        internal bool OnRelease;
        internal bool IsValid = true;
    }

    internal sealed class HotkeyInputState<T> where T : class
    {
        // Null entries still consume repeats, including keys pressed while the editor is open.
        private readonly Dictionary<int, HotkeyActivation<T>> pressed = new Dictionary<int, HotkeyActivation<T>>();
        private readonly HashSet<HotkeyActivation<T>> pending = new HashSet<HotkeyActivation<T>>();
        internal IEnumerable<int> PressedInputs { get { return pressed.Keys.ToArray(); } }
        internal bool HasActivation(int input)
        {
            HotkeyActivation<T> activation;
            return pressed.TryGetValue(input, out activation) && activation != null;
        }

        internal HotkeyActivation<T> Press(int input, int modifiers, IEnumerable<HotkeyBinding<T>> bindings,
            bool allowLocal, bool suppressed)
        {
            if (pressed.ContainsKey(input)) return null;
            pressed.Add(input, null);
            if (suppressed) return null;

            // Global wins over local. Within each scope the first sound in tab/grid order wins.
            // This also gives deterministic behavior for duplicates in old/imported configs.
            var binding = bindings.Where(b => b.Input == input && b.Modifiers == modifiers && (b.Global || allowLocal))
                .OrderByDescending(b => b.Global).FirstOrDefault();
            if (binding == null) return null;

            var existing = pressed.Values.FirstOrDefault(a => a != null && a.IsValid && a.Hold && Equals(a.Target, binding.Target));
            if (existing != null)
            {
                pressed[input] = existing; // Two bindings for the same hold sound share one playback.
                return null;
            }
            var activation = new HotkeyActivation<T>
            {
                Target = binding.Target, Hold = binding.Hold,
                OnRelease = !binding.Global && !binding.Hold
            };
            pressed[input] = activation;
            if (activation.OnRelease) return null;
            pending.Add(activation);
            return activation;
        }

        internal HotkeyActivation<T> Release(int input, bool allowLocal, bool suppressed, out T stop)
        {
            stop = null;
            HotkeyActivation<T> activation;
            if (!pressed.TryGetValue(input, out activation)) return null;
            pressed.Remove(input);
            if (activation == null || !activation.IsValid) return null;
            if (activation.Hold)
            {
                if (!pressed.Values.Contains(activation))
                {
                    activation.IsValid = false;
                    pending.Remove(activation);
                    stop = activation.Target;
                }
                return null;
            }
            if (!activation.OnRelease) return null;
            if (!allowLocal || suppressed)
            {
                activation.IsValid = false;
                return null;
            }
            pending.Add(activation);
            return activation;
        }

        // A dispatched normal start is now owned by the sound's PlaybackGate, not this queue.
        internal void Dispatched(HotkeyActivation<T> activation) { pending.Remove(activation); }

        internal IEnumerable<T> Cancel(T target = null)
        {
            var affected = pressed.Values.Concat(pending).Where(a => a != null &&
                (target == null || Equals(a.Target, target))).Distinct().ToArray();
            var stops = affected.Where(a => a.Hold && a.IsValid).Select(a => a.Target).Distinct().ToArray();
            foreach (var activation in affected)
            {
                activation.IsValid = false;
                pending.Remove(activation);
            }
            // Keep physical-down entries until release: stop-all/editor closure must not rearm repeats.
            return stops;
        }
    }

    // Protects asynchronous preparation and stale playback-completion callbacks.
    internal sealed class PlaybackGate
    {
        private long version;
        internal long Begin() { return ++version; }
        internal long Cancel() { return ++version; }
        internal bool IsCurrent(long request) { return version == request; }
    }
}
