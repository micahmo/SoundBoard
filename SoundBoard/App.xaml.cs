using System;
using System.Reflection;
using System.Runtime.Versioning;
using System.Windows;

namespace SoundBoard
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <inheritdoc />
        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                // If there's an unhandled exception, check if it's because the target framework version is not installed
                if (Utilities.IsRequiredNetFrameworkInstalled() == false)
                {
                    TargetFrameworkAttribute targetFrameworkAttribute = Assembly.GetExecutingAssembly()
                        .GetCustomAttribute(typeof(TargetFrameworkAttribute)) as TargetFrameworkAttribute;

                    MessageBox.Show(string.Format(SoundBoard.Properties.Resources.TargetFrameworkNotInstalled, targetFrameworkAttribute?.FrameworkDisplayName),
                        SoundBoard.Properties.Resources.NetFrameworkError, MessageBoxButton.OK, MessageBoxImage.Error);

                    // Exit gracefully
                    Environment.Exit(0);
                }
            };

            base.OnStartup(e);
        }
    }
}
