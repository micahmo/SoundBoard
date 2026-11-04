// https://tyrrrz.me/blog/hotkey-editor-control-in-wpf
using System.Windows.Input;
using System.Windows;

namespace SoundBoard
{
    /// <summary>
    /// Represents the control which accepts and records a hotkey
    /// </summary>
    public partial class HotkeyEditorControl
    {
        /// <summary>
        /// HotkeyProperty
        /// </summary>
        public static readonly DependencyProperty HotkeyProperty =
            DependencyProperty.Register(
                nameof(Hotkey),
                typeof(Hotkey),
                typeof(HotkeyEditorControl),
                new FrameworkPropertyMetadata(
                    default(Hotkey),
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    (d, e) => ((HotkeyEditorControl)d).UpdateModifiers()
                )
            );

        /// <summary>
        /// The Hotkey
        /// </summary>
        public Hotkey Hotkey
        {
            get => (Hotkey)GetValue(HotkeyProperty);
            set => SetValue(HotkeyProperty, value);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public HotkeyEditorControl()
        {
            InitializeComponent();
        }

        private void UpdateModifiers()
        {
            if (CtrlModifier == null) return;
            var modifiers = Hotkey?.Modifiers ?? ModifierKeys.None;
            CtrlModifier.IsChecked = modifiers.HasFlag(ModifierKeys.Control);
            ShiftModifier.IsChecked = modifiers.HasFlag(ModifierKeys.Shift);
            AltModifier.IsChecked = modifiers.HasFlag(ModifierKeys.Alt);
            WinModifier.IsChecked = modifiers.HasFlag(ModifierKeys.Windows);
        }

        private ModifierKeys SelectedModifiers =>
            (CtrlModifier.IsChecked == true ? ModifierKeys.Control : ModifierKeys.None) |
            (ShiftModifier.IsChecked == true ? ModifierKeys.Shift : ModifierKeys.None) |
            (AltModifier.IsChecked == true ? ModifierKeys.Alt : ModifierKeys.None) |
            (WinModifier.IsChecked == true ? ModifierKeys.Windows : ModifierKeys.None);

        private void Number_Click(object sender, RoutedEventArgs e)
        {
            var digit = int.Parse((string)((System.Windows.Controls.Button)sender).Tag);
            Hotkey = new Hotkey((Key)((int)Key.D0 + digit), ModifierKeys.None);
        }

        private void Mouse4_Click(object sender, RoutedEventArgs e) { Hotkey = new Hotkey(MouseButton.XButton1, SelectedModifiers); }
        private void Mouse5_Click(object sender, RoutedEventArgs e) { Hotkey = new Hotkey(MouseButton.XButton2, SelectedModifiers); }
        private void Clear_Click(object sender, RoutedEventArgs e) { Hotkey = null; }
        private void Modifier_Click(object sender, RoutedEventArgs e)
        {
            if (Hotkey == null) return;
            Hotkey = Hotkey.MouseButton.HasValue
                ? new Hotkey(Hotkey.MouseButton.Value, SelectedModifiers)
                : new Hotkey(Hotkey.Key, SelectedModifiers);
        }

        private void HotkeyTextBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.XButton1 && e.ChangedButton != MouseButton.XButton2) return;
            HotkeyTextBox.Focus();
            Hotkey = new Hotkey(e.ChangedButton, Keyboard.Modifiers);
            // Routed handling is local to the picker; global hook input is never suppressed.
            e.Handled = true;
        }

        private void HotkeyTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Don't let the event pass further
            // because we don't want standard textbox shortcuts working
            e.Handled = true;

            // Get modifiers and key data
            var modifiers = Keyboard.Modifiers;
            var key = e.Key;

            // When Alt is pressed, SystemKey is used instead
            if (key == Key.System)
            {
                key = e.SystemKey;
            }

            // Pressing delete, backspace or escape without modifiers clears the current value
            if (modifiers == ModifierKeys.None &&
                (key == Key.Delete || key == Key.Back || key == Key.Escape))
            {
                Hotkey = null;
                return;
            }

            // If no actual key was pressed - return
            if (
                key == Key.LeftCtrl ||
                key == Key.RightCtrl ||
                key == Key.LeftAlt ||
                key == Key.RightAlt ||
                key == Key.LeftShift ||
                key == Key.RightShift ||
                key == Key.LWin ||
                key == Key.RWin ||
                key == Key.Clear ||
                key == Key.OemClear ||
                key == Key.Apps)
            {
                return;
            }

            // Update the value
            Hotkey = new Hotkey(key, modifiers);
        }
    }
}