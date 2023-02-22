using System;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using BondTech.HotKeyManagement.WPF._4;
using Keys = BondTech.HotKeyManagement.WPF._4.Keys;

namespace SoundBoard
{
    /// <summary>
    /// Interaction logic for HotkeyDialog.xaml
    /// </summary>
    internal partial class HotkeyDialog
    {
        internal HotkeyDialog(SoundButton soundButton)
        {
            InitializeComponent();
            _soundButton = soundButton;
        }

        /// <summary>
        /// The result of the dialog
        /// </summary>
        public DialogResult DialogResult { get; private set; }

        public Hotkey LocalHotkey
        {
            get => LocalHotkeyControl.Hotkey;
            set => LocalHotkeyControl.Hotkey = value;
        }

        public Hotkey GlobalHotkey
        {
            get => GlobalHotkeyControl.Hotkey;
            set => GlobalHotkeyControl.Hotkey = value;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            WarningLabel.Visibility = Visibility.Hidden;
            WarningLabel.Text = string.Empty;

            // Both hotkeys are set, and are identical
            if (LocalHotkey != null && GlobalHotkey != null &&
                LocalHotkey.ToString() == GlobalHotkey.ToString())
            {
                WarningLabel.Text += Properties.Resources.IdenticalHotkeyWarning;
                WarningLabel.Visibility = Visibility.Visible;
            }

            // See if the local hotkey is used anywhere else
            if (LocalHotkey != null)
            {
                if (MainWindow.Instance.GetSoundButtons().FirstOrDefault(sb =>
                    {
                        return
                            ((sb.LocalHotkey != null && sb.LocalHotkey.ToString() == LocalHotkey.ToString())
                             || sb.GlobalHotkey != null && sb.GlobalHotkey.ToString() == LocalHotkey.ToString())
                            && sb != _soundButton;
                    }) is SoundButton sb1)
                {
                    WarningLabel.Text += string.Format(Properties.Resources.LocalHotkeyInUse, LocalHotkey, sb1.SoundName);
                    WarningLabel.Visibility = Visibility.Visible;
                }
            }

            if (GlobalHotkey != null)
            {
                if (MainWindow.Instance.GetSoundButtons().FirstOrDefault(sb =>
                    {
                        return
                            ((sb.LocalHotkey != null && sb.LocalHotkey.ToString() == GlobalHotkey.ToString())
                             || sb.GlobalHotkey != null && sb.GlobalHotkey.ToString() == GlobalHotkey.ToString())
                            && sb != _soundButton;
                    }) is SoundButton sb2)
                {
                    WarningLabel.Text += string.Format(Properties.Resources.GlobalHotkeyInuse, GlobalHotkey, sb2.SoundName);
                    WarningLabel.Visibility = Visibility.Visible;
                }
            }

            if (WarningLabel.Visibility == Visibility.Visible)
            {
                return;
            }

            // Try to register
            try
            {
                _soundButton.LocalHotkey = null;

                // Start by clearing any registrations
                _soundButton.UnregisterLocalHotkey();

                if (LocalHotkey != null)
                {
                    // Assign and register
                    _soundButton.LocalHotkey = LocalHotkey;
                    _soundButton.ReregisterLocalHotkey();
                }
            }
            catch
            {
                _soundButton.LocalHotkey = null;
                WarningLabel.Text = string.Format(Properties.Resources.HotkeyRegistrationFailed, LocalHotkey);
                WarningLabel.Visibility = Visibility.Visible;
            }

            try
            {
                _soundButton.GlobalHotkey = null;

                // Start by clearing any registration
                _soundButton.UnregisterGlobalHotkey();

                if (GlobalHotkey != null)
                {
                    // Assign and register
                    _soundButton.GlobalHotkey = GlobalHotkey;
                    _soundButton.ReregisterGlobalHotkey();
                }
            }
            catch
            {
                _soundButton.GlobalHotkey = null;
                WarningLabel.Text = string.Format(Properties.Resources.HotkeyRegistrationFailed, GlobalHotkey);
                WarningLabel.Visibility = Visibility.Visible;
            }

            if (WarningLabel.Visibility == Visibility.Visible)
            {
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private readonly SoundButton _soundButton;
    }
}
