## A BepInEx plugin that allows for the creation of custom cards in-game. ##
The code is a bit of a mess right now. I'll work on improving it soon.<br>
I'm somewhat new to C# and Harmony. Feel free to point out any bad conventions or other mistakes that aren't immediately obvious.<br>
Discord: **IngoH#3923**<br>
The repository has two DLLs. The main one, IGCCMod.dll, and the utility one for testing, UtilMod.dll<br>
The utility DLL contains features such as unlocking the painting when looking at it, infinite clover rerolls, and resetting the run when the clock is set to 666.

**To access the card creator, use the backroom door handle.**<br>

### Changelog ###
#### 1.1.0
- Update to API 1.12/JSONLoader 1.7
- Created cards no longer temporarily increase spawn rates.
- Added some info and descriptions for card properties.

### Planned Features ###
- Editing/deleting existing cards.
- Config for adding card to deck/increased spawn rates.
- Split IGCCMod and UtilMod
- Loading portraits from online database.

### Known Bugs ###
- Some game-crashing abilites can be chosen.
- Some options are missing.
- Regular death card creation doesn't work.
- Portraits can contain duplicates.
- Some properties do not show up correctly (Won't fix)

### Other Notes ###
- All portraits are exported to a new file, regardless of whether it already exists as a file.
