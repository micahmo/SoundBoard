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
using System.Windows.Controls;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using System.Windows.Input;
using Dsafa.WpfColorPicker;
using NAudio.Wave.SampleProviders;
using Timer = System.Timers.Timer;
using ControlPaint = System.Windows.Forms.ControlPaint;

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
            Width = 35;
            Height = 35;
            Margin = new Thickness(0, 15, 15, 15);
            Padding = new Thickness(0.5, 0, 0, 1.5);

            Style = (Style) FindResource(@"MetroCircleButtonStyle");
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="parentButton"></param>
        protected MenuButtonBase(SoundButton parentButton) : this()
        {
            ParentButton = parentButton;

            // Default mode is light, unless the parent button specifies otherwise
            Mode = ColorMode.Light;
            if (ParentButton?.SoundButtonStyle?.IsLightColor == false)
            {
                Mode = ColorMode.Dark;
            }

            SetUpStyle();
        }

        #endregion

        #region Protected methods

        /// <summary>
        /// Set the mode of the button
        /// </summary>
        public virtual void SetMode(ColorMode mode = ColorMode.Dark)
        {
            Mode = mode;
            SetUpStyle();
        }

        /// <summary>
        /// Sets up the WPF button style
        /// </summary>
        protected virtual void SetUpStyle()
        {
            Style style = new Style(GetType(), (Style)FindResource(@"MetroCircleButtonStyle"));

            if (Mode == ColorMode.Dark)
            {
                // If we're in dark mode, our button borders should be white.
                style.Setters.Add(new Setter(BorderBrushProperty, new SolidColorBrush(Colors.White)));
            }

            // Apply the style
            Style = style;
        }

        #endregion

        #region Properties

        protected readonly SoundButton ParentButton;

        public ColorMode Mode;

        #endregion

        #region Mode enum

        public enum ColorMode
        {
            /// <summary>
            /// Light mode means dark buttons
            /// </summary>
            Light,

            /// <summary>
            /// Dark mode means light buttons
            /// </summary>
            Dark
        }

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
            Padding = new Thickness(Padding.Left, Padding.Top + 2, Padding.Right, Padding.Bottom);

            Content = ImageHelper.GetImage(ImageHelper.MenuButtonPath, 15, 15, Mode == ColorMode.Dark);

            VerticalAlignment = VerticalAlignment.Bottom;
            HorizontalAlignment = HorizontalAlignment.Right;
        }

        #endregion

        #region Overrides

        protected override void OnClick()
        {
            base.OnClick();

            if (ParentButton.ContextMenu is null == false)
            {
                ParentButton.ContextMenu.IsOpen = true;
            }
        }

        public override void SetMode(ColorMode mode = ColorMode.Dark)
        {
            base.SetMode(mode);

            Content = ImageHelper.GetImage(ImageHelper.MenuButtonPath, 15, 15, Mode == ColorMode.Dark);
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

        #region Overrides

        protected override void OnClick()
        {
            base.OnClick();

            if (_playing)
            {
                ParentButton.Pause();
                _playing = false;
                Content = ImageHelper.GetImage(ImageHelper.PlayButtonPath, 11, 11, Mode == ColorMode.Dark);
            }
            else
            {
                ParentButton.Play();
                _playing = true;
                Content = ImageHelper.GetImage(ImageHelper.PauseButtonPath, 11, 11, Mode == ColorMode.Dark);
            }
        }

        public override void SetMode(ColorMode mode = ColorMode.Dark)
        {
            base.SetMode(mode);

            if (_playing)
            {
                Content = ImageHelper.GetImage(ImageHelper.PauseButtonPath, 11, 11, mode == ColorMode.Dark);
            }
            else
            {
                Content = ImageHelper.GetImage(ImageHelper.PlayButtonPath, 11, 11, mode == ColorMode.Dark);
            }
        }

        #endregion

        #region Public methods

        public override void Show()
        {
            base.Show();

            Content = ImageHelper.GetImage(ImageHelper.PauseButtonPath, 11, 11, Mode == ColorMode.Dark);
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
            Content = ImageHelper.GetImage(ImageHelper.StopButtonPath, 11, 11, Mode == ColorMode.Dark);

            VerticalAlignment = VerticalAlignment.Bottom;
            HorizontalAlignment = HorizontalAlignment.Center;
            Margin = new Thickness(Width, Margin.Top, 0, Margin.Bottom);

            Visibility = Visibility.Hidden; // Hidden by default
        }

        #endregion

        #region Overrides

        protected override void OnClick()
        {
            base.OnClick();

            ParentButton.Stop();
        }

        public override void SetMode(ColorMode mode = ColorMode.Dark)
        {
            base.SetMode(mode);

            Content = ImageHelper.GetImage(ImageHelper.StopButtonPath, 11, 11, mode == ColorMode.Dark);
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
        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public SoundProgressBar()
        {
            Margin = new Thickness(10);
            VerticalAlignment = VerticalAlignment.Bottom;

            // Hide by default
            Visibility = Visibility.Hidden;
        }

        #endregion
    }

    #endregion

    #region SoundButton class

    /// <summary>
    /// Defines a Button which plays a Sound
    /// </summary>
    internal sealed class SoundButton : Button, IUndoable<SoundButtonUndoState>
    {
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
            AllowDrop = true;

            SetUpStyle();
            SetUpContextMenu();
        }

        #endregion

        #region Event handlers

        private void ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            if (ContextMenu?.Items.Contains(_loopMenuItem) == true)
            {
                _loopMenuItem.Icon = Loop ? ImageHelper.GetImage(ImageHelper.CheckIconPath) : null;
            }

            _loopMenuItem.IsEnabled = _player.PlaybackState != PlaybackState.Playing;
        }

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
            SoundButtonUndoState soundButtonUndoState = SaveState();

            // Set up our UndoAction
            MainWindow.Instance.SetUndoAction(() => { LoadState(soundButtonUndoState); });

            // Create and show a snackbar
            string message = Properties.Resources.SoundWasCleared;
            string truncatedSoundName = Utilities.Truncate(SoundName, MainWindow.Instance.SnackbarMessageFont, (int)MainWindow.Instance.Width - 50, message);
            MainWindow.Instance.ShowUndoSnackbar(string.Format(message, truncatedSoundName));

            ClearButton();
        }

        private void ChooseSoundMenuItem_Click(object sender, RoutedEventArgs e)
        {
            BrowseForSound();
        }

        private void SoundStoppedHandler(object sender, StoppedEventArgs e)
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

        private void SetColorMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ColorPickerDialog colorPickerDialog = new ColorPickerDialog(Color ?? Colors.White) { ShowTransparencyPicker = false };

            if (colorPickerDialog.ShowDialog() == true)
            {
                Color = colorPickerDialog.Color;
            }
        }

        private void AdjustVolumeMenuItem_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            _adjustVolumeMenuItem.Items.Clear();

            for (int i = -5; i <= 5; ++i)
            {
                string header = (i > 0) ? $@"+{i}" : i.ToString();

                MenuItem volumeAdjustmentMenuItem = new MenuItem { Header = header };

                if (i == VolumeOffset)
                {
                    volumeAdjustmentMenuItem.Icon = ImageHelper.GetImage(ImageHelper.CheckIconPath);
                }

                if (_player.PlaybackState == PlaybackState.Playing)
                {
                    volumeAdjustmentMenuItem.IsEnabled = false;
                }

                int offset = i; // Copy i so we're not accessing modified closure
                volumeAdjustmentMenuItem.Click += (_, __) => { VolumeOffset = offset; };

                _adjustVolumeMenuItem.Items.Add(volumeAdjustmentMenuItem);
            }
        }

        private void LoopMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Loop = !Loop;
        }

        #endregion

        #region Overrides

        /// <inheritdoc />
        protected override void OnClick()
        {
            base.OnClick();

            if (string.IsNullOrEmpty(SoundPath))
            {
                // If this button doesn't have a sound yet, browse for it now
                BrowseForSound();
            }
            else
            {
                if (Mode == SoundButtonMode.Normal)
                {
                    StartSound();
                }
                else if (Mode == SoundButtonMode.Search && 
                         SourceTabAndButton.SourceButton is SoundButton sourceButton)
                {
                    sourceButton.StartSound();
                }
            }
        }

        /// <inheritdoc />
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.ChangedButton == MouseButton.Left)
            {
                _mouseDownPosition = Mouse.GetPosition(this);
            }
        }

        /// <inheritdoc />
        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);

            if (e.ChangedButton == MouseButton.Left)
            {
                _mouseDownPosition = null;
            }
        }

        /// <inheritdoc />
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_mouseDownPosition is null == false && 
                Utilities.PointsArePastThreshold((Point)_mouseDownPosition, Mouse.GetPosition(this)) &&
                Mode != SoundButtonMode.Search)
            {
                _mouseDownPosition = Mouse.GetPosition(this);
                DragDrop.DoDragDrop(this, new SoundDragData(this), DragDropEffects.Link);
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
                    SoundButtonUndoState sourceButtonState = sourceButton.SaveState();

                    SoundButton destinationButton = this;
                    SoundButtonUndoState destinationButtonState = destinationButton.SaveState();

                    // Make sure neither of the buttons is currently playing anything
                    sourceButton.Stop();
                    destinationButton.Stop();

                    // Do the swap!
                    sourceButton.LoadState(destinationButtonState);
                    destinationButton.LoadState(sourceButtonState);
                }
            }

            e.Handled = true;
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Start playing the sound associated with this button
        /// </summary>
        public async void StartSound()
        {
            try
            {
                if (!File.Exists(SoundPath))
                {
                    throw new Exception(string.Format(Properties.Resources.FileDoesNotExist, SoundPath));
                }

                // Stop any previous sounds
                Stop();
                _player.Dispose();

                // Reinitialize the player
                _player = new WaveOut();
                _audioFileReader = new AudioFileReader(SoundPath);
                IWaveProvider waveProvider;

                // Unmute the system audio for all active/render devices
                Utilities.UnmuteSystemAudio();

                // Handle stop
                _player.PlaybackStopped += SoundStoppedHandler;

                _stopWatch = Stopwatch.StartNew();

                MainWindow.Instance.SoundPlayers.Add(_player);

                // Show the additional buttons
                foreach (HideableMenuButtonBase hideableButton in ChildButtons.OfType<HideableMenuButtonBase>())
                {
                    hideableButton.Show();
                }
                
                // Handle looping
                if (Loop)
                {
                    waveProvider = new LoopStream(_audioFileReader);
                }
                else
                {
                    waveProvider = _audioFileReader;
                }

                // Set the volume
                if (VolumeOffset == 0)
                {
                    _player.Init(waveProvider);
                }
                else
                {
                    float volume = VolumeOffset < 0 ? 1f / (VolumeOffset * VOLUME_OFFSET_MULTIPLIER) : (VolumeOffset * VOLUME_OFFSET_MULTIPLIER);

                    _player.Init(new VolumeSampleProvider(waveProvider.ToSampleProvider())
                    {
                        Volume = volume
                    });
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
        public void ClearButton()
        {
            Stop();
            Color = null;
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

            SetUpStyle();
            SetUpContextMenu();
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
            if (_player.PlaybackState != PlaybackState.Stopped)
            {
                _player.Stop();
            }
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
                if (val >= byte.MaxValue)
                {
                    animationTimer.Stop();
                    animationTimer.Dispose();
                    return;
                }
                
                // Update the color (on the main thread)
                this.Invoke(() =>
                {
                    val = (byte)Math.Min(byte.MaxValue, val + 2); // Make sure we don't go over our target
                    SolidColorBrush adjustedColor = new SolidColorBrush(System.Windows.Media.Color.FromArgb(byte.MaxValue, val, val, val));
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
                // Remember to update the style on the main thread
                this.Invoke(SetUpStyle);
            };
        }

        #endregion

        #region Private methods

        private void SetDefaultText()
        {
            SoundPath = string.Empty;
            SoundName = string.Empty;
            Content = Properties.Resources.DragASoundHere;
            Color = null;
            VolumeOffset = 0;
            Loop = false;

            SetUpStyle();
            SetUpContextMenu();
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
                if (Loop && _player.PlaybackState != PlaybackState.Stopped)
                {
                    _stopWatch = Stopwatch.StartNew();
                }
                else
                {
                    SoundProgressBar.Visibility = Visibility.Hidden;
                }
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
        /// Sets up the <see cref="System.Windows.Controls.ContextMenu"/> for the current button.
        /// Should be called initially (i.e., in the constructor)
        /// and any time the button's state changes (i.e., when a sound is added/changed)
        /// </summary>
        private void SetUpContextMenu()
        {
            // If we don't have a context menu yet, create one and assign it
            if (ContextMenu is null)
            {
                ContextMenu = new ContextMenu();
            }

            // ----- Initialize our menu items ----- //

            // If the "Choose sound" menu item is null, create it and hook up its handler
            if (_chooseSoundMenuItem is null)
            {
                _chooseSoundMenuItem = new MenuItem {Header = Properties.Resources.ChooseSound};
                _chooseSoundMenuItem.Click += ChooseSoundMenuItem_Click;
            }

            // If the "Rename" menu item is null, create it and hook up its handler
            if (_renameMenuItem is null)
            {
                _renameMenuItem = new MenuItem {Header = Properties.Resources.Rename};
                _renameMenuItem.Click += RenameMenuItem_Click;
            }

            // If the "Clear" menu item is null, create it and hook up its handler
            if (_clearMenuItem is null)
            {
                _clearMenuItem = new MenuItem {Header = Properties.Resources.Clear};
                _clearMenuItem.Click += ClearMenuItem_Click;
            }

            if (_setColorMenuItem is null)
            {
                _setColorMenuItem = new MenuItem {Header = Properties.Resources.SetColor};
                _setColorMenuItem.Click += SetColorMenuItem_Click;
            }

            if (_loopMenuItem is null)
            {
                _loopMenuItem = new MenuItem { Header = Properties.Resources.Loop };
                _loopMenuItem.Click += LoopMenuItem_Click;
            }

            if (_adjustVolumeMenuItem is null)
            {
                _adjustVolumeMenuItem = new MenuItem {Header = Properties.Resources.AdjustVolume};

                // Add a dummy item so that this item becomes a parent with a sub-menu
                // The real items will be populated every time at run-time in the SubmenuOpened handler
                _adjustVolumeMenuItem.Items.Add(new MenuItem());

                _adjustVolumeMenuItem.SubmenuOpened += AdjustVolumeMenuItem_SubmenuOpened;
            }

            // If the path menu item is null, create it and hook up its handler
            if (_soundPathMenuItem is null)
            {
                _soundPathMenuItem = new MenuItem();
                _soundPathMenuItem.Click += SoundPathMenuItem_Click;
            }

            // If the "Source" menu item is null, create it and hook up its handler
            if (_viewSourceMenuItem is null)
            {
                _viewSourceMenuItem = new MenuItem {Header = Properties.Resources.Source};
                _viewSourceMenuItem.Items.Add(_soundPathMenuItem);
            }

            // If the "Go to sound" menu item is null, create it and hook up its handler
            if (_goToSoundMenuItem is null)
            {
                _goToSoundMenuItem = new MenuItem { Header = Properties.Resources.GoToSound };
                _goToSoundMenuItem.Click += GoToSoundMenuItem_Click;
            }

            // If our separator menu item is null, create it
            if (_separatorMenuItem is null)
            {
                _separatorMenuItem = new Separator();
            }

            // ----- Add our menu items to our context menu, depending on our current state ----- //

            if (Mode == SoundButtonMode.Normal)
            {
                // Add our menu items for our Normal mode

                if (ContextMenu.Items.Contains(_chooseSoundMenuItem) == false)
                {
                    ContextMenu.Items.Add(_chooseSoundMenuItem);
                }

                if (HasValidSound)
                {
                    if (ContextMenu.Items.Contains(_renameMenuItem) == false)
                    {
                        ContextMenu.Items.Add(_renameMenuItem);
                    }

                    if (ContextMenu.Items.Contains(_clearMenuItem) == false)
                    {
                        ContextMenu.Items.Add(_clearMenuItem);
                    }

                    if (ContextMenu.Items.Contains(_setColorMenuItem) == false)
                    {
                        ContextMenu.Items.Add(_setColorMenuItem);
                    }

                    if (ContextMenu.Items.Contains(_loopMenuItem) == false)
                    {
                        ContextMenu.Items.Add(_loopMenuItem);
                    }

                    if (ContextMenu.Items.Contains(_adjustVolumeMenuItem) == false)
                    {
                        ContextMenu.Items.Add(_adjustVolumeMenuItem);
                    }
                }
            }
            else if (Mode == SoundButtonMode.Search)
            {
                // Add our  menu items for our Search mode

                if (ContextMenu.Items.Contains(_goToSoundMenuItem) == false)
                {
                    ContextMenu.Items.Add(_goToSoundMenuItem);
                }
            }

            // Add our menu items for either mode
            if (HasValidSound)
            {
                _soundPathMenuItem.Header = SoundPath;

                if (ContextMenu.Items.Contains(_viewSourceMenuItem) == false)
                {
                    ContextMenu.Items.Add(_separatorMenuItem);
                    ContextMenu.Items.Add(_viewSourceMenuItem);
                }
            }
            else
            {
                // Remove some menu items that should not be in the menu if we no longer have a valid sound
                if (ContextMenu.Items.Contains(_renameMenuItem))
                {
                    ContextMenu.Items.Remove(_renameMenuItem);
                }

                if (ContextMenu.Items.Contains(_clearMenuItem))
                {
                    ContextMenu.Items.Remove(_clearMenuItem);
                }

                if (ContextMenu.Items.Contains(_setColorMenuItem))
                {
                    ContextMenu.Items.Remove(_setColorMenuItem);
                }

                if (ContextMenu.Items.Contains(_loopMenuItem))
                {
                    ContextMenu.Items.Remove(_loopMenuItem);
                }

                if (ContextMenu.Items.Contains(_adjustVolumeMenuItem))
                {
                    ContextMenu.Items.Remove(_adjustVolumeMenuItem);
                }

                if (ContextMenu.Items.Contains(_viewSourceMenuItem))
                {
                    ContextMenu.Items.Remove(_separatorMenuItem);
                    ContextMenu.Items.Remove(_viewSourceMenuItem);
                }
            }

            ContextMenu.Opened -= ContextMenu_Opened; // Unassign before re-assigning so we don't get double assignment
            ContextMenu.Opened += ContextMenu_Opened;
        }

        /// <summary>
        /// Creates and sets a <see cref="Style"/> from the current <see cref="Color"/>.
        /// </summary>
        private void SetUpStyle()
        {
            SoundButtonStyle soundButtonStyle = SoundButtonStyle;

            // Create a new style based on the SquareButtonStyle
            Style style = new Style(GetType(), (Style)FindResource(@"SquareButtonStyle"));

            // Add the background color
            if (soundButtonStyle.BackgroundColor is Color backgroundColor)
            {
                style.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush(backgroundColor)));
            }

            // Add the foreground color
            if (soundButtonStyle.ForegroundColor is Color foregroundColor)
            {
                style.Setters.Add(new Setter(ForegroundProperty, new SolidColorBrush(foregroundColor)));

                // Add the background hover color
                if (soundButtonStyle.BackgroundHoverColor is Color backgroundHoverColor)
                {
                    Trigger trigger = new Trigger { Property = IsMouseOverProperty, Value = true };
                    trigger.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush(backgroundHoverColor)));
                    trigger.Setters.Add(new Setter(ForegroundProperty, new SolidColorBrush(foregroundColor)));
                    style.Triggers.Add(trigger);
                }
            }

            // Add clicked colors
            if (soundButtonStyle.BackgroundClickColor is Color backgroundClickColor &&
                soundButtonStyle.ForegroundClickColor is Color foregroundClickColor)
            {
                Trigger trigger = new Trigger { Property = IsPressedProperty, Value = true };
                trigger.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush(backgroundClickColor)));
                trigger.Setters.Add(new Setter(ForegroundProperty, new SolidColorBrush(foregroundClickColor)));
                style.Triggers.Add(trigger);
            }

            // Assign the style!
            Style = style;

            // Restyle the child buttons
            if (soundButtonStyle.IsLightColor is bool isLightColor && isLightColor == false)
            {
                foreach (MenuButtonBase menuButtonBase in ChildButtons)
                {
                    menuButtonBase.SetMode(MenuButtonBase.ColorMode.Dark);
                }
            }
            else
            {
                foreach (MenuButtonBase menuButtonBase in ChildButtons)
                {
                    menuButtonBase.SetMode(MenuButtonBase.ColorMode.Light);
                }
            }
        }

        #endregion

        #region Private properties

        private bool HasValidSound => string.IsNullOrEmpty(SoundPath) == false;

        internal SoundButtonStyle SoundButtonStyle
        {
            get
            {
                SoundButtonStyle soundButtonStyle = new SoundButtonStyle();
                soundButtonStyle.BackgroundColor = Color;

                if (HasValidSound == false)
                {
                    // Not a valid sound yet, use a "placeholder" color
                    soundButtonStyle.ForegroundColor = Colors.Gray;
                }
                else
                {
                    // Valid sound; calculate our other colors based on our background color
                    if (soundButtonStyle.BackgroundColor is Color backgroundColor)
                    {
                        bool lightColor = backgroundColor.ToSystemDrawingColor().GetBrightness() > 0.5;

                        soundButtonStyle.ForegroundColor = lightColor ? Colors.Black : Colors.White;

                        soundButtonStyle.BackgroundHoverColor = lightColor
                                ? ControlPaint.Light(backgroundColor.ToSystemDrawingColor()).ToSystemWindowsMediaColor()
                                : ControlPaint.Dark(backgroundColor.ToSystemDrawingColor(), 0.1f).ToSystemWindowsMediaColor();

                        soundButtonStyle.BackgroundClickColor = Colors.Black;
                        soundButtonStyle.ForegroundClickColor = Colors.White;

                        soundButtonStyle.IsLightColor = lightColor;
                    }
                }

                return soundButtonStyle;
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
        /// Defines the background color of the button
        /// </summary>
        public Color? Color
        {
            get => _color;
            private set
            {
                _color = value;
                SetUpStyle();
            }
        }

        private Color? _color = null; // Backing field. Need the "= null", even though it's the default.

        public int VolumeOffset { get; private set; } = 0;

        public bool Loop { get; private set; }

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

        private MenuItem _chooseSoundMenuItem;
        private MenuItem _renameMenuItem;
        private MenuItem _clearMenuItem;
        private MenuItem _soundPathMenuItem;
        private MenuItem _viewSourceMenuItem;
        private MenuItem _goToSoundMenuItem;
        private Separator _separatorMenuItem;
        private MenuItem _setColorMenuItem;
        private MenuItem _adjustVolumeMenuItem;
        private MenuItem _loopMenuItem;

        private Point? _mouseDownPosition;

        #endregion

        #region Private consts

        private const int ONE_SECOND = 1000; // 1 s in ms

        private const int ANIMATION_TIMER_INTERVAL = 10; // 10 ms

        private const float VOLUME_OFFSET_MULTIPLIER = 2f;

        #endregion

        #region IUndoable members

        public SoundButtonUndoState SaveState()
        {
            return new SoundButtonUndoState
            {
                SoundPath = SoundPath,
                SoundName = SoundName,
                Color = Color,
                VolumeOffset = VolumeOffset,
                Loop = Loop
            };
        }

        public void LoadState(SoundButtonUndoState undoState)
        {
            if (string.IsNullOrEmpty(undoState.SoundPath) == false)
            {
                SetFile(undoState.SoundPath);
                Content = SoundName = undoState.SoundName;
                Color = undoState.Color;
                VolumeOffset = undoState.VolumeOffset;
                Loop = undoState.Loop;
            }
            else
            {
                SetDefaultText();
            }
        }

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

    #region SoundButtonStyle class

    /// <summary>
    /// Defines a Style applied to a <see cref="SoundButton"/>.
    /// </summary>
    internal class SoundButtonStyle
    {
        /// <summary>
        /// Defines the background color of the SoundButton
        /// </summary>
        public Color? BackgroundColor { get; set; }

        /// <summary>
        /// Defines the foreground color of the SoundButton
        /// </summary>
        public Color? ForegroundColor { get; set; }

        /// <summary>
        /// Defines the background color of the SoundButton when the mouse is over it
        /// </summary>
        public Color? BackgroundHoverColor { get; set; }

        /// <summary>
        /// Defines the background color of the SoundButton when it is clicked
        /// </summary>
        public Color? BackgroundClickColor { get; set; }

        /// <summary>
        /// Defines the foreground color of the SoundButton when it is clicked
        /// </summary>
        public Color? ForegroundClickColor { get; set; }

        /// <summary>
        /// Whether the main color palette of this style is light (e.g., requiring dark foreground)
        /// </summary>
        public bool? IsLightColor { get; set; }
    }

    #endregion

    #region SoundDragData class

    internal class SoundDragData
    {
        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public SoundDragData(SoundButton source = null)
        {
            Source = source;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// The control from which the drag data originated
        /// </summary>
        public SoundButton Source { get; }

        #endregion
    }

    #endregion
}
