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
    }
}
