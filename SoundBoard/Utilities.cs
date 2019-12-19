#region Usings

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;
using NAudio.CoreAudioApi;
using Point = System.Windows.Point;

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
        public static string Truncate(string input, System.Drawing.Font font, int maxWidth, string offsetString = "")
        {
            bool truncated = false;

            while (TextRenderer.MeasureText(input + ELLIPSES, font).Width + TextRenderer.MeasureText(offsetString, font).Width > maxWidth)
            {
                truncated = true;
                input = input.Substring(0, input.Length - 1);
            }

            return input + (truncated ? ELLIPSES : string.Empty);
        }

        /// <summary>
        /// Unmute the system audio device(s), with optional parameters to specify the <see cref="DataFlow"/> and <see cref="DeviceState"/>.
        /// </summary>
        public static void UnmuteSystemAudio(DataFlow dataFlow = DataFlow.Render, DeviceState deviceState = DeviceState.Active)
        {
            using (MMDeviceEnumerator deviceEnumerator = new MMDeviceEnumerator())
            {
                foreach (MMDevice device in deviceEnumerator.EnumerateAudioEndPoints(dataFlow, deviceState))
                {
                    device.AudioEndpointVolume.Mute = false;
                }
            }
        }

        /// <summary>
        /// Returns true if the distance (absolute value) between both <paramref name="firstPoint"/>.X and <paramref name="secondPoint"/>.X
        /// and <paramref name="firstPoint"/>.Y and <paramref name="secondPoint"/>.Y is greater than <paramref name="threshold"/>.
        /// </summary>
        /// <param name="firstPoint"></param>
        /// <param name="secondPoint"></param>
        /// <param name="threshold"></param>
        /// <returns></returns>
        public static bool PointsArePastThreshold(Point firstPoint, Point secondPoint, int threshold = MOUSE_MOVE_THRESHOLD)
        {
            return Math.Abs(firstPoint.X - secondPoint.X) > threshold &&
                   Math.Abs(firstPoint.Y - secondPoint.Y) > threshold;
        }

        /// <summary>
        /// Extension method on <see cref="System.Windows.Media.Color"/> which converts it to a <see cref="System.Drawing.Color"/>.
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static System.Drawing.Color ToSystemDrawingColor(this System.Windows.Media.Color color)
        {
            return System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        /// <summary>
        /// Extension method on <see cref="System.Drawing.Color"/> which converts it to a <see cref="System.Windows.Media.Color"/>.
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static System.Windows.Media.Color ToSystemWindowsMediaColor(this System.Drawing.Color color)
        {
            return System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        /// <summary>
        /// Returns <see langword="true"/> if any instances of <see cref="ChildWindowBase"/> are currently visible.
        /// </summary>
        /// <returns></returns>
        public static bool AreAnyDialogsVisible()
        {
            return ChildWindowBase.Instances.Any(childWindowBase => childWindowBase.IsOpen);
        }

        /// <summary>
        /// Checks whether the required framework version is installed.
        /// </summary>
        /// <remarks>
        /// Note: Update this if we update the target framework version.
        /// More info here: https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed
        /// </remarks>
        public static bool IsRequiredNetFrameworkInstalled()
        {
            bool result = false;

            const string subkey = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\";
            using (var ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(subkey))
            {
                if (ndpKey?.GetValue("Release") != null)
                {
                    if ((int)ndpKey.GetValue("Release") >= 461808)
                    {
                        result = true;
                    }
                }
            }

            return result;
        }

        #endregion

        #region P/Invoke utilities

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Win32Point pt);

        [StructLayout(LayoutKind.Sequential)]
        internal struct Win32Point
        {
            public int X;
            public int Y;
        }

        #endregion

        #region Private consts

        private const string ELLIPSES = @"...";

        private const int MOUSE_MOVE_THRESHOLD = 5; // The mouse will have to move at least 5 pixels for the drag operation to start

        #endregion
    }

    #endregion
}
