### Changelog

#### 2.5.2
- Fixed the emission issue for cards added through code.

#### 2.5.1
- Fixed an issue where portraits would not load if a vanilla emission was overwritten by a custom emission from a different mod.

#### 2.5.0

- Added support for custom tribes.
- Fixed text for evolution turn count.

#### 2.4.0

- Cards get added automatically now.

#### 2.3.1

- Fix for API 2.3.0.

#### 2.3.0

- Added support for alternate portraits and emissions.
- Emissions get automatically added when a file of the same name suffixed by `_emission` exists.
- Portraits only load once now.

#### 2.2.2

- Fixed README.

#### 2.2.1

- Fixed custom abilities again.

#### 2.2.0

- Added the ability to edit cards after the initial sequence.
- Update to API 2.2.0.
- Updated Lammergeier attack to follow Kaycee's Mod behavior.
- Shortened intro sequence.
- Selecting no tribes no longer adds the None tribe.
- Moved card export folder to ```IGCCExports``` and added explanation.

#### 2.1.0

- Fixed custom abilities and evolutions.

#### 2.0.0

- Kaycee's Mod update.

#### 1.2.3

- Fixed appearanceBehaviour spelling.

#### 1.2.2

- Fixed custom abilities.

#### 1.2.1

- Reverted Kaycee's Mod compatibility. Use 1.2.0 for Kaycee's Mod.

#### 1.2.0

- Added Kaycee's Mod compatibility.

#### 1.1.2

- Fixed incompatibility with DialogRemover.

#### 1.1.1

- Renamed the incorrectly named 'cost' property to 'bloodCost'.

#### 1.1.0

- Update to API 1.12/JSONLoader 1.7
- Created cards no longer temporarily increase spawn rates.
- Added some info and descriptions for card properties.

### Planned Features

- Editing/deleting existing cards.
- Config for adding card to deck/increased spawn rates.
- Split IGCCMod and UtilMod
- Loading portraits from online database.

### Known Bugs

- Some game-crashing abilites can be chosen.
- Some options are missing.
- Regular death card creation doesn't work.
- Portraits can contain duplicates.
- Switching cards back from rare to regular doesn't update the preview card.
- Some properties do not show up correctly (Won't fix)

### Other Notes

- All portraits are exported to a new file, regardless of whether it already exists as a file.
