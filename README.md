![alt text](https://s9.postimg.cc/n3e4vsb5b/logo.png "SoundBoard Logo")

# About SoundBoard

## Synopsis

SoundBoard is a elegant, easy-to-use application to save and play your favorite sounds.

Features include...
* highly customizable (create your perfect soundboard with multiple pages and color-coded sounds)
* background saving/loading of sound configuration (so you don't lose your changes)
* instant searching and playback of sounds from the keyboard (find the perfect sound at a moment's notice)
* clean, modern UI
* and more...

## Downloads

Grab the latest version [here](https://github.com/micahmo/SoundBoard/releases/latest/download/SoundBoard.exe). This will download a portable SoundBoard.exe that can be run from anywhere. Use the Export Configuration function to bring your configuration to a different system.


## Screenshots

###### Create multiple pages with multiple sounds on each
![alt text](https://i.postimg.cc/rwnDXNfN/2019-09-02-15-30-42-Glow-Window.png "Overview1")

###### Color-code and customize the number of sounds per page
![alt text](https://i.postimg.cc/SR6GDv7r/2019-09-02-15-33-07-Glow-Window.png "Overview2")

###### Quickly search and play a sound just by typing
![alt text](https://i.postimg.cc/4NXvXwQD/2019-09-02-15-33-53-Glow-Window.png "Overview3")

###### View and control playback of each sound individually
![alt text](https://i.postimg.cc/4xPHBgCy/2019-09-02-15-39-46-Glow-Window.png "Overview")

###### Select audio output device; allows you to route audio to a device that is not selected as the default in Windows

![image](https://user-images.githubusercontent.com/7417301/147796828-5dacc1a5-9056-4437-8210-1154a5098920.png)

###### Select multiple audio output devices

![image](https://user-images.githubusercontent.com/7417301/147796839-bb8d0abc-88a0-42e1-9c3a-b4b510c3e9d6.png)

> Left-click to select a single audio output device. Right-click to select or unselect additional devices.
>  
> Note that audio playback to multiple output devices is not guaranteed to be 100% synchronized. This functionality is not officially supported by Windows or [NAudio](https://github.com/naudio/NAudio), so SoundBoard is creating separate audio streams to each device which have the potential to drift.

###### Pass through an audio input device

You may also select an input to pipe to your output(s). This is essentially an audio passthrough, and should be roughly equivalent listen feature in the Windows sound properties. You may optionally tweak the desired latency in the configuration file. A too-low latency may result in choppy audio.

###### Assign Hotkeys

You may assign local and global hotkeys to sounds. Pressing a local hotkey will play the corresponding sound when the application is active. Pressing a global hotkey will play the sound regardless of the active window.

* Some shortcuts may be reserved by other apps or by Windows itself.
* Using single letters/number/character hotkeys may conflict with the quick search feature.
* Using standard Windows shortcuts may also produce unintended behavior (e.g., Tab or Win).

![image](https://user-images.githubusercontent.com/7417301/221188183-e1a320ff-9561-4722-887a-91e76560235a.png)

## License

This code is licenced under the [MIT License](https://opensource.org/licenses/MIT).
