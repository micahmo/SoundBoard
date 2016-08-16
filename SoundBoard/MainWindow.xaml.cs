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
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System.Runtime.InteropServices;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;


namespace SoundBoard
{ 
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        #region key_conversion_stuff

        public enum MapType : uint
        {
            MAPVK_VK_TO_VSC = 0x0,
            MAPVK_VSC_TO_VK = 0x1,
            MAPVK_VK_TO_CHAR = 0x2,
            MAPVK_VSC_TO_VK_EX = 0x3,
        }

        [DllImport("user32.dll")]
        public static extern int ToUnicode(
            uint wVirtKey,
            uint wScanCode,
            byte[] lpKeyState,
            [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 4)]
            StringBuilder pwszBuff,
            int cchBuff,
            uint wFlags);

        [DllImport("user32.dll")]
        public static extern bool GetKeyboardState(byte[] lpKeyState);

        [DllImport("user32.dll")]
        public static extern uint MapVirtualKey(uint uCode, MapType uMapType);

        public static char GetCharFromKey(Key key)
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

        private string searchString = "";
        private static MainWindow This;
        public RoutedEventHandler keyDownHandler;
        private SoundButton focusedButton;

        public static Dictionary<string, string> sounds = new Dictionary<string, string>();
        public static List<IWavePlayer> soundPlayers = new List<IWavePlayer>();

        public static MainWindow GetThis()
        {
            return This;
        }

        public MainWindow()
        {
            InitializeComponent();

            This = this;

            // make it a routed eventhandler so it'll get events already handled
            keyDownHandler = new RoutedEventHandler(RoutedKeyDownHandler);
            AddHandler(KeyDownEvent, keyDownHandler, true);
            AddHandler(KeyUpEvent, new KeyEventHandler(KeyUpHandler), true);
            Closing += new System.ComponentModel.CancelEventHandler(FormClosingHandler);

            RightWindowCommandsOverlayBehavior = WindowCommandsOverlayBehavior.Never;

            try
            {
                // try to load settings
                XmlDocument xmlDocument = new XmlDocument();

                xmlDocument.Load("soundboard.config");

                XmlElement xelRoot = xmlDocument.DocumentElement;
                XmlNodeList tabNodes = xelRoot.SelectNodes("/tabs/tab");

                // remove default tabs
                Tabs.Items.Clear();

                foreach (XmlNode node in tabNodes)
                {
                    string name = node["name"].InnerText;

                    MetroTabItem tab = new MetroTabItem();
                    tab.Header = name;
                    Tabs.Items.Add(tab);

                    List<Tuple<string, string>> buttons = new List<Tuple<string, string>>();

                    // read the button data
                    for (int i = 0; i < 10; ++i) {
                        buttons.Add(new Tuple<string, string>(node["button" + i].Attributes["name"].Value, node["button" + i].Attributes["path"].Value));
                    }

                    CreatePageContent(tab, buttons);
                }
            }
            // didn't work? make new stuff!
            catch
            {
                // populate content for "welcome"
                CreateHelpContent((MetroTabItem)Tabs.Items[0]);
            }
        }

        private void RoutedKeyDownHandler(object sender, RoutedEventArgs e)
        {
            KeyDownHandler(sender, (KeyEventArgs)e);
        }

        private void KeyUpHandler(object sender, KeyEventArgs e)
        {
            // if a focus gets messed up, re-focus it here
            if (focusedButton != null)
            {
                focusedButton.Focus();
            }
        }

        private void KeyDownHandler(object sender, KeyEventArgs e)
        {
            Mouse.Capture(null);

            char c = GetCharFromKey(e.Key);
            if (char.IsLetter(c) || char.IsPunctuation(c) || char.IsNumber(c))
            {
                searchString += c;
            }
            else if (e.Key == Key.Space) {
                searchString += ' ';
            }
            else if (e.Key == Key.Back && searchString.Length > 0)
            {
                searchString = searchString.Substring(0, searchString.Length - 1);
            }
            else if (e.Key == Key.Escape)
            {
                // if the search bar is open, close it
                if (Search.IsOpen) {
                    Search.IsOpen = false;
                }
                // otherwise, stop any playing sounds
                else {
                    ButtonAutomationPeer peer = new ButtonAutomationPeer(Silence);
                    IInvokeProvider invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                    invokeProv.Invoke();
                }
                return;
            }
            else if (e.Key == Key.Down)
            {
                bool foundFocused = false;
                // loop through the buttons and focus the next one
                foreach (var child in ResultsPanel.Children)
                {
                    if (child is SoundButton)
                    {
                        if (foundFocused)
                        {
                            // we found the last focused button, so focus this one
                            ((SoundButton)child).Focus();
                            focusedButton = ((SoundButton)child);
                            break;
                        }
                        if (((SoundButton)child).IsFocused)
                        {
                            // we found the focused button! focus the next one
                            foundFocused = true;
                        }
                    }
                }
                return;
            }
            else if (e.Key == Key.Up)
            {
                SoundButton previousButton = null;
                // loop through the buttons and focus the previous one
                foreach (var child in ResultsPanel.Children)
                {
                    if (child is SoundButton)
                    {
                        if (((SoundButton)child).IsFocused)
                        {
                            // focus the previous one!
                            if (previousButton != null)
                            {
                                previousButton.Focus();
                                focusedButton = previousButton;
                                break;
                            }
                        }
                        previousButton = (SoundButton)child;
                    }
                }
                return;
            }
            else if (e.Key == Key.Enter)
            {
                // play the sound!
                foreach (var child in ResultsPanel.Children)
                {
                    if (child is SoundButton && ((SoundButton)child).IsFocused)
                    {
                        ButtonAutomationPeer peer = new ButtonAutomationPeer((SoundButton)child);
                        IInvokeProvider invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                        invokeProv.Invoke();
                    }
                }
                return;
            }
            else
            {
                return;
            }

            Query.Text = searchString;
            Query.CaretIndex = Query.Text.Length;

            Search.IsOpen = true;

            // get rid of any previous buttons / do reverse iteration to prevent weird bugs
            for (int i = ResultsPanel.Children.Count - 1; i >= 0; --i)
            {
                if (ResultsPanel.Children[i] is SoundButton)
                {
                    ResultsPanel.Children.Remove((SoundButton)ResultsPanel.Children[i]);
                }
            }

            // perform search
            if (searchString != "")
            {
                foreach (KeyValuePair<string, string> entry in sounds)
                {
                    if (entry.Key.ToLower().Contains(searchString.ToLower()))
                    {
                        SoundButton button = new SoundButton(true);
                        button.SetFile(entry.Value, entry.Key, false);

                        ResultsPanel.Children.Add(button);
                    }
                }

                // if we've added at least one button, focus the first one
                if (ResultsPanel.Children.Count > 0)
                {
                    ((SoundButton)ResultsPanel.Children[0]).Focus();
                    focusedButton = ((SoundButton)ResultsPanel.Children[0]);
                }
            }

        }

        private void FlyoutCloseHandler(object sender, RoutedEventArgs e)
        {
            searchString = "";
        }

        private void silence_Click(object sender, EventArgs e)
        {
            foreach (IWavePlayer player in soundPlayers) {
                player.Stop();
            }
        }

        private void help_Click(object sender, RoutedEventArgs e)
        {
            MetroTabItem tab = new MetroTabItem();
            tab.Header = "welcome";
            CreateHelpContent(tab);
            Tabs.Items.Add(tab);
            tab.Focus();
        }

        private async void about_Click(object sender, RoutedEventArgs e)
        {
            Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            string version = AssemblyName.GetAssemblyName(assembly.Location).Version.ToString();

            await this.ShowMessageAsync("About Sound Board", "Created by Micah Morrison\nversion " + version);
        }

        private void addPage_Click(object sender, RoutedEventArgs e)
        {
            MetroTabItem tab = new MetroTabItem();
            tab.Header = "new page";
            CreatePageContent(tab);
            Tabs.Items.Add(tab);
            tab.Focus();
        }

        private async void renamePage_Click(object sender, RoutedEventArgs e)
        {
            RemoveHandler(KeyDownEvent, keyDownHandler);

            string result = await this.ShowInputAsync("Rename", "Whaddya wanna call it?");
            if (result != null && result != "")
            {
                MetroTabItem tab = (MetroTabItem)Tabs.SelectedItem;
                tab.Header = result;
            }

            AddHandler(KeyDownEvent, keyDownHandler, true);
        }

        private async void removePage_Click(object sender, RoutedEventArgs e)
        {
            MessageDialogResult result = await this.ShowMessageAsync("Just checking...", "Are you sure you want to delete this page?", MessageDialogStyle.AffirmativeAndNegative);
            if (result == MessageDialogResult.Affirmative)
                Tabs.Items.Remove(Tabs.SelectedItem);

            UpdateSoundList();
        }

        public void UpdateSoundList()
        {
            sounds.Clear();

            foreach (MetroTabItem tab in Tabs.Items)
            {
                if (tab.Content is Grid) {
                    Grid grid = (Grid)tab.Content;
                    foreach (var child in grid.Children) {
                        if (child is SoundButton) {
                            SoundButton button = (SoundButton)child;
                            sounds[button.Content.ToString()] = button.GetFile();
                        }
                    }
                }
            }
        }

        private void CreatePageContent(MetroTabItem tab, List<Tuple<string, string>> buttons = null)
        {
            Grid parentGrid = new Grid();

            ColumnDefinition col1 = new ColumnDefinition();
            col1.Width = new GridLength(1, GridUnitType.Star);
            parentGrid.ColumnDefinitions.Add(col1);

            ColumnDefinition col2 = new ColumnDefinition();
            col2.Width = new GridLength(1, GridUnitType.Star);
            parentGrid.ColumnDefinitions.Add(col2);

            RowDefinition row;

            for (int i = 0; i < 5; ++i)
            {
                row = new RowDefinition();
                row.Height = new GridLength(1, GridUnitType.Star);
                parentGrid.RowDefinitions.Add(row);

                // sound button
                SoundButton soundButton = new SoundButton();

                Grid.SetColumn(soundButton, 0);
                Grid.SetRow(soundButton, i);
                parentGrid.Children.Add(soundButton);

                if (buttons != null)
                {
                    soundButton.SetFile(buttons[i].Item2, buttons[i].Item1);
                }

                // menu button
                MenuButton menuButton = new MenuButton(soundButton);

                Grid.SetColumn(menuButton, 0);
                Grid.SetRow(menuButton, i);
                parentGrid.Children.Add(menuButton);

                // progress bar
                SoundProgressBar progressBar = new SoundProgressBar();

                Grid.SetColumn(progressBar, 0);
                Grid.SetRow(progressBar, i);
                parentGrid.Children.Add(progressBar);

                soundButton.SetSoundProgressBar(progressBar);
            }

            for (int i = 0; i < 5; ++i)
            { 
                // only have to add the rows once (above)

                // sound button
                SoundButton soundButton = new SoundButton();

                Grid.SetColumn(soundButton, 1);
                Grid.SetRow(soundButton, i);
                parentGrid.Children.Add(soundButton);

                if (buttons != null)
                {
                    soundButton.SetFile(buttons[i+5].Item2, buttons[i+5].Item1);
                }

                // menu button
                MenuButton menuButton = new MenuButton(soundButton);

                Grid.SetColumn(menuButton, 1);
                Grid.SetRow(menuButton, i);
                parentGrid.Children.Add(menuButton);

                // progress bar
                SoundProgressBar progressBar = new SoundProgressBar();

                Grid.SetColumn(progressBar, 1);
                Grid.SetRow(progressBar, i);
                parentGrid.Children.Add(progressBar);

                soundButton.SetSoundProgressBar(progressBar);
            }

            tab.Content = parentGrid;
        }

        private void CreateHelpContent(MetroTabItem tab)
        {
            StackPanel stackPanel = new StackPanel();

            TextBlock text = new TextBlock();
            text.Text = "WELCOME TO SOUND BOARD!";
            text.Padding = new Thickness(5);
            text.FontSize = 25;
            text.TextWrapping = TextWrapping.Wrap;
            stackPanel.Children.Add(text);

            text = new TextBlock();
            text.Text = "...an app where you can create and run your own custom sound board full of your favorite bytes, effects, and clips.";
            text.Padding = new Thickness(5);
            text.FontSize = 15;
            text.TextWrapping = TextWrapping.Wrap;
            stackPanel.Children.Add(text);

            text = new TextBlock();
            text.Text = "HOW DOES IT WORK?";
            text.Padding = new Thickness(5);
            text.FontSize = 20;
            text.FontWeight = FontWeights.Bold;
            text.TextWrapping = TextWrapping.Wrap;
            stackPanel.Children.Add(text);

            text = new TextBlock();
            text.Text = "Sound Board is split into pages, so you can group your favorite sounds together. To get started, add your first sound page by clicking \"add page\" above!";
            text.Padding = new Thickness(5);
            text.FontSize = 15;
            text.TextWrapping = TextWrapping.Wrap;
            stackPanel.Children.Add(text);

            text = new TextBlock();
            text.Text = "To load a sound, just drag an audio file onto a button or click the three little dots to browse. Then click the button with the sound name on it to play it!";
            text.Padding = new Thickness(5);
            text.FontSize = 15;
            text.TextWrapping = TextWrapping.Wrap;
            stackPanel.Children.Add(text);

            text = new TextBlock();
            text.Text = "To find a sound in a hurry, just start typing its name and an instant search bar will appear!";
            text.Padding = new Thickness(5);
            text.FontSize = 15;
            text.TextWrapping = TextWrapping.Wrap;
            stackPanel.Children.Add(text);

            text = new TextBlock();
            text.Text = "Pages and sounds are saved, so when you're done, just close the app, and it'll pick up right where you left off next time!";
            text.Padding = new Thickness(5);
            text.FontSize = 15;
            text.TextWrapping = TextWrapping.Wrap;
            stackPanel.Children.Add(text);

            text = new TextBlock();
            text.Text = "Add, remove pages, or rename pages at any time by clicking buttons at the top. Feel free to remove this page when you're ready to go! Bring it back any time by clicking \"help\".";
            text.Padding = new Thickness(5);
            text.FontSize = 15;
            text.TextWrapping = TextWrapping.Wrap;
            stackPanel.Children.Add(text);

            tab.Content = stackPanel;
        }

        private void FormClosingHandler(object sender, EventArgs e)
        {
            string filename = "soundboard.config";

            using (FileStream fileStream = new FileStream(filename, FileMode.Create))
            using (StreamWriter sw = new StreamWriter(fileStream))
            using (XmlTextWriter writer = new XmlTextWriter(sw))
            {
                writer.Formatting = Formatting.Indented;
                writer.Indentation = 4;

                writer.WriteStartDocument();
                writer.WriteStartElement("tabs");

                for (int i = 0; i < Tabs.Items.Count; ++i)
                {
                    MetroTabItem tab = (MetroTabItem)Tabs.Items[i];
                    if (tab.Content is Grid)
                    {

                        string name = tab.Header.ToString();
                        writer.WriteStartElement("tab");

                        writer.WriteElementString("name", name);


                        {
                            Grid grid = (Grid)tab.Content;
                            int j = 0;
                            foreach (var child in grid.Children)
                            {
                                if (child is SoundButton)
                                {
                                    SoundButton button = (SoundButton)child;
                                    writer.WriteStartElement("button" + j++);
                                    writer.WriteAttributeString("name", button.GetFileName());
                                    writer.WriteAttributeString("path", button.GetFile());
                                    //writer.WriteString("test");
                                    writer.WriteEndElement();
                                }
                            }
                        }

                        writer.WriteEndElement();
                    }
                    else // it doesn't have a grid, so it's not a sound page
                    {
                        continue;
                    }
                }

                writer.WriteEndElement();
                writer.WriteEndDocument();
            }
        }
    }
}
