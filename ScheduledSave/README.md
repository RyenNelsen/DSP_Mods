# Scheduled Save
Want a little more control with your saves? Want to have a persistent save automatically done for you? Want to look back on your base as it evolves? You are in the right place!

## Important Information
This mod does not change the default Auto Save behaviour. If you would like to just rely on the mod, you can disable game saves by going under `Settings`, going under the `Gameplay` tab and then sliding the `Auto-saving interval` slide all the way to the left.

DSP will also not load these saves by default, as it looks for the auto generated saves at load.

## Installation
Check the [DSP Wiki](https://dsp-wiki.com/Modding:Getting_Started) on how to install mods.

## How to use
Run the game with the mod installed to generate the configuration file `io.ryen.scheduledsave.cfg`.

The configuration file can be found within `DPS_GAME_DIRECTORY\BepInEx\config` or under the "Config editor" in r2modman.

The configuration options are:
- `SaveInterval` - Saves the game every X minutes. (Default: `30`)
- `Filename` - The filename for the save file(s). (Default: `ScheduledSave(count)`) You can add `(count)` anywhere in the name and it will count up starting at 0001. If you do not supply `(count)` then the save file will be overwritten at each scheduled save.

## Bugs/Issues
Please feel to reach out to me on discord! You can find me at **Killroy7777#2791**.

## Changelog
### v1.0.0
- Inital Release

