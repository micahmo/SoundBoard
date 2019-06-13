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
using System.Linq;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System.Runtime.InteropServices;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using Timer = System.Timers.Timer;

#endregion

namespace SoundBoard
{ 
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public sealed partial class MainWindow
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

            LoadSettings();

            CreateTabContextMenus();
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Load settings from the config file and populate the UI
        /// </summary>
        private void LoadSettings()
        {
            try
            {
                // try to load settings
                XmlDocument xmlDocument = new XmlDocument();

                xmlDocument.Load("soundboard.config");

                XmlElement xelRoot = xmlDocument.DocumentElement;
                if (xelRoot != null)
                {
                    XmlNodeList tabNodes = xelRoot.SelectNodes("/tabs/tab");

                    // Remove default tabs
                    Tabs.Items.Clear();

                    if (tabNodes != null)
                    {
                        foreach (XmlNode node in tabNodes)
                        {
                            string name = node["name"]?.InnerText;

                            MetroTabItem tab = new MetroTabItem {Header = name};
                            Tabs.Items.Add(tab);

                            List<Tuple<string, string>> buttons = new List<Tuple<string, string>>();

                            // Read the button data
                            for (int i = 0; i < 10; ++i)
                            {
                                buttons.Add(new Tuple<string, string>(node["button" + i]?.Attributes["name"].Value, node["button" + i]?.Attributes["path"].Value));
                            }

                            CreatePageContent(tab, buttons);
                        }
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
                ContextMenu contextMenu = new ContextMenu();

                if (tab.Tag?.ToString() != WELCOME_PAGE_TAG)
                {
                    MenuItem renameMenuItem = new MenuItem {Header = "Rename"};
                    renameMenuItem.Click += RenameMenuItem_Click;
                    contextMenu.Items.Add(renameMenuItem);
                }

                MenuItem removeMenuItem = new MenuItem {Header = "Remove"};
                removeMenuItem.Click += RemoveMenuItem_Click;
                contextMenu.Items.Add(removeMenuItem);

                tab.MouseRightButtonUp += MetroTabItem_RightClick;

                tab.ContextMenu = contextMenu;
            }
        }

        private void CreatePageContent(MetroTabItem tab, List<Tuple<string, string>> buttons = null)
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
                SoundButton soundButton = new SoundButton();

                Grid.SetColumn(soundButton, 0);
                Grid.SetRow(soundButton, i);
                parentGrid.Children.Add(soundButton);

                if (buttons is null == false)
                {
                    soundButton.SetFile(buttons[i].Item2, buttons[i].Item1);
                }

                // Menu button
                MenuButton menuButton = new MenuButton(soundButton);

                Grid.SetColumn(menuButton, 0);
                Grid.SetRow(menuButton, i);
                parentGrid.Children.Add(menuButton);

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
                SoundButton soundButton = new SoundButton();

                Grid.SetColumn(soundButton, 1);
                Grid.SetRow(soundButton, i);
                parentGrid.Children.Add(soundButton);

                if (buttons is null == false)
                {
                    soundButton.SetFile(buttons[i + 5].Item2, buttons[i + 5].Item1);
                }

                // Menu button
                MenuButton menuButton = new MenuButton(soundButton);

                Grid.SetColumn(menuButton, 1);
                Grid.SetRow(menuButton, i);
                parentGrid.Children.Add(menuButton);

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
                Text = "WELCOME TO SOUND BOARD!",
                Padding = new Thickness(5),
                FontSize = 25,
                TextWrapping = TextWrapping.Wrap
            };
            stackPanel.Children.Add(text);

            text = new TextBlock
            {
                Text = "...an app where you can create and run your own custom sound board full of your favorite bytes, effects, and clips.",
                Padding = new Thickness(5),
                FontSize = 15,
                TextWrapping = TextWrapping.Wrap
            };
            stackPanel.Children.Add(text);

            text = new TextBlock
            {
                Text = "HOW DOES IT WORK?",
                Padding = new Thickness(5),
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                TextWrapping = TextWrapping.Wrap
            };
            stackPanel.Children.Add(text);

            text = new TextBlock
            {
                Text = "Sound Board is split into pages, so you can group your favorite sounds together. To get started, add your first sound page by clicking \"add page\" above!",
                Padding = new Thickness(5),
                FontSize = 15,
                TextWrapping = TextWrapping.Wrap
            };
            stackPanel.Children.Add(text);

            text = new TextBlock
            {
                Text = "To load a sound, just drag an audio file onto a button or click the three little dots to browse. Then click the button with the sound name on it to play it!",
                Padding = new Thickness(5),
                FontSize = 15,
                TextWrapping = TextWrapping.Wrap
            };
            stackPanel.Children.Add(text);

            text = new TextBlock
            {
                Text = "To find a sound in a hurry, just start typing its name and an instant search bar will appear!",
                Padding = new Thickness(5),
                FontSize = 15,
                TextWrapping = TextWrapping.Wrap
            };
            stackPanel.Children.Add(text);

            text = new TextBlock
            {
                Text = "Pages and sounds are saved, so when you're done, just close the app, and it'll pick up right where you left off next time!",
                Padding = new Thickness(5),
                FontSize = 15,
                TextWrapping = TextWrapping.Wrap
            };
            stackPanel.Children.Add(text);

            text = new TextBlock
            {
                Text = "Add, remove pages, or rename pages at any time by clicking buttons at the top. Feel free to remove this page when you're ready to go! Bring it back any time by clicking \"help\".",
                Padding = new Thickness(5),
                FontSize = 15,
                TextWrapping = TextWrapping.Wrap
            };
            stackPanel.Children.Add(text);

            tab.Content = stackPanel;
        }

