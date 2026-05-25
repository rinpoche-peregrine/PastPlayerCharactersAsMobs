using System;
using System.Collections.Generic;
using System.Reflection;
using Cosmetics;
using World;
using World.Characters;
using World.Evolutions.Affinities;
using World.Evolutions.Specializations;
using World.Evolutions;
using World.Stats;

namespace PastPlayerCharactersAsMobsPlugin;

// Reads live game state into a BuildSnapshot.
public static class SnapshotBuilder {
	// PlayerStats properties to capture. Each one is an AStatValue<T> subclass — we use reflection
	// to call the generic .Value getter (returns float or int). HybridStatValue also gets its
	// PercentageMultiplier captured as a sibling stat.
	static readonly string[] StatProperties = {
		// AEntityStats — shared between player and enemy; useful for Phase 3 stat transfer to ghosts
		"MaxHp",
		"HpRegeneration",
		"BasePhysicalDamage",
		"BaseAbilityDamage",
		"GeneralDamageMultiplier",
		"MovementSpeed",
		"SprintSpeedMultiplier",
		"Size",
		"AttackAreaModifier",
		"CooldownModifier",
		// PlayerStats-only — captured for completeness, ignored by enemy applier
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
		CaptureAffinities(snap);
		CaptureSpecialisations(snap);
		CaptureCosmetics(snap);
		CaptureAbilityPicks(snap);
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
	static void CaptureAffinities(BuildSnapshot snap) {
		try {
			var ws = WorldStats.Instance;
			if (ws == null) return;
			var affs = ws.CurrentAffinityPoints;
			if (affs == null) return;
			// Iterate EAffinity values via GetAffinityScore for safety; record non-zero entries.
			foreach (EAffinity a in System.Enum.GetValues(typeof(EAffinity))) {
				if (a == EAffinity.None) continue;
				int score;
				try { score = affs.GetAffinityScore(a); } catch { continue; }
				if (score != 0) snap.Affinities[a.ToString()] = score;
			}
		} catch (Exception ex) {
			Plugin.Log.LogWarning($"CaptureAffinities failed: {ex.Message}");
		}
	}

	static void CaptureSpecialisations(BuildSnapshot snap) {
		try {
			var pc = PlayerCharacter.Instance;
			if (pc == null) return;
			var eh = pc._evolutionHandler;
			if (eh == null) return;
			foreach (ESpecializationAbilityId id in System.Enum.GetValues(typeof(ESpecializationAbilityId))) {
				if (id == ESpecializationAbilityId.Invalid) continue;
				try { if (eh.HasSpecialization(id)) snap.Specialisations.Add(id.ToString()); }
				catch { /* skip */ }
			}
		} catch (Exception ex) {
			Plugin.Log.LogWarning($"CaptureSpecialisations failed: {ex.Message}");
		}
	}

	static void CaptureCosmetics(BuildSnapshot snap) {
		try {
			var cm = CosmeticsRuntimeManager.Instance;
			if (cm == null) return;
			var dict = cm._currentlyEquippedCosmetics;
			if (dict == null) return;
			foreach (var kv in dict) {
				if (kv.Value == ECosmetic.None) continue;
				snap.Cosmetics[kv.Key.ToString()] = kv.Value.ToString();
			}
		} catch (Exception ex) {
			Plugin.Log.LogWarning($"CaptureCosmetics failed: {ex.Message}");
		}
	}
	static void CaptureAbilityPicks(BuildSnapshot snap) {
		try {
			var pc = PlayerCharacter.Instance;
			if (pc == null) return;
			var eh = pc._evolutionHandler;
			if (eh == null) return;
			foreach (World.Evolutions.EEvolutionAbilityId id in System.Enum.GetValues(typeof(World.Evolutions.EEvolutionAbilityId))) {
				if ((int)id == 0) continue; // Invalid
				try {
					int lvl = 0;
					bool has = false;
					try { has = eh.HasEvolution(id, out lvl); } catch { continue; }
					if (!has || lvl <= 0) continue;
					General.ERarity rarity = General.ERarity.Common;
					try { eh.TryGetRarityForAbility(id, out rarity); } catch { }
					snap.AbilityPicks.Add(new AbilityPick {
						AbilityId = id.ToString(),
						Rarity = rarity.ToString(),
						Level = lvl,
					});
				} catch { /* skip individual */ }
			}
		} catch (System.Exception ex) {
			Plugin.Log.LogWarning($"CaptureAbilityPicks failed: {ex.Message}");
		}
	}
}
