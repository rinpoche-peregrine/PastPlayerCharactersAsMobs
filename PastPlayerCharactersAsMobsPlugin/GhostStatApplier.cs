using System;
using System.Reflection;
using World.Characters.Enemies;
using World.Stats;

namespace PastPlayerCharactersAsMobsPlugin;

// Applies a BuildSnapshot's stat values to a spawned enemy's EnemyStats object.
// Only stats whose names exist on EnemyStats (via AEntityStats inheritance) transfer.
// Player-only stats (DodgeChance, FoodContributionMultiplier, etc.) silently skipped.
public static class GhostStatApplier {
	const BindingFlags PubInst = BindingFlags.Public | BindingFlags.Instance;

	public static int Apply(IEnemyCharacter enemy, BuildSnapshot snap, float scale) {
		if (enemy == null || snap == null || snap.Stats == null || snap.Stats.Count == 0) return 0;
		AEnemyStats stats;
		try { stats = enemy.EnemyStats; } catch { return 0; }
		if (stats == null) return 0;

		int applied = 0;
		var type = stats.GetType();
		foreach (var kv in snap.Stats) {
			var name = kv.Key;
			if (string.IsNullOrEmpty(name)) continue;
			if (name.Contains(".")) continue; // sub-stats like "Social.PercentageMultiplier"; rare for shared fields
			try {
				var prop = type.GetProperty(name, PubInst);
				if (prop == null) continue;
				var statValue = prop.GetValue(stats);
				if (statValue == null) continue;
				if (SetStatValue(statValue, kv.Value * scale)) applied++;
			} catch (Exception ex) {
				Plugin.Log.LogWarning($"ApplyStat '{name}' failed: {ex.Message}");
			}
		}
		return applied;
	}

	// Sets the numeric backing of a SummedPercentage / Flat / FlatInt / HybridStatValue.
	// Uses the public _currentXxxValue field/property since it's settable and skips IL2CPP runtime invoke.
	static bool SetStatValue(object statValue, float newValue) {
		var t = statValue.GetType();
		var name = t.Name;
		try {
			if (name.Contains("SummedPercentage")) {
				return TrySet(statValue, t, "_currentPercentageValue", newValue)
					|| TrySet(statValue, t, "PercentageValue", newValue);
			}
			// FlatStatValue, FlatIntStatValue, HybridStatValue all have a numeric flat backing
			if (TrySet(statValue, t, "_currentFlatValue", newValue)) return true;
			if (TrySet(statValue, t, "FlatValue", newValue)) return true;
		} catch { }
		return false;
	}

	static bool TrySet(object obj, Type t, string propName, float value) {
		var p = t.GetProperty(propName, PubInst);
		if (p == null) return false;
		if (!p.CanWrite) return false;
		if (p.PropertyType == typeof(int)) p.SetValue(obj, (int)value);
		else if (p.PropertyType == typeof(float)) p.SetValue(obj, value);
		else if (p.PropertyType == typeof(double)) p.SetValue(obj, (double)value);
		else return false;
		return true;
	}
}