        private void SaveSettings()
        {
            string filename = "soundboard.config";

            using (FileStream fileStream = new FileStream(filename, FileMode.Create))
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

                        textWriter.WriteElementString("name", name);

                        int j = 0;
                        foreach (var child in grid.Children)
                        {
                            if (child is SoundButton button)
                            {
                                textWriter.WriteStartElement("button" + j++);
                                textWriter.WriteAttributeString("name", button.SoundName);
                                textWriter.WriteAttributeString("path", button.SoundPath);
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

        #endregion

        #region Event handlers

        private void MetroTabItem_RightClick(object sender, EventArgs e)
        {
            if (sender is MetroTabItem metroTabItem)
            {
                metroTabItem.Focus();
            }
        }

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
                        _searchString = string.Empty; // Don't wait for it to close to clear the query
                        Search.IsOpen = false;
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
                    foreach (KeyValuePair<string, string> entry in Sounds)
                    {
                        if (entry.Key.ToLower().Contains(_searchString.ToLower()))
                        {
                            SoundButton button = new SoundButton(SoundButtonMode.Search);
                            button.SetFile(entry.Value, entry.Key, false);

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
            MetroTabItem tab = new MetroTabItem {Header = "welcome"};
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

            await this.ShowMessageAsync("About Sound Board", "Created by Micah Morrison\nversion " + version);
        }

        private void addPage_Click(object sender, RoutedEventArgs e)
        {
            MetroTabItem tab = new MetroTabItem {Header = "new page"};
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
                string result = await this.ShowInputAsync("Rename", "What do you want to call it?",
                    new MetroDialogSettings {DefaultText = tab.Header.ToString()});

                if (string.IsNullOrEmpty(result) == false)
                {
                    tab.Header = result;
                }
            }

            AddHandler(KeyDownEvent, KeyDownHandler, true);
        }

        private async void removePage_Click(object sender, RoutedEventArgs e)
        {
            MessageDialogResult result = await this.ShowMessageAsync("Just checking...", "Are you sure you want to delete this page?", MessageDialogStyle.AffirmativeAndNegative);
            if (result == MessageDialogResult.Affirmative)
            {
                Tabs.Items.Remove(Tabs.SelectedItem);
            }

            UpdateSoundList();
        }

        private void FormClosingHandler(object sender, EventArgs e)
        {
            SaveSettings();
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
        /// Contains a map of SoundName to SoundPath
        /// </summary>
        public Dictionary<string, string> Sounds { get; } = new Dictionary<string, string>();

        #endregion

        #region Public methods

        /// <summary>
        /// Ensures that the the local <see cref="Sounds"/> list accurately reflects the UI
        /// </summary>
        public void UpdateSoundList()
        {
            Sounds.Clear();

            foreach (MetroTabItem tab in Tabs.Items)
            {
                if (tab.Content is Grid grid)
                {
                    foreach (var child in grid.Children)
                    {
                        if (child is SoundButton soundButton)
                        {
                            Sounds[soundButton.SoundName] = soundButton.SoundPath;
                        }
                    }
                }
            }
        }

        #endregion

        #region Private fields

        private string _searchString = string.Empty;
        private SoundButton _focusedButton;

        #endregion

        #region Consts

        private const int TWO_MINUTES_IN_MILLISECONDS = 120000;

        private const string WELCOME_PAGE_TAG = nameof(WELCOME_PAGE_TAG);

        #endregion
    }
}
