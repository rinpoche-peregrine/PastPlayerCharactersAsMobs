using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using World.Evolutions.Abilities.Attacks;

namespace PastPlayerCharactersAsMobsPlugin.Patches;

// When an EICCharacterAttack begins, fire the player's attack-animation triggers on the
// cloned ghost visual's animators (if the attack belongs to a ghost-tagged enemy).
[HarmonyPatch]
public static class AttackForwardPatch {
	static int _readyHash, _headHash, _armsHash, _lowerHash, _rangedHash;
	static bool _hashesResolved;
	static int _diagFireCount;
	static int _diagGhostCount;
	const int DiagLogEvery = 25; // log a summary every 25 attacks to keep noise down

	static MethodBase TargetMethod() {
		const BindingFlags F = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
		foreach (var m in typeof(EICCharacterAttack).GetMethods(F)) {
			if (m.Name == "StartAttackMainLoop" && m.GetParameters().Length == 0) return m;
		}
		throw new Exception("Could not locate EICCharacterAttack.StartAttackMainLoop()");
	}

	[HarmonyPostfix]
	public static void Postfix(EICCharacterAttack __instance) {
		try {
			if (__instance == null) return;

			if (!_hashesResolved) {
				try {
					_readyHash = EICCharacterAttack.ATTACK_READY_ANIM_HASH;
					_headHash = EAttackAnimationTypeExt.HEAD_ANIMATION_ATTACK_PARAM;
					_armsHash = EAttackAnimationTypeExt.ARMS_ANIMATION_ATTACK_PARAM;
					_lowerHash = EAttackAnimationTypeExt.LOWER_BODY_ANIMATION_ATTACK_PARAM;
					_rangedHash = EAttackAnimationTypeExt.RANGED_ANIMATION_ATTACK_PARAM;
					_hashesResolved = true;
					Plugin.Log.LogInfo($"Attack hashes resolved: ready={_readyHash} head={_headHash} arms={_armsHash} lower={_lowerHash} ranged={_rangedHash}");
				} catch (Exception ex) {
					Plugin.Log.LogWarning($"Failed to read attack hashes: {ex.Message}");
					return;
				}
			}

			Component attackComp;
			try { attackComp = __instance.TryCast<Component>(); } catch { return; }
			if (attackComp == null) return;

			_diagFireCount++;
			var entry = GhostRegistry.Find(attackComp.transform);
			if (entry == null) {
				if ((_diagFireCount % DiagLogEvery) == 0)
					Plugin.Log.LogInfo($"AttackForward: {_diagFireCount} attacks seen, {_diagGhostCount} on ghosts ({(attackComp.transform != null ? attackComp.transform.root.name : "?")})");
				return;
			}
			_diagGhostCount++;

			// Fire ALL the attack-trigger candidates. Animator state machine
			// transitions will only react to triggers whose conditions match.
			var anims = entry.CloneAnimators;
			if (anims == null) return;
			var nameHashes = PlayerAttackHashes();
			for (int i = 0; i < anims.Length; i++) {
				var a = anims[i];
				if (a == null) continue;
				try {
					// Old hash-based triggers (kept for any state machine that might use them)
					a.SetTrigger(_readyHash);
					a.SetTrigger(_headHash);
					a.SetTrigger(_armsHash);
					a.SetTrigger(_lowerHash);
					a.SetTrigger(_rangedHash);
					// Real attack trigger names confirmed from runtime animator dump
					for (int j = 0; j < nameHashes.Length; j++) a.SetTrigger(nameHashes[j]);
					PulseAttackBools(a);
					PlayAttackClip(a);
				} catch { /* skip */ }
			}
		} catch (Exception ex) {
			Plugin.Log.LogWarning($"AttackForwardPatch.Postfix failed: {ex.Message}");
		}
	}
	// Cache attack-related Bool parameter hashes per animator so we don't re-scan every attack.
	static readonly System.Collections.Generic.Dictionary<Animator, int[]> _attackBoolsCache = new();
	static readonly string[] AttackBoolNameFragments = {
		"attack", "swing", "swinging", "charge", "wind", "fire", "shoot",
		"stab", "bite", "lunge", "throw", "punch", "kick", "slash", "ranged",
	};
	const float AttackPulseSeconds = 0.6f;

	static void PulseAttackBools(Animator a) {
		if (!_attackBoolsCache.TryGetValue(a, out var hashes)) {
			var list = new System.Collections.Generic.List<int>();
			try {
				foreach (var p in a.parameters) {
					if (p.type != AnimatorControllerParameterType.Bool) continue;
					var lname = p.name.ToLowerInvariant();
					for (int i = 0; i < AttackBoolNameFragments.Length; i++) {
						if (lname.Contains(AttackBoolNameFragments[i])) { list.Add(p.nameHash); break; }
					}
				}
			} catch { }
			hashes = list.ToArray();
			_attackBoolsCache[a] = hashes;
		}
		for (int i = 0; i < hashes.Length; i++) AnimatorBoolPulse.Pulse(a, hashes[i], AttackPulseSeconds);
	}
	// State-hash cache per animator for attack-named clips. State name often equals clip name in
	// this game's controllers, so hashing the clip name as a state name and calling Play() works.
	static readonly System.Collections.Generic.Dictionary<Animator, int[]> _attackClipHashes = new();
	static readonly string[] AttackClipNameFragments = {
		"attack", "swing", "swinging", "shoot", "fire", "stab", "bite", "lunge",
		"throw", "punch", "kick", "slash", "ranged", "charge",
	};

