#region Usings

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
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
