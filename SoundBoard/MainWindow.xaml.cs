#region Usings

using System;
using System.IO;
using System.Xml;
using System.Text;
using NAudio.Wave;
using System.Windows;
using System.Reflection;
using System.Windows.Input;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System.Runtime.InteropServices;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using Gma.System.MouseKeyHook;
using Microsoft.Win32;
using Color = System.Windows.Media.Color;
using Timer = System.Timers.Timer;
using ContextMenu = System.Windows.Controls.ContextMenu;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MenuItem = System.Windows.Controls.MenuItem;

#endregion

namespace SoundBoard
{ 
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public sealed partial class MainWindow : IUndoable<TabPageUndoState>, IUndoable<ConfigUndoState>, IUndoable<TabPageSoundsUndoState>
    {
        #region P/Invoke stuff

        enum MapType : uint
        {
            MAPVK_VK_TO_VSC = 0x0,
            MAPVK_VSC_TO_VK = 0x1,
            MAPVK_VK_TO_CHAR = 0x2,
            MAPVK_VSC_TO_VK_EX = 0x3,
        }

        [DllImport("user32.dll")]
        static extern int ToUnicode(uint wVirtKey, uint wScanCode, byte[] lpKeyState, [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 4)] StringBuilder pwszBuff, int cchBuff, uint wFlags);

        [DllImport("user32.dll")]
        static extern bool GetKeyboardState(byte[] lpKeyState);

        [DllImport("user32.dll")]
        static extern uint MapVirtualKey(uint uCode, MapType uMapType);

