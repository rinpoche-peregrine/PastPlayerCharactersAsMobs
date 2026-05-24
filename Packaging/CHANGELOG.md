# Changelog

## 0.2.0 (Phase 2)

- Past selves can now spawn in the world. Regular enemy spawns have a small chance to be promoted to Alpha + Shiny and represent a saved snapshot.
- Spawn rate, level tolerance, and the master switch are all in the BepInEx config.
- The promoted enemy is visually distinct (Alpha + Shiny) but does not yet use the snapshot's stats. That comes in v0.3.0.

## 0.1.1

- Internal cleanup.

## 0.1.0 (Phase 1)

- Snapshot capture every 5 player levels starting at level 20.
- Run-end commit. 100% on a winning run. 25% per snapshot on a non-winning run.
- 200 per level bucket. Evicts random from the oldest 50%.
- No ghost spawning yet. See roadmap in the GitHub README.
