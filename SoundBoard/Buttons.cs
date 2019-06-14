#region Usings

using System;
using System.Collections.Generic;
using System.IO;
using NAudio.Wave;
using System.Windows;
using System.Threading;
using System.Diagnostics;
using System.Linq;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows.Controls;
using MahApps.Metro.Controls.Dialogs;
using System.Runtime.InteropServices;
using MahApps.Metro.Controls;
using Microsoft.Win32;

#endregion

namespace SoundBoard
{
    #region MenuButtonBase class

    /// <summary>
    /// Defines a button that can be placed on a sound button to offer additional functionality
    /// </summary>
    internal abstract class MenuButtonBase : Button
    {
        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        protected MenuButtonBase()
        {
            FontSize = 13;
            Style = (Style)FindResource(@"MetroCircleButtonStyle");
            Width = 35;
            Height = 35;
            Margin = new Thickness(0, 15, 15, 15);
            Padding = new Thickness(0.5, 0, 0, 1.5);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="parentButton"></param>
        protected MenuButtonBase(SoundButton parentButton) : this()
        {
            ParentButton = parentButton;
        }

        #endregion

        #region Properties

        protected readonly SoundButton ParentButton;

        #endregion
    }

    #endregion

    #region HideableMenuButtonBase class

    /// <summary>
    /// Defines a hideable button that can be placed on a sound button to offer additional functionality
    /// </summary>
    internal abstract class HideableMenuButtonBase : MenuButtonBase
    {
        #region Constructor

        protected HideableMenuButtonBase(SoundButton parentButton) : base(parentButton) { }

        #endregion

        #region Public methods

        /// <summary>
        /// Show the button
        /// </summary>
        public virtual void Show()
        {
            Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Hide the button
        /// </summary>
        public virtual void Hide()
        {
            Visibility = Visibility.Hidden;
        }

        #endregion
    }

    #endregion

    #region MenuButton class

    /// <summary>
    /// Defines a menu button that is placed on a sound button to offer additional functionality
    /// </summary>
    internal sealed class MenuButton : MenuButtonBase
    {
        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="parentButton"></param>
        public MenuButton(SoundButton parentButton) : base(parentButton)
        {
            Content = @"•••";

            VerticalAlignment = VerticalAlignment.Bottom;
            HorizontalAlignment = HorizontalAlignment.Right;
        }

        #endregion

        #region Event handlers

        protected override void OnClick()
        {
            base.OnClick();

            if (ParentButton.ContextMenu is null == false)
            {
                ParentButton.ContextMenu.IsOpen = true;
            }
        }

        #endregion
    }

    #endregion

    #region PlayPauseButton class

    /// <summary>
    /// Defines a menu button that is placed on a sound button to offer play/pause functionality
    /// </summary>
    internal sealed class PlayPauseButton : HideableMenuButtonBase
    {
        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="parentButton"></param>
        public PlayPauseButton(SoundButton parentButton) : base(parentButton)
        {
            VerticalAlignment = VerticalAlignment.Bottom;
            HorizontalAlignment = HorizontalAlignment.Center;
            Margin = new Thickness(0, Margin.Top, Width, Margin.Bottom);

            Visibility = Visibility.Hidden; // Hidden by default
        }

        #endregion

        #region Event handlers

        protected override void OnClick()
        {
            base.OnClick();

            if (_playing)
            {
                ParentButton.Pause();
                _playing = false;
                Content = @"⯈";
            }
            else
            {
                ParentButton.Play();
                _playing = true;
                Content = @"❚❚";
            }
        }

        #endregion

        #region Public methods

        public override void Show()
        {
            base.Show();
            
            Content = @"❚❚";
            _playing = true;
        }

        #endregion

        #region Private fields

        private bool _playing = false;

        #endregion
    }

    #endregion

    #region StopButton class

    /// <summary>
    /// Defines a menu button that is placed on a sound button to offer individual silencing (stopping) functionality
    /// </summary>
    internal sealed class StopButton : HideableMenuButtonBase
    {
        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="parentButton"></param>
        public StopButton(SoundButton parentButton) : base(parentButton)
        {
            Content = @"■";

            VerticalAlignment = VerticalAlignment.Bottom;
            HorizontalAlignment = HorizontalAlignment.Center;
            Margin = new Thickness(Width, Margin.Top, 0, Margin.Bottom);

            Visibility = Visibility.Hidden; // Hidden by default
        }

        #endregion

        #region Event handlers

        protected override void OnClick()
        {
            base.OnClick();

            ParentButton.Stop();
        }

        #endregion
    }

    #endregion

    #region SoundProgressBar class

    /// <summary>
    /// Defines a ProgressBar control to visually indicate the progress of a playing sound
    /// </summary>
    internal sealed class SoundProgressBar : MetroProgressBar
    {
        public SoundProgressBar()
        {
            Margin = new Thickness(10);
            VerticalAlignment = VerticalAlignment.Bottom;

            // Hide by default
            Visibility = Visibility.Hidden;
        }
    }