        static char GetCharFromKey(Key key)
        {
            char ch = ' ';

            int virtualKey = KeyInterop.VirtualKeyFromKey(key);
            byte[] keyboardState = new byte[256];
            GetKeyboardState(keyboardState);

            uint scanCode = MapVirtualKey((uint)virtualKey, MapType.MAPVK_VK_TO_VSC);
            StringBuilder stringBuilder = new StringBuilder(2);

            int result = ToUnicode((uint)virtualKey, scanCode, keyboardState, stringBuilder, stringBuilder.Capacity, 0);
            switch (result)
            {
                case -1:
                    break;
                case 0:
                    break;
                case 1:
                    {
                        ch = stringBuilder[0];
                        break;
                    }
                default:
                    {
                        ch = stringBuilder[0];
                        break;
                    }
            }
            return ch;
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            Instance = this;

            // Set up our event handlers
            AddHandler(KeyDownEvent, KeyDownHandler, true);
            AddHandler(KeyUpEvent, KeyUpHandler, true);
            Closing += FormClosingHandler;

            // Set up timer to automatically save settings on an interval
            Timer timer = new Timer
            {
                Interval = TWO_MINUTES_IN_MILLISECONDS
            };
            timer.Elapsed += (_, __) => this.Invoke(SaveSettings);
            timer.Start();

            RightWindowCommandsOverlayBehavior = WindowCommandsOverlayBehavior.Never;

            LoadSettingsCompat();

            CreateTabContextMenus();

            CloseSnackbarButton.Content = ImageHelper.GetImage(ImageHelper.CloseButtonPath, 11, 11);

            // Subscribe to any mouse down. We want any interaction with the application to close the snackbar
            _globalMouseEvents = Hook.AppEvents();
            _globalMouseEvents.MouseDown += Global_MouseDown;
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Loads settings with compatibility for legacy config files
        /// </summary>
        private void LoadSettingsCompat()
        {
            // For backwards compatibility, see if the legacy config file exists.
            if (File.Exists(LegacyConfigFilePath))
            {
                // Load the settings with the legacy path
                LoadSettings(LegacyConfigFilePath);

                // Save the settings to the new path
                SaveSettings(ConfigFilePath);

                // Save the legacy config file in case there is an error
                if (File.Exists(TempConfigFilePath)) File.Delete(TempConfigFilePath);
                File.Move(LegacyConfigFilePath, TempConfigFilePath);
            }
            else
            {
                // The legacy file doesn't exist, so go ahead and load the new one
                LoadSettings(ConfigFilePath);
            }
        }

        private void LoadSettings()
        {
            LoadSettings(ConfigFilePath);
        }

        /// <summary>
        /// Load settings from the config file and populate the UI.
        /// </summary>
        private void LoadSettings(string configFilePath)
        {
            try
            {
                // try to load settings
                XmlDocument xmlDocument = new XmlDocument();

                xmlDocument.Load(configFilePath);

                XmlElement xelRoot = xmlDocument.DocumentElement;
                if (xelRoot != null)
                {
                    XmlNodeList tabNodes = xelRoot.SelectNodes("/tabs/tab");

                    // Remove default tabs
                    Tabs.Items.Clear();

                    if (tabNodes != null)
                    {
                        TabItem selectedTab = null;

                        foreach (XmlNode node in tabNodes)
                        {
                            string name = node["name"]?.InnerText;

                            MetroTabItem tab = new MyMetroTabItem {Header = name};
                            Tabs.Items.Add(tab);

                            if (node.Attributes?["focused"]?.Value is string focusedString &&
                                bool.TryParse(focusedString, out bool focused) && focused)
                            {
                                selectedTab = tab;
                            }

                            List<SoundButtonUndoState> buttons = new List<SoundButtonUndoState>();

                            // Read the button data
                            for (int i = 0; i < 10; ++i)
                            {
                                SoundButtonUndoState soundButtonUndoState = new SoundButtonUndoState
                                {
                                    SoundName = node["button" + i]?.Attributes["name"].Value,
                                    SoundPath = node["button" + i]?.Attributes["path"].Value,
                                };

                                if (node["button" + i]?.Attributes["color"]?.Value is string colorString && string.IsNullOrEmpty(colorString) == false)
                                {
                                    var drawingColor = ColorTranslator.FromHtml(colorString);
                                    soundButtonUndoState.Color = Color.FromArgb(drawingColor.A, drawingColor.R, drawingColor.G, drawingColor.B);
                                }

                                if (node["button" + i]?.Attributes["volumeOffset"]?.Value is string volumeOffsetString &&
                                    string.IsNullOrEmpty(volumeOffsetString) == false && int.TryParse(volumeOffsetString, out int volumeOffset))
                                {
                                    soundButtonUndoState.VolumeOffset = volumeOffset;
                                }

                                if (node["button" + i]?.Attributes["loop"]?.Value is string loopString &&
                                    string.IsNullOrEmpty(loopString) == false && bool.TryParse(loopString, out bool loop))
                                {
                                    soundButtonUndoState.Loop = loop;
                                }

                                buttons.Add(soundButtonUndoState);
                            }

                            CreatePageContent(tab, buttons);
                        }

                        if (selectedTab is null == false)
                        {
                            Tabs.SelectedItem = selectedTab;
                        }

                        CreateTabContextMenus();
                    }
                }
            }
            // If anything failed, add the startup page
            catch
            {
                // Populate content for "welcome"
                CreateHelpContent((MetroTabItem)Tabs.Items[0]);
            }
        }

        private void CreateTabContextMenus()
        {
            // Add context menu to each tab
            foreach (MetroTabItem tab in Tabs.Items)
            {
                if (_tabContextMenus.ContainsKey(tab)) continue;

                ContextMenu contextMenu = new ContextMenu();

                if (tab.Tag?.ToString() != WELCOME_PAGE_TAG)
                {
                    MenuItem renameMenuItem = new MenuItem {Header = Properties.Resources.Rename};
                    renameMenuItem.Click += RenameMenuItem_Click;
                    contextMenu.Items.Add(renameMenuItem);
                }

                MenuItem removeMenuItem = new MenuItem {Header = Properties.Resources.Remove};
                removeMenuItem.Click += RemoveMenuItem_Click;
                contextMenu.Items.Add(removeMenuItem);

                if (tab.Tag?.ToString() != WELCOME_PAGE_TAG)
                {
                    MenuItem clearAllSoundsMenuItem = new MenuItem { Header = Properties.Resources.ClearAllSounds };
                    clearAllSoundsMenuItem.Click += ClearAllSoundsMenuItem_Click;
                    contextMenu.Items.Add(clearAllSoundsMenuItem);
                }

                // Handle showing the context menu manually (instead of assigning it to the tab's ContextMenu property)
                //  so that we can filter out bubbled events from child controls and only show it when the tab itself is clicked.
                // args.Source shows the real object from which the event originated.
                tab.MouseRightButtonUp += (_, args) =>
                {
                    if (args.Source is MetroTabItem metroTabItem)
                    {
                        metroTabItem.Focus();
                        contextMenu.IsOpen = true;
                    }
                };

                // Because we're managing the tab context menus manually (instead of assigning to the tab's ContextMenu property)
                // we also have to keep track of whether we've created a context menu for this tab yet.
                _tabContextMenus[tab] = contextMenu;
            }
        }

        private void CreatePageContent(MetroTabItem tab, List<SoundButtonUndoState> buttons = null)
        {
            Grid parentGrid = new Grid();

            ColumnDefinition col1 = new ColumnDefinition {Width = new GridLength(1, GridUnitType.Star)};
            parentGrid.ColumnDefinitions.Add(col1);

            ColumnDefinition col2 = new ColumnDefinition {Width = new GridLength(1, GridUnitType.Star)};
            parentGrid.ColumnDefinitions.Add(col2);

            for (int i = 0; i < 5; ++i)
            {
                RowDefinition row = new RowDefinition {Height = new GridLength(1, GridUnitType.Star)};
                parentGrid.RowDefinitions.Add(row);

                // Sound button
                SoundButton soundButton = new SoundButton(parentTab: tab);

                Grid.SetColumn(soundButton, 0);
                Grid.SetRow(soundButton, i);
                parentGrid.Children.Add(soundButton);

                if (buttons is null == false)
                {
                    soundButton.LoadState(buttons[i]);
                }

                // Menu button
                MenuButton menuButton = new MenuButton(soundButton);

                Grid.SetColumn(menuButton, 0);
                Grid.SetRow(menuButton, i);
                parentGrid.Children.Add(menuButton);
                soundButton.ChildButtons.Add(menuButton);

                // Play/pause button
                PlayPauseButton playPauseButton = new PlayPauseButton(soundButton);

                Grid.SetColumn(playPauseButton, 0);
                Grid.SetRow(playPauseButton, i);
                parentGrid.Children.Add(playPauseButton);
                soundButton.ChildButtons.Add(playPauseButton);

                // Stop button
                StopButton stopButton = new StopButton(soundButton);

                Grid.SetColumn(stopButton, 0);
                Grid.SetRow(stopButton, i);
                parentGrid.Children.Add(stopButton);
                soundButton.ChildButtons.Add(stopButton);

                // Loop icon
                LoopIconButton loopIconButton = new LoopIconButton(soundButton);

                Grid.SetColumn(loopIconButton, 0);
                Grid.SetRow(loopIconButton, i);
                parentGrid.Children.Add(loopIconButton);
                soundButton.ChildButtons.Add(loopIconButton);

                // Volume offset icon
                VolumeOffsetIconButton volumeOffsetIconButton = new VolumeOffsetIconButton(soundButton);

                Grid.SetColumn(volumeOffsetIconButton, 0);
                Grid.SetRow(volumeOffsetIconButton, i);
                parentGrid.Children.Add(volumeOffsetIconButton);
                soundButton.ChildButtons.Add(volumeOffsetIconButton);

                // Progress bar
                SoundProgressBar progressBar = new SoundProgressBar();

                Grid.SetColumn(progressBar, 0);
                Grid.SetRow(progressBar, i);
                parentGrid.Children.Add(progressBar);

                soundButton.SoundProgressBar = progressBar;
            }

            for (int i = 0; i < 5; ++i)
            {
                // Only have to add the rows once (above)

                // Sound button
                SoundButton soundButton = new SoundButton(parentTab: tab);

                Grid.SetColumn(soundButton, 1);
                Grid.SetRow(soundButton, i);
                parentGrid.Children.Add(soundButton);

                if (buttons is null == false)
                {
                    soundButton.LoadState(buttons[i + 5]);
                }

                // Menu button
                MenuButton menuButton = new MenuButton(soundButton);

                Grid.SetColumn(menuButton, 1);
                Grid.SetRow(menuButton, i);
                parentGrid.Children.Add(menuButton);
                soundButton.ChildButtons.Add(menuButton);

                // Play/pause button
                PlayPauseButton playPauseButton = new PlayPauseButton(soundButton);

                Grid.SetColumn(playPauseButton, 1);
                Grid.SetRow(playPauseButton, i);
                parentGrid.Children.Add(playPauseButton);
                soundButton.ChildButtons.Add(playPauseButton);

                // Stop button
                StopButton stopButton = new StopButton(soundButton);

                Grid.SetColumn(stopButton, 1);
                Grid.SetRow(stopButton, i);
                parentGrid.Children.Add(stopButton);
                soundButton.ChildButtons.Add(stopButton);

                // Loop icon
                LoopIconButton loopIconButton = new LoopIconButton(soundButton);

                Grid.SetColumn(loopIconButton, 1);
                Grid.SetRow(loopIconButton, i);
                parentGrid.Children.Add(loopIconButton);
                soundButton.ChildButtons.Add(loopIconButton);

                // Volume offset icon
                VolumeOffsetIconButton volumeOffsetIconButton = new VolumeOffsetIconButton(soundButton);

                Grid.SetColumn(volumeOffsetIconButton, 1);
                Grid.SetRow(volumeOffsetIconButton, i);
                parentGrid.Children.Add(volumeOffsetIconButton);
                soundButton.ChildButtons.Add(volumeOffsetIconButton);

                // Progress bar
                SoundProgressBar progressBar = new SoundProgressBar();

                Grid.SetColumn(progressBar, 1);
                Grid.SetRow(progressBar, i);
                parentGrid.Children.Add(progressBar);

                soundButton.SoundProgressBar = progressBar;
            }

            tab.Content = parentGrid;
        }

        private void CreateHelpContent(MetroTabItem tab)
        {
            tab.Tag = WELCOME_PAGE_TAG;

            StackPanel stackPanel = new StackPanel();

            TextBlock text = new TextBlock
            {
                Text = Properties.Resources.WelcomeToSoundBoard.ToUpper(),
                Padding = new Thickness(5),
                FontSize = 25,
                TextWrapping = TextWrapping.Wrap
            };
            stackPanel.Children.Add(text);

            text = new TextBlock
            {
                Text = Properties.Resources.SoundBoardDescription,
                Padding = new Thickness(5),
                FontSize = 15,
                TextWrapping = TextWrapping.Wrap
            };
            stackPanel.Children.Add(text);

            text = new TextBlock
            {
                Text = Properties.Resources.HowDoesItWork.ToUpper(),
                Padding = new Thickness(5),
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                TextWrapping = TextWrapping.Wrap
            };
            stackPanel.Children.Add(text);

            text = new TextBlock
            {
                Text = Properties.Resources.SoundBoardExplanation1,
                Padding = new Thickness(5),
                FontSize = 15,
                TextWrapping = TextWrapping.Wrap
            };
            stackPanel.Children.Add(text);

            text = new TextBlock
            {
                Text = Properties.Resources.SoundBoardExplanation2,
                Padding = new Thickness(5),
                FontSize = 15,
                TextWrapping = TextWrapping.Wrap
            };
            stackPanel.Children.Add(text);

            text = new TextBlock
            {
                Text = Properties.Resources.SoundBoardExplanation3,
                Padding = new Thickness(5),
                FontSize = 15,
                TextWrapping = TextWrapping.Wrap
            };
            stackPanel.Children.Add(text);

            text = new TextBlock
            {
                Text = Properties.Resources.SoundBoardExplanation4,
                Padding = new Thickness(5),
                FontSize = 15,
                TextWrapping = TextWrapping.Wrap
            };
            stackPanel.Children.Add(text);

            text = new TextBlock
            {
                Text = Properties.Resources.SoundBoardExplanation5,
                Padding = new Thickness(5),
                FontSize = 15,
                TextWrapping = TextWrapping.Wrap
            };
            stackPanel.Children.Add(text);

            tab.Content = stackPanel;
        }

        private void SaveSettings()
        {
            SaveSettings(ConfigFilePath);
        }

        private void SaveSettings(string configFilePath)
        {
            // Ensure that the directory for the given config file exists
            try
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                Directory.CreateDirectory(Path.GetDirectoryName(configFilePath));
            }
            catch { /* Ignored */ }

            using (FileStream fileStream = new FileStream(configFilePath, FileMode.Create))
            using (StreamWriter streamWriter = new StreamWriter(fileStream))
            using (XmlTextWriter textWriter = new XmlTextWriter(streamWriter))
            {
                textWriter.Formatting = Formatting.Indented;
                textWriter.Indentation = 4;

                textWriter.WriteStartDocument();
                textWriter.WriteStartElement("tabs");

                foreach (MetroTabItem tab in Tabs.Items.OfType<MetroTabItem>())
                {
                    if (tab.Content is Grid grid)
                    {
                        string name = tab.Header.ToString();
                        textWriter.WriteStartElement("tab");
                        textWriter.WriteAttributeString("focused", tab.IsSelectedItem().ToString());

                        textWriter.WriteElementString("name", name);

                        int j = 0;
                        foreach (var child in grid.Children)
                        {
                            if (child is SoundButton button)
                            {
                                textWriter.WriteStartElement("button" + j++);
                                textWriter.WriteAttributeString("name", button.SoundName);
                                textWriter.WriteAttributeString("path", button.SoundPath);
                                textWriter.WriteAttributeString("color", button.Color.ToString());
                                textWriter.WriteAttributeString("volumeOffset", button.VolumeOffset.ToString());
                                textWriter.WriteAttributeString("loop", button.Loop.ToString());
                                textWriter.WriteEndElement();
                            }
                        }

                        textWriter.WriteEndElement();
                    }
                    else // It doesn't have a grid, so it's not a sound page
                    {
                        continue;
                    }
                }

                textWriter.WriteEndElement();
                textWriter.WriteEndDocument();
            }
        }

