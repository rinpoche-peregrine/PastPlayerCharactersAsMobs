# Changelog

## 1.0.0

- Ghosts now render via the game's own VisualController + SimulatedEvolutionHandler pipeline. Past selves get the full PlayerCharacter animator (walk, attack, eat, all 76 params) and a visual rebuilt from the snapshot's evolutions, with zero side effects on the live player. No more singleton violation, no more stat/achievement pollution.
- Removed deprecated config keys: UseFullPlayerClone, TintGhostAppearance, ShowCosmeticBadges. Half-measure visual paths gone.
- Fixed sprite flip direction (was inverted).

## 0.7.0 (Phase 7)

- Captured ERarity per ability in snapshots so visuals could be rebuilt later.
- Reworked the rebuild attempt around the game's own evolution-application pipeline (later superseded in 1.0.0).


## 0.6.0 (Phase 6)

- Cosmetic icons from the snapshot now float above each spawned ghost. Looking at a ghost tells you which cosmetics it was wearing in life (GhostCrab, BoxTurtle, Goat, etc.).
- New config: ShowCosmeticBadges (default true) to toggle the badge layer.

## 0.5.0 (Phase 5)

- Ghosts tinted by snapshot genetic.
- Spawn notifications off by default.

## 0.4.0 (Phase 4)

- Snapshots record affinities, specialisations, cosmetics.

## 0.3.0 (Phase 3)

- Past selves inherit stats from the snapshot.

## 0.2.0 (Phase 2)

- Past selves spawn as Alpha+Shiny mobs.

## 0.1.0 (Phase 1)

- Snapshot capture every 5 levels.
