using System;
using System.Collections.Generic;
using System.Reflection;
using Unlockables.Genetics;
using World.Stats;

namespace PastPlayerCharactersAsMobsPlugin;

// Reads live game state into a BuildSnapshot.
public static class SnapshotBuilder {
	// PlayerStats properties to capture. Each one is an AStatValue<T> subclass — we use reflection
	// to call the generic .Value getter (returns float or int). HybridStatValue also gets its
	// PercentageMultiplier captured as a sibling stat.
	static readonly string[] StatProperties = {
		"DodgeChance",
		"DamageReductionPercentage",
		"EffectAreaSize",
		"DashCooldownModifier",
		"MovementAttackPenaltyReduction",
		"AdditionalDamageToAlphas",
		"FoodContributionMultiplier",
		"MeatFoodContributionMultiplier",
		"FishFoodContributionMultiplier",
		"VegetationFoodContributionMultiplier",
		"CarrionContributionMultiplier",
		"ProgressGainMultiplier",
		"StatChangeMultiplier",
		"TemporaryInvulnerabilityDuration",
		"Senses",
		"Social",
		"NumberOfStandardEvolutionChoices",
		"NumberOfBranchingEvolutionChoices",
		"UpgradeEvolutionDiscount",
		"RerollEvolutionDiscount",
		"ProgressToNextLevelReductionPercentage",
	};

	const BindingFlags PubInst = BindingFlags.Public | BindingFlags.Instance;

	public static BuildSnapshot Capture(int level, string runId) {
		var snap = new BuildSnapshot {
			Id = Guid.NewGuid().ToString("N"),
			RunId = runId,
			CapturedAt = DateTime.UtcNow,
			EvolutionLevel = level,
		};
		CaptureStats(snap);
		CaptureRunData(snap);
		return snap;
	}

	static void CaptureStats(BuildSnapshot snap) {
		try {
			var stats = PlayerStats.Instance;
			if (stats == null) {
				Plugin.Log.LogWarning("PlayerStats.Instance is null; skipping stat capture.");
				return;
			}
			var type = stats.GetType();
			foreach (var name in StatProperties) {
				try {
					var prop = type.GetProperty(name, PubInst);
					if (prop == null) continue;
					var statValueObj = prop.GetValue(stats);
					if (statValueObj == null) continue;
					if (TryReadStatValue(statValueObj, out var v)) snap.Stats[name] = v;
					// Hybrid stats: also pull PercentageMultiplier as a sibling
					if (TryReadFloatProperty(statValueObj, "PercentageMultiplier", out var pct))
						snap.Stats[$"{name}.PercentageMultiplier"] = pct;
				} catch (Exception ex) {
					Plugin.Log.LogWarning($"Stat '{name}' read failed: {ex.GetType().Name}: {ex.Message}");
				}
			}
		} catch (Exception ex) {
			Plugin.Log.LogError($"CaptureStats failed: {ex}");
		}
	}

	// Reads AStatValue<T>.Value via reflection. T is float or int for this game.
	static bool TryReadStatValue(object statValue, out float result) {
		result = 0f;
		var t = statValue.GetType();
		// 'Value' on AStatValue<T> — declared on the generic base, walk inheritance.
		var p = t.GetProperty("Value", PubInst);
		while (p == null && t.BaseType != null) {
			t = t.BaseType;
			p = t.GetProperty("Value", PubInst);
		}
		if (p == null) return false;
		var v = p.GetValue(statValue);
		return TryToFloat(v, out result);
	}

	static bool TryReadFloatProperty(object obj, string propName, out float result) {
		result = 0f;
		var p = obj.GetType().GetProperty(propName, PubInst);
		if (p == null) return false;
		return TryToFloat(p.GetValue(obj), out result);
	}

	static bool TryToFloat(object v, out float result) {
		switch (v) {
			case float f: result = f; return true;
			case int i: result = i; return true;
			case double d: result = (float)d; return true;
			case long l: result = l; return true;
			default: result = 0f; return false;
		}
	}

	static void CaptureRunData(BuildSnapshot snap) {
		try {
			var rd = RuntimeDataManager.RunData;
			if (rd == null) return;
			snap.Difficulty = (int)rd.Difficulty;
			snap.Genetic1 = rd.Genetic_1.ToString();
			try {
				var g2 = rd.Genetic_2;
				if (g2.HasValue) snap.Genetic2 = g2.Value.ToString();
			} catch { /* IL2CPP nullable wrapper can be flaky; tolerate */ }
		} catch (Exception ex) {
			Plugin.Log.LogError($"CaptureRunData failed: {ex}");
		}
	}
}