        /// <summary>
        /// Returns all <see cref="SoundButton"/>s in the given <see cref="MainWindow"/>.
        /// If parameter <paramref name="metroTabItem"/> is passed, only <see cref="SoundButton"/>s which appear on the given <paramref name="metroTabItem"/> are returned.
        /// </summary>
        private IEnumerable<SoundButton> GetSoundButtons(MetroTabItem metroTabItem = null)
        {
            foreach (MetroTabItem tab in Tabs.Items.OfType<MetroTabItem>())
            {
                if (metroTabItem is null || tab == metroTabItem)
                {
                    if (tab.Content is Grid grid)
                    {
                        foreach (var child in grid.Children)
                        {
                            if (child is SoundButton button)
                            {
                                yield return button;
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region Event handlers

        private void RenameMenuItem_Click(object sender, EventArgs e)
        {
            // Tab will be focused (because of right-click handler), so just invoke "rename" button
            ButtonAutomationPeer peer = new ButtonAutomationPeer(Rename);
            IInvokeProvider invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
            invokeProv?.Invoke();
        }

        private void RemoveMenuItem_Click(object sender, EventArgs e)
        {
            // Tab will be focused (because of right-click handler), so just invoke "remove" button
            ButtonAutomationPeer peer = new ButtonAutomationPeer(Remove);
            IInvokeProvider invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
            invokeProv?.Invoke();
        }

        private void ClearAllSoundsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (Tabs.SelectedItem is MetroTabItem metroTabItem)
            {
                TabPageSoundsUndoState tabPageSoundsUndoState = (this as IUndoable<TabPageSoundsUndoState>).SaveState();

                // Set up our UndoAction
                SetUndoAction(() => { LoadState(tabPageSoundsUndoState); });

                // Create and show a snackbar
                string message = Properties.Resources.AllSoundsClearedFromTab;
                string truncatedTabName = Utilities.Truncate(metroTabItem.Header.ToString(), SnackbarMessageFont, (int)Width - 50, message);
                ShowUndoSnackbar(string.Format(message, truncatedTabName));

                foreach (SoundButton soundButton in GetSoundButtons(metroTabItem))
                {
                    soundButton.ClearButton();
                }
            }
        }

        private void RoutedKeyDownHandler(object sender, RoutedEventArgs args)
        {
            if (args is KeyEventArgs e)
            {
                Mouse.Capture(null);

                char c = GetCharFromKey(e.Key);
                if (char.IsLetter(c) || char.IsPunctuation(c) || char.IsNumber(c))
                {
                    _searchString += c;
                }
                else if (e.Key == Key.Space)
                {
                    _searchString += ' ';
                }
                else if (e.Key == Key.Back && _searchString.Length > 0)
                {
                    _searchString = _searchString.Substring(0, _searchString.Length - 1);
                }
                else if (e.Key == Key.Escape)
                {
                    // If the search bar is open, close it
                    if (Search.IsOpen)
                    {
                        CloseSearch();
                    }
                    // Otherwise, stop any playing sounds
                    else
                    {
                        ButtonAutomationPeer peer = new ButtonAutomationPeer(Silence);
                        IInvokeProvider invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                        invokeProv?.Invoke();
                    }
                    return;
                }
                else if (e.Key == Key.Down)
                {
                    bool foundFocused = false;
                    
                    // Loop through the buttons and focus the next one
                    foreach (var child in ResultsPanel.Children)
                    {
                        if (child is SoundButton soundButton)
                        {
                            if (foundFocused)
                            {
                                // We found the last focused button, so focus this one
                                soundButton.Focus();
                                _focusedButton = soundButton;
                                break;
                            }
                            if (soundButton.IsFocused)
                            {
                                // We found the focused button! focus the next one
                                foundFocused = true;
                            }
                        }
                    }
                    return;
                }
                else if (e.Key == Key.Up)
                {
                    SoundButton previousButton = null;
                    
                    // Loop through the buttons and focus the previous one
                    foreach (var child in ResultsPanel.Children)
                    {
                        if (child is SoundButton soundButton)
                        {
                            if (soundButton.IsFocused)
                            {
                                // Focus the previous one!
                                if (previousButton != null)
                                {
                                    previousButton.Focus();
                                    _focusedButton = previousButton;
                                    break;
                                }
                            }
                            previousButton = soundButton;
                        }
                    }
                    return;
                }
                else if (e.Key == Key.Enter)
                {
                    // Play the sound!
                    foreach (var child in ResultsPanel.Children)
                    {
                        if (child is SoundButton soundButton && soundButton.IsFocused)
                        {
                            ButtonAutomationPeer peer = new ButtonAutomationPeer(soundButton);
                            IInvokeProvider invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                            invokeProv?.Invoke();
                        }
                    }
                    return;
                }
                else
                {
                    return;
                }

                Query.Text = _searchString;
                Query.CaretIndex = Query.Text.Length;

                Search.IsOpen = true;

                // Get rid of any previous buttons (do reverse iteration to prevent collection modified errors)
                for (int i = ResultsPanel.Children.Count - 1; i >= 0; --i)
                {
                    if (ResultsPanel.Children[i] is SoundButton soundButton)
                    {
                        ResultsPanel.Children.Remove(soundButton);
                    }
                }

                // Perform search
                if (string.IsNullOrEmpty(_searchString) == false)
                {
                    foreach (SoundButton soundButton in GetSoundButtons())
                    {
                        if (soundButton.SoundName.ToLower().Contains(_searchString.ToLower()))
                        {
                            SoundButton button = new SoundButton(SoundButtonMode.Search, sourceTabAndButton: (soundButton.ParentTab, soundButton));
                            button.SetFile(soundButton.SoundPath, soundButton.SoundName);

                            ResultsPanel.Children.Add(button);
                        }
                    }

                    // If we've added at least one button, focus the first one
                    if (ResultsPanel.Children.Count > 0)
                    {
                        (ResultsPanel.Children[0] as SoundButton)?.Focus();
                        _focusedButton = ResultsPanel.Children[0] as SoundButton;
                    }
                }
            }
        }

        private void RoutedKeyUpHandler(object sender, RoutedEventArgs args)
        {
            // If a focus gets messed up, re-focus it here
            _focusedButton?.Focus();
        }

        private void FlyoutCloseHandler(object sender, RoutedEventArgs e)
        {
            _searchString = string.Empty;
        }

        private void silence_Click(object sender, EventArgs e)
        {
            foreach (IWavePlayer player in SoundPlayers)
            {
                player.Stop();
            }
        }

        private void help_Click(object sender, RoutedEventArgs e)
        {
            MetroTabItem tab = new MyMetroTabItem {Header = Properties.Resources.Welcome.ToLower()};
            CreateHelpContent(tab);
            Tabs.Items.Add(tab);
            tab.Focus();

            // Make sure the new tab has a context menu
            CreateTabContextMenus();
        }

        private async void about_Click(object sender, RoutedEventArgs e)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string version = AssemblyName.GetAssemblyName(assembly.Location).Version.ToString();

            await this.ShowMessageAsync(Properties.Resources.AboutSoundBoard,
                Properties.Resources.CreatedByMicahMorrison + Environment.NewLine +
                string.Format(Properties.Resources.VersionNumber, version));
        }

        private void overflow_Click(object sender, RoutedEventArgs e)
        {
            ContextMenu overflowMenu = Overflow.ContextMenu;

            if (overflowMenu is null)
            {
                overflowMenu = new ContextMenu();

                MenuItem importConfig = new MenuItem {Header = Properties.Resources.ImportConfiguration};
                importConfig.Click += ImportConfig_Click;

                MenuItem exportConfig = new MenuItem {Header = Properties.Resources.ExportConfiguration};
                exportConfig.Click += ExportConfig_Click;

                MenuItem clearConfig = new MenuItem {Header = Properties.Resources.ClearConfiguration};
                clearConfig.Click += ClearConfig_Click;

                overflowMenu.Items.Add(importConfig);
                overflowMenu.Items.Add(exportConfig);
                overflowMenu.Items.Add(clearConfig);

                Overflow.ContextMenu = overflowMenu;
            }

            overflowMenu.IsOpen = true;
        }

        private void addPage_Click(object sender, RoutedEventArgs e)
        {
            MetroTabItem tab = new MyMetroTabItem {Header = Properties.Resources.NewPage.ToLower()};
            CreatePageContent(tab);
            Tabs.Items.Add(tab);
            tab.Focus();

            // Make sure the new tab has a context menu
            CreateTabContextMenus();
        }

        private async void renamePage_Click(object sender, RoutedEventArgs e)
        {
            RemoveHandler(KeyDownEvent, KeyDownHandler);

            if (Tabs.SelectedItem is MetroTabItem tab)
            {
                string result = await this.ShowInputAsync(Properties.Resources.Rename, Properties.Resources.WhatDoYouWantToCallIt,
                    new MetroDialogSettings {DefaultText = tab.Header.ToString()});

                if (string.IsNullOrEmpty(result) == false)
                {
                    tab.Header = result;
                }
            }

            AddHandler(KeyDownEvent, KeyDownHandler, true);
        }

        private void removePage_Click(object sender, RoutedEventArgs e)
        {
            if (Tabs.SelectedItem is MetroTabItem metroTabItem)
            {
                TabPageUndoState tabPageUndoState = SaveState();

                // Set up our UndoAction
                SetUndoAction(() => { LoadState(tabPageUndoState); });

                // Create and show a snackbar
                string message = Properties.Resources.TabWasRemoved;
                string truncatedTabName = Utilities.Truncate(metroTabItem.Header.ToString(), SnackbarMessageFont, (int)Width - 50, message);
                ShowUndoSnackbar(string.Format(message, truncatedTabName));

                // Stop all sounds on this page
                foreach (SoundButton soundButton in GetSoundButtons(metroTabItem))
                {
                    soundButton.Stop();
                }

                // Remove the page
                Tabs.Items.Remove(Tabs.SelectedItem);
            }
        }

        private void FormClosingHandler(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private async void ExportConfig_Click(object sender, RoutedEventArgs e)
        {
            // First, make sure our current settings are saved
            SaveSettings();

            // Prompt the user to browse for where the file should be saved.
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                FileName = $@"SoundBoardConfiguration-{DateTime.Now.ToString(@"s").Replace(@":", @".")}",
                Filter = Properties.Resources.ConfigurationFiles + @" (*.config)|*.config"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    File.Copy(ConfigFilePath, saveFileDialog.FileName, true);
                }
                catch (Exception ex)
                {
                    await this.ShowMessageAsync(Properties.Resources.Oops,
                        Properties.Resources.ThereWasAProblem + Environment.NewLine + Environment.NewLine + ex.Message);
                }
            }
        }

        private async void ImportConfig_Click(object sender, RoutedEventArgs e)
        {
            // Prompt the user to browse for a config file
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = Properties.Resources.ConfigurationFiles + @" (*.config)|*.config"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Stop all sounds
                    foreach (SoundButton soundButton in GetSoundButtons())
                    {
                        soundButton.Stop();
                    }

                    ConfigUndoState configUndoState = (this as IUndoable<ConfigUndoState>).SaveState();

                    // Set up our UndoAction
                    SetUndoAction(() => { LoadState(configUndoState); });

                    // Create and show a snackbar
                    string message = Properties.Resources.ConfigurationWasImported;
                    string truncatedMessage = Utilities.Truncate(message, SnackbarMessageFont, (int) Width - 50);
                    ShowUndoSnackbar(truncatedMessage);

                    // Load settings with the given file
                    LoadSettings(openFileDialog.FileName);

                    // Immediately save the settings to overwrite our file
                    SaveSettings();

                    // Select the first tab, if any
                    if (Tabs.Items.Count >= 1) (Tabs.Items[0] as MetroTabItem)?.Focus();
                }
                catch (Exception ex)
                {
                    await this.ShowMessageAsync(Properties.Resources.Oops,
                        Properties.Resources.ThereWasAProblem + Environment.NewLine + Environment.NewLine + ex.Message);
                }
            }
        }

        private async void ClearConfig_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Stop all sounds
                foreach (SoundButton soundButton in GetSoundButtons())
                {
                    soundButton.Stop();
                }

                ConfigUndoState configUndoState = (this as IUndoable<ConfigUndoState>).SaveState();

                // Set up our UndoAction
                SetUndoAction(() => { LoadState(configUndoState); });

                // Create and show a snackbar
                string message = Properties.Resources.ConfigurationWasCleared;
                string truncatedMessage = Utilities.Truncate(message, SnackbarMessageFont, (int)Width - 50);
                ShowUndoSnackbar(truncatedMessage);

                // Clear the config from the UI by removing all the tabs
                Tabs.Items.Clear();
            }
            catch (Exception ex)
            {
                await this.ShowMessageAsync(Properties.Resources.Oops,
                    Properties.Resources.ThereWasAProblem + Environment.NewLine + Environment.NewLine + ex.Message);
            }
        }

        private void CloseSnackbarButton_Click(object sender, RoutedEventArgs e)
        {
            Snackbar.IsOpen = false;
        }

        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            // Invoke the assigned undo action
            _undoAction?.Invoke();

            // Always close the snackbar when undoing
            Snackbar.IsOpen = false;
        }

        private void Global_MouseDown(object sender, EventArgs e)
        {
            // Always close the snackbar on any user interaction (unless they're interacting with the snackbar itself)
            if (Snackbar.IsMouseOver == false)
            {
                Snackbar.IsOpen = false;
            }
        }

        #endregion

        #region Overrides

        /// <inheritdoc />
        protected override void OnClosed(EventArgs e)
        {
            // Do cleanup
            _globalMouseEvents.MouseDown -= Global_MouseDown;
            _globalMouseEvents.Dispose();

            base.OnClosed(e);
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Returns the MainWindow instance
        /// </summary>
        public static MainWindow Instance { get; private set; }

        /// <summary>
        /// Event handler for KeyDown event
        /// </summary>
        public RoutedEventHandler KeyDownHandler => RoutedKeyDownHandler;

        /// <summary>
        /// Event handler for KeyUp event
        /// </summary>
        public RoutedEventHandler KeyUpHandler => RoutedKeyUpHandler;

        /// <summary>
        /// Contains the list of <see cref="IWavePlayer"/> objects contained in this instance of the MainWindow
        /// </summary>
        public List<IWavePlayer> SoundPlayers { get; } = new List<IWavePlayer>();

        /// <summary>
        /// Returns the <see cref="Font"/> of the <see cref="SnackbarMessage"/>.
        /// </summary>
        public Font SnackbarMessageFont => new Font(SnackbarMessage.FontFamily.ToString(), (float) SnackbarMessage.FontSize);

        #endregion

        #region Public methods

        /// <summary>
        /// Closes the search pane and clears any query
        /// </summary>
        public void CloseSearch()
        {
            _searchString = string.Empty; // Don't wait for it to close to clear the query
            Search.IsOpen = false;
        }

        /// <summary>
        /// Defines an <see cref="Action"/> to call if <see cref="ShowUndoSnackbar(string, int)"/> is called
        /// and the user chooses to perform the undo.
        /// </summary>
        /// <param name="action"></param>
        public void SetUndoAction(Action action)
        {
            _undoAction = action;
        }

        /// <summary>
        /// Shows a snackbar that allows the user to undo an action.
        /// </summary>
        /// <param name="message">Message to show the user on the snackbar</param>
        /// <param name="timeout">Time in ms until the snackbar is closed automatically. Defaults to 5 seconds.</param>
        public void ShowUndoSnackbar(string message = "", int timeout = 5000)
        {
            SnackbarMessage.Text = message;
            Snackbar.AutoCloseInterval = timeout;
            Snackbar.IsOpen = true;
        }

        #endregion

        #region Private fields

        private readonly IKeyboardMouseEvents _globalMouseEvents;
        private string _searchString = string.Empty;
        private SoundButton _focusedButton;
        private Action _undoAction;
        private readonly Dictionary<MetroTabItem, ContextMenu> _tabContextMenus = new Dictionary<MetroTabItem, ContextMenu>();

        #endregion

        #region Private properties

        private string ConfigFilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ApplicationName, @"soundboard.config");

        private string TempConfigFilePath => ConfigFilePath + @".temp";

        private string LegacyConfigFilePath => @"soundboard.config";

        private string ApplicationName => @"SoundBoard";

        #endregion

        #region Consts

        private const int TWO_MINUTES_IN_MILLISECONDS = 120000;

        private const string WELCOME_PAGE_TAG = nameof(WELCOME_PAGE_TAG);

        #endregion

        #region IUndoable members

        /// <inheritdoc />
        public TabPageUndoState SaveState()
        {
            return new TabPageUndoState {MetroTabItem = Tabs.SelectedItem as MetroTabItem, Index = Tabs.SelectedIndex};
        }

        /// <inheritdoc />
        public void LoadState(TabPageUndoState undoState)
        {
            Tabs.Items.Insert(undoState.Index, undoState.MetroTabItem);
            Tabs.SelectedIndex = undoState.Index;
        }


        /// <inheritdoc />
        ConfigUndoState IUndoable<ConfigUndoState>.SaveState()
        {
            if (File.Exists(TempConfigFilePath)) File.Delete(TempConfigFilePath);
            SaveSettings();
            File.Move(ConfigFilePath, TempConfigFilePath);
            return new ConfigUndoState {SavedConfigStatePath = TempConfigFilePath};
        }

        /// <inheritdoc />
        public void LoadState(ConfigUndoState undoState)
        {
            foreach (SoundButton soundButton in GetSoundButtons())
            {
                soundButton.Stop();
            }

            LoadSettings(undoState.SavedConfigStatePath);
            SaveSettings();
            if (Tabs.Items.Count >= 1) (Tabs.Items[0] as MetroTabItem)?.Focus();
        }

        /// <inheritdoc />
        TabPageSoundsUndoState IUndoable<TabPageSoundsUndoState>.SaveState()
        {
            var soundButtonUndoStates = new List<(SoundButtonUndoState, int)>();

            int index = 0;
            foreach (SoundButton soundButton in GetSoundButtons(Tabs.SelectedItem as MetroTabItem))
            {
                soundButtonUndoStates.Add((soundButton.SaveState(), index));
                ++index;
            }

            return new TabPageSoundsUndoState {SoundButtonUndoStates = soundButtonUndoStates};
        }

        /// <inheritdoc />
        public void LoadState(TabPageSoundsUndoState undoState)
        {
            IList<SoundButton> soundButtons = GetSoundButtons(Tabs.SelectedItem as MetroTabItem).ToList();

            foreach (var soundButtonUndoState in undoState.SoundButtonUndoStates)
            {
                soundButtons[soundButtonUndoState.ButtonIndex].LoadState(soundButtonUndoState.SoundButtonUndoState);
            }
        }

        #endregion
    }
}
