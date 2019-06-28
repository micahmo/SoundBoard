namespace SoundBoard
{
    #region IUndoable interface

    /// <summary>
    /// Defines a class which can save and load its state via an object which derives from <see cref="UndoStateBase"/>
    /// </summary>
    public interface IUndoable<T> where T : UndoStateBase
    {
        /// <summary>
        /// Save the current state of the object
        /// </summary>
        T SaveState();

        /// <summary>
        /// Load a given object state
        /// </summary>
        void LoadState(T undoState);
    }

    #endregion

    #region UndoState classes

    /// <summary>
    /// Defines the base class from which all object state classes should derive
    /// </summary>
    public abstract class UndoStateBase { }

    /// <summary>
    /// Defines the undo save state for SoundButtons
    /// </summary>
    public class SoundButtonUndoState : UndoStateBase
    {
        /// <summary>
        /// SoundPath
        /// </summary>
        public string SoundPath { get; set; }

        /// <summary>
        /// SoundName
        /// </summary>
        public string SoundName { get; set; }
    }

    /// <summary>
    /// Defines the undo save state for TabPages
    /// </summary>
    public class TabPageUndoState : UndoStateBase
    {
        /// <summary>
        /// MetroTabItem
        /// </summary>
        public MahApps.Metro.Controls.MetroTabItem MetroTabItem { get; set; }

        /// <summary>
        /// Index
        /// </summary>
        public int Index { get; set; }
    }

    #endregion
}
