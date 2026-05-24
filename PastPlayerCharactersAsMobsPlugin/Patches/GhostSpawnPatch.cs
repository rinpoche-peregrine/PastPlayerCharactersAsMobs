using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using World.Characters.Enemies;

namespace PastPlayerCharactersAsMobsPlugin.Patches;

// Prefix on EnemyDirector.CreateEnemy(GridCellId, EEnemyArchetype, EEnemyRank, EEnemyShinyState, bool).
// On the dice-roll, mutates rank+shiny so the enemy spawns as Alpha+Shiny.
// Optionally also overrides archetype to ensure the spawn is visible on land.
// Postfix logs the spawn result so we can confirm something actually appeared.
[HarmonyPatch]
public static class GhostSpawnPatch {
	static readonly System.Random _rng = new();
	static bool _lastTriggered;
	static BuildSnapshot _lastPick = null;
	static EEnemyArchetype _lastOriginalArchetype;

	static MethodBase TargetMethod() {
		foreach (var m in typeof(EnemyDirector).GetMethods()) {
			if (m.Name != "CreateEnemy") continue;
			var ps = m.GetParameters();
			if (ps.Length == 5
				&& ps[1].ParameterType == typeof(EEnemyArchetype)
				&& ps[2].ParameterType == typeof(EEnemyRank)
				&& ps[3].ParameterType == typeof(EEnemyShinyState))
				return m;
		}
		throw new Exception("Could not locate EnemyDirector.CreateEnemy(GridCellId, EEnemyArchetype, EEnemyRank, EEnemyShinyState, bool)");
	}

	[HarmonyPrefix]
	public static void Prefix(EEnemyArchetype archetype, ref EEnemyRank rank, ref EEnemyShinyState shinyState) {
		_lastTriggered = false;
		_lastPick = null;
		_lastOriginalArchetype = archetype;
		try {
			if (!RunState.InRun) return;
			if (!Plugin.Config.EnableGhostSpawning.Value) return;
			if (rank != EEnemyRank.Basic) return;

			var level = TryGetPlayerLevel();
			if (level <= 0) return;

			var matches = Plugin.Store.ForLevel(level, Plugin.Config.GhostLevelTolerance.Value);
			if (matches.Count == 0) return;

			if (_rng.NextDouble() >= Plugin.Config.GhostSpawnChance.Value) return;

			var pick = matches[_rng.Next(matches.Count)];
			rank = EEnemyRank.Alpha;
			shinyState = EEnemyShinyState.Shiny;
			_lastTriggered = true;
			_lastPick = pick;

			Plugin.Log.LogInfo($"Past Self promoting spawn: archetype={archetype}, snapshot lv{pick.EvolutionLevel} genetic={pick.Genetic1} run={pick.RunId[..8]}");
		} catch (Exception ex) {
			Plugin.Log.LogError($"GhostSpawnPatch.Prefix failed: {ex}");
		}
	}

	[HarmonyPostfix]
	public static void Postfix(IEnemyCharacter __result) {
		if (!_lastTriggered) return;
		try {
			if (__result == null) {
				Plugin.Log.LogWarning($"Past Self spawn returned NULL (archetype {_lastOriginalArchetype}, snapshot lv{(_lastPick != null ? _lastPick.EvolutionLevel : 0)}). The game refused the spawn; try increasing GhostSpawnChance and let more dice roll.");
				return;
			}
			Vector3 pos = default;
			string nameInfo = "(no transform)";
			try {
				var comp = __result.TryCast<UnityEngine.Component>();
				if (comp != null) {
					nameInfo = comp.gameObject.name;
					pos = comp.transform.position;
				}
			} catch { }
			float? distFromPlayer = null;
			try {
				var pc = World.Characters.PlayerCharacter.Instance;
				if (pc != null) {
					var pcomp = pc.TryCast<UnityEngine.Component>();
					if (pcomp != null) distFromPlayer = Vector3.Distance(pcomp.transform.position, pos);
				}
			} catch { }
			Plugin.Log.LogInfo(
				$"Past Self spawned: name={nameInfo} pos=({pos.x:F1},{pos.y:F1},{pos.z:F1})"
				+ (distFromPlayer.HasValue ? $" dist_from_player={distFromPlayer:F1}" : "")
			);
		} catch (Exception ex) {
			Plugin.Log.LogError($"GhostSpawnPatch.Postfix failed: {ex}");
		} finally {
			_lastTriggered = false;
			_lastPick = null;
		}
	}

	static int TryGetPlayerLevel() {
		try {
			var pc = World.Characters.PlayerCharacter.Instance;
			if (pc == null) return -1;
			var eh = pc._evolutionHandler;
			if (eh == null) return -1;
			return eh.EvolutionLevel;
		} catch { return -1; }
	}
}
