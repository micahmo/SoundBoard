using System;
using System.Collections.Generic;
using System.Linq;

namespace SoundBoard
{
    /// <summary>
    /// Holds settings that are global to the application
    /// </summary>
    public static class GlobalSettings
    {
        #region Output device

        /// <summary>
        /// Add an output device to the current list
        /// </summary>
        public static void AddOutputDeviceGuid(Guid guid) => OutputDeviceGuids.Add(guid);

        /// <summary>
        /// Remove an output device from the current list
        /// </summary>
        public static void RemoveOutputDeviceGuid(Guid guid) => OutputDeviceGuids.Remove(guid);

        /// <summary>
        /// Removes all current output devices
        /// </summary>
        public static void RemoveAllOutputDeviceGuids() => OutputDeviceGuids.Clear();

        /// <summary>
        /// Get the current list of output devices
        /// </summary>
        public static List<Guid> GetOutputDeviceGuids() => (OutputDeviceGuids.Any() ? OutputDeviceGuids : new HashSet<Guid> {Guid.Empty}).ToList();

        /// <summary>
        /// The name of the OutputDeviceGuid setting name. 
        /// </summary>
        /// <remarks>
        /// This is for backwards compatibility with old settings files. We used to use nameof(OutputDeviceGuid),
        /// but there is no property with that name any more.
        /// </remarks>
        public static string OutputDeviceGuidSettingName = "OutputDeviceGuid";

        /// <summary>
        /// Defines the ID(s) of the audio output device(s) to use when playing sounds
        /// </summary>
        /// <remarks>
        /// HashSet to prevent duplicate GUIDs.
        /// </remarks>
        private static HashSet<Guid> OutputDeviceGuids { get; } = new HashSet<Guid>();

        #endregion

        #region Input device

        /// <summary>
        /// Add an input device to the current list
        /// </summary>
        public static void AddInputDeviceGuid(Guid guid) => InputDeviceGuids.Add(guid);

        /// <summary>
        /// Remove an input device from the current list
        /// </summary>
        public static void RemoveInputDeviceGuid(Guid guid) => InputDeviceGuids.Remove(guid);

        /// <summary>
        /// Removes all current input devices
        /// </summary>
        public static void RemoveAllInputDeviceGuids() => InputDeviceGuids.Clear();

        /// <summary>
        /// Get the current list of input devices
        /// </summary>
        public static List<Guid> GetInputDeviceGuids() => (InputDeviceGuids.Any() ? InputDeviceGuids : Enumerable.Empty<Guid>()).ToList();

        /// <summary>
        /// The name of the InputDeviceGuid setting name. 
        /// </summary>
        public static string InputDeviceGuidSettingName = "InputDeviceGuid";

        /// <summary>
        /// Defines the ID(s) of the audio input device(s) to use when playing sounds
        /// </summary>
        /// <remarks>
        /// HashSet to prevent duplicate GUIDs.
        /// </remarks>
        private static HashSet<Guid> InputDeviceGuids { get; } = new HashSet<Guid>();

        /// <summary>
        /// The number of button columns to use by default for new pages
        /// </summary>
        public static int NewPageDefaultColumns { get; set; } = 2;

        /// <summary>
        /// The number of button rows to use by default for new pages
        /// </summary>
        public static int NewPageDefaultRows { get; set; } = 5;

        #endregion

        /// <summary>
        /// The latency to use when chaining input to outputs
        /// </summary>
        public static int AudioPassthroughLatency { get; set; } = 10;
    }
}
