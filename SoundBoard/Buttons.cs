using System;
using WMPLib;
using System.IO;
using System.Windows;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using MahApps.Metro.Controls.Dialogs;

namespace SoundBoard
{
    class MenuButton : Button
    {
        SoundButton buddy;

        public MenuButton(SoundButton _buddy) : base()
        {
            Content = "•••";
            FontSize = 15;
            Style = (Style)FindResource("MetroCircleButtonStyle");
            Width = 40;
            Height = 40;
            Margin = new Thickness(0, 15, 15, 0);
            VerticalAlignment = VerticalAlignment.Top;
            HorizontalAlignment = HorizontalAlignment.Right;

            Click += new RoutedEventHandler(menuButton_Click);

            buddy = _buddy;
        }

        private void menuButton_Click(object sender, RoutedEventArgs e)
        {
            // show file dialog
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();

            // file type filter
            dialog.DefaultExt = ".wav";
            dialog.Filter = "Audo Files (*.wav, *.mp3)|*.wav;*.mp3";


            var result = dialog.ShowDialog();

            // if we got a file
            if (result == true)
            {
                buddy.SetFile(dialog.FileName);
            }
        }

    }

    class SoundProgressBar : MahApps.Metro.Controls.MetroProgressBar
    {
        public SoundProgressBar() : base()
        {
            Margin = new Thickness(10, 10, 10, 10);
            VerticalAlignment = VerticalAlignment.Bottom;

            // by default it's hidden
            Visibility = Visibility.Hidden;
        }
    }

    class SoundButton : Button
    {
        private string soundPath;
        private string soundName;
        private string soundExtension;

        WindowsMediaPlayer player = new WindowsMediaPlayer();

        private SoundProgressBar soundProgressBar;

        public SoundButton() : base()
        {
            FontSize = 20;
            Content = "<no sound>";
            Margin = new Thickness(10, 10, 10, 10);
            Style = (Style)FindResource("SquareButtonStyle");
        }

        public void SetSoundProgressBar(SoundProgressBar _soundProgressBar)
        {
            soundProgressBar = _soundProgressBar;
        }

        public void SetFile(string _soundPath, bool newSound = true)
        {
            if (_soundPath == "")
            {
                Content = "<no sound>";
                return;
            }

            // if there was a previous sound here, get rid of it
            try
            {
                MainWindow.sounds.Remove(soundName);
            } catch { }

            soundPath = _soundPath;
            soundExtension = Path.GetExtension(_soundPath);
            soundName = Path.GetFileNameWithoutExtension(_soundPath);

            Content = soundName;

            // add new sound to list
            if (newSound)
            {
                MainWindow.sounds[soundName] = soundPath;
            }


            Click += new RoutedEventHandler(soundButton_Click);
            
            // add sound to universal list for later searching
        }

        public string GetFile()
        {
            return soundPath;
        }

        private async void soundButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!File.Exists(soundPath)) throw new Exception("File " + soundPath + " doesn't seem to exist!");

                player.URL = soundPath;
                player.controls.play();

                // begin updating progress
                CancellationTokenSource tokenSource = new CancellationTokenSource();
                Task timerTask = UpdateProgressTask(UpdateProgressAction, TimeSpan.FromMilliseconds(10), tokenSource.Token);

                MainWindow.soundPlayers.Add(player);
            }
            catch (Exception ex)
            {
                await MainWindow.GetThis().ShowMessageAsync("Oops!", "There's a problem!\n\n" + ex.Message, MessageDialogStyle.AffirmativeAndNegative);
            }
        }

        async Task UpdateProgressTask(Action action, TimeSpan interval, CancellationToken token)
        {
            while (true)
            {
                action();
                await Task.Delay(interval, token);
            }
        }

        private void UpdateProgressAction()
        {
            double maxSeconds = player.controls.currentItem.duration;
            double curSeconds = player.controls.currentPosition;

            if (maxSeconds != curSeconds && curSeconds != 0)
            {
                soundProgressBar.Visibility = Visibility.Visible;
                soundProgressBar.Maximum = maxSeconds;
                soundProgressBar.Value = curSeconds;
            }
            else
            {
                soundProgressBar.Visibility = Visibility.Hidden;
            }
        }
    }
}
