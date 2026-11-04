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

        /// <summary>Side mouse button, or null for a keyboard binding.</summary>
        public MouseButton? MouseButton { get; private set; }

        /// <summary>Windows virtual-key identity (XButton1 = 5, XButton2 = 6).</summary>
        public int VirtualKey => MouseButton == System.Windows.Input.MouseButton.XButton1 ? 5 :
            MouseButton == System.Windows.Input.MouseButton.XButton2 ? 6 : KeyInterop.VirtualKeyFromKey(Key);

        /// <summary>Creates a side-button binding without assigning any keyboard key.</summary>
        public Hotkey(MouseButton mouseButton, ModifierKeys modifiers)
        {
            if (mouseButton != System.Windows.Input.MouseButton.XButton1 && mouseButton != System.Windows.Input.MouseButton.XButton2)
                throw new ArgumentOutOfRangeException(nameof(mouseButton));
            MouseButton = mouseButton;
            Modifiers = modifiers;
        }

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

            str.Append(MouseButton == System.Windows.Input.MouseButton.XButton1 ? "Mouse4" :
                MouseButton == System.Windows.Input.MouseButton.XButton2 ? "Mouse5" : Key.ToString());

            return str.ToString();
        }

        /// <summary>Readable labels; persistence continues to use canonical WPF key names.</summary>
        public string DisplayText
        {
            get
            {
                string canonical = ToString();
                string name = MouseButton.HasValue ? null : Key.ToString();
                if (name != null && name.Length == 2 && name[0] == 'D' && char.IsDigit(name[1]))
                    return canonical.Substring(0, canonical.Length - name.Length) + name[1];
                if (name != null && name.StartsWith("NumPad"))
                    return canonical.Substring(0, canonical.Length - name.Length) + "Num " + name.Substring(6);
                return canonical.Replace("Mouse4", "Mouse 4").Replace("Mouse5", "Mouse 5");
            }
        }

        /// <summary>
        /// "Deserializes" a string to a HotKey
        /// </summary>
        public static Hotkey FromString(string hotkeyStr)
        {
            if (string.IsNullOrWhiteSpace(hotkeyStr)) return null;
            var parts = hotkeyStr.Split(new[] { " + " }, StringSplitOptions.None);
            ModifierKeys modifiers = ModifierKeys.None;
            for (int i = 0; i < parts.Length - 1; ++i)
            {
                switch (parts[i])
                {
                    case "Ctrl": modifiers |= ModifierKeys.Control; break;
                    case "Shift": modifiers |= ModifierKeys.Shift; break;
                    case "Alt": modifiers |= ModifierKeys.Alt; break;
                    case "Win": modifiers |= ModifierKeys.Windows; break;
                    default: return null; // Never silently weaken an unrecognized modifier.
                }
            }
            string keyName = parts[parts.Length - 1];
            if (keyName == "Mouse4" || keyName == "XButton1")
                return new Hotkey(System.Windows.Input.MouseButton.XButton1, modifiers);
            if (keyName == "Mouse5" || keyName == "XButton2")
                return new Hotkey(System.Windows.Input.MouseButton.XButton2, modifiers);
            Key key;
            if (Enum.TryParse(keyName, out key) && key != Key.None && Enum.IsDefined(typeof(Key), key) &&
                !char.IsDigit(keyName[0]))
                return new Hotkey(key, modifiers);
            return null;
        }
    }
}