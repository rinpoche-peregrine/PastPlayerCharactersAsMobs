# Past Player Characters as Mobs

<img src="Packaging/icon.png" width="160" align="right" alt="Past Player Characters as Mobs icon" />

A BepInEx mod for *Everything is Crab*. Every 5 player levels (starting at level 20) the mod saves a snapshot of your current build. In future runs, those past selves can spawn as Alpha mobs in the world.

## Current state

This is v0.1.0, the first phase. It only does the snapshot half. It records your builds and writes them to disk. Ghosts do not spawn yet. That ships in v0.2.0.

To verify it is working, after a run check `BepInEx/config/com.bungus.everythingiscrab.pastplayercharactersasmobs/snapshots.json`. It will contain your captured builds.

## Requirements

- *Everything is Crab* (tested 1.0.1__8213).
- BepInEx 6 BleedingEdge IL2CPP (CoreCLR), build 755 or newer.

## Install

1. Install BepInEx into your game folder.
2. Launch the game once and close it to generate `BepInEx/interop/`.
3. Download the latest zip from the [Releases](../../releases) page.
4. Extract the `plugins/Bungus-PastPlayerCharactersAsMobs/` folder into `BepInEx/plugins/`.
5. Launch and play normally.

## Features (v0.1.0)

- Snapshots your build every 5 player levels, starting at level 20.
- Stores snapshots as JSON. Inspectable and wipeable.
- On a winning run, saves every buffered snapshot. On a non-winning run, each snapshot is rolled at 25% to keep.
- 200 snapshots per level bucket. When a bucket fills, a random snapshot from the oldest half gets evicted.

## Roadmap

- v0.2.0: Spawn past selves as Alpha + Shiny mobs in the world.
- v0.3.0: Apply snapshot stats to the spawned ghosts so they fight with your old build.
- v0.4.0+: Specialisations, affinities, cosmetics in snapshots. Config tuning. Custom name plates.

## Credits

- Bungus
- Built with [BepInEx](https://github.com/BepInEx/BepInEx) and [HarmonyX](https://github.com/BepInEx/HarmonyX).
