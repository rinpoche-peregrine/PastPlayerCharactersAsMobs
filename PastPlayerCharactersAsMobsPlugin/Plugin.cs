using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace PastPlayerCharactersAsMobsPlugin;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin {
	internal static new ManualLogSource Log;
	internal static SnapshotStore Store;
	internal static new ModConfig Config;

	public override void Load() {
		Log = base.Log;
		Log.LogInfo($"{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION} loading...");

		Config = new ModConfig(base.Config);
		var storeDir = Path.Combine(Paths.ConfigPath, MyPluginInfo.PLUGIN_GUID);
		Directory.CreateDirectory(storeDir);
		var storeFile = Path.Combine(storeDir, "snapshots.json");
		Store = new SnapshotStore(storeFile, Config.PerBucketCap.Value);
		Log.LogInfo($"Loaded {Store.TotalSnapshots} existing snapshot(s) from {storeFile}");

		var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
		int ok = 0, failed = 0;
		foreach (var t in typeof(Plugin).Assembly.GetTypes()) {
			if (t.GetCustomAttributes(typeof(HarmonyPatch), true).Length == 0) continue;
			try {
				harmony.CreateClassProcessor(t).Patch();
				ok++;
				Log.LogInfo($"Patched: {t.Name}");
			} catch (System.Exception ex) {
				failed++;
				Log.LogError($"Patch failed on {t.Name}: {ex.Message}");
			}
		}
		Log.LogInfo($"Harmony patches applied: {ok} ok, {failed} failed");

		try {
			Il2CppInterop.Runtime.Injection.ClassInjector.RegisterTypeInIl2Cpp<GhostNotificationOverlay>();
			AddComponent<GhostNotificationOverlay>();
			Log.LogInfo("GhostNotificationOverlay registered.");
		} catch (System.Exception ex) {
			Log.LogError($"Failed to register notification overlay: {ex.Message}");
		}

		try {
			Il2CppInterop.Runtime.Injection.ClassInjector.RegisterTypeInIl2Cpp<GhostDirectionSync>();
			Log.LogInfo("GhostDirectionSync registered.");
		} catch (System.Exception ex) {
			Log.LogError($"Failed to register direction sync: {ex.Message}");
		}

		try {
			var opts = new Il2CppInterop.Runtime.Injection.RegisterTypeOptions {
				Interfaces = new[] { typeof(World.Characters.IVisualStatCharacter) }
			};
			Il2CppInterop.Runtime.Injection.ClassInjector.RegisterTypeInIl2Cpp<GhostVisualShim>(opts);
			Log.LogInfo("GhostVisualShim registered (implements IVisualStatCharacter).");
		} catch (System.Exception ex) {
			Log.LogError($"Failed to register GhostVisualShim: {ex.Message}");
		}
	}
}

// BepInEx-config-backed knobs. Editable at BepInEx/config/com.bungus.everythingiscrab.pastplayercharactersasmobs.cfg
public class ModConfig {
	public ConfigEntry<int> MinLevel;
	public ConfigEntry<int> LevelsBetweenSnapshots;
	public ConfigEntry<int> PerBucketCap;
	public ConfigEntry<float> LossKeepChance;
	public ConfigEntry<float> GhostSpawnChance;
	public ConfigEntry<int> GhostLevelTolerance;
	public ConfigEntry<bool> EnableGhostSpawning;
	public ConfigEntry<bool> ApplyGhostStats;
	public ConfigEntry<float> GhostStatScale;
	public ConfigEntry<bool> ShowGhostNotifications;

	public ModConfig(ConfigFile cfg) {
		MinLevel = cfg.Bind("Snapshot", "MinLevel", 20,
			"Start capturing snapshots once player reaches this Evolution Level.");
		LevelsBetweenSnapshots = cfg.Bind("Snapshot", "LevelsBetweenSnapshots", 5,
			"Capture a snapshot every N levels after MinLevel.");
		PerBucketCap = cfg.Bind("Snapshot", "PerBucketCap", 200,
			"Max snapshots stored per level bucket. When full, the oldest 50% is candidate for random eviction.");
		LossKeepChance = cfg.Bind("Snapshot", "LossKeepChance", 0.25f,
			"On a non-winning run, each buffered snapshot is committed to disk with this chance.");
		EnableGhostSpawning = cfg.Bind("Spawn", "EnableGhostSpawning", true,
			"Master switch for spawning past-self ghosts as Alpha+Shiny mobs.");
		GhostSpawnChance = cfg.Bind("Spawn", "GhostSpawnChance", 0.005f,
			"Chance per regular enemy spawn that a matching snapshot is promoted to a past-self ghost (0.005 = 1 in 200).");
		GhostLevelTolerance = cfg.Bind("Spawn", "GhostLevelTolerance", 2,
			"A snapshot is eligible to spawn if its EvolutionLevel is within +/- this many levels of the current player level.");
		ApplyGhostStats = cfg.Bind("Spawn", "ApplyGhostStats", true,
			"Override the spawned ghost's stats (MaxHp, BasePhysicalDamage, etc.) with the snapshot's values. Disable to keep generic Alpha behavior.");
		GhostStatScale = cfg.Bind("Spawn", "GhostStatScale", 1.0f,
			"Multiplier applied to transferred stats. 1.0 = full snapshot power, 0.5 = half. Tune to taste.");
		ShowGhostNotifications = cfg.Bind("UI", "ShowGhostNotifications", false,
			"Show a fading bottom-left text notification when a past self spawns nearby.");
	}
}