    #endregion

    #region SoundButton class

    /// <summary>
    /// Defines a Button which plays a Sound
    /// </summary>
    internal sealed class SoundButton : Button
    {
        #region P/Invoke stuff

        private const int APPCOMMAND_VOLUME_UP = 0xA0000;
        private const int APPCOMMAND_VOLUME_DOWN = 0x90000;
        private const int WM_APPCOMMAND = 0x319;

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessageW(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public SoundButton(SoundButtonMode soundButtonMode = SoundButtonMode.Normal)
        {
            Mode = soundButtonMode;

            if (soundButtonMode == SoundButtonMode.Normal)
            {
                SetDefaultText();
            }

            FontSize = 20;
            Margin = new Thickness(10);
            Style = (Style)FindResource(@"SquareButtonStyle");
            AllowDrop = true;
            Drop += SoundFileDrop;
            Click += soundButton_Click;

            // Create context menu and items
            ContextMenu contextMenu = new ContextMenu();

            _renameMenuItem = new MenuItem {Header = Properties.Resources.Rename};
            _renameMenuItem.Click += RenameMenuItem_Click;

            MenuItem chooseSoundMenuItem = new MenuItem {Header = Properties.Resources.ChooseSound};
            chooseSoundMenuItem.Click += ChooseSoundMenuItem_Click;

            contextMenu.Items.Add(chooseSoundMenuItem);
            // (Don't add the "Rename" button until we get a real sound)
            
            ContextMenu = contextMenu;
        }

        #endregion

        #region Event handlers

        private async void RenameMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Stop handling keypresses in the main window
            MainWindow.Instance.RemoveHandler(KeyDownEvent, MainWindow.Instance.KeyDownHandler);

            string result = await MainWindow.Instance.ShowInputAsync(Properties.Resources.Rename,
                Properties.Resources.WhatDoYouWantToCallIt,
                new MetroDialogSettings {DefaultText = Content.ToString()});

            if (!string.IsNullOrEmpty(result))
            {
                Content = SoundName = result;
                MainWindow.Instance.UpdateSoundList();
            }

            // Rehandle keypresses in main window
            MainWindow.Instance.AddHandler(KeyDownEvent, MainWindow.Instance.KeyDownHandler, true);
        }

        private void ChooseSoundMenuItem_Click(object sender, RoutedEventArgs e)
        {
            BrowseForSound();
        }

        private async void SoundFileDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Get the dropped file(s)
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                // Only care about the first file
                string file = files?[0];

                if (string.IsNullOrEmpty(file) == false)
                {
                    // Can only have .mp3 and .wav
                    if (Path.GetExtension(file) != @".mp3" && Path.GetExtension(file) != @".wav")
                    {
                        await MainWindow.Instance.ShowMessageAsync(Properties.Resources.UhOh, Properties.Resources.SupportedFileTypes);
                        return;
                    }

                    // Set it
                    SetFile(file);
                }
            }
        }

