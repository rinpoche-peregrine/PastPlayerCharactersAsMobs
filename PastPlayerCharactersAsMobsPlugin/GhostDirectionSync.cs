using System;
using System.Collections.Generic;
using UnityEngine;
using World.Evolutions.Abilities.Attacks;

namespace PastPlayerCharactersAsMobsPlugin;

// Drives the reconstructed visual's animators based on the parent enemy's velocity.
// Reads the game's actual animator-parameter hashes from SetFacingDirectionAnimationParam
// so we drive the same int parameter the original visual uses.
// Sprite flipping for left/right is done directly on each SpriteRenderer.
public class GhostDirectionSync : MonoBehaviour {
	const int MaxAnimators = 8;
	const float StationarySpeedThreshold = 0.05f;
	const float MinDeltaTime = 0.0001f;
	const float VerticalDominanceMinAngle = 30f;

	static int _facingParamHash = -1;
	static int _dirStraight, _dirUp, _dirDown;
	static bool _facingResolved;

	static readonly int WalkingHash = Animator.StringToHash("Walking");
	static readonly int SpeedHash = Animator.StringToHash("Speed");
	static readonly int IdleHash = Animator.StringToHash("Idle");

	struct AnimTarget {
		public Animator A;
		public bool HasFacingFloat;
		public bool HasFacingInt;
		public bool HasWalking;
		public bool HasSpeed;
		public bool HasIdle;
	}

	AnimTarget[] _targets = Array.Empty<AnimTarget>();
	int _targetCount;
	SpriteRenderer[] _flipRenderers = Array.Empty<SpriteRenderer>();
	int _flipCount;
	Transform _parent;
	Vector3 _lastPos;
	float _lastTime;
	int _lastDirValue = int.MinValue;
	bool _lastFlipX;

	public GhostDirectionSync(IntPtr ptr) : base(ptr) { }

	void Start() {
		try {
			ResolveFacingConstants();
			_parent = transform.parent;
			if (_parent != null) _lastPos = _parent.position;
			_lastTime = Time.time;

			var animators = GetComponentsInChildren<Animator>(true);
			if (animators != null && animators.Length > 0) {
				var list = new List<AnimTarget>();
				foreach (var a in animators) {
					if (a == null || a.runtimeAnimatorController == null) continue;
					var t = new AnimTarget { A = a };
					try {
						foreach (var p in a.parameters) {
							if (p.type == AnimatorControllerParameterType.Float && _facingResolved && p.nameHash == _facingParamHash) t.HasFacingFloat = true;
							else if (p.type == AnimatorControllerParameterType.Int && _facingResolved && p.nameHash == _facingParamHash) t.HasFacingInt = true;
							else if (p.type == AnimatorControllerParameterType.Bool && p.nameHash == WalkingHash) t.HasWalking = true;
							else if (p.type == AnimatorControllerParameterType.Float && p.nameHash == SpeedHash) t.HasSpeed = true;
							else if (p.type == AnimatorControllerParameterType.Bool && p.nameHash == IdleHash) t.HasIdle = true;
						}
					} catch { continue; }
					if (t.HasFacingFloat || t.HasFacingInt || t.HasWalking || t.HasSpeed || t.HasIdle) {
						list.Add(t);
						if (list.Count >= MaxAnimators) break;
					}
				}
				_targets = list.ToArray();
				_targetCount = _targets.Length;
			}

			var renderers = GetComponentsInChildren<SpriteRenderer>(true);
			if (renderers != null) {
				var flipList = new List<SpriteRenderer>(renderers.Length);
				foreach (var sr in renderers) {
					if (sr == null) continue;
					flipList.Add(sr);
				}
				_flipRenderers = flipList.ToArray();
				_flipCount = _flipRenderers.Length;
			}

			if (animators != null) {
				for (int ai = 0; ai < Math.Min(3, animators.Length); ai++) {
					var a = animators[ai];
					if (a == null || a.runtimeAnimatorController == null) continue;
					try {
						var paramNames = new List<string>();
						foreach (var p in a.parameters) paramNames.Add($"{p.name}:{p.type}");
						Plugin.Log.LogInfo($"  anim[{ai}] controller='{a.runtimeAnimatorController.name}' params=[{string.Join(", ", paramNames)}]");
					} catch (Exception ex) {
						Plugin.Log.LogWarning($"anim[{ai}] params dump failed: {ex.Message}");
					}
				}
			}
			Plugin.Log.LogInfo($"GhostDirectionSync: animators_with_match={_targetCount}, renderers_to_flip={_flipCount}, facing_resolved={_facingResolved}");
			if (_targetCount == 0 && _flipCount == 0) enabled = false;
		} catch (Exception ex) {
			Plugin.Log.LogWarning($"GhostDirectionSync.Start failed: {ex.Message}");
			enabled = false;
		}
	}

