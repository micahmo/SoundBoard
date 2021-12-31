#region Usings

using System;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace SoundBoard
{
    /// <summary>
    /// Defines a custom data structure which stores objects with a row and column index
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class TwoDimensionalList<T>
    {
        #region Public methods

        /// <summary>
        /// Adds an item to the list
        /// </summary>
        /// <param name="item"></param>
        /// <param name="row"></param>
        /// <param name="column"></param>
        public void Add(T item, int row, int column)
        {
            _list.Add((item, row, column));
        }

        /// <summary>
        /// Tries to gets the item at the specified <paramref name="row"/> and <paramref name="column"/>.
        /// If found, sets <paramref name="value"/> to the item and returns <see langword="true"/>.
        /// If not found, returns <see langword="false"/> and sets <paramref name="value"/> to <code>default(<typeparamref name="T"/>)</code>.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="column"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGet(int row, int column, out T value)
        {
            try
            {
                if (_list.FirstOrDefault(item => item.row == row && item.column == column).item is T foundValue)
                {
                    value = foundValue;
                    return true;
                }
                else
                {
                    value = default;
                    return false;
                }
            }
            // This exception handler was present when we were using First instead of FirstOrDefault.
            // It is probably safe to remove, but remains as a safety precaution.
            catch (Exception ex) when (ex is ArgumentNullException || ex is InvalidOperationException)
            {
                value = default;
                return false;
            }
        }

        #endregion
        
        #region Private fields

        private readonly List<(T item, int row, int column)> _list = new List<(T item, int row, int column)>();

        #endregion
    }
}
