# Past Player Characters as Mobs — Everything is Crab

<img src="Packaging/icon.png" width="160" align="right" alt="Past Player Characters as Mobs icon" />

A BepInEx mod for *Everything is Crab* by Odd Dreams Digital.

## Description

Roguelite runs are usually one-shot stories — your build dies with you. This mod remembers them. Every 5 player levels (starting at level 20) your current build is captured. In future runs, when you reach a similar level, there's a chance one of your past selves will spawn as an Alpha mob roaming the world — same stats, same evolutions, ready to fight you.

The more runs you complete, the bigger your library of past selves gets, and the more variety you'll encounter.

## Current state

**v0.1.0 is Phase 1 of a multi-release project.** This release only does the snapshot capture half — recording your builds and persisting them to disk. **No ghosts spawn yet.** That's the next release.

You won't see any visible in-game change with v0.1.0. To verify it's working, after a run check `BepInEx/config/com.bungus.everythingiscrab.pastplayercharactersasmobs/snapshots.json` — it should contain your captured builds.

## Installation instructions

1. Install **BepInEx 6 BleedingEdge IL2CPP (CoreCLR)** build 755 or newer from [builds.bepinex.dev](https://builds.bepinex.dev/projects/bepinex_be). Extract into your *Everything is Crab* install folder.
2. Launch the game once and close it (lets BepInEx generate `BepInEx/interop/`).
3. Download the latest `Bungus-PastPlayerCharactersAsMobs-*.zip` from the [Releases](../../releases) page.
4. Extract; drop the `plugins/Bungus-PastPlayerCharactersAsMobs/` folder into `BepInEx/plugins/`.
5. Launch the game and play normally.

## Main features

- Snapshots your build every 5 player levels, starting at level 20
- Persists to JSON in BepInEx config dir — fully inspectable, fully wipeable
- Win-aware: all snapshots saved on a winning run, only ~25% per snapshot on a losing run
- 200 snapshots per level bucket, automatically pruning the oldest 50% to make room

## Requirements

- **Everything is Crab** (tested on 1.0.1__8213)
- **BepInEx 6 BleedingEdge IL2CPP (CoreCLR)** — build 755 or newer

## Roadmap

- **v0.2** — Spawn placeholder past-self ghosts as Alpha+Shiny mobs in the world
- **v0.3** — Apply snapshot stats to the spawned ghosts
- **v0.4** — Per-snapshot names, config tuning, additional snapshot data (specialisations, affinities)

## Credits

- **Bungus** — mod author
- Built with [BepInEx](https://github.com/BepInEx/BepInEx) and [HarmonyX](https://github.com/BepInEx/HarmonyX)
