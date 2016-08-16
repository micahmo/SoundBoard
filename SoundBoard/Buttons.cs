using System;
using System.IO;
using NAudio.Wave;
using System.Windows;
using System.Threading;
using System.Diagnostics;
using System.Windows.Media;
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
        private const int APPCOMMAND_VOLUME_DOWN = 0x90000;
        private const int WM_APPCOMMAND = 0x319;

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessageW(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        #endregion

        private string soundPath;
        private string soundName;

        IWavePlayer player = new WaveOut();
        AudioFileReader audioFileReader;
        Stopwatch stopWatch;

        SoundProgressBar soundProgressBar = new SoundProgressBar();

        ContextMenu contextMenu = new ContextMenu();
        MenuItem renameMenuItem = new MenuItem();

        public SoundButton(bool searchButton = false) : base()
        {
            if (!searchButton)
            {
                SetDefaultText();
            }
            FontSize = 20;
            Margin = new Thickness(10, 10, 10, 10);
            Style = (Style)FindResource("SquareButtonStyle");
            AllowDrop = true;
            Drop += new DragEventHandler(SoundFileDrop);
            Click += new RoutedEventHandler(soundButton_Click);

            // context menu stuff
            renameMenuItem.Header = "Rename";
            renameMenuItem.Click += RenameMenuItem_Click;
            contextMenu.Items.Add(renameMenuItem);
            
            // hide the context menu until we hav an actual sound
            ContextMenu = contextMenu;
            ContextMenu.Visibility = Visibility.Hidden;
        }

        private async void RenameMenuItem_Click(object sender, RoutedEventArgs e) {
            // stop handling keypresses in the main window
            MainWindow.GetThis().RemoveHandler(KeyDownEvent, MainWindow.GetThis().keyDownHandler);

            string result = await MainWindow.GetThis().ShowInputAsync("Rename", "What do you want to change " + soundName + " to?");

            if (result != null && result != "") {
                soundName = result;
                Content = soundName;
                MainWindow.GetThis().UpdateSoundList();
            }

            // rehandle keypresses in main window
            MainWindow.GetThis().AddHandler(KeyDownEvent, MainWindow.GetThis().keyDownHandler, true);
        }

        private async void SoundFileDrop(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
                // get the dropped file(s)
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                // only care about the first file
                string file = files[0];

                // can only have .mp3 and .wav
                if (Path.GetExtension(file) != ".mp3" && Path.GetExtension(file) != ".wav") {
                    await MainWindow.GetThis().ShowMessageAsync("Uh oh!", "Only .wav and .mp3 files supported.", MessageDialogStyle.Affirmative);
                    return;
                }

                // set it
                SetFile(file);
            }
        }

        public void SetSoundProgressBar(SoundProgressBar _soundProgressBar)
        {
            soundProgressBar = _soundProgressBar;
        }

        private void SetDefaultText() {
            Content = "Drag a sound here...";
            Foreground = new SolidColorBrush(Colors.Gray);
        }

        public void SetFile(string _soundPath, string _soundName = "", bool newSound = true)
        {
            if (_soundPath == "")
            {
                SetDefaultText();
                return;
            }

            // if there was a previous sound here, get rid of it
            try {
                MainWindow.sounds.Remove(soundName);
            } catch { }

            soundPath = _soundPath;

            if (_soundName == "") {
                // for some whacked out reason, I have to remove underscores to avoid crazy bug
                soundName = Path.GetFileNameWithoutExtension(_soundPath).Replace("_", "");
            } else {
                soundName = _soundName.Replace("_", "");
            }
            Content = soundName;

            // if this is a new sound on the main soundboard
            if (newSound)
            {
                // set text color
                Foreground = new SolidColorBrush(Colors.Black);

                // IMPORTANT, add this sounhd to dictionary
                MainWindow.sounds[soundName] = soundPath;

                // show the context menu
                ContextMenu.Visibility = Visibility.Visible;
            }
        }

        public string GetFileName() {
            return soundName;
        }

        public string GetFile()
        {
            return soundPath;
        }

        private async void soundButton_Click(object sender, RoutedEventArgs e)
        {
            if (soundPath == "" || soundPath == null) return;

            try
            {
                if (!File.Exists(soundPath)) throw new Exception("File " + soundPath + " doesn't seem to exist!");

                // stop any previous sounds
                player.Stop();
                player.Dispose();
                //audioFileReader.Dispose()

                // reinitialize
                player = new WaveOut();
                audioFileReader = new AudioFileReader(soundPath);
                player.Init(audioFileReader);

                // unmute volume by turning it up and back down again
                SendMessageW(new WindowInteropHelper(MainWindow.GetThis()).Handle, WM_APPCOMMAND, new WindowInteropHelper(MainWindow.GetThis()).Handle, (IntPtr)APPCOMMAND_VOLUME_UP);
                SendMessageW(new WindowInteropHelper(MainWindow.GetThis()).Handle, WM_APPCOMMAND, new WindowInteropHelper(MainWindow.GetThis()).Handle, (IntPtr)APPCOMMAND_VOLUME_DOWN);

                // handle stop
                player.PlaybackStopped += new EventHandler<StoppedEventArgs>(SoundStoppedHandler);

                stopWatch = Stopwatch.StartNew();

                MainWindow.soundPlayers.Add(player);

                // aaaaand play
                player.Play();

                // begin updating progress
                CancellationTokenSource tokenSource = new CancellationTokenSource();
                Task timerTask = UpdateProgressTask(UpdateProgressAction, TimeSpan.FromMilliseconds(5), tokenSource.Token);
            }
            catch (Exception ex)
            {
                await MainWindow.GetThis().ShowMessageAsync("Oops!", "There's a problem!\n\n" + ex.Message, MessageDialogStyle.Affirmative);
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
        }
    }
}