	static void ResolveFacingConstants() {
		if (_facingResolved) return;
		try {
			_facingParamHash = SetFacingDirectionAnimationParam.FACING_DIRECTION_ANIMATION_PARAM;
			_dirStraight = SetFacingDirectionAnimationParam.DIRECTION_STRAIGHT;
			_dirUp = SetFacingDirectionAnimationParam.DIRECTION_UP;
			_dirDown = SetFacingDirectionAnimationParam.DIRECTION_DOWN;
			_facingResolved = true;
			Plugin.Log.LogInfo($"Facing constants resolved: hash={_facingParamHash} straight={_dirStraight} up={_dirUp} down={_dirDown}");
		} catch (Exception ex) {
			Plugin.Log.LogWarning($"Couldn't resolve facing constants: {ex.Message}. Using fallback values.");
			_facingParamHash = Animator.StringToHash("FacingDirection");
			_dirStraight = 0; _dirUp = 1; _dirDown = -1;
			_facingResolved = true;
		}
	}

	void LateUpdate() {
		AnimatorBoolPulse.Tick();
		if (_parent == null) return;
		var now = Time.time;
		var dt = now - _lastTime;
		if (dt <= MinDeltaTime) return;

		var pos = _parent.position;
		var deltaX = pos.x - _lastPos.x;
		var deltaY = pos.y - _lastPos.y;
		_lastPos = pos;
		_lastTime = now;

		var invDt = 1f / dt;
		var vx = deltaX * invDt;
		var vy = deltaY * invDt;
		var speed = Mathf.Sqrt(vx * vx + vy * vy);
		var moving = speed > StationarySpeedThreshold;

		int dirValue = _dirStraight;
		if (moving) {
			var angleFromX = Mathf.Abs(Mathf.Atan2(vy, vx) * Mathf.Rad2Deg);
			if (angleFromX > 90f) angleFromX = 180f - angleFromX;
			if (angleFromX > 90f - VerticalDominanceMinAngle) {
				dirValue = vy > 0f ? _dirUp : _dirDown;
			} else {
				dirValue = _dirStraight;
			}
		}

		bool dirChanged = dirValue != _lastDirValue;
		_lastDirValue = dirValue;
		for (int i = 0; i < _targetCount; i++) {
			var t = _targets[i];
			if (t.A == null) continue;
			if (dirChanged && t.HasFacingInt) t.A.SetInteger(_facingParamHash, dirValue);
			if (dirChanged && t.HasFacingFloat) t.A.SetFloat(_facingParamHash, (float)dirValue);
			if (t.HasWalking) t.A.SetBool(WalkingHash, moving);
			if (t.HasIdle) t.A.SetBool(IdleHash, !moving);
			if (t.HasSpeed) t.A.SetFloat(SpeedHash, speed);
		}

		// Player sprites face LEFT by default, so flip when moving RIGHT (vx > 0).
		if (moving && Mathf.Abs(vx) > 0.1f) {
			var wantFlip = vx > 0f;
			if (wantFlip != _lastFlipX) {
				_lastFlipX = wantFlip;
				for (int i = 0; i < _flipCount; i++) {
					var sr = _flipRenderers[i];
					if (sr != null) sr.flipX = wantFlip;
				}
			}
		}
	}
}
