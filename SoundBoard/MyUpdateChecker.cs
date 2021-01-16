using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using Bluegrams.Application;

namespace SoundBoard
{
    /// <inheritdoc/>
    public class MyUpdateChecker : WpfUpdateChecker
    {
        /// <inheritdoc/>
        public MyUpdateChecker(string url, Window owner = null, string identifier = null) : base(url, owner, identifier)
        {
        }

        /// <inheritdoc/>
        public override void ShowUpdateDownload(string file)
        {
            // Instead of showing the file in explorer (as the original method does),
            // we want to kill the current app, copy the file to the original location (with a backup of the original file)
            // and start it.

            string currentApplicationPath = new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath;

            string[] commands =
            {
                // Kill the current process
                $"taskkill /f /pid {Process.GetCurrentProcess().Id}",

                // Wait for the process to die before we can rename the exe
                $"timeout 1",

                // Rename the current exe
                $"move /y \"{currentApplicationPath}\" \"{currentApplicationPath}.old\"",

                // Move the download to the current folder
                $"move /y \"{file}\" \"{currentApplicationPath}\"",

                // Launch the new exe
                $"\"{currentApplicationPath}\"",
            };

            new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    Verb = "runas", // For elevated privileges
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "cmd.exe",
                    Arguments = "/C " + string.Join(" & ", commands)
                }
            }.Start();
        }
    }
}
