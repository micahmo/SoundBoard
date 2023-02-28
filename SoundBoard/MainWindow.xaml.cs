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
using System.Threading.Tasks;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Threading;
using Bluegrams.Application;
using Gma.System.MouseKeyHook;
using MahApps.Metro.SimpleChildWindow;
using Microsoft.Win32;
using NAudio.CoreAudioApi;
using Color = System.Windows.Media.Color;
using Timer = System.Timers.Timer;
using ContextMenu = System.Windows.Controls.ContextMenu;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MenuItem = System.Windows.Controls.MenuItem;
using BondTech.HotKeyManagement.WPF._4;

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

            Task.Run(CleanupBackups);

            CreateTabContextMenus();

            CloseSnackbarButton.Content = ImageHelper.GetImage(ImageHelper.CloseButtonPath, 11, 11);

            // Subscribe to any mouse down. We want any interaction with the application to close the snackbar
            _globalMouseEvents = Hook.AppEvents();
            _globalMouseEvents.MouseDown += Global_MouseDown;

            _updateChecker = new MyUpdateChecker("https://raw.githubusercontent.com/micahmo/SoundBoard/master/SoundBoard/VersionInfo.xml")
            {
                Owner = this,
                DownloadIdentifier = "portable"
            };

            HandleInputOutputChange();

            Tabs.SelectionChanged += (_, __) =>
            {
                GetSoundButtons().Where(sb => sb.IsSelected).ToList().ForEach(sb => sb.IsSelected = false);
            };
        }

        #endregion

        #region Overrides

        /// <inheritdoc/>
        protected override async void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            HotKeyManager = new HotKeyManager(this);
            HotKeyManager.GlobalHotKeyPressed += (_, args) =>
            {
                if (!IsHotkeyPickerOpen)
                {
                    GetSoundButtons().FirstOrDefault(sb => Utilities.SanitizeId(sb.Id) == args.HotKey.Name)?.StartSound();
                }
            };
            HotKeyManager.LocalHotKeyPressed += (_, args) =>
            {
                if (!IsHotkeyPickerOpen)
                {
                    GetSoundButtons().FirstOrDefault(sb => Utilities.SanitizeId(sb.Id) == args.HotKey.Name)?.StartSound();
                }
            };

            // Load existing hotkeys
            List<Tuple<string, Hotkey>> badHotkeys = new List<Tuple<string, Hotkey>>();
            GetSoundButtons().ToList().ForEach(sb =>
            {
                if (sb.LocalHotkey != null)
                {
                    try
                    {
                        sb.ReregisterLocalHotkey();
                    }
                    catch
                    {
                        badHotkeys.Add(new Tuple<string, Hotkey>(sb.SoundName, sb.LocalHotkey));
                    }
                }

                if (sb.GlobalHotkey != null)
                {
                    try
                    {
                        sb.ReregisterGlobalHotkey();
                    }
                    catch
                    {
                        badHotkeys.Add(new Tuple<string, Hotkey>(sb.SoundName, sb.GlobalHotkey));
                    }
                }
            });

            if (badHotkeys.Any())
            {
                await this.ShowMessageAsync(Properties.Resources.Error, string.Format(Properties.Resources.HotkeyRegistrationFailedOnLoad, string.Join(Environment.NewLine, badHotkeys.Select(tup => $"{tup.Item2} ({tup.Item1})"))),
                    MessageDialogStyle.Affirmative, new MetroDialogSettings
                    {
                        AffirmativeButtonText = Properties.Resources.OK
                    });
            }
        }

        /// <inheritdoc/>
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                GetSoundButtons().ToList().ForEach(sb => sb.IsSelected = false);
            }
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
            if (!File.Exists(configFilePath) && Tabs.Items.Count == 1)
            {
                // Populate content for "welcome"
                CreateHelpContent((MetroTabItem)Tabs.Items[0]);
                return;
            }

            // If we get here, we can remove the default tab.
            if (Tabs.Items.Count == 1)
            {
                Tabs.Items.RemoveAt(0);
            }

            try
            {
                // try to load settings
                XmlDocument xmlDocument = new XmlDocument();

                xmlDocument.Load(configFilePath);

                XmlElement xelRoot = xmlDocument.DocumentElement;
                if (xelRoot != null)
                {
                    // Get global settings
                    if (xmlDocument.SelectSingleNode($"/tabs/{nameof(GlobalSettings)}") is XmlNode globalSettings)
                    {
                        if (globalSettings.Attributes?[GlobalSettings.OutputDeviceGuidSettingName] is XmlAttribute outputDeviceGuidAttribute)
                        {
                            outputDeviceGuidAttribute.Value.Split(',').ToList().ForEach(guid =>
                            {
                                if (Guid.TryParse(guid, out var outputDeviceGuid))
                                {
                                    GlobalSettings.AddOutputDeviceGuid(outputDeviceGuid);
                                }
                            });
                        }

                        if (globalSettings.Attributes?[GlobalSettings.InputDeviceGuidSettingName] is XmlAttribute inputDeviceGuidAttribute)
                        {
                            inputDeviceGuidAttribute.Value.Split(',').ToList().ForEach(guid =>
                            {
                                if (Guid.TryParse(guid, out var inputDeviceGuid))
                                {
                                    GlobalSettings.AddInputDeviceGuid(inputDeviceGuid);
                                }
                            });
                        }

                        if (globalSettings.Attributes?[nameof(GlobalSettings.AudioPassthroughLatency)] is XmlAttribute audioPassthroughLatencyAttribute
                            && int.TryParse(audioPassthroughLatencyAttribute.Value, out int audioPassthroughLatency))
                        {
                            GlobalSettings.AudioPassthroughLatency = audioPassthroughLatency;
                        }
                    }

                    // Get tabs
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

                            if (node.Attributes?["rows"]?.Value is string rowsString &&
                                int.TryParse(rowsString, out int rows))
                            {
                                tab.SetRows(rows);
                            }

                            if (node.Attributes?["columns"]?.Value is string columnsString &&
                                int.TryParse(columnsString, out int columns))
                            {
                                tab.SetColumns(columns);
                            }

                            TwoDimensionalList<SoundButtonUndoState> buttons = new TwoDimensionalList<SoundButtonUndoState>();

                            // Read the button data
                            int i = 0;
                            for (int rowIndex = 0; rowIndex < tab.GetRows() || node["button" + (i + 1)] is null == false; ++rowIndex)
                            {
                                for (int columnIndex = 0; columnIndex < tab.GetColumns(); ++columnIndex, ++i)
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

                                    if (node["button" + i]?.Attributes["stopAllSounds"]?.Value is string stopAllSoundsString &&
                                        string.IsNullOrEmpty(stopAllSoundsString) == false && bool.TryParse(stopAllSoundsString, out bool stopAllSounds))
                                    {
                                        soundButtonUndoState.StopAllSounds = stopAllSounds;
                                    }

                                    if (node["button" + i]?.Attributes["id"]?.Value is string id &&
                                        string.IsNullOrEmpty(id) == false)
                                    {
                                        soundButtonUndoState.Id = id;
                                    }

                                    if (node["button" + i]?.Attributes["localHotkey"]?.Value is string localHotKeyStr
                                        && !string.IsNullOrEmpty(localHotKeyStr))
                                    {
                                        soundButtonUndoState.LocalHotkey = Hotkey.FromString(localHotKeyStr);
                                    }

                                    if (node["button" + i]?.Attributes["globalHotkey"]?.Value is string globalHotKeyStr
                                        && !string.IsNullOrEmpty(globalHotKeyStr))
                                    {
                                        soundButtonUndoState.GlobalHotkey = Hotkey.FromString(globalHotKeyStr);
                                    }

                                    int buttonRow = rowIndex;
                                    if (node["button" + i]?.Attributes["row"]?.Value is string rowString &&
                                        string.IsNullOrEmpty(rowString) == false && int.TryParse(rowString, out int row))
                                    {
                                        buttonRow = row;
                                    }

                                    int buttonColumn = columnIndex;
                                    if (node["button" + i]?.Attributes["column"]?.Value is string columnString &&
                                        string.IsNullOrEmpty(columnString) == false && int.TryParse(columnString, out int column))
                                    {
                                        buttonColumn = column;
                                    }

                                    buttons.Add(soundButtonUndoState, buttonRow, buttonColumn);
                                }
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
            catch (Exception ex)
            {
                // Immediately back up the config
                File.Copy(ConfigFilePath, TempConfigFilePath, overwrite: true);

                // Do better error handling
                Dispatcher.Invoke(async () =>
                {
                    var res = await this.ShowMessageAsync(Properties.Resources.Error,
                        string.Join(Environment.NewLine, string.Format(Properties.Resources.ConfigLoadError, TempConfigFilePath), string.Empty, ex.Message),
                        MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings
                        {
                            AffirmativeButtonText = Properties.Resources.CopyDetails,
                            NegativeButtonText = Properties.Resources.OK
                        });

                    if (res == MessageDialogResult.Affirmative)
                    {
                        Clipboard.SetText(string.Join(Environment.NewLine, TempConfigFilePath, string.Empty, ex.ToString()));
                    }
                });
            }

            // If there are no tabs after we load, show the help screen
            if (Tabs.Items.Count == 0)
            {
                ButtonAutomationPeer peer = new ButtonAutomationPeer(Help);
                IInvokeProvider invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                invokeProv?.Invoke();
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

                    contextMenu.Items.Add(new Separator());

                    MenuItem changeButtonGrid = new MenuItem {Header = Properties.Resources.ChangeButtonGrid};
                    changeButtonGrid.Click += ChangeButtonGridMenuItem_Click;
                    contextMenu.Items.Add(changeButtonGrid);
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

        private void CreatePageContent(MetroTabItem tab, TwoDimensionalList<SoundButtonUndoState> buttons = null)
        {
            Grid parentGrid = new Grid();

            // Add column definitions to the grid
            for (int i = 0; i < tab.GetColumns(); ++i)
            {
                ColumnDefinition col = new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) };
                parentGrid.ColumnDefinitions.Add(col);
            }

            // Add row definitions to the grid
            for (int i = 0; i < tab.GetRows(); ++i)
            {
                RowDefinition row = new RowDefinition { Height = new GridLength(1, GridUnitType.Star) };
                parentGrid.RowDefinitions.Add(row);
            }

            // Add the buttons to the grid
            for (int rowIndex = 0; rowIndex < tab.GetRows(); ++rowIndex) 
            {
                for (int columnIndex = 0; columnIndex < tab.GetColumns(); ++columnIndex)
                {
                    // Sound button
                    SoundButton soundButton = new SoundButton(parentTab: tab);

                    if (buttons is null == false && buttons.TryGet(rowIndex, columnIndex, out var buttonState))
                    {
                        soundButton.LoadState(buttonState);
                    }

                    Grid.SetColumn(soundButton, columnIndex);
                    Grid.SetRow(soundButton, rowIndex);
                    parentGrid.Children.Add(soundButton);

                    // Menu button
                    MenuButton menuButton = new MenuButton(soundButton);

                    Grid.SetColumn(menuButton, columnIndex);
                    Grid.SetRow(menuButton, rowIndex);
                    parentGrid.Children.Add(menuButton);
                    soundButton.ChildButtons.Add(menuButton);

                    // Play/pause button
                    PlayPauseButton playPauseButton = new PlayPauseButton(soundButton);

                    Grid.SetColumn(playPauseButton, columnIndex);
                    Grid.SetRow(playPauseButton, rowIndex);
                    parentGrid.Children.Add(playPauseButton);
                    soundButton.ChildButtons.Add(playPauseButton);

                    // Stop button
                    StopButton stopButton = new StopButton(soundButton);

                    Grid.SetColumn(stopButton, columnIndex);
                    Grid.SetRow(stopButton, rowIndex);
                    parentGrid.Children.Add(stopButton);
                    soundButton.ChildButtons.Add(stopButton);

                    // Loop icon
                    LoopIconButton loopIconButton = new LoopIconButton(soundButton);

                    Grid.SetColumn(loopIconButton, columnIndex);
                    Grid.SetRow(loopIconButton, rowIndex);
                    parentGrid.Children.Add(loopIconButton);
                    soundButton.ChildButtons.Add(loopIconButton);

                    // Volume offset icon
                    VolumeOffsetIconButton volumeOffsetIconButton = new VolumeOffsetIconButton(soundButton);

                    Grid.SetColumn(volumeOffsetIconButton, columnIndex);
                    Grid.SetRow(volumeOffsetIconButton, rowIndex);
                    parentGrid.Children.Add(volumeOffsetIconButton);
                    soundButton.ChildButtons.Add(volumeOffsetIconButton);

                    // Warning icon
                    SoundWarningIconButton soundWarningIconButton = new SoundWarningIconButton(soundButton);

                    Grid.SetColumn(soundWarningIconButton, columnIndex);
                    Grid.SetRow(soundWarningIconButton, rowIndex);
                    parentGrid.Children.Add(soundWarningIconButton);
                    soundButton.ChildButtons.Add(soundWarningIconButton);

                    // Hotkey indicator
                    HotkeyIndicatorButton hotkeyIndicatorButton = new HotkeyIndicatorButton(soundButton);

                    Grid.SetColumn(hotkeyIndicatorButton, columnIndex);
                    Grid.SetRow(hotkeyIndicatorButton, rowIndex);
                    parentGrid.Children.Add(hotkeyIndicatorButton);
                    soundButton.ChildButtons.Add(hotkeyIndicatorButton);

                    // Progress bar
                    SoundProgressBar progressBar = new SoundProgressBar();

                    Grid.SetColumn(progressBar, columnIndex);
                    Grid.SetRow(progressBar, rowIndex);
                    parentGrid.Children.Add(progressBar);

                    soundButton.SoundProgressBar = progressBar;
                }
            }

            tab.Content = parentGrid;
        }

        private void CreateHelpContent(MetroTabItem tab)
        {
            tab.Header = Properties.Resources.Welcome;

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

            // If a config file already exists, create a backup in case any part of the saving fails
            if (File.Exists(configFilePath))
            {
                File.Copy(configFilePath, $"{configFilePath}-{GetDateTimeString()}.bak", true);
            }

            using (FileStream fileStream = new FileStream(configFilePath, FileMode.Create))
            using (StreamWriter streamWriter = new StreamWriter(fileStream))
            using (XmlTextWriter textWriter = new XmlTextWriter(streamWriter))
            {
                textWriter.Formatting = Formatting.Indented;
                textWriter.Indentation = 4;

                textWriter.WriteStartDocument();
                textWriter.WriteStartElement("tabs"); // <tabs>

                // Save global settings
                textWriter.WriteStartElement(nameof(GlobalSettings)); // <GlobalSettings>
                textWriter.WriteAttributeString(GlobalSettings.OutputDeviceGuidSettingName, string.Join(@",", GlobalSettings.GetOutputDeviceGuids()));
                textWriter.WriteAttributeString(GlobalSettings.InputDeviceGuidSettingName, string.Join(@",", GlobalSettings.GetInputDeviceGuids()));
                textWriter.WriteAttributeString(nameof(GlobalSettings.AudioPassthroughLatency), GlobalSettings.AudioPassthroughLatency.ToString());
                textWriter.WriteEndElement();  // <GlobalSettings>

                foreach (MetroTabItem tab in Tabs.Items.OfType<MetroTabItem>())
                {
                    if (tab.Content is Grid grid)
                    {
                        string name = tab.Header.ToString();
                        textWriter.WriteStartElement("tab");
                        textWriter.WriteAttributeString("focused", tab.IsSelectedItem().ToString());
                        textWriter.WriteAttributeString("rows", tab.GetRows().ToString());
                        textWriter.WriteAttributeString("columns", tab.GetColumns().ToString());

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
                                textWriter.WriteAttributeString("stopAllSounds", button.StopAllSounds.ToString());
                                textWriter.WriteAttributeString("id", button.Id);
                                textWriter.WriteAttributeString("localHotkey", button.LocalHotkey?.ToString() ?? string.Empty);
                                textWriter.WriteAttributeString("globalHotkey", button.GlobalHotkey?.ToString() ?? string.Empty);
                                textWriter.WriteAttributeString("row", button.GetRow().ToString());
                                textWriter.WriteAttributeString("column", button.GetColumn().ToString());
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

                textWriter.WriteEndElement(); // </tabs>
                textWriter.WriteEndDocument();
            }
        }

        /// <summary>
        /// Returns all <see cref="SoundButton"/>s in the given <see cref="MainWindow"/>.
        /// If parameter <paramref name="metroTabItem"/> is passed, only <see cref="SoundButton"/>s which appear on the given <paramref name="metroTabItem"/> are returned.
        /// </summary>
        internal IEnumerable<SoundButton> GetSoundButtons(MetroTabItem metroTabItem = null)
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

        private async Task ShowAboutBox()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string version = AssemblyName.GetAssemblyName(assembly.Location).Version.ToString();

            var res = await this.ShowMessageAsync(Properties.Resources.AboutSoundBoard,
                Properties.Resources.CreatedByMicahMorrison + Environment.NewLine +
                string.Format(Properties.Resources.VersionNumber, version),
                MessageDialogStyle.AffirmativeAndNegative,
                new MetroDialogSettings { AffirmativeButtonText = Properties.Resources.CheckForUpdates, NegativeButtonText = Properties.Resources.OK });

            if (res == MessageDialogResult.Affirmative)
            {
                _updateChecker.CheckForUpdates(UpdateNotifyMode.Always);
            }
        }

        private string GetDateTimeString() => DateTime.Now.ToString(@"s").Replace(@":", @".");

        private void CleanupBackups()
        {
            // Check if there are any backup config files
            string directory = Path.GetDirectoryName(ConfigFilePath);
            if (Directory.Exists(directory))
            {
                var files = Directory.GetFiles(directory, "*.bak").OrderByDescending(File.GetCreationTime).ToList();
                if (files.Count > MAX_BACKUP_FILES)
                {
                    // Delete all but the newest five
                    files.Skip(MAX_BACKUP_FILES).ToList().ForEach(File.Delete);
                }
            }
        }

        #endregion

        #region Event handlers

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _updateChecker.CheckForUpdates(UpdateNotifyMode.Auto);
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

        private async void ChangeButtonGridMenuItem_Click(object sender, EventArgs e)
        {
            ButtonGridDialog buttonGridDialog = new ButtonGridDialog(SelectedTab.GetRows(),SelectedTab.GetColumns());
            await this.ShowChildWindowAsync(buttonGridDialog);

            if (buttonGridDialog.DialogResult == System.Windows.Forms.DialogResult.OK)
            {
                bool proceed = true;

                if (buttonGridDialog.RowCount * buttonGridDialog.ColumnCount > 300)
                {
                    var res = await this.ShowMessageAsync(Properties.Resources.Warning, Properties.Resources.LargeButtonCountWarning, MessageDialogStyle.AffirmativeAndNegative);
                    proceed = res == MessageDialogResult.Affirmative;
                }

                if (proceed)
                {
                    ChangeButtonGrid(buttonGridDialog.RowCount, buttonGridDialog.ColumnCount);
                }
            }
        }

        /// <summary>
        /// Change the button grid
        /// </summary>
        public void ChangeButtonGrid(int rowCount, int columnCount)
        {
            using (new WaitCursor())
            {
                // Stop all sounds
                foreach (SoundButton soundButton in GetSoundButtons())
                {
                    soundButton.Stop();
                    soundButton.UnregisterLocalHotkey();
                    soundButton.UnregisterGlobalHotkey();
                }

                ConfigUndoState configUndoState = (this as IUndoable<ConfigUndoState>).SaveState();

                // Set up our UndoAction
                SetUndoAction(() => { LoadState(configUndoState); });

                // Create and show a snackbar
                string message = Properties.Resources.ButtonLayoutWasChanged;
                string truncatedMessage = Utilities.Truncate(message, SnackbarMessageFont, (int)Width - 50);
                ShowUndoSnackbar(truncatedMessage);

                // Do the change
                SelectedTab.SetRows(rowCount);
                SelectedTab.SetColumns(columnCount);
                SaveSettings();
                LoadSettings();
            }
        }

        private void RoutedKeyDownHandler(object sender, RoutedEventArgs args)
        {
            if (Utilities.AreAnyDialogsVisible() == false)
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
                        // If there are any selected sounds, unselect them
                        else if (GetSoundButtons().Any(sb => sb.IsSelected))
                        {
                            GetSoundButtons().ToList().ForEach(sb => sb.IsSelected = false);
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
                        // If the texbox is focused, we want to focus the first button.
                        // Then we'll let Windows handle the navigation and pressing

                        if (Query.IsFocused)
                        {
                            ResultsPanel.Children.OfType<SoundButton>().FirstOrDefault()?.Focus();
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
                            Dispatcher.BeginInvoke(new Action(() => ResultsPanel.Children.OfType<SoundButton>().FirstOrDefault()?.Focus()), DispatcherPriority.ApplicationIdle);
                        }
                    }
                }
            }
        }

        private void RoutedKeyUpHandler(object sender, RoutedEventArgs args)
        {
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
            MetroTabItem tab = new MyMetroTabItem();
            CreateHelpContent(tab);
            Tabs.Items.Add(tab);
            tab.Focus();

            // Make sure the new tab has a context menu
            CreateTabContextMenus();
        }

        private async void about_Click(object sender, RoutedEventArgs e)
        {
            await ShowAboutBox();
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
                clearConfig.SetSeparator(true);
                clearConfig.Click += ClearConfig_Click;

                _inputDeviceMenu = new MenuItem { Header = Properties.Resources.InputDevice };
                _inputDeviceMenu.SubmenuOpened += InputDeviceMenuOpened;

                _outputDeviceMenu = new MenuItem {Header = Properties.Resources.OutputDevice};
                _outputDeviceMenu.SubmenuOpened += OutputDeviceMenuOpened;

                // Add a placeholder menu item so that "Output device" will have a submenu
                // even before we have evaluated the audio devices to add to the menu
                MenuItem placeholder = new MenuItem();
                _outputDeviceMenu.Items.Add(placeholder);

                // Add a placeholder menu item so that "Input device" will have a submenu
                // even before we have evaluated the audio devices to add to the menu
                placeholder = new MenuItem();
                _inputDeviceMenu.Items.Add(placeholder);

                overflowMenu.Items.Add(importConfig);
                overflowMenu.Items.Add(exportConfig);
                overflowMenu.Items.Add(clearConfig);
                overflowMenu.Items.Add(_inputDeviceMenu);
                overflowMenu.Items.Add(_outputDeviceMenu);

                overflowMenu.AddSeparators();

                Overflow.ContextMenu = overflowMenu;
            }

            overflowMenu.IsOpen = true;
        }

        private void addPage_Click(object sender, RoutedEventArgs e)
        {
            MetroTabItem tab = new MyMetroTabItem {Header = Properties.Resources.NewPage};
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
                    soundButton.UnregisterLocalHotkey();
                    soundButton.UnregisterGlobalHotkey();
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
                FileName = $@"SoundBoardConfiguration-{GetDateTimeString()}",
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
                    await this.ShowMessageAsync(Properties.Resources.Error,
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
                        soundButton.UnregisterLocalHotkey();
                        soundButton.UnregisterGlobalHotkey();
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
                }
                catch (Exception ex)
                {
                    await this.ShowMessageAsync(Properties.Resources.Error,
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
                    soundButton.UnregisterLocalHotkey();
                    soundButton.UnregisterGlobalHotkey();
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
                await this.ShowMessageAsync(Properties.Resources.Error,
                    Properties.Resources.ThereWasAProblem + Environment.NewLine + Environment.NewLine + ex.Message);
            }
        }

        private void InputDeviceMenuOpened(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem inputDeviceMenu)
            {
                // Clear the current items, whether they are the placeholder
                // or the previously evaluated audio devices.
                // We're marking them for removal instead of removing them immediately so that the 
                // menu doesn't resize and decide to close because our mouse is no longer over it.
                // Instead we'll add all the new items, then remove the old ones at the very end.
                // Use Control istead of MenuItem to capture the Separator.
                List<Control> itemsToRemove = inputDeviceMenu.Items.OfType<Control>().ToList();

                // Create a menu item for each output device
                using (MMDeviceEnumerator deviceEnumerator = new MMDeviceEnumerator())
                {
                    // Note: We're going in reverse order to preserve the separator and "Close" item at the bottom

                    // Now add the rest
                    foreach (MMDevice device in deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).Reverse())
                    {
                        MenuItem menuItem = new MenuItem
                        {
                            Header = string.Format(Properties.Resources.SingleSpecifier, device.FriendlyName),
                            Icon = GlobalSettings.GetInputDeviceGuids().Contains(device.GetGuid()) ? ImageHelper.GetImage(ImageHelper.CheckIconPath) : null,
                            StaysOpenOnClick = true
                        };
                        menuItem.PreviewMouseUp += (_, args) => HandleInputDeviceSelection(device.GetGuid());
                        inputDeviceMenu.Items.Insert(0, menuItem);
                    }

                    MenuItem closeDeviceMenuMenuItem = new MenuItem
                    {
                        Header = Properties.Resources.Close
                    };
                    inputDeviceMenu.Items.Add(new Separator());
                    inputDeviceMenu.Items.Add(closeDeviceMenuMenuItem);
                }

                // Finally, remove the items marked for removal
                foreach (Control control in itemsToRemove)
                {
                    inputDeviceMenu.Items.Remove(control);
                }

                // We only have close and separator, which looks funny, so remove the separator
                if (inputDeviceMenu.Items.Count == 2
                    && inputDeviceMenu.Items.OfType<Separator>().FirstOrDefault() is Separator separator)
                {
                    inputDeviceMenu.Items.Remove(separator);
                }
            }
        }

        private void OutputDeviceMenuOpened(object sender, RoutedEventArgs e)
        {
            // Re-evaluate the audio devices every time this sub-menu is opened
            if (sender is MenuItem outputDeviceMenuItem)
            {
                // Clear the current items, whether they are the placeholder
                // or the previously evaluated audio devices.
                // We're marking them for removal instead of removing them immediately so that the 
                // menu doesn't resize and decide to close because our mouse is no longer over it.
                // Instead we'll add all the new items, then remove the old ones at the very end.
                // Use Control istead of MenuItem to capture the Separator.
                List<Control> itemsToRemove = outputDeviceMenuItem.Items.OfType<Control>().ToList();

                // Create a menu item for each output device
                using (MMDeviceEnumerator deviceEnumerator = new MMDeviceEnumerator())
                {
                    // Note: We're going in reverse order to preserve the separator and "Close" item at the bottom

                    // Now add the rest
                    foreach (MMDevice device in deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active).Reverse())
                    {
                        MenuItem menuItem = new MenuItem
                        {
                            Header = string.Format(Properties.Resources.SingleSpecifier, device.FriendlyName),
                            Icon = GlobalSettings.GetOutputDeviceGuids().Contains(device.GetGuid()) ? ImageHelper.GetImage(ImageHelper.CheckIconPath) : null,
                            StaysOpenOnClick = true
                        };
                        menuItem.PreviewMouseUp += (_, args) => HandleOutputDeviceSelection(device.GetGuid(), args.ChangedButton);
                        outputDeviceMenuItem.Items.Insert(0, menuItem);
                    }

                    // First, add the default device
                    var defaultDevice = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                    var defaultDeviceMenuItem = new MenuItem
                    {
                        Header = string.Format(Properties.Resources.DefaultDevice, defaultDevice.FriendlyName),
                        Icon = GlobalSettings.GetOutputDeviceGuids().Contains(Guid.Empty) ? ImageHelper.GetImage(ImageHelper.CheckIconPath) : null,
                        StaysOpenOnClick = true
                    };
                    defaultDeviceMenuItem.PreviewMouseUp += (_, args) => HandleOutputDeviceSelection(Guid.Empty, args.ChangedButton);
                    outputDeviceMenuItem.Items.Insert(0, defaultDeviceMenuItem);

                    MenuItem closeDeviceMenuMenuItem = new MenuItem
                    {
                        Header = Properties.Resources.Close
                    };
                    outputDeviceMenuItem.Items.Add(new Separator());
                    outputDeviceMenuItem.Items.Add(closeDeviceMenuMenuItem);

                    // If, after adding all audio devices, none of them are selected, then select the default
                    if (outputDeviceMenuItem.Items.OfType<MenuItem>().All(item => item.Icon is null))
                    {
                        defaultDeviceMenuItem.Icon = ImageHelper.GetImage(ImageHelper.CheckIconPath);
                    }
                }

                // Finally, remove the items marked for removal
                foreach (Control control in itemsToRemove)
                {
                    outputDeviceMenuItem.Items.Remove(control);
                }

                // We only have close and separator, which looks funny, so remove the separator
                if (outputDeviceMenuItem.Items.Count == 2
                    && outputDeviceMenuItem.Items.OfType<Separator>().FirstOrDefault() is Separator separator)
                {
                    outputDeviceMenuItem.Items.Remove(separator);
                }
            }
        }

        // This behavior is different from the output devices in that we can only select one.
        // However, all of the pieces are in place to allow multiple selection if needed.
        private void HandleInputDeviceSelection(Guid deviceGuid)
        {
            if (GlobalSettings.GetInputDeviceGuids().Contains(deviceGuid))
            {
                // Toggle it off
                GlobalSettings.RemoveInputDeviceGuid(deviceGuid);
            }
            else
            {
                // Toggle it on and remove others
                GlobalSettings.RemoveAllInputDeviceGuids();
                GlobalSettings.AddInputDeviceGuid(deviceGuid);
            }

            // Refresh the menu
            InputDeviceMenuOpened(_inputDeviceMenu, new RoutedEventArgs());

            HandleInputOutputChange();
        }

        private void HandleInputOutputChange()
        {
            // Clear any existing chaining
            CleanUpAudioPassthrough();

            if (GlobalSettings.GetInputDeviceGuids().Any())
            {
                foreach (var outputDeviceId in GlobalSettings.GetOutputDeviceGuids())
                {
                    // Create the input
                    Guid inputDeviceId = GlobalSettings.GetInputDeviceGuids().First();
                    MMDevice inputDevice = Utilities.GetDevice(inputDeviceId, DataFlow.Capture);
                    WasapiCapture inputCapture = new WasapiCapture(inputDevice);
                    inputCapture.RecordingStopped += HandleRecordingStopped;
                    _inputCaptures.Add(inputCapture);

                    // Create the buffer
                    BufferedWaveProvider bufferedWaveProvider = new BufferedWaveProvider(inputDevice.AudioClient.MixFormat)
                    {
                        DiscardOnBufferOverflow = true
                    };
                    _bufferedWaveProviders.Add(bufferedWaveProvider);

                    inputCapture.DataAvailable += (_, args) =>
                    {
                        bufferedWaveProvider.AddSamples(args.Buffer, 0, args.BytesRecorded);
                    };

                    // Create the outputs
                    WasapiOut output = new WasapiOut(Utilities.GetDevice(outputDeviceId, DataFlow.Render), AudioClientShareMode.Shared, true, GlobalSettings.AudioPassthroughLatency);
                    _outputCaptures.Add(output);

                    output.Init(bufferedWaveProvider);
                    output.Play();

                    inputCapture.StartRecording();
                }
            }
        }

        private void HandleRecordingStopped(object sender, StoppedEventArgs args)
        {
            // Handle the device being disabled/disconnected/etc.
            GlobalSettings.RemoveAllInputDeviceGuids();
            CleanUpAudioPassthrough();
        }

        private void CleanUpAudioPassthrough()
        {
            try
            {
                _inputCaptures.ForEach(ic =>
                {
                    ic.RecordingStopped -= HandleRecordingStopped;
                    ic.StopRecording();
                    ic.Dispose();
                });
            }
            finally
            {
                _inputCaptures.Clear();
            }

            try
            {
                _outputCaptures.ForEach(oc =>
                {
                    oc.Stop();
                    oc.Dispose();
                });
            }
            finally
            {
                _outputCaptures.Clear();
            }

            try
            {
                _bufferedWaveProviders.ForEach(bwp => bwp.ClearBuffer());
            }
            finally
            {
                _bufferedWaveProviders.Clear();
            }
        }

        private readonly List<WasapiCapture> _inputCaptures = new List<WasapiCapture>();
        private readonly List<WasapiOut> _outputCaptures = new List<WasapiOut>();
        private readonly List<BufferedWaveProvider> _bufferedWaveProviders = new List<BufferedWaveProvider>();

        private void HandleOutputDeviceSelection(Guid deviceGuid, MouseButton mouseButton)
        {
            if (mouseButton == MouseButton.Right)
            {
                // This is a toggle. Do not clear the list, and add or removing depending on existence.
                if (GlobalSettings.GetOutputDeviceGuids().Contains(deviceGuid))
                {
                    if (GlobalSettings.GetOutputDeviceGuids().All(g => g == deviceGuid))
                    {
                        // This is the only device in the list, so we can't really toggle it. Do nothing.
                    }
                    else
                    {
                        // This is in the list, and it's being toggled off, so remove it.
                        GlobalSettings.RemoveOutputDeviceGuid(deviceGuid);
                    }
                }
                else
                {
                    // This is not the list, and it's being togged on, so add it.
                    GlobalSettings.AddOutputDeviceGuid(deviceGuid);
                }
            }
            else
            {
                // Not a toggle, just a selection
                GlobalSettings.RemoveAllOutputDeviceGuids();
                GlobalSettings.AddOutputDeviceGuid(deviceGuid);
            }

            // Refresh the menu
            OutputDeviceMenuOpened(_outputDeviceMenu, new RoutedEventArgs());
            
            HandleInputOutputChange();
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

        private async void AboutBoxCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            await ShowAboutBox();
        }

        #endregion

        #region Overrides

        /// <inheritdoc />
        protected override void OnClosed(EventArgs e)
        {
            // Do cleanup
            _globalMouseEvents.MouseDown -= Global_MouseDown;
            _globalMouseEvents.Dispose();

            CleanUpAudioPassthrough();

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

        /// <summary>
        /// The hot key manager for the application
        /// </summary>
        public HotKeyManager HotKeyManager { get; private set; }

        /// <summary>
        /// Whether or not any instance of the hotkey picker dialog is open
        /// </summary>
        public bool IsHotkeyPickerOpen { get; set; }

        /// <summary>
        /// The currently selected tab
        /// </summary>
        public MetroTabItem SelectedTab => Tabs.SelectedItem as MetroTabItem;

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
        private Action _undoAction;
        private readonly Dictionary<MetroTabItem, ContextMenu> _tabContextMenus = new Dictionary<MetroTabItem, ContextMenu>();
        private readonly WpfUpdateChecker _updateChecker;
        private MenuItem _inputDeviceMenu;
        private MenuItem _outputDeviceMenu;

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

        private const int MAX_BACKUP_FILES = 5;

        #endregion

        #region IUndoable members

        /// <inheritdoc />
        public TabPageUndoState SaveState()
        {
            return new TabPageUndoState {MetroTabItem = SelectedTab, Index = Tabs.SelectedIndex};
        }

        /// <inheritdoc />
        public void LoadState(TabPageUndoState undoState)
        {
            Tabs.Items.Insert(undoState.Index, undoState.MetroTabItem);
            Tabs.SelectedIndex = undoState.Index;

            if (Tabs.SelectedItem is MetroTabItem metroTabItem)
            {
                foreach (SoundButton soundButton in GetSoundButtons(metroTabItem))
                {
                    try
                    {
                        soundButton.ReregisterLocalHotkey();
                    }
                    catch
                    {
                        // Swallow
                    }

                    try
                    {
                        soundButton.ReregisterGlobalHotkey();
                    }
                    catch
                    {
                        // Swallow
                    }
                }
            }
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
                soundButton.UnregisterLocalHotkey();
                soundButton.UnregisterGlobalHotkey();
            }

            LoadSettings(undoState.SavedConfigStatePath);
            SaveSettings();
        }

        /// <inheritdoc />
        TabPageSoundsUndoState IUndoable<TabPageSoundsUndoState>.SaveState()
        {
            var soundButtonUndoStates = new List<(SoundButtonUndoState, int)>();

            int index = 0;
            foreach (SoundButton soundButton in GetSoundButtons(SelectedTab))
            {
                soundButtonUndoStates.Add((soundButton.SaveState(), index));
                ++index;
            }

            return new TabPageSoundsUndoState {SoundButtonUndoStates = soundButtonUndoStates};
        }

        /// <inheritdoc />
        public void LoadState(TabPageSoundsUndoState undoState)
        {
            IList<SoundButton> soundButtons = GetSoundButtons(SelectedTab).ToList();

            foreach (var soundButtonUndoState in undoState.SoundButtonUndoStates)
            {
                soundButtons[soundButtonUndoState.ButtonIndex].LoadState(soundButtonUndoState.SoundButtonUndoState);
            }
        }

        #endregion
    }
}