	static void PlayAttackClip(Animator a) {
		if (a == null || a.runtimeAnimatorController == null) return;
		if (!_attackClipHashes.TryGetValue(a, out var hashes)) {
			var list = new System.Collections.Generic.List<int>();
			var seenNames = new System.Collections.Generic.List<string>();
			try {
				var clips = a.runtimeAnimatorController.animationClips;
				if (clips != null) {
					foreach (var c in clips) {
						if (c == null || string.IsNullOrEmpty(c.name)) continue;
						var lname = c.name.ToLowerInvariant();
						for (int i = 0; i < AttackClipNameFragments.Length; i++) {
							if (lname.Contains(AttackClipNameFragments[i])) {
								// Cache multiple plausible state-name hashes per clip — the actual
								// state often has the clip's name with prefix/suffix variations.
								AddVariations(list, seenNames, c.name);
								break;
							}
						}
					}
				}
			} catch { }
			hashes = list.ToArray();
			_attackClipHashes[a] = hashes;
			if (seenNames.Count > 0)
				Plugin.Log.LogInfo($"  cached attack states for '{a.runtimeAnimatorController.name}': {string.Join(", ", seenNames)}");
		}
		if (hashes.Length == 0) return;
		// Try CrossFade on every layer with every cached variation. CrossFade is a no-op
		// for unknown state hashes, so this is safe; whichever combination matches wins.
		int layerCount = 1;
		try { layerCount = a.layerCount; } catch { }
		bool isPlayerAC = false;
		try { isPlayerAC = a.runtimeAnimatorController.name == "PlayerCharacter_AC"; } catch { }
		for (int li = 0; li < layerCount; li++) {
			for (int hi = 0; hi < hashes.Length; hi++) {
				try { a.CrossFadeInFixedTime(hashes[hi], 0.08f, li); } catch { }
			}
		}
	}

	static void AddVariations(System.Collections.Generic.List<int> hashes, System.Collections.Generic.List<string> names, string clipName) {
		void Add(string s) {
			if (string.IsNullOrEmpty(s)) return;
			if (names.Contains(s)) return;
			names.Add(s);
			hashes.Add(Animator.StringToHash(s));
		}
		Add(clipName);
		// Strip "PlayerCharacter_" prefix
		const string Prefix = "PlayerCharacter_";
		if (clipName.StartsWith(Prefix, System.StringComparison.OrdinalIgnoreCase))
			Add(clipName.Substring(Prefix.Length));
		// Strip "_Anim" suffix
		if (clipName.EndsWith("_Anim", System.StringComparison.OrdinalIgnoreCase))
			Add(clipName.Substring(0, clipName.Length - "_Anim".Length));
		// Try the base name (e.g. "HeadAttack" -> "Attack_Head" swap)
		var lower = clipName.ToLowerInvariant();
		if (lower.Contains("headattack")) Add(clipName.Replace("HeadAttack", "Attack_Head"));
		if (lower.Contains("armsattack")) Add(clipName.Replace("ArmsAttack", "Attack_Arms"));
		if (lower.Contains("lowerbodyattack")) Add(clipName.Replace("LowerBodyAttack", "Attack_LowerBody"));
		if (lower.Contains("rangedattack")) Add(clipName.Replace("RangedAttack", "Attack_Ranged"));
	}
	// Actual attack trigger names on PlayerCharacter_AC, confirmed from the runtime animator dump.
	// We fire them all because we don't yet know which one the snapshot's build would use; the state
	// machine only responds to triggers it has transitions for, the others are harmlessly consumed.
	static readonly string[] PlayerAttackTriggerNames = {
		"HeadAttack", "ArmsAttack", "LowerBodyAttack", "RangedAttack",
		"Attack_Pincers", "Attack_Beak", "Attack_Stinger", "Attack_Antlers",
		"Attack_Horns", "Attack_Jaws", "Attack_Claws", "Attack_PistolPincer",
		"Attack_ToeBeans", "Attack_Leech", "Attack_Trunk",
	};
	static int[] _playerAttackHashes;

	static int[] PlayerAttackHashes() {
		if (_playerAttackHashes == null) {
			_playerAttackHashes = new int[PlayerAttackTriggerNames.Length];
			for (int i = 0; i < PlayerAttackTriggerNames.Length; i++)
				_playerAttackHashes[i] = Animator.StringToHash(PlayerAttackTriggerNames[i]);
		}
		return _playerAttackHashes;
	}
}
