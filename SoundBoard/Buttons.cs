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
using BondTech.HotKeyManagement.WPF._4;

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
            if (ParentButton.VolumeOffset == 0)
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

            _loopMenuItem.IsEnabled = _players.All(p => p.PlaybackState != PlaybackState.Playing);

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
                string truncatedTabName = Utilities.Truncate(ParentTab.Header.ToString(), MainWindow.Instance.SnackbarMessageFont, (int)Width - 50, message);
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

        private void SoundStoppedHandler(object sender, StoppedEventArgs e)
        {
            if (sender is IWavePlayer player)
            {
                HandleSoundStopped(player);
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

        private void SetColorMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ColorPickerDialog colorPickerDialog = new ColorPickerDialog(Color ?? Colors.White) { ShowTransparencyPicker = false };

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

        private async void HotkeysMenuItemClick(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance.IsHotkeyPickerOpen = true;
            HotkeyDialog hotkeyDialog = new HotkeyDialog(this)
            {
                LocalHotkey = LocalHotkey,
                GlobalHotkey = GlobalHotkey
            };
            await MainWindow.Instance.ShowChildWindowAsync(hotkeyDialog);
            MainWindow.Instance.IsHotkeyPickerOpen = false;
        }

        #endregion

        #region Overrides

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
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
                        StartSound();
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

        #endregion

        #region Public methods

        /// <summary>
        /// Start playing the sound associated with this button
        /// </summary>
        public async void StartSound()
        {
            // Every time the sound is started, update the warning status
            ChildButtons.OfType<SoundWarningIconButton>().FirstOrDefault()?.Update();

            try
            {
                if (!File.Exists(SoundPath))
                {
                    var res = await MainWindow.Instance.ShowMessageAsync(Properties.Resources.Error, string.Format(Properties.Resources.FileDoesNotExist, SoundPath),
                        MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings
                        {
                            AffirmativeButtonText = Properties.Resources.Browse,
                            NegativeButtonText = Properties.Resources.OK
                        });

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

                // Stop any previous sounds
                _players.ForEach(p => p.PlaybackStopped -= SoundStoppedHandler);
                _players.ForEach(HandleSoundStopped);
                Stop();
                _players.ForEach(p => p.Dispose());
                MainWindow.Instance.SoundPlayers.RemoveAll(p => _players.Contains(p));

                _players.Clear();

                // Reinitialize the player
                bool addedDefaultDevice = false;
                GlobalSettings.GetOutputDeviceGuids().ForEach(d =>
                {
                    if (Utilities.DoesOutAudioDeviceExist(d))
                    {
                        _players.Add(new DirectSoundOut(d));
                    }
                    else if (!addedDefaultDevice)
                    {
                        _players.Add(new DirectSoundOut(Guid.Empty));
                        addedDefaultDevice = true;
                    }
                });

                _waveProviders.ToList().ForEach(kvp => { try { kvp.Value.Close(); } catch { /* Swallow */ } });
                _waveProviders.Clear();

                _audioFileReaders.ToList().ForEach(kvp => { try { kvp.Value.Close(); } catch { /* Swallow */ } });

                _audioFileReaders.Clear();
                _players.ForEach(p => _audioFileReaders[p] = new AudioFileReader(SoundPath));

                // Unmute the selected device(s)
                GlobalSettings.GetOutputDeviceGuids().ForEach(d =>
                {
                    Utilities.UnmuteDeviceAudio(d, unmuteDefaultIfGivenNotFound: true);
                });

                // Handle stop
                _players.ForEach(p => p.PlaybackStopped += SoundStoppedHandler);

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

                // Aaaaand play
                Parallel.ForEach(_players, p => p.Play());

                // Begin updating progress bar
                _progressBarCancellationToken?.Cancel();
                _progressBarCancellationToken?.Dispose();
                _progressBarCancellationToken = new CancellationTokenSource();
                UpdateProgressTask(UpdateProgressAction, TimeSpan.FromMilliseconds(5), _progressBarCancellationToken.Token);
            }
            catch (Exception ex)
            {
                await MainWindow.Instance.ShowMessageAsync(Properties.Resources.Error,
                    Properties.Resources.ThereWasAProblem + Environment.NewLine + Environment.NewLine + ex.Message);
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
            Parallel.ForEach(_players.Where(p => p.PlaybackState != PlaybackState.Stopped), p => p.Stop());
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

        public void UnregisterLocalHotkey()
        {
            foreach (var existingLocalHotKey in MainWindow.Instance.HotKeyManager?.EnumerateLocalHotKeys.OfType<LocalHotKey>() ?? Enumerable.Empty<LocalHotKey>())
            {
                if (existingLocalHotKey.Name == Utilities.SanitizeId(Id))
                {
                    try
                    {
                        MainWindow.Instance.HotKeyManager.RemoveLocalHotKey(existingLocalHotKey);
                    }
                    catch
                    {
                        // Swallow
                    }

                    break;
                }
            }
        }

        public void UnregisterGlobalHotkey()
        {
            foreach (var existingGlobalHotKey in MainWindow.Instance.HotKeyManager?.EnumerateGlobalHotKeys.OfType<GlobalHotKey>() ?? Enumerable.Empty<GlobalHotKey>())
            {
                if (existingGlobalHotKey.Name == Utilities.SanitizeId(Id))
                {
                    try
                    {
                        MainWindow.Instance.HotKeyManager.RemoveGlobalHotKey(existingGlobalHotKey);
                    }
                    catch
                    {
                        // Swallow
                    }

                    break;
                }
            }
        }

        /// <summary>
        /// Always wrap this in a try/catch
        /// </summary>
        public void ReregisterLocalHotkey()
        {
            Keys mappedKey = Utilities.MapKey(LocalHotkey.Key);

            // The mapping failed
            if (mappedKey == default)
            {
                throw new Exception();
            }

            LocalHotKey localHotKey = new LocalHotKey(Utilities.SanitizeId(Id), LocalHotkey.Modifiers, mappedKey, RaiseLocalEvent.OnKeyUp, true);
            MainWindow.Instance.HotKeyManager.AddLocalHotKey(localHotKey);
        }

        /// <summary>
        /// Always wrap this in a try/catch
        /// </summary>
        public void ReregisterGlobalHotkey()
        {
            Keys mappedKey = Utilities.MapKey(GlobalHotkey.Key);

            // The mapping failed
            if (mappedKey == default)
            {
                throw new Exception();
            }

            GlobalHotKey globalHotKey = new GlobalHotKey(Utilities.SanitizeId(Id), GlobalHotkey.Modifiers, mappedKey, true);
            MainWindow.Instance.HotKeyManager.AddGlobalHotKey(globalHotKey);
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
            LocalHotkey = null;
            GlobalHotkey = null;

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

                // Hide the progress bar if the sound is done or has been stopped
                if (curSeconds > maxSeconds || audioFileReader.Position == 0)
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

            while (token.IsCancellationRequested == false || result == false)
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
                _hotkeysMenuItem.SetSeparator(true);
                _hotkeysMenuItem.Click += HotkeysMenuItemClick;
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

                    if (ContextMenu.Items.Contains(_adjustVolumeMenuItem) == false)
                    {
                        ContextMenu.Items.Add(_adjustVolumeMenuItem);
                    }

                    if (ContextMenu.Items.Contains(_hotkeysMenuItem) == false)
                    {
                        ContextMenu.Items.Add(_hotkeysMenuItem);
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
                    ContextMenu.Items.Remove(_viewSourceMenuItem);
                }

                if (ContextMenu.Items.Contains(_hotkeysMenuItem))
                {
                    ContextMenu.Items.Remove(_hotkeysMenuItem);
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

        private void HandleSoundStopped(IWavePlayer player)
        {
            _progressBarCancellationToken?.Cancel();

            if (_audioFileReaders.TryGetValue(player, out var audioFileReader) && audioFileReader != null)
            {
                {
                    audioFileReader.Position = 0;
                }
            }

            // Hide the additional buttons
            foreach (HideableMenuButtonBase hideableButton in ChildButtons
                .OfType<HideableMenuButtonBase>()
                .Where(hideableButton => hideableButton.ShowHideAutomatically))
            {
                hideableButton.Hide();
            }
        }

        private void SetContent(string text)
        {
            if (Mode == SoundButtonMode.Normal)
            {
                TextBlock textBlock = new TextBlock
                {
                    Text = text,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap
                };

                ViewboxPanel viewboxPanel = new ViewboxPanel
                {
                    Margin = new Thickness(30)
                };
                viewboxPanel.Children.Add(textBlock);

                Content = viewboxPanel;
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

        public int VolumeOffset
        {
            get => _volumeOffset;
            private set
            {
                _volumeOffset = value;

                ChildButtons.OfType<VolumeOffsetIconButton>().FirstOrDefault()?.Update();
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

        public string Id { get; set; }

        public Hotkey LocalHotkey
        {
            get => _localHotkey;
            set
            {
                _localHotkey = value;
                ChildButtons.OfType<HotkeyIndicatorButton>().FirstOrDefault()?.Update();
            }
        }
        private Hotkey _localHotkey;

        public Hotkey GlobalHotkey
        {
            get => _globalHotkey;
            set
            {
                _globalHotkey = value;
                ChildButtons.OfType<HotkeyIndicatorButton>().FirstOrDefault()?.Update();
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
        public MetroTabItem ParentTab { get; }

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

        #endregion

        #region Public static properties

        public static SoundButton LastSelected { get; set; }

        #endregion

        #region Private fields

        #region Players

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
        private MenuItem _hotkeysMenuItem;

        private Point? _mouseDownPosition;

        private CancellationTokenSource _progressBarCancellationToken;

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
                Id = Id,
                LocalHotkey = LocalHotkey,
                GlobalHotkey = GlobalHotkey
            };
        }

        public void LoadState(SoundButtonUndoState undoState)
        {
            if (string.IsNullOrEmpty(undoState.SoundPath) == false)
            {
                SetFile(undoState.SoundPath);
                SetContent(SoundName = undoState.SoundName);
                Color = undoState.Color;
                VolumeOffset = undoState.VolumeOffset;
                Loop = undoState.Loop;

                if (!string.IsNullOrEmpty(undoState.Id))
                {
                    Id = undoState.Id;
                }

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
