// https://tyrrrz.me/blog/hotkey-editor-control-in-wpf
using System;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace SoundBoard
{
    /// <summary>
    /// Represents a Hotkey
    /// </summary>
    public class Hotkey
    {
        /// <summary>
        /// The key (without the modifier)
        /// </summary>
        public Key Key { get; }

        /// <summary>
        /// The modifier
        /// </summary>
        public ModifierKeys Modifiers { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public Hotkey(Key key, ModifierKeys modifiers)
        {
            Key = key;
            Modifiers = modifiers;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var str = new StringBuilder();

            if (Modifiers.HasFlag(ModifierKeys.Control))
                str.Append("Ctrl + ");
            if (Modifiers.HasFlag(ModifierKeys.Shift))
                str.Append("Shift + ");
            if (Modifiers.HasFlag(ModifierKeys.Alt))
                str.Append("Alt + ");
            if (Modifiers.HasFlag(ModifierKeys.Windows))
                str.Append("Win + ");

            str.Append(Key);

            return str.ToString();
        }

        /// <summary>
        /// "Deserializes" a string to a HotKey
        /// </summary>
        public static Hotkey FromString(string hotkeyStr)
        {
            ModifierKeys modifierKeys = default;

            if (hotkeyStr.Contains("Ctrl + "))
            {
                modifierKeys |= ModifierKeys.Control;
            }

            if (hotkeyStr.Contains("Shift + "))
            {
                modifierKeys |= ModifierKeys.Shift;
            }

            if (hotkeyStr.Contains("Alt + "))
            {
                modifierKeys |= ModifierKeys.Alt;
            }

            if (hotkeyStr.Contains("Win + "))
            {
                modifierKeys |= ModifierKeys.Windows;
            }

            string keyStr = hotkeyStr.Contains('+') ? hotkeyStr.Substring(hotkeyStr.LastIndexOf("+") + 2, hotkeyStr.Length - hotkeyStr.LastIndexOf("+") - 2) : hotkeyStr;
            Key key = Enum.GetValues(typeof(Key)).OfType<Key>().FirstOrDefault(k => k.ToString() == keyStr);

            if (key != default)
            {
                return new Hotkey(key, modifierKeys);
            }

            return default;
        }
    }
}