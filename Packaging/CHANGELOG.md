# Changelog

## 0.1.0 — Phase 1

- Initial release.
- Snapshot capture every 5 player levels (starting at level 20).
- Run-end commit logic: 100% on win, 25% per snapshot on loss.
- JSON persistence with 200-per-level-bucket retention and random-from-oldest-50% eviction.
- No ghost spawning yet — see roadmap.
