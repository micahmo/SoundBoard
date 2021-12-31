using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
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

            string[] updateFileCommands =
            {
                // Kill the current process
                $"taskkill /f /pid {Process.GetCurrentProcess().Id}",

                // Wait for the process to die before we can rename the exe
                $"timeout 1",

                // Rename the current exe
                $"move /y \"{currentApplicationPath}\" \"{currentApplicationPath}.old\"",

                // Move the download to the current folder
                $"move /y \"{file}\" \"{currentApplicationPath}\"",
            };

            Process updateFileProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    Verb = "runas", // For elevated privileges
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "cmd.exe",
                    Arguments = "/C " + string.Join(" & ", updateFileCommands)
                }
            };

            // Need a separate process to start the updated file with the same permissions as the currently running process

            // Get a hash of the newly downloaded file so we know what we're waiting for
            string updatedFileHash;
            using (var sha256 = SHA256.Create())
            {
                using (var stream = File.OpenRead(file))
                {
                    updatedFileHash = BitConverter.ToString(sha256.ComputeHash(stream)).Replace(@"-", string.Empty);
                }
            }

            Process startUpdatedFileProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    // NO verb so it runs as us
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "powershell.exe",
                    Arguments = $"/C " +
                                $"\"while (!(test-path -pathtype leaf \\\"{currentApplicationPath}\\\")" + // If the updated file isn't in place
                                $" -or !((get-filehash \\\"{currentApplicationPath}\\\").Hash -eq \\\"{updatedFileHash}\\\")) " + // Or the updated file doesn't match the hash we expect
                                $"{{start-sleep 1}};" + // Sleep
                                $" .\\\"{currentApplicationPath}\\\"\"" // Finally, once the conditions are met, run the updated app
                }
            };

            Parallel.Invoke(
                () => updateFileProcess.Start(),
                () => startUpdatedFileProcess.Start()
            );
        }
    }
}
