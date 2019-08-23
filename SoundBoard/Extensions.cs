#region Usings

using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

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

    #region ColorExtensions class

    /// <summary>
    /// Extensions on the <see cref="System.Windows.Media.Color"/> class.
    /// </summary>
    internal static class ColorExtensions
    {
        /// <summary>
        /// Returns true if the R, G, and B values of the <paramref name="color"/> are full (ignores the A value).
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static bool IsWhite(this System.Windows.Media.Color color)
        {
            return color.R == byte.MaxValue && color.G == byte.MaxValue && color.B == byte.MaxValue;
        }

        /// <summary>
        /// Returns true if the R, G, and B values of the <paramref name="color"/> are empty (ignores the A value).
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static bool IsBlack(this System.Windows.Media.Color color)
        {
            return color.R == 0 && color.G == 0 && color.B == 0;
        }
    }

    #endregion

    #region TabItemExtensions class

    /// <summary>
    /// Extensions on the <see cref="TabItem"/> class.
    /// </summary>
    internal static class TabItemExtensions
    {
        /// <summary>
        /// Returns <see langword="true"/> if the given <paramref name="tabItem"/> is the <see cref="Selector.SelectedItem"/> of its <see cref="FrameworkElement.Parent"/> <see cref="TabControl"/>.
        /// </summary>
        /// <param name="tabItem"></param>
        /// <returns></returns>
        public static bool IsSelectedItem(this TabItem tabItem)
        {
            return tabItem == (tabItem.Parent as TabControl)?.SelectedItem;
        }

        #region Rows property

        /// <summary>
        /// Get the Rows property
        /// </summary>
        /// <param name="tabItem"></param>
        /// <returns></returns>
        public static int GetRows(this TabItem tabItem)
        {
            if (_rows.TryGetValue(tabItem, out int result))
            {
                return result;
            }
            else return GetDefaultRows(tabItem);
        }

        /// <summary>
        /// Set the Rows property
        /// </summary>
        /// <param name="tabItem"></param>
        /// <param name="value"></param>
        public static void SetRows(this TabItem tabItem, int value)
        {
            _rows[tabItem] = value;
        }

        /// <summary>
        /// Get the default value for the Rows property
        /// </summary>
        /// <param name="tabItem"></param>
        public static int GetDefaultRows(this TabItem tabItem)
        {
            return 5;
        }

        private static readonly Dictionary<TabItem, int> _rows = new Dictionary<TabItem, int>();

        #endregion

        #region Columns property

        /// <summary>
        /// Get the Columns property
        /// </summary>
        /// <param name="tabItem"></param>
        /// <returns></returns>
        public static int GetColumns(this TabItem tabItem)
        {
            if (_columns.TryGetValue(tabItem, out int result))
            {
                return result;
            }
            else return GetDefaultColumns(tabItem);
        }

        /// <summary>
        /// Set the Columns property
        /// </summary>
        /// <param name="tabItem"></param>
        /// <param name="value"></param>
        public static void SetColumns(this TabItem tabItem, int value)
        {
            _columns[tabItem] = value;
        }

        /// <summary>
        /// Get the default value for the Columns property
        /// </summary>
        /// <param name="tabItem"></param>
        /// <returns></returns>
        public static int GetDefaultColumns(this TabItem tabItem)
        {
            return 2;
        }

        private static readonly Dictionary<TabItem, int> _columns = new Dictionary<TabItem, int>();

        #endregion
    }

    #endregion
}
