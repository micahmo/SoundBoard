using System;

namespace SoundBoard
{
    /// <summary>
    /// Holds settings that are global to the application
    /// </summary>
    public static class GlobalSettings
    {
        /// <summary>
        /// Defines the ID of the audio output device to use when playing sounds
        /// </summary>
        public static Guid OutputDeviceGuid { get; set; } = Guid.Empty;
    }
}
