# Past Player Characters as Mobs

Snapshots your evolution build every 5 player levels (starting at level 20). In future runs, those past selves spawn as Alpha + Shiny mobs with the snapshot's stats, evolutions, and animations.

Saved snapshots live at `BepInEx/config/com.bungus.everythingiscrab.pastplayercharactersasmobs/snapshots.json`. Inspectable and safe to wipe.

## Requirements

- *Everything is Crab* (tested 1.0.1__8213).
- BepInEx 6 BleedingEdge IL2CPP (CoreCLR), build 755 or newer. Stable Thunderstore BepInEx packs do not work on Unity 6.

## Install (manual)

1. Install BepInEx into your game folder. Launch the game once to generate `BepInEx/interop/`.
2. Drop `plugins/Bungus-PastPlayerCharactersAsMobs/` from this zip into your `BepInEx/plugins/` folder.
3. Launch the game.

## Install (mod manager)

Use "Install from file" with this zip in r2modman, Gale, or the Thunderstore App.

## Uninstall

Delete `BepInEx/plugins/Bungus-PastPlayerCharactersAsMobs/`. To also wipe saved snapshots, delete `BepInEx/config/com.bungus.everythingiscrab.pastplayercharactersasmobs/`.

## Author

Bungus

## Source

https://github.com/rinpoche-peregrine/PastPlayerCharactersAsMobs
