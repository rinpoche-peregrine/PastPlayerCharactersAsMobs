# Past Player Characters as Mobs

Snapshots your build every 5 player levels starting at 20. Future versions will spawn those past selves as Alpha mobs. This v0.1.0 release only captures the snapshots. No ghosts yet.

You can confirm it works after a run by opening `BepInEx/config/com.bungus.everythingiscrab.pastplayercharactersasmobs/snapshots.json`.

## Requirements

- *Everything is Crab* (tested 1.0.1__8213).
- BepInEx 6 BleedingEdge IL2CPP (CoreCLR), build 755 or newer. The stable Thunderstore BepInEx packs do not work on Unity 6.

## Install (manual)

1. Install BepInEx into your game folder. Launch the game once to generate `BepInEx/interop/`.
2. Drop `plugins/Bungus-PastPlayerCharactersAsMobs/` from this zip into your `BepInEx/plugins/` folder.
3. Launch the game.

## Install (mod manager)

Use "Install from file" with this zip in r2modman, Gale, or the Thunderstore App.

## Uninstall

Delete `BepInEx/plugins/Bungus-PastPlayerCharactersAsMobs/`. You may also want to delete `BepInEx/config/com.bungus.everythingiscrab.pastplayercharactersasmobs/` if you want to wipe saved snapshots.

## Author

Bungus

## Source

<https://github.com/rinpoche-peregrine/PastPlayerCharactersAsMobs>
