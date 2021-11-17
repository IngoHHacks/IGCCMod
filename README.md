## A BepInEx plugin that allows for the creation of custom cards in-game. ##
The code is a bit of a mess right now. I'll work on improving it soon.<br>
I'm somewhat new to C# and Harmony. Feel free to point out any bad conventions or other mistakes that aren't immediately obvious.<br>
Discord: **IngoH#3923**<br>

### Planned Features ###
- Editing/deleting existing cards.
- Add descriptions for card properties.
- Loading portraits from online database.

### Known Bugs ###
- Hidden cards appear in card selections if the game is not restarted. (Might be due to API; might be fixed)
- Some game-crashing abilites can be chosen.
- Some options are missing.
- Regular death card creation doesn't work.
- Portraits can contain duplicates.

### Other Notes ###
- All portraits are exported to a new file, regardless of whether it already exists as file.
