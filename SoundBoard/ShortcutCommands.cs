using System.Windows.Input;

namespace SoundBoard
{
    /// <summary>
    /// Defines a set of commands which can be invoked via keyboard shortcut from the Main Window
    /// </summary>
    public class ShortcutCommands
    {
        static ShortcutCommands()
        {
            AboutBoxCommand.InputGestures.Add(new KeyGesture(Key.F1));
        }

        /// <summary>
        /// Shows the AboutBox
        /// </summary>
        public static RoutedCommand AboutBoxCommand { get; } = new RoutedCommand();

    }
}
