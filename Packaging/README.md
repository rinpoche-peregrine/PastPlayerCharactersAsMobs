# Past Player Characters as Mobs

A BepInEx mod for *Everything is Crab* by Odd Dreams Digital.

## Current state (v0.1.0 — Phase 1)

**Snapshot capture only.** Every 5 levels (starting at level 20), the mod buffers a snapshot of your current build. On run end, snapshots are committed to disk (all on win, 25% per snapshot on loss). **Ghost spawning is NOT in this release** — that's Phase 2.

You won't see any in-game effect yet. Verify the mod is working by checking that `BepInEx/config/com.bungus.everythingiscrab.pastplayercharactersasmobs/snapshots.json` accumulates entries.

## Requirements

- *Everything is Crab* (tested 1.0.1__8213)
- BepInEx 6 BleedingEdge IL2CPP (CoreCLR), build 755+

## Install

Drop `plugins/Bungus-PastPlayerCharactersAsMobs/` into your `BepInEx/plugins/` folder, or use a mod manager's "Install from file".

## Author

Bungus
