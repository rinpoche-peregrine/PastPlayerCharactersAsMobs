using System.Collections.Generic;
using UnityEngine;

namespace PastPlayerCharactersAsMobsPlugin;

// Tracks which spawned enemies are ghosts, keyed by the IL2CPP object pointer.
// Lets the attack-forwarding patch skip a hierarchy walk per attack.
public static class GhostRegistry {
	public class Entry {
		public Transform EnemyTransform;
		public GameObject Clone;
		public Animator[] CloneAnimators;
	}

	static readonly Dictionary<int, Entry> _byInstanceId = new();

	public static void Register(Component enemyComp, GameObject clone) {
		if (enemyComp == null || clone == null) return;
		try {
			var anims = clone.GetComponentsInChildren<Animator>(true);
			_byInstanceId[enemyComp.gameObject.GetInstanceID()] = new Entry {
				EnemyTransform = enemyComp.transform,
				Clone = clone,
				CloneAnimators = anims ?? System.Array.Empty<Animator>(),
			};
		} catch { /* tolerate */ }
	}

	// Walk up to find a registered ancestor (the attack might originate on a sub-component).
	public static Entry Find(Transform t) {
		while (t != null) {
			if (_byInstanceId.TryGetValue(t.gameObject.GetInstanceID(), out var e)) return e;
			t = t.parent;
		}
		return null;
	}

	public static void PruneStale() {
		// Cheap periodic prune — remove entries whose clone is destroyed.
		var stale = new List<int>();
		foreach (var kv in _byInstanceId) if (kv.Value.Clone == null) stale.Add(kv.Key);
		foreach (var k in stale) _byInstanceId.Remove(k);
	}
}
