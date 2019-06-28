namespace SoundBoard
{
    #region IUndoable interface

    internal interface IUndoable<T> where T : UndoStateBase
    {
        T SaveState();

        void LoadState(T undoState);
    }

    #endregion
    
    #region UndoState classes

    internal class UndoStateBase { }

    internal class SoundButtonUndoState : UndoStateBase
    {
        public string SoundPath { get; set; }

        public string SoundName { get; set; }
    }

    #endregion
}
