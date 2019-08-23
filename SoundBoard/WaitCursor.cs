using System;
using System.Windows.Input;

namespace SoundBoard
{
    /// <summary>
    /// Shows a WaitCursor. Dispose to restore the previous cursor.
    /// Use in a using block to automatically restore cursor after a long-running operation.
    /// </summary>
    /// <remarks>
    /// Lifted from StackOverflow: https://stackoverflow.com/a/3481274/4206279
    /// </remarks>
    internal class WaitCursor : IDisposable
    {
        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public WaitCursor()
        {
            _previousCursor = Mouse.OverrideCursor;
            Mouse.OverrideCursor = Cursors.Wait;
        }
        
        #endregion

        #region IDisposable members

        /// <inheritdoc />
        public void Dispose()
        {
            Mouse.OverrideCursor = _previousCursor;
        }

        #endregion

        #region Private fields

        private readonly Cursor _previousCursor;

        #endregion
    }
}
