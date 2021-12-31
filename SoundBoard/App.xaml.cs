using MahApps.Metro.Controls.Dialogs;
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

            // Handle global exceptions
            DispatcherUnhandledException += (_, args) =>
            {
                Dispatcher.Invoke(async () =>
                {
                    var res = await SoundBoard.MainWindow.Instance.ShowMessageAsync(SoundBoard.Properties.Resources.Error,
                        string.Join(Environment.NewLine, SoundBoard.Properties.Resources.UnexpectedError, string.Empty, args.Exception.Message),
                        MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings
                        {
                            AffirmativeButtonText = SoundBoard.Properties.Resources.CopyDetails,
                            NegativeButtonText = SoundBoard.Properties.Resources.OK
                        });

                    if (res == MessageDialogResult.Affirmative)
                    {
                        Clipboard.SetText(args.Exception.ToString());
                    }
                });

                // Don't crash.
                args.Handled = true;
            };

            base.OnStartup(e);
        }
    }
}
