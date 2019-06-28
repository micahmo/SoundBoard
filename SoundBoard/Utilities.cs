#region Usings

using System.Drawing;
using System.Windows.Forms;

#endregion

namespace SoundBoard
{

    #region Utilities class
    
    /// <summary>
    /// Helper class with static methods
    /// </summary>
    internal static class Utilities
    {
        #region Public static methods

        /// <summary>
        /// If necessary, truncates the given <paramref name="input"/> with ellipses (...) until it is shorter than <paramref name="maxWidth"/>
        /// by measuring its size in the given <paramref name="font"/>. Pass in an optional <paramref name="offsetString"/>
        ///  to add to the total width of the input string.
        /// </summary>
        public static string Truncate(string input, Font font, int maxWidth, string offsetString = "")
        {
            bool truncated = false;

            while (TextRenderer.MeasureText(input + ELLIPSES, font).Width + TextRenderer.MeasureText(offsetString, font).Width > maxWidth)
            {
                truncated = true;
                input = input.Substring(0, input.Length - 1);
            }

            return input + (truncated ? ELLIPSES : string.Empty);
        }

        #endregion

        #region Private consts

        private const string ELLIPSES = @"...";

        #endregion
    }

    #endregion
}
