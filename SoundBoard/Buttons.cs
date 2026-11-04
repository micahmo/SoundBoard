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
using MahApps.Metro.SimpleChildWindow;
using NAudio.Wave.SampleProviders;
using Timer = System.Timers.Timer;
using ControlPaint = System.Windows.Forms.ControlPaint;
using System.Media;
using System.Windows.Media.Animation;
using Humanizer;

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

        #region Public properties

        /// <summary>
        /// Whether or not this button should participate in automatic showing/hiding in relation to sounds playing/stopping
        /// </summary>
        public bool ShowHideAutomatically { get; set; } = true;

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
                MainWindow.Instance.OnAnySoundStopped(ParentButton);
                Content = ImageHelper.GetImage(ImageHelper.PlayButtonPath, 11, 11, Mode == ColorMode.Dark);
            }
            else
            {
                ParentButton.Play();
                _playing = true;
                MainWindow.Instance.OnAnySoundStarted(ParentButton);
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

    #region IconButtonBase class

    /// <summary>
    /// Defines an icon "button" which can be used to display an icon
    /// </summary>
    internal abstract class IconButtonBase : HideableMenuButtonBase
    {
        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="parentButton"></param>
        protected IconButtonBase(SoundButton parentButton) : base(parentButton)
        {
            ShowHideAutomatically = false;

            HorizontalAlignment = HorizontalAlignment.Right;
            VerticalAlignment = VerticalAlignment.Bottom;

            BorderThickness = new Thickness(0);
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Override and handle the left mouse button down.
        /// This essentially makes the button unclickable (without disabling it),
        ///  and prevents the click animation from running
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        #endregion
    }

    #endregion

    #region LoopIconButton class

    internal sealed class LoopIconButton : IconButtonBase
    {
        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="parentButton"></param>
        public LoopIconButton(SoundButton parentButton) : base(parentButton)
        {
            Margin = new Thickness(Margin.Left, Margin.Top, Margin.Right, Margin.Bottom + 25);
            ToolTip = Properties.Resources.SoundSetToLoop;

            if (ParentButton.Loop)
            {
                Show();
            }
            else
            {
                Hide();
            }

            SetUpStyle();
        }

        #endregion

        #region Overrides

        /// <inheritdoc />
        protected override void SetUpStyle()
        {
            Content = ImageHelper.GetImage(ImageHelper.LoopIconPath, 13, 13, Mode == ColorMode.Dark);
        }

        #endregion
    }

    #endregion

    #region VolumeOffsetIconButton class

    internal sealed class VolumeOffsetIconButton : IconButtonBase
    {
        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="parentButton"></param>
        public VolumeOffsetIconButton(SoundButton parentButton) : base(parentButton)
        {
            Margin = new Thickness(Margin.Left, Margin.Top, Margin.Right, Margin.Bottom + 45);
            FontWeight = FontWeights.SemiBold;

            SetUpStyle();
        }

        #endregion

        #region Overrides

        /// <inheritdoc />
        protected override void SetUpStyle()
        {
            Update();
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Updates the volume offset icon so that it reflects the current value, or hides if there is no offset
        /// </summary>
        public void Update()
        {
            if (ParentButton.VolumeOffset == 0 || ParentButton.GlobalHotkey != null || ParentButton.LocalHotkey != null)
            {
                Hide();
            }
            else
            {
                Show();

                Foreground = Mode == ColorMode.Dark
                    ? new SolidColorBrush(Colors.White)
                    : new SolidColorBrush(Colors.Black);

                string volumeOffset = ParentButton.VolumeOffset.ToString(@"+#;-#;0");
                Content = volumeOffset;
                ToolTip = string.Format(Properties.Resources.VolumeOfSoundIsOffset, volumeOffset);
            }
        }

        #endregion
    }

    #endregion

    #region SoundWarningIconButton class

    internal sealed class SoundWarningIconButton : IconButtonBase
    {
        public SoundWarningIconButton(SoundButton parentButton) : base(parentButton)
        {
            VerticalAlignment = VerticalAlignment.Top;
            FontWeight = FontWeights.SemiBold;
            ToolTipService.SetShowDuration(this, (int)TimeSpan.FromSeconds(10).TotalMilliseconds);
            
            SetUpStyle();
        }

        #region Overrides

        /// <inheritdoc />
        protected override void SetUpStyle()
        {
            Content = ImageHelper.GetImage(ImageHelper.WarningIconPath, 16, 16, Mode == ColorMode.Dark);
            Update();
        }

        #endregion

        #region Public methods

        // Playback already prepares a reader in the background; do not probe the codec twice.
        internal void SetPlaybackWarning(string message)
        {
            ToolTip = message;
            Visibility = message == null ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <summary>
        /// Updates the warning depending on whether or not an audio track is detected in the current file
        /// </summary>
        public void Update()
        {
            if (string.IsNullOrEmpty(ParentButton.SoundPath))
            {
                Visibility = Visibility.Collapsed;
            }
            else
            {
                if (!File.Exists(ParentButton.SoundPath))
                {
                    ToolTip = string.Format(Properties.Resources.FileNotFoundWarning, ParentButton.SoundPath);
                    Visibility = Visibility.Visible;
                }
                else
                {
                    try
                    {
                        // Try to instantiate a new reader for this audio file.
                        using (new AudioFileReader(ParentButton.SoundPath))
                        {
                            // If we get here, it's a good sound! Hide the warning.
                            Visibility = Visibility.Collapsed;
                        }

                        // Don't do anything else. Let it get disposed immediately.
                    }
                    catch
                    {
                        // AudioFileReader will throw an exception if the file doesn't contain audio.
                        ToolTip = string.Format(Properties.Resources.NoAudioTrackWarning, Path.GetFileName(ParentButton.SoundPath));
                        Visibility = Visibility.Visible;
                    }
                }
            }
        }

        #endregion
    }

    internal sealed class HotkeyIndicatorButton : IconButtonBase
    {
        public HotkeyIndicatorButton(SoundButton parentButton) : base(parentButton)
        {
            VerticalAlignment = VerticalAlignment.Bottom;
            HorizontalAlignment = HorizontalAlignment.Left;
            Padding = new Thickness(Padding.Left + 20, Padding.Top, Padding.Right, Padding.Bottom);

            SetUpStyle();
        }

        protected override void SetUpStyle()
        {
            Content = ImageHelper.GetImage(ImageHelper.KeyboardIconPath, 16, 16, Mode == ColorMode.Dark);
            Update();
        }

        public void Update()
        {
            if (ParentButton.LocalHotkey != null || ParentButton.GlobalHotkey != null)
            {
                Visibility = Visibility.Visible;
                ToolTip = string.Format(Properties.Resources.HotkeyIndicatorToolTip, ParentButton.LocalHotkey?.ToString() ?? Properties.Resources.None, ParentButton.GlobalHotkey?.ToString() ?? Properties.Resources.None);
            }
            else
            {
                Visibility = Visibility.Collapsed;
                ToolTip = default;
            }
        }
    }

    #endregion

    #region StopAllSoundsIconButton class

    internal sealed class StopAllSoundsIconButton : IconButtonBase
    {
        public StopAllSoundsIconButton(SoundButton parentButton) : base(parentButton)
        {
            VerticalAlignment = VerticalAlignment.Bottom;
            HorizontalAlignment = HorizontalAlignment.Left;
            Padding = new Thickness(Padding.Left + 20, Padding.Top, Padding.Right, Padding.Bottom);
            Margin = new Thickness(Margin.Left, Margin.Top, Margin.Right, Margin.Bottom + 20);
            ToolTip = Properties.Resources.StopAllSoundsIcon;

            SetUpStyle();
        }

        protected override void SetUpStyle()
        {
            Content = ImageHelper.GetImage(ImageHelper.XIconPath, 16, 16, Mode == ColorMode.Dark);
            Update();
        }

        public void Update()
        {
            Visibility = ParentButton.StopAllSounds ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    #endregion

    #region NextSoundIconButton class

    internal sealed class NextSoundIconButton : IconButtonBase
    {
        public NextSoundIconButton(SoundButton parentButton) : base(parentButton)
        {
            VerticalAlignment = VerticalAlignment.Bottom;
            HorizontalAlignment = HorizontalAlignment.Left;
            Padding = new Thickness(Padding.Left + 20, Padding.Top, Padding.Right, Padding.Bottom);
            Margin = new Thickness(Margin.Left, Margin.Top, Margin.Right, Margin.Bottom + 40);

            SetUpStyle();
        }

        protected override void SetUpStyle()
        {
            Content = ImageHelper.GetImage(ImageHelper.RightIconPath, 16, 16, Mode == ColorMode.Dark);
            Update();
        }

        public void Update()
        {
            Visibility = Visibility.Collapsed;

            if (!string.IsNullOrEmpty(ParentButton.NextSound))
            {
                SoundButton soundButton = MainWindow.Instance.GetSoundButtons().FirstOrDefault(sb => sb.Id == ParentButton.NextSound);
                if (soundButton?.HasValidSound == true)
                {
                    Visibility = Visibility.Visible;

                    ToolTip = soundButton.ParentTab != ParentButton.ParentTab
                        ? string.Format(Properties.Resources.NextSoundTab, soundButton.ParentTab.HeaderText, soundButton.SoundName)
                        : string.Format(Properties.Resources.NextSoundName, soundButton.SoundName);
                }
            }
        }
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
                           MyMetroTabItem parentTab = null, 
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
                if (IsSelected)
                {
                    bool anyNotLooped = MainWindow.Instance.GetSoundButtons(ParentTab).Where(sb => sb.IsSelected).Any(sb => !sb.Loop);
                    _loopMenuItem.Icon = !anyNotLooped ? ImageHelper.GetImage(ImageHelper.CheckIconPath) : null;
                }
                else
                {
                    _loopMenuItem.Icon = Loop ? ImageHelper.GetImage(ImageHelper.CheckIconPath) : null;
                }
            }

            _loopMenuItem.IsEnabled = !IsPlaying;
            _holdToPlayMenuItem.IsChecked = IsSelected
                ? MainWindow.Instance.GetSoundButtons(ParentTab).Where(s => s.IsSelected).All(s => s.HoldToPlay)
                : HoldToPlay;

            if (ContextMenu?.Items.Contains(_stopAllSoundsMenuItem) == true)
            {
                if (IsSelected)
                {
                    bool anyNotStopped = MainWindow.Instance.GetSoundButtons(ParentTab).Where(sb => sb.IsSelected).Any(sb => !sb.StopAllSounds);
                    _stopAllSoundsMenuItem.Icon = !anyNotStopped ? ImageHelper.GetImage(ImageHelper.CheckIconPath) : null;
                }
                else
                {
                    _stopAllSoundsMenuItem.Icon = StopAllSounds ? ImageHelper.GetImage(ImageHelper.CheckIconPath) : null;
                }
            }

            // Verify that NextSound is valid
            if (!MainWindow.Instance.GetSoundButtons().Any(sb => sb.Id == NextSound)) // Do not replace !Any with All
            {
                NextSound = default;
            }

            if (ContextMenu?.Items.Contains(_nextSoundMenuItem) == true)
            {
                _nextSoundMenuItem.Icon = string.IsNullOrEmpty(NextSound) ? null : ImageHelper.GetImage(ImageHelper.CheckIconPath);
            }

            // Make everything visible
            ContextMenu?.Items.OfType<Control>().ToList().ForEach(i => i.Visibility = Visibility.Visible);

            // If there is a multi-selection in progress and this is one of the selected buttons,
            // hide things that are not multi applicable.
            if (IsSelected)
            {
                _chooseSoundMenuItem.Visibility = Visibility.Collapsed;
                _renameMenuItem.Visibility = Visibility.Collapsed;
                _viewSourceMenuItem.Visibility = Visibility.Collapsed;
                _hotkeysMenuItem.Visibility = Visibility.Collapsed;
                _nextSoundMenuItem.Visibility = Visibility.Collapsed;

                ContextMenu?.Items.OfType<Separator>().ToList().ForEach(s => s.Visibility = Visibility.Collapsed);
            }
        }

        private async void RenameMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Stop handling keypresses in the main window
            MainWindow.Instance.RemoveHandler(KeyDownEvent, MainWindow.Instance.KeyDownHandler);

            string result = await MainWindow.Instance.ShowInputAsync(Properties.Resources.Rename,
                Properties.Resources.WhatDoYouWantToCallIt,
                new MetroDialogSettings {DefaultText = SoundName});

            if (!string.IsNullOrEmpty(result))
            {
                SetContent(SoundName = result);
            }

            // Rehandle keypresses in main window
            MainWindow.Instance.AddHandler(KeyDownEvent, MainWindow.Instance.KeyDownHandler, true);
        }

        private void ClearMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (IsSelected)
            {
                TabPageSoundsUndoState tabPageSoundsUndoState = (MainWindow.Instance as IUndoable<TabPageSoundsUndoState>).SaveState();

                // Set up our UndoAction
                MainWindow.Instance.SetUndoAction(() => { MainWindow.Instance.LoadState(tabPageSoundsUndoState); });

                // Create and show a snackbar
                string message = Properties.Resources.MultipleSoundsClearedFromTab;
                string truncatedTabName = Utilities.Truncate(ParentTab.HeaderText, MainWindow.Instance.SnackbarMessageFont, (int)Width - 50, message);
                MainWindow.Instance.ShowUndoSnackbar(string.Format(message, truncatedTabName));

                MainWindow.Instance.GetSoundButtons(ParentTab).Where(sb => sb.IsSelected).ToList().ForEach(sb => sb.ClearButton());
            }
            else
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
        }

        private void ChooseSoundMenuItem_Click(object sender, RoutedEventArgs e)
        {
            BrowseForSound();
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

        private void ViewSourceMenuItem_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            _viewSourceMenuItem.Items.Clear();

            MenuItem soundPathMenuItem = new MenuItem();
            soundPathMenuItem.Click += SoundPathMenuItem_Click;

            // Create a textblock to hold the sound path so that we can control truncation
            TextBlock headerTextBlock = new TextBlock
            {
                Text = SoundPath,
                TextWrapping = TextWrapping.Wrap
            };

            soundPathMenuItem.Header = headerTextBlock;

            soundPathMenuItem.IsVisibleChanged += (_, args) =>
            {
                if (args.NewValue as bool? == true) // The menu item is becoming visible
                {
                    if (VisualTreeHelper.GetParent(soundPathMenuItem) is StackPanel stackPanel &&
                        VisualTreeHelper.GetParent(stackPanel) is ItemsPresenter itemsPresenter &&
                        VisualTreeHelper.GetParent(itemsPresenter) is ScrollContentPresenter scrollContentPresenter)
                    {
                        // We've navigated up the visual tree to find the parent with the ACTUAL width
                        void ScrollContentPresenterLoaded(object __, EventArgs ___)
                        {
                            // When it loads, assign the ACTUAL width to the text block so we get proper truncation
                            headerTextBlock.Width = scrollContentPresenter.ActualWidth - 50;
                            scrollContentPresenter.Loaded -= ScrollContentPresenterLoaded;
                        }
                        scrollContentPresenter.Loaded += ScrollContentPresenterLoaded;
                    }
                }
            };

            // Add it to our submenu
            _viewSourceMenuItem.Items.Add(soundPathMenuItem);
        }

        private void SoundPathMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Open explorer with the current path selected
            Process.Start("explorer.exe", $"/select, \"{SoundPath}\"");
        }

        private static readonly IEnumerable<Color> _defaultPalette = (IEnumerable<Color>)typeof(ColorPickerDialog)
            .GetField("DefaultPalette", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)?
            .GetValue(null) ?? Enumerable.Empty<Color>();

        private void SetColorMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var palette = MainWindow.Instance.GetSoundButtons().Where(sb => sb.Color != null).Select(sb => sb.Color.Value) // Existing colors
                .Concat(_defaultPalette) // The default palette
                .Distinct(); // Remove dupes

            ColorPickerDialog colorPickerDialog = new ColorPickerDialog(Color ?? Colors.White, palette)
            {
                Width = 584,
                Height = 491,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.ToolWindow,
                ShowTransparencyPicker = false
            };

            if (colorPickerDialog.ShowDialog() == true)
            {
                if (IsSelected)
                {
                    MainWindow.Instance.GetSoundButtons(ParentTab).Where(sb => sb.IsSelected).ToList().ForEach(sb => sb.Color = colorPickerDialog.Color);
                }
                else
                {
                    Color = colorPickerDialog.Color;
                }
            }
        }

        private void AdjustVolumeMenuItem_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            _adjustVolumeMenuItem.Items.Clear();

            // See if this is a multi-selection and if so, whether all selected sounds have the same volume
            int? volume = null;
            bool multiSelectSameVolume = true;
            foreach (var sb in MainWindow.Instance.GetSoundButtons(ParentTab).Where(sb => sb.IsSelected))
            {
                if (volume == null)
                {
                    volume = sb.VolumeOffset;
                }
                else if (volume != sb.VolumeOffset)
                {
                    multiSelectSameVolume = false;
                    break;
                }
            }

            for (int i = -5; i <= 5; ++i)
            {
                string header = i.ToString(@"+#;-#;0");

                MenuItem volumeAdjustmentMenuItem = new MenuItem {Header = header};

                if (i == VolumeOffset && multiSelectSameVolume)
                {
                    volumeAdjustmentMenuItem.Icon = ImageHelper.GetImage(ImageHelper.CheckIconPath);
                }

                if (_players.Any(p => p.PlaybackState == PlaybackState.Playing))
                {
                    volumeAdjustmentMenuItem.IsEnabled = false;
                }

                int offset = i; // Copy i so we're not accessing modified closure
                volumeAdjustmentMenuItem.Click += (_, __) =>
                {
                    if (IsSelected)
                    {
                        MainWindow.Instance.GetSoundButtons(ParentTab).Where(sb => sb.IsSelected).ToList().ForEach(sb => sb.VolumeOffset = offset);
                    }
                    else
                    {
                        VolumeOffset = offset;
                    }
                };

                _adjustVolumeMenuItem.Items.Add(volumeAdjustmentMenuItem);
            }
        }

        private void NextSoundMenuItem_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            _nextSoundMenuItem.Items.Clear();

            List<MyMetroTabItem> tabs = MainWindow.Instance.Tabs.Items.OfType<MyMetroTabItem>().ToList();
            foreach (MyMetroTabItem metroTabItem in tabs)
            {
                MenuItem tabMenuItem = new MenuItem { Header = metroTabItem.HeaderText.Truncate(50), IsEnabled = false };

                IEnumerable<SoundButton> soundButtons = MainWindow.Instance.GetSoundButtons(metroTabItem).Where(sb => sb.HasValidSound).ToList();
                if (soundButtons.Any())
                {
                    _nextSoundMenuItem.Items.Add(tabMenuItem);

                    foreach (SoundButton soundButton in soundButtons)
                    {
                        MenuItem soundButtonMenuItem = new MenuItem
                        {
                            Header = soundButton.SoundName.Truncate(50),
                            ToolTip = soundButton.SoundName,
                            StaysOpenOnClick = true,
                            Tag = soundButton.Id,
                            Icon = NextSound == soundButton.Id ? ImageHelper.GetImage(ImageHelper.CheckIconPath) : null,
                            IsEnabled = soundButton != this
                        };

                        soundButtonMenuItem.Click += NextSoundItem_Clicked;

                        _nextSoundMenuItem.Items.Add(soundButtonMenuItem);

                        if (soundButton == soundButtons.Last())
                        {
                            _nextSoundMenuItem.Items.Add(new Separator());
                        }
                    }
                }
            }

            if (_nextSoundMenuItem.Items.OfType<object>().LastOrDefault() is Separator separator)
            {
                _nextSoundMenuItem.Items.Remove(separator);
            }
        }

        private void NextSoundItem_Clicked(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && Guid.TryParse(menuItem.Tag?.ToString(), out Guid guid))
            {
                string id = guid.ToString();
                NextSound = NextSound == id ? default : id;

                // Recalculate everything
                foreach (MenuItem otherMenuItem in _nextSoundMenuItem.Items.OfType<MenuItem>())
                {
                    if (Guid.TryParse(otherMenuItem.Tag?.ToString(), out Guid otherGuid))
                    {
                        string otherId = otherGuid.ToString();
                        otherMenuItem.Icon = NextSound == otherId ? ImageHelper.GetImage(ImageHelper.CheckIconPath) : null;
                    }
                }

                // Recalculate the main menu item
                _nextSoundMenuItem.Icon = string.IsNullOrEmpty(NextSound) ? null : ImageHelper.GetImage(ImageHelper.CheckIconPath);
            }
        }

        private void HoldToPlayMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sounds = IsSelected
                ? MainWindow.Instance.GetSoundButtons(ParentTab).Where(s => s.IsSelected).ToList()
                : new List<SoundButton> { this };
            var previous = sounds.ToDictionary(s => s, s => s.SaveState());
            bool enabled = sounds.Any(s => !s.HoldToPlay);
            sounds.ForEach(s => s.HoldToPlay = enabled);
            MainWindow.Instance.SetUndoAction(() =>
            {
                foreach (var item in previous) item.Key.LoadState(item.Value);
                MainWindow.Instance.SaveSettings();
            });
            MainWindow.Instance.ShowUndoSnackbar("Hold-to-play mode changed.");
            MainWindow.Instance.SaveSettings();
        }

        private void LoopMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (IsSelected)
            {
                bool anyNotLooped = MainWindow.Instance.GetSoundButtons(ParentTab).Where(sb => sb.IsSelected).Any(sb => !sb.Loop);

                MainWindow.Instance.GetSoundButtons(ParentTab).Where(sb => sb.IsSelected).ToList().ForEach(sb => sb.Loop = anyNotLooped);
            }
            else
            {
                Loop = !Loop;
            }
        }

        private void StopAllSoundsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (IsSelected)
            {
                bool anyNotStopped = MainWindow.Instance.GetSoundButtons(ParentTab).Where(sb => sb.IsSelected).Any(sb => !sb.StopAllSounds);

                MainWindow.Instance.GetSoundButtons(ParentTab).Where(sb => sb.IsSelected).ToList().ForEach(sb => sb.StopAllSounds = anyNotStopped);
            }
            else
            {
                StopAllSounds = !StopAllSounds;
            }
        }

        private async void HotkeysMenuItemClick(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance.IsHotkeyPickerOpen = true;
            HotkeyDialog hotkeyDialog = new HotkeyDialog(this)
            {
                LocalHotkey = LocalHotkey,
                GlobalHotkey = GlobalHotkey,
                HoldToPlay = HoldToPlay
            };
            try { await MainWindow.Instance.ShowChildWindowAsync(hotkeyDialog); }
            finally { MainWindow.Instance.IsHotkeyPickerOpen = false; }
        }

        #endregion

        #region Overrides

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.XButton1 || e.ChangedButton == MouseButton.XButton2) return;
            // Editing a badge must not select/drag/play its containing sound tile.
            if (_tileHotkeyBadge != null && e.OriginalSource is DependencyObject source &&
                (source == _tileHotkeyBadge || _tileHotkeyBadge.IsAncestorOf(source))) return;
            if (Mode == SoundButtonMode.Normal && HasValidSound)
            {
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                {
                    IsSelected = !IsSelected;

                    LastSelected = this;
                }
                else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                {
                    IsSelected = true;

                    // We also want to select everything between any prior selected button and this one.

                    // Make sure LastSelected is still in the collection
                    if (LastSelected != null && LastSelected != this && LastSelected.IsSelected)
                    {
                        var buttons = MainWindow.Instance.GetSoundButtons(ParentTab).ToList();
                        if (buttons.Contains(LastSelected))
                        {
                            int indexOfThis = buttons.IndexOf(this);
                            int indexOfLastSelected = buttons.IndexOf(LastSelected);
                            for (int i = Math.Min(indexOfThis, indexOfLastSelected); i < Math.Max(indexOfThis, indexOfLastSelected); ++i)
                            {
                                if (buttons[i].HasValidSound)
                                {
                                    buttons[i].IsSelected = true;
                                }
                            }
                        }
                    }

                    LastSelected = this;
                }
            }
        }

        /// <inheritdoc />
        protected override void OnClick()
        {
            base.OnClick();

            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) || Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                // Don't play the sound. This will be handled by OnPreviewMouseDown.
            }
            else
            {
                if (string.IsNullOrEmpty(SoundPath))
                {
                    // If this button doesn't have a sound yet, browse for it now
                    BrowseForSound();
                }
                else
                {
                    if (Mode == SoundButtonMode.Normal)
                    {
                        if (IsSelected)
                        {
                            MainWindow.Instance.GetSoundButtons(ParentTab).Where(sb => sb.IsSelected).ToList().ForEach(sb => sb.StartSound());
                        }
                        else
                        {
                            StartSound();
                        }
                    }
                    else if (Mode == SoundButtonMode.Search &&
                             SourceTabAndButton.SourceButton is SoundButton sourceButton)
                    {
                        sourceButton.StartSound();
                    }
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
                MainWindow.Instance.GetSoundButtons(ParentTab).Where(sb => sb.IsSelected).ToList().ForEach(sb => sb.IsSelected = false);
                
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
        protected override void OnDrop(DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Get the dropped file(s)
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                LoadFiles(files);
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

        /// <inheritdoc/>
        public override string ToString()
        {
            // For debugging
            return $"{ParentTab?.HeaderText} - {SoundName}";
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Start playing the sound associated with this button
        /// </summary>
        public async void StartSound(Func<bool> canStart = null, bool fromHold = false)
        {
            if (canStart != null && !canStart()) return;
            // A direct tile/chain start takes ownership away from any older held input.
            // Do not invalidate canStart for a start dispatched by the hotkey service itself.
            if (canStart == null) MainWindow.Instance?.SoundHotkeys?.CancelSound(this);
            CancelPendingPlayback();
            long request = _playbackGate.Begin();
            Func<bool> current = () => _playbackGate.IsCurrent(request) && (canStart == null || canStart());
            var preparation = new CancellationTokenSource();
            _prepareCancellation = preparation;
            var preparedReaders = new List<AudioFileReader>();

            string path = SoundPath;
            bool readersPrepared = false;
            var warning = ChildButtons.OfType<SoundWarningIconButton>().FirstOrDefault();

            try
            {
                DisposePlaybackResources();
                // Even an existence check can block on network media. Keep it off the hook/UI thread.
                bool exists = await Task.Run(() => File.Exists(path));
                if (!current()) return;
                if (!exists)
                {
                    warning?.SetPlaybackWarning(string.Format(Properties.Resources.FileNotFoundWarning, path));
                    var res = await MainWindow.Instance.ShowMessageAsync(Properties.Resources.Error, string.Format(Properties.Resources.FileDoesNotExist, SoundPath),
                        MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings
                        {
                            AffirmativeButtonText = Properties.Resources.Browse,
                            NegativeButtonText = Properties.Resources.OK
                        });

                    if (!current()) return;
                    if (res == MessageDialogResult.Affirmative)
                    {
                        string originalSoundFileName = Path.GetFileName(SoundPath);
                        if (BrowseForSound(initialFileName: originalSoundFileName))
                        {
                            // We selected a sound, check if it's an exact match
                            if (Path.GetFileName(SoundPath) == originalSoundFileName && Path.GetDirectoryName(SoundPath) is string newDirectory)
                            {
                                Dictionary<SoundButton, string> potentialMatches = new Dictionary<SoundButton, string>();

                                // This is a relinking, so check if there are any other missing sounds that can be relinked from this new directory.
                                foreach (SoundButton soundButton in MainWindow.Instance.GetSoundButtons())
                                {
                                    originalSoundFileName = Path.GetFileName(soundButton.SoundPath);
                                    string potentialNewSoundPath = Path.Combine(newDirectory, originalSoundFileName);

                                    if (!File.Exists(soundButton.SoundPath) // The sound link is missing
                                        && File.Exists(potentialNewSoundPath)) // The broken sound file is found in this new directory
                                    {
                                        potentialMatches[soundButton] = potentialNewSoundPath;
                                    }
                                }

                                // If we found any matches, tell the user and let them decide
                                if (potentialMatches.Any())
                                {
                                    res = await MainWindow.Instance.ShowMessageAsync(Properties.Resources.FixLinksHeader, string.Format(Properties.Resources.FixLinksMessage, potentialMatches.Count),
                                        MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings
                                        {
                                            AffirmativeButtonText = Properties.Resources.Yes,
                                            NegativeButtonText = Properties.Resources.No
                                        });

                                    if (res == MessageDialogResult.Affirmative)
                                    {
                                        using (new WaitCursor())
                                        {
                                            foreach (var kvp in potentialMatches)
                                            {
                                                kvp.Key.SetFile(kvp.Value);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    return;
                }

                if (StopAllSounds) MainWindow.Instance.StopAllPlayback(this);

                // Resolve devices without changing the user's selected Wave Link/output settings.
                var devices = new List<Guid>();
                bool defaultAdded = false;
                foreach (var device in GlobalSettings.GetOutputDeviceGuids())
                {
                    if (Utilities.DoesOutAudioDeviceExist(device)) devices.Add(device);
                    else if (!defaultAdded) { devices.Add(Guid.Empty); defaultAdded = true; }
                }
                // File/codec preparation may be slow. Never block the input hook on it.
                await Task.Run(() =>
                {
                    foreach (var device in devices)
                    {
                        preparation.Token.ThrowIfCancellationRequested();
                        preparedReaders.Add(new AudioFileReader(path));
                    }
                });
                if (!current()) return;
                readersPrepared = true;
                warning?.SetPlaybackWarning(null);

                // Reinitialize the player
                for (int i = 0; i < devices.Count; ++i)
                {
                    var player = new DirectSoundOut(devices[i]);
                    _players.Add(player);
                    _audioFileReaders[player] = preparedReaders[0];
                    preparedReaders.RemoveAt(0); // ownership transferred to this playback
                }

                // Unmute the selected device(s)
                GlobalSettings.GetOutputDeviceGuids().ForEach(d =>
                {
                    Utilities.UnmuteDeviceAudio(d, unmuteDefaultIfGivenNotFound: true);
                });

                // A callback queued by an old/stopped player must never start NextSound.
                bool completionHandled = false;
                _playbackStoppedHandler = (sender, args) => Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (!_playbackGate.IsCurrent(request) || completionHandled || !_players.Contains(sender as IWavePlayer) ||
                        _players.Any(p => p.PlaybackState != PlaybackState.Stopped)) return;
                    completionHandled = true;
                    HandleSoundStopped(!fromHold && args.Exception == null);
                }));
                _players.ForEach(p => p.PlaybackStopped += _playbackStoppedHandler);

                _stopWatch = Stopwatch.StartNew();

                MainWindow.Instance.SoundPlayers.AddRange(_players);

                // Show the additional buttons
                foreach (HideableMenuButtonBase hideableButton in ChildButtons
                    .OfType<HideableMenuButtonBase>()
                    .Where(hideableButton => hideableButton.ShowHideAutomatically))
                {
                    hideableButton.Show();
                }

                // Handle looping
                if (Loop)
                {
                    _audioFileReaders.ToList().ForEach(kvp => _waveProviders[kvp.Key] = new LoopStream(kvp.Value));
                }
                else
                {
                    _audioFileReaders.ToList().ForEach(kvp => _waveProviders[kvp.Key] = kvp.Value);
                }

                // Set the volume
                if (VolumeOffset == 0)
                {
                    _players.ForEach(p => p.Init(_waveProviders[p]));
                }
                else
                {
                    float volume = VolumeOffset < 0 ? 1f / (VolumeOffset * VOLUME_OFFSET_MULTIPLIER) : (VolumeOffset * VOLUME_OFFSET_MULTIPLIER);

                    _players.ForEach(p => p.Init(new VolumeSampleProvider(_waveProviders[p].ToSampleProvider()) {Volume = volume}));
                }

                // A final yield lets queued releases/stop-all win before any audio starts.
                await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.Background);
                if (!current())
                {
                    // A newer request may already own the shared players.
                    if (_playbackGate.IsCurrent(request)) DisposePlaybackResources();
                    return;
                }
                _players.ForEach(p => p.Play());

                CalculateTextMargin();

                MainWindow.Instance.OnAnySoundStarted(this);

                // Begin updating progress bar
                _progressBarCancellationToken?.Cancel();
                _progressBarCancellationToken?.Dispose();
                _progressBarCancellationToken = new CancellationTokenSource();
                UpdateProgressTask(UpdateProgressAction, TimeSpan.FromMilliseconds(5), _progressBarCancellationToken.Token);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                if (current())
                {
                    if (!readersPrepared)
                        warning?.SetPlaybackWarning(string.Format(Properties.Resources.NoAudioTrackWarning, Path.GetFileName(path)));
                    DisposePlaybackResources();
                    await MainWindow.Instance.ShowMessageAsync(Properties.Resources.Error,
                        Properties.Resources.ThereWasAProblem + Environment.NewLine + Environment.NewLine + ex.Message);
                }
            }
            finally
            {
                // Unadopted resources belong exclusively to this request, even after a newer start.
                foreach (var reader in preparedReaders)
                    try { reader.Dispose(); } catch (Exception ex) { Debug.WriteLine(ex); }
                if (ReferenceEquals(_prepareCancellation, preparation)) _prepareCancellation = null;
                preparation.Dispose();
            }
        }

        private object _lock = new object();

        /// <summary>
        /// Prompt the user to browse for and choose a sound for this button
        /// </summary>
        public bool BrowseForSound(string initialFileName = "")
        {
            // Show file dialog
            OpenFileDialog dialog = new OpenFileDialog
            {
                // Set file type filters
                FileName = initialFileName,
                Filter = $@"{Properties.Resources.AudioVideoFiles}|{Utilities.SupportedAudioFileTypes}|All files|*.*",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                LoadFiles(dialog.FileNames);
                return true;
            }

            return false;
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

            Stop();
            SoundPath = soundPath;

            SoundName = string.IsNullOrEmpty(soundName)
                ? Path.GetFileNameWithoutExtension(soundPath).Replace(@"_", "")
                : soundName.Replace(@"_", "");

            SetContent(SoundName);

            SetUpStyle();
            SetUpContextMenu();
        }

        /// <summary>
        /// Resumes the sound
        /// </summary>
        public void Play()
        {
            Parallel.ForEach(_players, p => p.Play());
            _stopWatch.Start();
        }

        /// <summary>
        /// Pauses the sound
        /// </summary>
        public void Pause()
        {
             Parallel.ForEach(_players, p => p.Pause());
            _stopWatch.Stop();
        }

        /// <summary>
        /// Stops the sound
        /// </summary>
        public void Stop()
        {
            MainWindow.Instance?.SoundHotkeys?.CancelSound(this);
            CancelPendingPlayback();
            DisposePlaybackResources();
        }

        internal long CancelPendingPlayback()
        {
            _prepareCancellation?.Cancel();
            return _playbackGate.Cancel();
        }

        internal void StopCanceledPlayback(long version)
        {
            if (_playbackGate.IsCurrent(version)) DisposePlaybackResources();
        }

        internal void DisposePlayback() { Stop(); }

        private void DisposePlaybackResources()
        {
            _progressBarCancellationToken?.Cancel();
            foreach (var player in _players)
            {
                if (_playbackStoppedHandler != null) player.PlaybackStopped -= _playbackStoppedHandler;
                try { player.Stop(); } catch (Exception ex) { Debug.WriteLine(ex); }
                try { player.Dispose(); } catch (Exception ex) { Debug.WriteLine(ex); }
            }
            MainWindow.Instance?.SoundPlayers.RemoveAll(p => _players.Contains(p));
            _players.Clear();
            // LoopStream does not own/dispose its source. Dispose each distinct stream once.
            foreach (var stream in _waveProviders.Values.Concat(_audioFileReaders.Values).Distinct())
                try { stream.Dispose(); } catch (Exception ex) { Debug.WriteLine(ex); }
            _waveProviders.Clear();
            _audioFileReaders.Clear();
            ResetPlaybackDisplay();
        }

        private void ResetPlaybackDisplay()
        {
            _stopWatch?.Stop();
            foreach (var button in ChildButtons.OfType<HideableMenuButtonBase>().Where(b => b.ShowHideAutomatically)) button.Hide();
            if (SoundProgressBar != null) SoundProgressBar.Visibility = Visibility.Hidden;
            CalculateTextMargin();
            if (ParentTab != null) MainWindow.Instance?.OnAnySoundStopped(this);
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

        /// <summary>
        /// Returns the row
        /// </summary>
        /// <returns></returns>
        public int GetRow()
        {
            return Grid.GetRow(this);
        }

        /// <summary>
        /// Returns the column
        /// </summary>
        /// <returns></returns>
        public int GetColumn()
        {
            return Grid.GetColumn(this);
        }

        public void UnregisterLocalHotkey() { MainWindow.Instance?.SoundHotkeys?.CancelSound(this); }
        public void UnregisterGlobalHotkey() { MainWindow.Instance?.SoundHotkeys?.CancelSound(this); }

        // Bindings are read live by the shared, non-suppressing input monitor. No RegisterHotKey
        // reservation is required, so games and other applications still receive the same input.
        public void ReregisterLocalHotkey() { ValidateHotkey(LocalHotkey); }
        public void ReregisterGlobalHotkey() { ValidateHotkey(GlobalHotkey); }

        private static void ValidateHotkey(Hotkey hotkey)
        {
            if (hotkey != null && hotkey.VirtualKey == 0)
                throw new ArgumentException("The hotkey has no usable input.");
        }

        public void CalculateTextMargin()
        {
            if (_viewboxPanel != null && _textBlock != null)
            {
                if (AreTransportControlsVisible // We only shrink when playing
                    && _viewboxPanel.ActualHeight - _textBlock.ActualHeight < 50 // There's not enough room to comfortable display everything
                    && _targetViewboxMarginBottom < 30) // We haven't done this yet
                {
                    _textMarginStoryboard.Stop();
                    _textMarginStoryboard.Children.Clear();

                    ThicknessAnimation animation = new ThicknessAnimation
                    {
                        From = new Thickness(30, 0, 30, _viewboxPanel.Margin.Bottom),
                        To = new Thickness(30, 0, 30, 30),
                        Duration = TimeSpan.FromSeconds(.1)
                    };

                    _targetViewboxMarginBottom = 30;

                    Storyboard.SetTarget(animation, _viewboxPanel);
                    Storyboard.SetTargetProperty(animation, new PropertyPath(MarginProperty));
                    _textMarginStoryboard.Children.Add(animation);
                    _textMarginStoryboard.Begin();
                }
                else if (!AreTransportControlsVisible // Always reset when not playing
                         || (_targetViewboxMarginBottom > 0 && ((_viewboxPanel.ActualHeight + _targetViewboxMarginBottom) - _textBlock.ActualHeight) >= 50)) // The bottom margin is set, but the height that it would be without it set is sufficient
                {
                    _textMarginStoryboard.Stop();
                    _textMarginStoryboard.Children.Clear();

                    ThicknessAnimation animation = new ThicknessAnimation
                    {
                        From = new Thickness(30, 0, 30, _viewboxPanel.Margin.Bottom),
                        To = new Thickness(30, 0, 30, 0),
                        Duration = TimeSpan.FromSeconds(.1)
                    };

                    _targetViewboxMarginBottom = 0;

                    Storyboard.SetTarget(animation, _viewboxPanel);
                    Storyboard.SetTargetProperty(animation, new PropertyPath(MarginProperty));
                    _textMarginStoryboard.Children.Add(animation);
                    _textMarginStoryboard.Begin();
                }
            }
        }

        #endregion

        #region Private methods

        private void SetDefaultText()
        {
            SoundPath = string.Empty;
            SoundName = string.Empty;
            SetContent(Properties.Resources.DragASoundHere);
            Color = null;
            VolumeOffset = 0;
            Loop = false;
            HoldToPlay = false;
            StopAllSounds = false;
            LocalHotkey = null;
            GlobalHotkey = null;
            NextSound = default;

            // Clear any hotkeys
            UnregisterLocalHotkey();
            UnregisterGlobalHotkey();

            Id = Guid.NewGuid().ToString();
            IsSelected = false;

            SetUpStyle();
            SetUpContextMenu();
        }

        /// <summary>
        /// Returns false as long as there is still processing to perform.
        /// Returns true when progress no longer needs to be updated.
        /// </summary>
        private bool UpdateProgressAction()
        {
            bool result = false;

            if (_audioFileReaders.Values.FirstOrDefault() is AudioFileReader audioFileReader)
            {
                double maxSeconds = audioFileReader.TotalTime.TotalMilliseconds;
                double curSeconds = _stopWatch.Elapsed.TotalMilliseconds;

                SoundProgressBar.Visibility = Visibility.Visible;
                SoundProgressBar.Maximum = maxSeconds;
                SoundProgressBar.Value = curSeconds;

                // Position may still be zero before the audio worker reads its first sample.
                // Explicit stop/completion already cancels progress; zero alone is not completion.
                if (curSeconds > maxSeconds)
                {
                    if (Loop && _players.All(p => p.PlaybackState != PlaybackState.Stopped))
                    {
                        _stopWatch = Stopwatch.StartNew();
                    }
                    else
                    {
                        SoundProgressBar.Visibility = Visibility.Hidden;
                        result = true;
                    }
                }
            }

            return result;
        }

        private async void UpdateProgressTask(Func<bool> action, TimeSpan interval, CancellationToken token)
        {
            bool result = false;

            while (!token.IsCancellationRequested && !result)
            {
                result = action();
                await Task.Delay(interval);
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
                _chooseSoundMenuItem.SetSeparator(true);
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
                _clearMenuItem.SetSeparator(true);
                _clearMenuItem.Click += ClearMenuItem_Click;
            }

            if (_setColorMenuItem is null)
            {
                _setColorMenuItem = new MenuItem {Header = Properties.Resources.SetColor};
                _setColorMenuItem.Click += SetColorMenuItem_Click;
            }

            if (_loopMenuItem is null)
            {
                _loopMenuItem = new MenuItem {Header = Properties.Resources.Loop};
                _loopMenuItem.Click += LoopMenuItem_Click;
            }

            if (_holdToPlayMenuItem is null)
            {
                _holdToPlayMenuItem = new MenuItem
                {
                    Header = "Hold to play (hotkeys)",
                    ToolTip = "Press starts once from the beginning; release stops. Loop repeats while held. Click playback is unchanged."
                };
                _holdToPlayMenuItem.Click += HoldToPlayMenuItem_Click;
            }
            _holdToPlayMenuItem.IsChecked = HoldToPlay;

            if (_stopAllSoundsMenuItem is null)
            {
                _stopAllSoundsMenuItem = new MenuItem { Header = Properties.Resources.StopAllSounds, ToolTip = Properties.Resources.StopAllSoundsToolTip };
                _stopAllSoundsMenuItem.Click += StopAllSoundsMenuItem_Click;
            }

            if (_adjustVolumeMenuItem is null)
            {
                _adjustVolumeMenuItem = new MenuItem {Header = Properties.Resources.AdjustVolume};

                // Add a dummy item so that this item becomes a parent with a sub-menu
                // The real items will be populated every time at run-time in the SubmenuOpened handler
                _adjustVolumeMenuItem.Items.Add(new MenuItem());

                _adjustVolumeMenuItem.SubmenuOpened += AdjustVolumeMenuItem_SubmenuOpened;
            }

            if (_hotkeysMenuItem is null)
            {
                _hotkeysMenuItem = new MenuItem { Header = Properties.Resources.SetHotkeys };
                _hotkeysMenuItem.Click += HotkeysMenuItemClick;
            }

            if (_nextSoundMenuItem is null)
            {
                _nextSoundMenuItem = new MenuItem { Header = Properties.Resources.NextSound };
                _nextSoundMenuItem.Items.Add(new MenuItem()); // Placeholder for submenu
                _nextSoundMenuItem.SetSeparator(true);
                _nextSoundMenuItem.SubmenuOpened += NextSoundMenuItem_SubmenuOpened;
            }

            // If the "Source" menu item is null, create it and hook up its handler
            if (_viewSourceMenuItem is null)
            {
                _viewSourceMenuItem = new MenuItem {Header = Properties.Resources.Source};
                _viewSourceMenuItem.Items.Add(new MenuItem()); // Add a dummy menu item so this item always has a submenu
                _viewSourceMenuItem.SubmenuOpened += ViewSourceMenuItem_SubmenuOpened;
            }

            // If the "Go to sound" menu item is null, create it and hook up its handler
            if (_goToSoundMenuItem is null)
            {
                _goToSoundMenuItem = new MenuItem { Header = Properties.Resources.GoToSound };
                _goToSoundMenuItem.SetSeparator(true);
                _goToSoundMenuItem.Click += GoToSoundMenuItem_Click;
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

                    if (!ContextMenu.Items.Contains(_holdToPlayMenuItem)) ContextMenu.Items.Add(_holdToPlayMenuItem);

                    if (ContextMenu.Items.Contains(_stopAllSoundsMenuItem) == false)
                    {
                        ContextMenu.Items.Add(_stopAllSoundsMenuItem);
                    }

                    if (ContextMenu.Items.Contains(_adjustVolumeMenuItem) == false)
                    {
                        ContextMenu.Items.Add(_adjustVolumeMenuItem);
                    }

                    if (ContextMenu.Items.Contains(_hotkeysMenuItem) == false)
                    {
                        ContextMenu.Items.Add(_hotkeysMenuItem);
                    }

                    if (ContextMenu.Items.Contains(_nextSoundMenuItem) == false)
                    {
                        ContextMenu.Items.Add(_nextSoundMenuItem);
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
                if (ContextMenu.Items.Contains(_viewSourceMenuItem) == false)
                {
                    ContextMenu.Items.Add(_viewSourceMenuItem);
                }
            }
            else
            {
                ContextMenu.Items.Remove(_holdToPlayMenuItem);
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

                if (ContextMenu.Items.Contains(_stopAllSoundsMenuItem))
                {
                    ContextMenu.Items.Remove(_stopAllSoundsMenuItem);
                }

                if (ContextMenu.Items.Contains(_adjustVolumeMenuItem))
                {
                    ContextMenu.Items.Remove(_adjustVolumeMenuItem);
                }

                if (ContextMenu.Items.Contains(_viewSourceMenuItem))
                {
                    ContextMenu.Items.Remove(_viewSourceMenuItem);
                }

                if (ContextMenu.Items.Contains(_hotkeysMenuItem))
                {
                    ContextMenu.Items.Remove(_hotkeysMenuItem);
                }

                if (ContextMenu.Items.Contains(_nextSoundMenuItem))
                {
                    ContextMenu.Items.Remove(_nextSoundMenuItem);
                }
            }

            ContextMenu.AddSeparators();

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
            Style style = new Style(GetType(), (Style)FindResource(@"MySquareButtonStyle"));

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

            // Add focused colors
            if (Mode == SoundButtonMode.Search)
            {
                Trigger focusTrigger = new Trigger { Property = IsFocusedProperty, Value = true };
                focusTrigger.Setters.Add(new Setter(BorderThicknessProperty, new Thickness(5)));
                focusTrigger.Setters.Add(new Setter(BorderBrushProperty, new SolidColorBrush(Colors.SlateGray)));
                style.Triggers.Add(focusTrigger);
            }

            // Don't show the ugly dotted line around focused elements
            FocusVisualStyle = null;

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

        private void HandleSoundStopped(bool allowNext)
        {
            _progressBarCancellationToken?.Cancel();
            bool finished = _audioFileReaders.Values.Any(r => r.Position >= r.Length);
            ResetPlaybackDisplay();
            if (allowNext && finished && !string.IsNullOrEmpty(NextSound) &&
                MainWindow.Instance.GetSoundButtons().FirstOrDefault(sb => sb.HasValidSound && sb.Id == NextSound) is SoundButton nextSoundButton)
            {
                if (MainWindow.Instance.IsActive && nextSoundButton.ParentTab != MainWindow.Instance.SelectedTab)
                    nextSoundButton.ParentTab.Focus();
                nextSoundButton.StartSound();
            }
        }

        private void SetContent(string text)
        {
            if (Mode == SoundButtonMode.Normal)
            {
                _textBlock = new TextBlock
                {
                    Text = text,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap
                };

                _viewboxPanel = new ViewboxPanel
                {
                    Margin = new Thickness(30, 0, 30, 0)
                };
                _viewboxPanel.Children.Add(_textBlock);

                // Reserve independent space for the binding, so long names never cover it.
                var layout = new Grid { ClipToBounds = true };
                _viewboxPanel.IsHitTestVisible = false;
                layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                layout.Children.Add(_viewboxPanel);
                _tileHotkeyText = new TextBlock
                {
                    FontSize = 32, FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Center
                };
                _tileHotkeyDetail = new TextBlock
                {
                    FontSize = 11, TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 3, 0, 0)
                };
                var keyStack = new StackPanel();
                keyStack.Children.Add(_tileHotkeyText);
                keyStack.Children.Add(_tileHotkeyDetail);
                var badgeBorder = new FrameworkElementFactory(typeof(Border));
                badgeBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(5));
                badgeBorder.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(BackgroundProperty));
                badgeBorder.SetValue(Border.PaddingProperty, new TemplateBindingExtension(PaddingProperty));
                badgeBorder.AppendChild(new FrameworkElementFactory(typeof(ContentPresenter)));
                _tileHotkeyBadge = new Button
                {
                    Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(225, 20, 23, 29)),
                    Padding = new Thickness(8, 5, 8, 5), Cursor = Cursors.Hand,
                    Margin = new Thickness(12, 4, 12, 44),
                    Template = new ControlTemplate(typeof(Button)) { VisualTree = badgeBorder },
                    Content = new Viewbox { StretchDirection = StretchDirection.DownOnly, Child = keyStack }
                };
                _tileHotkeyBadge.Click += (sender, args) =>
                {
                    args.Handled = true;
                    if (MainWindow.Instance?.IsHotkeyPickerOpen == false) HotkeysMenuItemClick(sender, args);
                };
                _tileHotkeyText.Foreground = Brushes.White;
                _tileHotkeyDetail.Foreground = Brushes.White;
                Grid.SetRow(_tileHotkeyBadge, 1);
                layout.Children.Add(_tileHotkeyBadge);
                HorizontalContentAlignment = HorizontalAlignment.Stretch;
                VerticalContentAlignment = VerticalAlignment.Stretch;
                Content = layout;
                UpdateTileHotkey();
            }
            else if (Mode == SoundButtonMode.Search)
            {
                // Just do straight scaling with no wrapping
                TextBlock textBlock = new TextBlock
                {
                    Text = text,
                    TextAlignment = TextAlignment.Center
                };

                Viewbox viewbox = new Viewbox
                {
                    StretchDirection = StretchDirection.DownOnly
                };
                viewbox.Child = textBlock;

                Content = viewbox;
            }
        }

        private void UpdateTileHotkey()
        {
            if (_tileHotkeyBadge == null) return;
            var primary = GlobalHotkey ?? LocalHotkey;
            _tileHotkeyBadge.Visibility = primary == null ? Visibility.Collapsed : Visibility.Visible;
            _tileHotkeyText.Text = primary?.DisplayText ?? string.Empty;
            string scope = GlobalHotkey != null ? "GLOBAL" : "LOCAL";
            _tileHotkeyDetail.Text = (HoldToPlay ? "HOLD · " : "") + scope;
            if (GlobalHotkey != null && LocalHotkey != null)
                _tileHotkeyDetail.Text += " · Local: " + LocalHotkey.DisplayText;
            if (VolumeOffset != 0)
                _tileHotkeyDetail.Text += " · VOL " + VolumeOffset.ToString(@"+#;-#;0");
            ChildButtons.OfType<VolumeOffsetIconButton>().FirstOrDefault()?.Update();
            _tileHotkeyBadge.ToolTip = "Click to edit hotkey — " + _tileHotkeyText.Text;
            System.Windows.Automation.AutomationProperties.SetName(_tileHotkeyBadge,
                "Edit hotkey: " + _tileHotkeyText.Text);
        }

        private Button _tileHotkeyBadge;
        private TextBlock _tileHotkeyText, _tileHotkeyDetail;

        private void LoadFiles(params string[] files)
        {
            List<string> multiFileDrop = new List<string>();

            if (files?.Length > 1)
            {
                multiFileDrop.AddRange(files);
            }
            else if (!string.IsNullOrEmpty(files?[0]) && Directory.Exists(files[0]))
            {
                multiFileDrop.AddRange(Directory.GetFiles(files[0]));
            }

            if (multiFileDrop.Any())
            {
                // This is a multi-file drop!

                // Since this is a big operation, make it undoable
                ConfigUndoState configUndoState = (MainWindow.Instance as IUndoable<ConfigUndoState>).SaveState();
                MainWindow.Instance.SetUndoAction(() => { MainWindow.Instance.LoadState(configUndoState); });

                // Set our grid size to exactly match the number
                int rows = ParentTab.GetRows();
                int columns = ParentTab.GetColumns();
                bool? lastOperation = false; // False means added column, true means added first row, null means added second row
                while (rows * columns < multiFileDrop.Count)
                {
                    if (lastOperation == false)
                    {
                        ++rows;
                        lastOperation = true;
                    }
                    else if (lastOperation == true)
                    {
                        ++rows;
                        lastOperation = null;
                    }
                    else if (lastOperation == null)
                    {
                        ++columns;
                        lastOperation = false;
                    }
                }

                // Get starting index before potentially changing grid, since that recreates all buttons
                var startingIndex = MainWindow.Instance.GetSoundButtons(ParentTab).ToList().IndexOf(this);

                if (rows != ParentTab.GetRows() || columns != ParentTab.GetColumns())
                {
                    MainWindow.Instance.ChangeButtonGrid(rows, columns);
                }

                // Start populating the buttons
                var buttons = MainWindow.Instance.GetSoundButtons(MainWindow.Instance.SelectedTab).ToList();
                buttons = buttons.GetRange(startingIndex, buttons.Count - startingIndex).Concat(buttons.GetRange(0, startingIndex)).ToList();
                for (int i = 0; i < multiFileDrop.Count; ++i)
                {
                    buttons[i].Stop();
                    buttons[i].SetFile(multiFileDrop[i]);
                }

                // Finally, make it undoable
                string message = Properties.Resources.MultipleSoundsAdded;
                string truncatedMessage = Utilities.Truncate(message, MainWindow.Instance.SnackbarMessageFont, (int)Width - 50);
                MainWindow.Instance.ShowUndoSnackbar(truncatedMessage);
            }
            else
            {
                // Only care about the first file
                string file = files?[0];

                if (string.IsNullOrEmpty(file) == false)
                {
                    // Stop any current playback
                    Stop();

                    // Set it
                    SetFile(file);
                }
            }
        }

        #endregion

        #region Private properties

        internal bool HasValidSound => string.IsNullOrEmpty(SoundPath) == false;

        internal SoundButtonStyle SoundButtonStyle
        {
            get
            {
                SoundButtonStyle soundButtonStyle = new SoundButtonStyle();
                soundButtonStyle.BackgroundColor = Color;

                if (HasValidSound == false)
                {
                    // Not a valid sound yet, use a "placeholder" color
                    soundButtonStyle.ForegroundColor = System.Windows.Media.Color.FromRgb(168, 168, 168);
                }
                else
                {
                    // Valid sound; calculate our other colors based on our background color
                    if (soundButtonStyle.BackgroundColor is Color backgroundColor)
                    {
                        bool lightColor = backgroundColor.ToSystemDrawingColor().GetBrightness() > 0.5;
                        soundButtonStyle.ForegroundColor = lightColor ? Colors.Black : Colors.White;

                        if (backgroundColor.IsWhite() || backgroundColor.IsBlack())
                        {
                            // If the background is completely black or white, use the hover color from the built in SquareButtonStyle
                            Style defaultStyle = (Style) FindResource(@"SquareButtonStyle");
                            Trigger mouseOverTrigger = defaultStyle.Triggers.OfType<Trigger>().FirstOrDefault(trigger =>
                                trigger.Property == IsMouseOverProperty && trigger.Value as bool? == true);
                            Setter backgroundPropertySetter = mouseOverTrigger?.Setters.OfType<Setter>()
                                .FirstOrDefault(setter => setter.Property == BackgroundProperty);

                            soundButtonStyle.BackgroundHoverColor = backgroundPropertySetter?.Value as Color?;
                        }
                        else
                        {
                            // For normal background colors, pick a hover color that is slightly lighter or slightly darker
                            soundButtonStyle.BackgroundHoverColor = lightColor
                                ? ControlPaint.Light(backgroundColor.ToSystemDrawingColor()).ToSystemWindowsMediaColor()
                                : ControlPaint.Dark(backgroundColor.ToSystemDrawingColor(), 0.1f).ToSystemWindowsMediaColor();
                        }

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
        public string SoundPath
        {
            get => _soundPath;
            private set
            {
                _soundPath = value;
                ChildButtons.OfType<SoundWarningIconButton>().FirstOrDefault()?.Update();
            }
        }
        private string _soundPath;

        /// <summary>
        /// Defines the name of the sound file as displayed on the button
        /// </summary>
        public string SoundName
        {
            get => _soundName;
            private set
            {
                _soundName = value;
                MainWindow.Instance.OnAnySoundRenamed();
            }
        }
        private string _soundName;

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

        public int VolumeOffset
        {
            get => _volumeOffset;
            private set
            {
                _volumeOffset = value;

                ChildButtons.OfType<VolumeOffsetIconButton>().FirstOrDefault()?.Update();
                UpdateTileHotkey();
            }
        }

        private int _volumeOffset = 0; // Backing field

        public bool Loop
        {
            get => _loop;
            private set
            {
                _loop = value;

                if (_loop)
                {
                    ChildButtons.OfType<LoopIconButton>().FirstOrDefault()?.Show();
                }
                else
                {
                    ChildButtons.OfType<LoopIconButton>().FirstOrDefault()?.Hide();
                }
            }
        }

        private bool _loop; // Backing field

        public bool HoldToPlay
        {
            get => _holdToPlay;
            set
            {
                if (_holdToPlay == value) return;
                Stop();
                _holdToPlay = value;
                if (_holdToPlayMenuItem != null) _holdToPlayMenuItem.IsChecked = value;
                UpdateTileHotkey();
            }
        }
        private bool _holdToPlay;

        public bool StopAllSounds
        {
            get => _stopAllSounds;
            set
            {
                _stopAllSounds = value;
                ChildButtons.OfType<StopAllSoundsIconButton>().FirstOrDefault()?.Update();
            }
        }
        private bool _stopAllSounds;

        public string Id { get; set; }

        public Hotkey LocalHotkey
        {
            get => _localHotkey;
            set
            {
                MainWindow.Instance?.SoundHotkeys?.CancelSound(this);
                _localHotkey = value;
                ChildButtons.OfType<HotkeyIndicatorButton>().FirstOrDefault()?.Update();
                UpdateTileHotkey();
            }
        }
        private Hotkey _localHotkey;

        public Hotkey GlobalHotkey
        {
            get => _globalHotkey;
            set
            {
                MainWindow.Instance?.SoundHotkeys?.CancelSound(this);
                _globalHotkey = value;
                ChildButtons.OfType<HotkeyIndicatorButton>().FirstOrDefault()?.Update();
                UpdateTileHotkey();
            }
        }
        private Hotkey _globalHotkey;

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
        public MyMetroTabItem ParentTab { get; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;

                if (_isSelected)
                {
                    BorderThickness = new Thickness(5);
                    BorderBrush = new SolidColorBrush(Colors.SlateGray);
                }
                else
                {
                    BorderThickness = new Thickness(2);
                    BorderBrush = new SolidColorBrush(Colors.Black);
                }
            }
        }
        private bool _isSelected;

        public bool IsPlaying => !_players.All(p => p.PlaybackState != PlaybackState.Playing); // Do not let ReSharper refactor this as !All is different than Any

        public bool AreTransportControlsVisible => ChildButtons.OfType<HideableMenuButtonBase>().Where(b => b.ShowHideAutomatically).Any(b => b.Visibility == Visibility.Visible);

        public string NextSound
        {
            get => _nextSound;
            set
            {
                _nextSound = value;
                ChildButtons.OfType<NextSoundIconButton>().FirstOrDefault()?.Update();
            }
        }
        private string _nextSound;

        #endregion

        #region Public static properties

        public static SoundButton LastSelected { get; set; }

        #endregion

        #region Private fields

        #region Players

        private readonly PlaybackGate _playbackGate = new PlaybackGate();
        private CancellationTokenSource _prepareCancellation;
        private EventHandler<StoppedEventArgs> _playbackStoppedHandler;
        private readonly List<IWavePlayer> _players = new List<IWavePlayer>();
        private readonly Dictionary<IWavePlayer, AudioFileReader> _audioFileReaders = new Dictionary<IWavePlayer, AudioFileReader>();
        private readonly Dictionary<IWavePlayer, WaveStream> _waveProviders = new Dictionary<IWavePlayer, WaveStream>();

        #endregion

        private Stopwatch _stopWatch;

        private MenuItem _chooseSoundMenuItem;
        private MenuItem _renameMenuItem;
        private MenuItem _clearMenuItem;
        private MenuItem _viewSourceMenuItem;
        private MenuItem _goToSoundMenuItem;
        private MenuItem _setColorMenuItem;
        private MenuItem _adjustVolumeMenuItem;
        private MenuItem _loopMenuItem;
        private MenuItem _holdToPlayMenuItem;
        private MenuItem _stopAllSoundsMenuItem;
        private MenuItem _hotkeysMenuItem;
        private MenuItem _nextSoundMenuItem;

        private Point? _mouseDownPosition;

        private CancellationTokenSource _progressBarCancellationToken;

        // Related to text resizing
        private ViewboxPanel _viewboxPanel;
        private int _targetViewboxMarginBottom;
        private TextBlock _textBlock;
        private readonly Storyboard _textMarginStoryboard = new Storyboard();

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
                Loop = Loop,
                HoldToPlay = HoldToPlay,
                StopAllSounds = StopAllSounds,
                NextSound = NextSound,
                Id = Id,
                LocalHotkey = LocalHotkey,
                GlobalHotkey = GlobalHotkey
            };
        }

        public void LoadState(SoundButtonUndoState undoState)
        {
            Stop();
            if (string.IsNullOrEmpty(undoState.SoundPath) == false)
            {
                if (!string.IsNullOrEmpty(undoState.Id))
                {
                    Id = undoState.Id;
                }

                SetFile(undoState.SoundPath);
                SetContent(SoundName = undoState.SoundName);
                Color = undoState.Color;
                VolumeOffset = undoState.VolumeOffset;
                Loop = undoState.Loop;
                HoldToPlay = undoState.HoldToPlay;
                StopAllSounds = undoState.StopAllSounds;
                NextSound = undoState.NextSound;

                LocalHotkey = undoState.LocalHotkey;
                GlobalHotkey = undoState.GlobalHotkey;

                try
                {
                    ReregisterLocalHotkey();
                }
                catch
                {
                    // Swallow
                }

                try
                {
                    ReregisterGlobalHotkey();
                }
                catch
                {
                    // Swallow
                }
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
