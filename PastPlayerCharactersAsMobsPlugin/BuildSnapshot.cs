using System;
using System.Collections.Generic;

namespace PastPlayerCharactersAsMobsPlugin;

// Persisted as JSON. Keep fields public for System.Text.Json default serialization.
public class BuildSnapshot {
	public int SchemaVersion { get; set; } = 1;
	public string Id { get; set; }
	public string RunId { get; set; }
	public DateTime CapturedAt { get; set; }
	public int EvolutionLevel { get; set; }
	public int Difficulty { get; set; }
	public string Genetic1 { get; set; }
	public string Genetic2 { get; set; }
	public Dictionary<string, float> Stats { get; set; } = new();
	public Dictionary<string, int> AbilityLevels { get; set; } = new();
}

// Top-level on-disk shape.
public class SnapshotStoreFile {
	public int SchemaVersion { get; set; } = 1;
	// Keyed by EvolutionLevel (the bucket). String key for JSON friendliness.
	public Dictionary<string, List<BuildSnapshot>> Buckets { get; set; } = new();
}
