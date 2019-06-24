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
using System.Windows.Input;
using Timer = System.Timers.Timer;

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

        protected HideableMenuButtonBase(SoundButton parentButton) : base(parentButton)
        {
            Padding = new Thickness(Padding.Left, Padding.Top + 2, Padding.Right, Padding.Bottom);
        }

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
                Content = ImageHelper.GetImage(ImageHelper.PlayButtonPath, 11, 11);
            }
            else
            {
                ParentButton.Play();
                _playing = true;
                Content = ImageHelper.GetImage(ImageHelper.PauseButtonPath, 11, 11);
            }
        }

        #endregion

        #region Public methods

        public override void Show()
        {
            base.Show();

            Content = ImageHelper.GetImage(ImageHelper.PauseButtonPath, 11, 11);
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
            Content = ImageHelper.GetImage(ImageHelper.StopButtonPath, 11, 11);

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
        public SoundButton(SoundButtonMode soundButtonMode = SoundButtonMode.Normal, 
                           MetroTabItem parentTab = null, 
                           (MetroTabItem SourceTab, SoundButton SourceButton) sourceTabAndButton = default)
        {
            Mode = soundButtonMode;
            ParentTab = parentTab;
            SourceTabAndButton = sourceTabAndButton;

            if (soundButtonMode == SoundButtonMode.Normal)
            {
                SetDefaultText();
            }

            FontSize = 20;
            Margin = new Thickness(10);
            Style = (Style)FindResource(@"SquareButtonStyle");
            AllowDrop = true;
            Click += soundButton_Click;

            // Create context menu and items
            ContextMenu contextMenu = new ContextMenu();

            _renameMenuItem = new MenuItem {Header = Properties.Resources.Rename};
            _renameMenuItem.Click += RenameMenuItem_Click;

            _clearMenuItem = new MenuItem { Header = Properties.Resources.Clear };
            _clearMenuItem.Click += ClearMenuItem_Click;

            MenuItem chooseSoundMenuItem = new MenuItem {Header = Properties.Resources.ChooseSound};
            chooseSoundMenuItem.Click += ChooseSoundMenuItem_Click;

            MenuItem goToSoundMenuItem = new MenuItem {Header = Properties.Resources.GoToSound};
            goToSoundMenuItem.Click += GoToSoundMenuItem_Click;

            _soundPathMenuItem = new MenuItem();
            _soundPathMenuItem.Click += SoundPathMenuItem_Click;

            _viewSourceMenuItem = new MenuItem { Header = Properties.Resources.Source };
            _viewSourceMenuItem.Items.Add(_soundPathMenuItem);

            if (soundButtonMode == SoundButtonMode.Normal)
            {
                contextMenu.Items.Add(chooseSoundMenuItem);
                // (Don't add the menu items that require a real sound in SetFile is called)
            }
            else if (soundButtonMode == SoundButtonMode.Search)
            {
                contextMenu.Items.Add(goToSoundMenuItem);
            }

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
            }

            // Rehandle keypresses in main window
            MainWindow.Instance.AddHandler(KeyDownEvent, MainWindow.Instance.KeyDownHandler, true);
        }

        private void ClearMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ClearFile();
        }

        private void ChooseSoundMenuItem_Click(object sender, RoutedEventArgs e)
        {
            BrowseForSound();
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
                        Properties.Resources.ThereWasAProblem + Environment.NewLine + Environment.NewLine + ex.Message);
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

        private void GoToSoundMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // First, close the search
            MainWindow.Instance.CloseSearch();

            // Now find the button for the given sound

            if (SourceTabAndButton.SourceTab is MetroTabItem metroTabItem && 
                SourceTabAndButton.SourceButton is SoundButton soundButton)
            {
                // Focus the parent tab
                metroTabItem.Focus();

                // Highlight the sound button
                soundButton.Highlight();
            }
        }

        private void SoundPathMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Open explorer with the current path selected
            Process.Start("explorer.exe", $"/select, \"{SoundPath}\"");
        }

        #endregion

        #region Overrides

        /// <inheritdoc />
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            _mouseDownPosition = Mouse.GetPosition(this);
        }

        /// <inheritdoc />
        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
            _mouseDownPosition = null;
        }

        /// <inheritdoc />
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_mouseDownPosition is null == false && 
                PointsArePastThreshold((Point)_mouseDownPosition, Mouse.GetPosition(this), MOUSE_MOVE_THRESHOLD))
            {
                _mouseDownPosition = Mouse.GetPosition(this);
                DragDrop.DoDragDrop(this, new SoundDragData(SoundName, SoundPath, this), DragDropEffects.Link);
            }
        }

        /// <inheritdoc />
        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            _mouseDownPosition = null;
        }

        /// <inheritdoc />
        protected override void OnDragEnter(DragEventArgs e)
        {
            SoundDragData soundDragData = e.Data.GetData(typeof(SoundDragData)) as SoundDragData;

            if (soundDragData is null == false && soundDragData.Source != this)
            {
                e.Effects = DragDropEffects.Link;
            }
            else if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Link;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

            e.Handled = true;
        }

        /// <inheritdoc />
        protected override void OnDragOver(DragEventArgs e)
        {
            SoundDragData soundDragData = e.Data.GetData(typeof(SoundDragData)) as SoundDragData;

            if (soundDragData is null == false && soundDragData.Source != this)
            {
                e.Effects = DragDropEffects.Link;
            }
            else if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Link;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

            e.Handled = true;
        }

        /// <inheritdoc />
        protected override async void OnDrop(DragEventArgs e)
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

                    // Stop any current recording
                    Stop();

                    // Set it
                    SetFile(file);
                }
            }
            else
            {
                SoundDragData soundDragData = e.Data.GetData(typeof(SoundDragData)) as SoundDragData;
                if (soundDragData is null == false && soundDragData.Source != this)
                {
                    // Set up some placeholders for our source and destination (so we don't lose anything)
                    SoundButton sourceButton = soundDragData.Source;
                    SoundDragData sourceButtonData = soundDragData;

                    SoundButton destinationButton = this;
                    SoundDragData destinationButtonData = new SoundDragData(this.SoundName, this.SoundPath);

                    // Make sure neither of the buttons is currently playing anything
                    sourceButton.Stop();
                    destinationButton.Stop();

                    // Do the swap!
                    sourceButton.SetFile(destinationButtonData.SoundPath, destinationButtonData.SoundName);
                    destinationButton.SetFile(sourceButtonData.SoundPath, sourceButtonData.SoundName);
                }
            }

            e.Handled = true;
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
        /// Removes the file associated with this button
        /// </summary>
        public void ClearFile()
        {
            SetFile(string.Empty);
        }

        /// <summary>
        /// Set the sound file associated with this button
        /// </summary>
        /// <param name="soundPath"></param>
        /// <param name="soundName"></param>
        public void SetFile(string soundPath, string soundName = "")
        {
            if (string.IsNullOrEmpty(soundPath))
            {
                SetDefaultText();
                return;
            }

            SoundPath = soundPath;

            SoundName = string.IsNullOrEmpty(soundName)
                ? Path.GetFileNameWithoutExtension(soundPath).Replace(@"_", "")
                : soundName.Replace(@"_", "");

            Content = SoundName;

            // If this is a normal button on the main soundboard, set up some additional properties
            if (Mode != SoundButtonMode.Search)
            {
                // Set text color
                Foreground = new SolidColorBrush(Colors.Black);

                // Now we can add our menu items which require having a real sound on the button
                if (ContextMenu?.Items.Contains(_renameMenuItem) == false)
                {
                    ContextMenu?.Items.Add(_renameMenuItem);
                }

                if (ContextMenu?.Items.Contains(_clearMenuItem) == false)
                {
                    ContextMenu?.Items.Add(_clearMenuItem);
                }
            }

            if (ContextMenu?.Items.Contains(_viewSourceMenuItem) == false)
            {
                ContextMenu.Items.Add(_separatorMenuItem);
                ContextMenu.Items.Add(_viewSourceMenuItem);
            }

            _soundPathMenuItem.Header = SoundPath;
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

        /// <summary>
        /// Temporarily highlights the button to draw the user's attention to it
        /// </summary>
        public async void Highlight()
        {
            // Change our highlight color to dark gray and
            Resources[@"HighlightColor"] = new SolidColorBrush(Colors.DarkGray);
            Style = (Style) FindResource(@"MyHighlightedSquareButtonStyle");

            // Leave the button highlighted for a second so that the user clearly sees it
            await Task.Delay(ONE_SECOND);

            // ----- Do a homebrew animation using a timer ----- //

            Timer animationTimer = new Timer { Interval = ANIMATION_TIMER_INTERVAL };

            // Find our starting value for R (since it's gray, it will be the same for G and B).
            byte val = ((SolidColorBrush) Resources[@"HighlightColor"]).Color.R;

            // Hook up our timer. Use a local function so that we can unsubscribe by name
            animationTimer.Elapsed += timer_Elapsed;

            void timer_Elapsed(object sender, EventArgs e)
            {
                // Unsubscribe right away so that we don't get double hits
                animationTimer.Elapsed -= timer_Elapsed;

                // Check if we've reached our destination color. If so, stop the timer
                if (val >= 255)
                {
                    animationTimer.Stop();
                    animationTimer.Dispose();
                    return;
                }
                
                // Update the color (on the main thread)
                this.Invoke(() =>
                {
                    val = (byte)Math.Min(255, val + 2); // Make sure we don't go over our target
                    SolidColorBrush adjustedColor = new SolidColorBrush(Color.FromArgb(255, val, val, val));
                    Resources[@"HighlightColor"] = adjustedColor;
                });

                // Subscribe again
                animationTimer.Elapsed += timer_Elapsed;
            }

            // Start our timer
            animationTimer.Start();

            // When our animation is completely done, reset our style
            animationTimer.Disposed += (_, __) =>
            {
                // Remember to modify the style on the main thread
                this.Invoke(() => { Style = (Style) FindResource(@"SquareButtonStyle"); });
            };
        }

        #endregion

        #region Private methods

        private void SetDefaultText()
        {
            SoundPath = string.Empty;
            SoundName = string.Empty;
            Content = Properties.Resources.DragASoundHere;
            Foreground = new SolidColorBrush(Colors.Gray);

            if (ContextMenu?.Items.Contains(_renameMenuItem) == true)
            {
                ContextMenu?.Items.Remove(_renameMenuItem);
            }

            if (ContextMenu?.Items.Contains(_clearMenuItem) == true)
            {
                ContextMenu?.Items.Remove(_clearMenuItem);
            }

            if (ContextMenu?.Items.Contains(_viewSourceMenuItem) == true)
            {
                ContextMenu?.Items.Remove(_separatorMenuItem);
                ContextMenu?.Items.Remove(_viewSourceMenuItem);
            }
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

        /// <summary>
        /// Returns true if the distance (absolute value) between both <paramref name="firstPoint"/>.X and <paramref name="secondPoint"/>.X
        /// and <paramref name="firstPoint"/>.Y and <paramref name="secondPoint"/>.Y is greater than <paramref name="threshold"/>.
        /// </summary>
        /// <param name="firstPoint"></param>
        /// <param name="secondPoint"></param>
        /// <param name="threshold"></param>
        /// <returns></returns>
        private bool PointsArePastThreshold(Point firstPoint, Point secondPoint, int threshold)
        {
            return Math.Abs(firstPoint.X - secondPoint.X) > threshold &&
                   Math.Abs(firstPoint.Y - secondPoint.Y) > threshold;
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

        /// <summary>
        /// When in <see cref="SoundButtonMode.Search"/>, this property specifies the underlying <see cref="MetroTabItem"/> and <see cref="SoundButton"/>
        /// that this search result originated from.
        /// </summary>
        public (MetroTabItem SourceTab, SoundButton SourceButton) SourceTabAndButton { get; }

        /// <summary>
        /// Specifies the <see cref="MetroTabItem"/> on which this sound lives. Will be null when in <see cref="SoundButtonMode.Search"/>.
        /// </summary>
        public MetroTabItem ParentTab { get; }

        #endregion

        #region Private fields

        private IWavePlayer _player = new WaveOut();
        private AudioFileReader _audioFileReader;
        private Stopwatch _stopWatch;

        private readonly MenuItem _renameMenuItem;
        private readonly MenuItem _clearMenuItem;
        private readonly MenuItem _soundPathMenuItem;
        private readonly MenuItem _viewSourceMenuItem;
        private readonly Separator _separatorMenuItem = new Separator();

        private Point? _mouseDownPosition = null;

        #endregion

        #region Private consts

        private const int MOUSE_MOVE_THRESHOLD = 5; // The mouse will have to move at least 5 pixels for the drag operation to start

        private const int ONE_SECOND = 1000; // 1 s in ms

        private const int ANIMATION_TIMER_INTERVAL = 10; // 10 ms

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

    #region SoundDragData class

    internal class SoundDragData
    {
        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="soundName"></param>
        /// <param name="soundPath"></param>
        /// <param name="source"></param>
        public SoundDragData(string soundName, string soundPath, SoundButton source = null)
        {
            SoundName = soundName;
            SoundPath = soundPath;
            Source = source;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// The name (label) of the sound being dragged
        /// </summary>
        public string SoundName { get; }

        /// <summary>
        /// The path of the sound being dragged
        /// </summary>
        public string SoundPath { get; }

        /// <summary>
        /// The control from which the drag data originated
        /// </summary>
        public SoundButton Source { get; }

        #endregion
    }

    #endregion
}
