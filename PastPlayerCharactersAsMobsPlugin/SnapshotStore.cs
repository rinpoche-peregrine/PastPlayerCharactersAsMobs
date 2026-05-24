using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace PastPlayerCharactersAsMobsPlugin;

public class SnapshotStore {
	readonly string _filePath;
	readonly int _perBucketCap;
	readonly Random _rng = new();
	SnapshotStoreFile _data = new();
	readonly List<BuildSnapshot> _runBuffer = new();

	static readonly JsonSerializerOptions JsonOpts = new() {
		WriteIndented = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	public SnapshotStore(string filePath, int perBucketCap = 200) {
		_filePath = filePath;
		_perBucketCap = perBucketCap;
		Load();
	}

	public int TotalSnapshots => _data.Buckets.Values.Sum(b => b.Count);

	public void Load() {
		try {
			if (!File.Exists(_filePath)) {
				_data = new SnapshotStoreFile();
				return;
			}
			var json = File.ReadAllText(_filePath).TrimEnd('\0', '\r', '\n', ' ', '\t');
			_data = JsonSerializer.Deserialize<SnapshotStoreFile>(json, JsonOpts) ?? new SnapshotStoreFile();
		} catch (Exception ex) {
			Plugin.Log.LogError($"SnapshotStore.Load failed: {ex}. Starting fresh.");
			_data = new SnapshotStoreFile();
		}
	}

	public void Save() {
		try {
			Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
			var json = JsonSerializer.Serialize(_data, JsonOpts);
			var bytes = System.Text.Encoding.UTF8.GetBytes(json);
			if (File.Exists(_filePath)) File.Delete(_filePath);
			// Byte-precise, length-locked write. File.WriteAllText was leaving the
			// file logical size pinned at a previous larger value on this runtime,
			// resulting in trailing nulls; SetLength forces the size to match.
			using (var fs = new FileStream(_filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None)) {
				fs.SetLength(bytes.Length);
				fs.Write(bytes, 0, bytes.Length);
				fs.Flush(flushToDisk: true);
			}
			var actual = new FileInfo(_filePath).Length;
			if (actual != bytes.Length)
				Plugin.Log.LogWarning($"SnapshotStore.Save: wrote {bytes.Length} bytes, file size is {actual}");
			else
				Plugin.Log.LogInfo($"SnapshotStore.Save: wrote {bytes.Length} bytes cleanly.");
		} catch (Exception ex) {
			Plugin.Log.LogError($"SnapshotStore.Save failed: {ex}");
		}
	}

	// Called by LevelUpPatch — adds to current-run buffer, not yet persisted.
	public void BufferDuringRun(BuildSnapshot snap) => _runBuffer.Add(snap);

	public int RunBufferCount => _runBuffer.Count;

	public void ResetRunBuffer() => _runBuffer.Clear();

	// Called at run end. On win: persist all buffered. On loss: each rolled independently at lossKeepChance.
	public int CommitRunBuffer(bool isWin, float lossKeepChance) {
		var kept = 0;
		foreach (var snap in _runBuffer) {
			if (isWin || _rng.NextDouble() < lossKeepChance) {
				AddWithEviction(snap);
				kept++;
			}
		}
		_runBuffer.Clear();
		Save();
		return kept;
	}

	void AddWithEviction(BuildSnapshot snap) {
		var bucketKey = snap.EvolutionLevel.ToString();
		if (!_data.Buckets.TryGetValue(bucketKey, out var bucket)) {
			bucket = new List<BuildSnapshot>();
			_data.Buckets[bucketKey] = bucket;
		}
		if (bucket.Count >= _perBucketCap) {
			// Evict: random pick from the oldest 50% by CapturedAt.
			var sorted = bucket.OrderBy(s => s.CapturedAt).ToList();
			var half = Math.Max(1, sorted.Count / 2);
			var victim = sorted[_rng.Next(half)];
			bucket.Remove(victim);
		}
		bucket.Add(snap);
	}
}
