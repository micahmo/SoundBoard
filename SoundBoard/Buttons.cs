using System;
using System.IO;
using NAudio.Wave;
using System.Windows;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows.Controls;
using MahApps.Metro.Controls.Dialogs;
using System.Runtime.InteropServices;

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
        #region volume_stuff

        private const int APPCOMMAND_VOLUME_UP = 0xA0000;
        private const int WM_APPCOMMAND = 0x319;

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessageW(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        #endregion

        private string soundPath;
        private string soundName;
        private string soundExtension;

        IWavePlayer player = new WaveOut();
        AudioFileReader audioFileReader;
        Stopwatch stopWatch;

        private SoundProgressBar soundProgressBar = new SoundProgressBar();

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

                // stop any previous sounds
                player.Stop();

                // reinitialize
                player = new WaveOut();
                audioFileReader = new AudioFileReader(soundPath);
                player.Init(audioFileReader);

                SendMessageW(new WindowInteropHelper(MainWindow.GetThis()).Handle, WM_APPCOMMAND, new WindowInteropHelper(MainWindow.GetThis()).Handle, (IntPtr)APPCOMMAND_VOLUME_UP);

                // and play
                player.Play();

                // handle stop
                player.PlaybackStopped += new EventHandler<StoppedEventArgs>(SoundStoppedHandler);

                stopWatch = Stopwatch.StartNew();

                // begin updating progress
                CancellationTokenSource tokenSource = new CancellationTokenSource();
                Task timerTask = UpdateProgressTask(UpdateProgressAction, TimeSpan.FromMilliseconds(1), tokenSource.Token);

                MainWindow.soundPlayers.Add(player);
            }
            catch (Exception ex)
            {
                await MainWindow.GetThis().ShowMessageAsync("Oops!", "There's a problem!\n\n" + ex.Message, MessageDialogStyle.AffirmativeAndNegative);
            }
        }

        private void MediaFailedHandler(object sender, EventArgs e) {
            MessageBox.Show("well there's your problem!");
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
            double maxSeconds = audioFileReader.TotalTime.TotalMilliseconds;
            double curSeconds = stopWatch.Elapsed.TotalMilliseconds;

            soundProgressBar.Visibility = Visibility.Visible;
            soundProgressBar.Maximum = maxSeconds;
            soundProgressBar.Value = curSeconds;

            // is the sound done or has it been stopped? hide the progress bar
            if (curSeconds > maxSeconds || audioFileReader.Position == 0) {
                soundProgressBar.Visibility = Visibility.Hidden;
            }
        }

        private void SoundStoppedHandler(object sender, EventArgs e) {
            audioFileReader.Position = 0;
            soundProgressBar.Visibility = Visibility.Hidden;
        }
    }
}
