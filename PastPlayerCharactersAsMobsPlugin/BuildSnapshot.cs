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
	public List<AbilityPick> AbilityPicks { get; set; } = new();

	public Dictionary<string, int> Affinities { get; set; } = new();
	public List<string> Specialisations { get; set; } = new();
	public Dictionary<string, string> Cosmetics { get; set; } = new();
}

public class AbilityPick {
	public string AbilityId { get; set; } = "";
	public string Rarity { get; set; } = "Common";
	public int Level { get; set; }
}

// Top-level on-disk shape.
public class SnapshotStoreFile {
	public int SchemaVersion { get; set; } = 1;
	// Keyed by EvolutionLevel (the bucket). String key for JSON friendliness.
	public Dictionary<string, List<BuildSnapshot>> Buckets { get; set; } = new();
}
