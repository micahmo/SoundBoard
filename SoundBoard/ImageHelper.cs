#region Usings

using System;
using System.Windows.Media.Imaging;
using Image = System.Windows.Controls.Image;

#endregion

namespace SoundBoard
{
    /// <summary>
    /// Static helper class to retrieve images
    /// </summary>
    internal static class ImageHelper
    {
        #region Public static methods

        /// <summary>
        /// Returns an <see cref="Image"/> for the given <paramref name="path"/>.
        /// </summary>
        public static Image GetImage(string path, int? width = null, int? height = null)
        {
            if (width is null == false && height is null == false)
            {
                return new Image {Source = new BitmapImage(new Uri(path)), Width = (int) width, Height = (int) height};
            }
            else if (width is null == false)
            {
                return new Image {Source = new BitmapImage(new Uri(path)), Width = (int) width};
            }
            else if (height is null == false)
            {
                return new Image {Source = new BitmapImage(new Uri(path)), Height = (int) height};
            }
            else
            {
                return new Image {Source = new BitmapImage(new Uri(path))};
            }
        }

        #endregion

        #region Public static consts

        public static string PlayButtonPath = @"pack://application:,,,/Images/play-arrow.png";

        public static string PauseButtonPath = @"pack://application:,,,/Images/pause-button.png";

        public static string StopButtonPath = @"pack://application:,,,/Images/stop-button.png";

        public static string CloseButtonPath = @"pack://application:,,,/Images/close.png";

        public static string AddButtonPath = @"pack://application:,,,/Images/add.png";

        public static string AddFocusButtonPath = @"pack://application:,,,/Images/add_focus.png";

        #endregion
    }
}
