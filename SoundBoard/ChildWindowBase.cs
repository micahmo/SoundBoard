#region Usings

using System.Collections.Generic;
using MahApps.Metro.SimpleChildWindow;

#endregion

namespace SoundBoard
{
    /// <summary>
    /// Defines a ChildWindow from which all other ChildWindows in this project shall derive
    /// </summary>
    internal class ChildWindowBase : ChildWindow
    {
        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public ChildWindowBase()
        {
            _instances.Add(this);
        }

        #endregion

        #region Public properties

        public static IEnumerable<ChildWindowBase> Instances => _instances;

        #endregion

        #region Private fields

        private static readonly List<ChildWindowBase> _instances = new List<ChildWindowBase>();

        #endregion
    }
}
