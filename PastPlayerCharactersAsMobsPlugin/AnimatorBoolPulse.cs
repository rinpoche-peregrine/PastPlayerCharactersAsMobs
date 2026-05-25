using System.Collections.Generic;
using UnityEngine;

namespace PastPlayerCharactersAsMobsPlugin;

// Lets a caller set an Animator Bool to true and have it automatically flip back
// to false after a short delay. Keyed entries collapse so re-pulsing during the
// active window just extends the window.
public static class AnimatorBoolPulse {
	struct Entry { public Animator A; public int Hash; public float ResetAt; }
	static readonly List<Entry> _active = new(64);

	public static void Pulse(Animator a, int hash, float duration) {
		if (a == null || hash == 0) return;
		try {
			a.SetBool(hash, true);
		} catch { return; }
		var resetAt = Time.time + duration;
		for (int i = 0; i < _active.Count; i++) {
			if (_active[i].A == a && _active[i].Hash == hash) {
				var e = _active[i]; e.ResetAt = resetAt; _active[i] = e; return;
			}
		}
		_active.Add(new Entry { A = a, Hash = hash, ResetAt = resetAt });
	}

	// Called every frame from GhostDirectionSync.LateUpdate (any one instance is enough).
	public static void Tick() {
		var now = Time.time;
		for (int i = _active.Count - 1; i >= 0; i--) {
			var e = _active[i];
			if (e.A == null) { _active.RemoveAt(i); continue; }
			if (now >= e.ResetAt) {
				try { e.A.SetBool(e.Hash, false); } catch { }
				_active.RemoveAt(i);
			}
		}
	}
}
