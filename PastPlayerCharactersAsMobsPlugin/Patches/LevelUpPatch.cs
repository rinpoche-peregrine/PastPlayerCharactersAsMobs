using System;
using HarmonyLib;

namespace PastPlayerCharactersAsMobsPlugin.Patches;

// EvolutionHandler is in the global namespace (no namespace prefix).
[HarmonyPatch(typeof(EvolutionHandler), nameof(EvolutionHandler.IncreasePlayerEvolutionLevel))]
public static class LevelUpPatch {
	[HarmonyPostfix]
	public static void Postfix(EvolutionHandler __instance) {
		try {
			if (!RunState.InRun) return;
			var level = __instance.EvolutionLevel;
			var min = Plugin.Config.MinLevel.Value;
			var step = Plugin.Config.LevelsBetweenSnapshots.Value;
			if (level < min) return;
			if (step <= 0 || (level - min) % step != 0) return;

			var snap = SnapshotBuilder.Capture(level, RunState.CurrentRunId);
			Plugin.Store.BufferDuringRun(snap);
			Plugin.Log.LogInfo($"Buffered snapshot at level {level} (stats:{snap.Stats.Count}, buffer:{Plugin.Store.RunBufferCount})");
		} catch (Exception ex) {
			Plugin.Log.LogError($"LevelUpPatch failed: {ex}");
		}
	}
}
