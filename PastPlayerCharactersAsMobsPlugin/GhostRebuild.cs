using System;
using General;
using Il2CppInterop.Runtime;
using UnityEngine;
using World.Characters;
using World.Characters.Enemies;
using World.Evolutions;
using World.Stats;

namespace PastPlayerCharactersAsMobsPlugin;

// Builds a past-self visual by:
//  1. Cloning ONLY the live player's _visualController GameObject (not PlayerCharacter itself).
//  2. Creating a SimulatedEvolutionHandler and replaying snapshot evolutions into it.
//  3. Attaching a GhostVisualShim (implements IVisualStatCharacter) wrapping the sim handler.
//  4. Pointing the cloned VisualController._owningVisualStatCharacter at the shim.
//  5. Calling RefreshVisuals() so the game's own pipeline rebuilds the visual.
// Avoids: PlayerCharacter singleton violation, ApplyEvolution side-effects on live stats/achievements.
public static class GhostRebuild {
	const string MarkerName = "PPC_GhostVisual";

	public static GameObject Apply(IEnemyCharacter enemy, BuildSnapshot snap) {
		try {
			if (enemy == null || snap == null) return null;
			Component enemyComp;
			try { enemyComp = enemy.TryCast<Component>(); } catch { return null; }
			if (enemyComp == null) return null;

			var live = PlayerCharacter.Instance;
			if (live == null) { Plugin.Log.LogWarning("GhostRebuild: no live PlayerCharacter.Instance"); return null; }

			var liveVC = live._visualController;
			if (liveVC == null) { Plugin.Log.LogWarning("GhostRebuild: live player has no _visualController"); return null; }

			GameObject liveVCGO = null;
			try { liveVCGO = liveVC.gameObject; } catch { }
			if (liveVCGO == null) { Plugin.Log.LogWarning("GhostRebuild: live VC has no gameObject"); return null; }

			foreach (var sr in enemyComp.GetComponentsInChildren<SpriteRenderer>(true)) {
				if (sr != null) sr.enabled = false;
			}

			var instance = UnityEngine.Object.Instantiate(liveVCGO);
			instance.name = MarkerName;

			var clonedVC = instance.GetComponent<VisualController>();
			if (clonedVC == null) {
				Plugin.Log.LogWarning("GhostRebuild: cloned object has no VisualController component");
				UnityEngine.Object.Destroy(instance);
				return null;
			}

			var sim = new SimulatedEvolutionHandler();
			int applied = 0, failed = 0;
			if (snap.AbilityPicks != null) {
				foreach (var pick in snap.AbilityPicks) {
					if (pick == null || string.IsNullOrEmpty(pick.AbilityId)) continue;
					try {
						if (!Enum.TryParse<EEvolutionAbilityId>(pick.AbilityId, out var abilityId)) { failed++; continue; }
						var rarity = ERarity.Common;
						Enum.TryParse<ERarity>(pick.Rarity ?? "Common", out rarity);
						var data = new EvolutionUpgradeData(abilityId, rarity, 1);
						int levels = pick.Level < 1 ? 1 : pick.Level;
						for (int i = 0; i < levels; i++) {
							sim.ApplyEvolution(data, shouldIncreaseLevel: true);
						}
						applied++;
					} catch (Exception ex) {
						failed++;
						Plugin.Log.LogWarning($"sim.ApplyEvolution failed for {pick.AbilityId}@{pick.Rarity}: {ex.Message}");
					}
				}
			}
			Plugin.Log.LogInfo($"GhostRebuild: sim applied={applied} failed={failed}");

			var shim = instance.AddComponent<GhostVisualShim>();
			shim.Sim = sim;
			try { shim.BorrowedFactDB = live.FactDB; } catch { }
			try {
				var liveStats = live.Stats;
				if (liveStats != null) shim.BorrowedStats = liveStats.TryCast<AEntityStats>();
			} catch { }
			try { shim.BorrowedStatusController = live.StatusController; } catch { }

			try { clonedVC._owningVisualStatCharacter = shim.TryCast<IVisualStatCharacter>(); }
			catch (Exception ex) { Plugin.Log.LogWarning($"set _owningVisualStatCharacter failed: {ex.Message}"); }
			try { clonedVC._isRealGameplayVisuals = false; } catch { }
			try { clonedVC.RefreshVisuals(); } catch (Exception ex) { Plugin.Log.LogWarning($"RefreshVisuals threw: {ex.Message}"); }

			instance.transform.SetParent(enemyComp.transform, worldPositionStays: false);
			instance.transform.localPosition = Vector3.zero;
			instance.transform.localRotation = Quaternion.identity;
			return instance;
		} catch (Exception ex) {
			Plugin.Log.LogError($"GhostRebuild.Apply failed: {ex}");
			return null;
		}
	}
}
