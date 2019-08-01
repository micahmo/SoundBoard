#region Usings

using System.Collections.Generic;
using System.Windows.Controls;

#endregion

namespace SoundBoard
{
    #region MenuItemExtensions class

    /// <summary>
    /// Extensions on the <see cref="MenuItem"/> class.
    /// </summary>
    internal static class MenuItemExtensions
    {
        #region Separator property

        /// <summary>
        /// Get the Separator property
        /// </summary>
        public static bool GetSeparator(this MenuItem menuItem)
        {
            _separator.TryGetValue(menuItem, out bool result);
            return result;
        }

        /// <summary>
        /// Set the Separator property
        /// </summary>
        /// <param name="menuItem"></param>
        /// <param name="value"></param>
        public static void SetSeparator(this MenuItem menuItem, bool value)
        {
            _separator[menuItem] = value;
        }

        private static readonly Dictionary<MenuItem, bool> _separator = new Dictionary<MenuItem, bool>();

        #endregion
    }

    #endregion
}