        private async void soundButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(SoundPath))
            {
                // If this button doesn't have a sound yet, browse for it now
                BrowseForSound();
            }
            else
            {
                try
                {
                    if (!File.Exists(SoundPath)) throw new Exception(string.Format(Properties.Resources.FileDoesNotExist, SoundPath));

                    // Stop any previous sounds
                    _player.Stop();
                    _player.Dispose();

                    // Reinitialize the player
                    _player = new WaveOut();
                    _audioFileReader = new AudioFileReader(SoundPath);
                    _player.Init(_audioFileReader);

                    // Trick to unmute volume by turning it up and back down again
                    SendMessageW(new WindowInteropHelper(MainWindow.Instance).Handle, WM_APPCOMMAND, new WindowInteropHelper(MainWindow.Instance).Handle, (IntPtr)APPCOMMAND_VOLUME_UP);
                    SendMessageW(new WindowInteropHelper(MainWindow.Instance).Handle, WM_APPCOMMAND, new WindowInteropHelper(MainWindow.Instance).Handle, (IntPtr)APPCOMMAND_VOLUME_DOWN);

                    // Handle stop
                    _player.PlaybackStopped += SoundStoppedHandler;

                    _stopWatch = Stopwatch.StartNew();

                    MainWindow.Instance.SoundPlayers.Add(_player);

                    // Show the additional buttons
                    foreach (HideableMenuButtonBase hideableButton in ChildButtons.OfType<HideableMenuButtonBase>())
                    {
                        hideableButton.Show();
                    }

                    // Aaaaand play
                    _player.Play();

                    // Begin updating progress bar
                    CancellationTokenSource tokenSource = new CancellationTokenSource();
                    await UpdateProgressTask(UpdateProgressAction, TimeSpan.FromMilliseconds(5), tokenSource.Token);
                }
                catch (Exception ex)
                {
                    await MainWindow.Instance.ShowMessageAsync(Properties.Resources.Oops,
                        Properties.Resources.ThereIsAProblem + Environment.NewLine + Environment.NewLine + ex.Message);
                }
            }
        }

        private void SoundStoppedHandler(object sender, EventArgs e)
        {
            _audioFileReader.Position = 0;

            // Hide the additional buttons
            foreach (HideableMenuButtonBase hideableButton in ChildButtons.OfType<HideableMenuButtonBase>())
            {
                hideableButton.Hide();
            }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Prompt the user to browse for and choose a sound for this button
        /// </summary>
        public void BrowseForSound()
        {
            // Show file dialog
            OpenFileDialog dialog = new OpenFileDialog
            {
                // Set file type filters
                DefaultExt = @".wav",
                Filter = Properties.Resources.AudioFiles + @" (*.wav, *.mp3)|*.wav;*.mp3"
            };

            if (dialog.ShowDialog() == true)
            {
                SetFile(dialog.FileName);
            }
        }

        /// <summary>
        /// Set the sound file associated with this button
        /// </summary>
        /// <param name="soundPath"></param>
        /// <param name="soundName"></param>
        /// <param name="newSound"></param>
        public void SetFile(string soundPath, string soundName = "", bool newSound = true)
        {
            if (string.IsNullOrEmpty(soundPath))
            {
                SetDefaultText();
                return;
            }

            // If there was a previous sound here, get rid of it
            try
            {
                if (newSound)
                {
                    MainWindow.Instance.Sounds.Remove(SoundName);
                }
            }
            catch
            {
                // ignored
            }

            SoundPath = soundPath;

            SoundName = string.IsNullOrEmpty(soundName)
                ? Path.GetFileNameWithoutExtension(soundPath).Replace(@"_", "")
                : soundName.Replace(@"_", "");

            Content = SoundName;

            // If this is a new sound on the main soundboard, set up some additional properties
            if (newSound)
            {
                // Set text color
                Foreground = new SolidColorBrush(Colors.Black);

                // Add this sound to dictionary
                MainWindow.Instance.Sounds[SoundName] = SoundPath;

                // Now we can add Rename to the menu
                if (ContextMenu?.Items.Contains(_renameMenuItem) == false)
                {
                    ContextMenu?.Items.Add(_renameMenuItem);
                }
            }
        }

        /// <summary>
        /// Resumes the sound
        /// </summary>
        public void Play()
        {
            _player.Play();
            _stopWatch.Start();
        }

        /// <summary>
        /// Pauses the sound
        /// </summary>
        public void Pause()
        {
            _player.Pause();
            _stopWatch.Stop();
        }

        /// <summary>
        /// Stops the sound
        /// </summary>
        public void Stop()
        {
            _player.Stop();
        }

        #endregion

        #region Private methods

        private void SetDefaultText()
        {
            Content = Properties.Resources.DragASoundHere;
            Foreground = new SolidColorBrush(Colors.Gray);
        }

        private void UpdateProgressAction()
        {
            double maxSeconds = _audioFileReader.TotalTime.TotalMilliseconds;
            double curSeconds = _stopWatch.Elapsed.TotalMilliseconds;

            SoundProgressBar.Visibility = Visibility.Visible;
            SoundProgressBar.Maximum = maxSeconds;
            SoundProgressBar.Value = curSeconds;

            // Hide the progress bar if the sound is done or has been stopped
            if (curSeconds > maxSeconds || _audioFileReader.Position == 0)
            {
                SoundProgressBar.Visibility = Visibility.Hidden;
            }
        }

        private async Task UpdateProgressTask(Action action, TimeSpan interval, CancellationToken token)
        {
            while (true)
            {
                action();
                await Task.Delay(interval, token);
            }
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Defines the mode used by the button
        /// </summary>
        public SoundButtonMode Mode { get; }

        /// <summary>
        /// Defines the progress bar used to show the progress of this sound
        /// </summary>
        public SoundProgressBar SoundProgressBar { get; set; } = new SoundProgressBar();

        /// <summary>
        /// Defines the path of the underlying sound file
        /// </summary>
        public string SoundPath { get; private set; }

        /// <summary>
        /// Defines the name of the sound file as displayed on the button
        /// </summary>
        public string SoundName { get; private set; } = string.Empty;

        /// <summary>
        /// Contains a list of child buttons
        /// </summary>
        public ICollection<MenuButtonBase> ChildButtons { get; } = new List<MenuButtonBase>();

        #endregion

        #region Private fields

        private IWavePlayer _player = new WaveOut();
        private AudioFileReader _audioFileReader;
        private Stopwatch _stopWatch;

        private readonly MenuItem _renameMenuItem;

        #endregion
    }

    #endregion

    #region SoundButtonMode enum

    /// <summary>
    /// Defines the mode used by an instance of <see cref="SoundButton"/>
    /// </summary>
    internal enum SoundButtonMode
    {
        /// <summary>
        /// The button is used in a normal context
        /// </summary>
        Normal,

        /// <summary>
        /// The button is used as a search result
        /// </summary>
        Search
    }

    #endregion
}
