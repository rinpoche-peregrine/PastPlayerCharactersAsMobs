using System.IO;
using System.Linq;
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
		harmony.PatchAll(typeof(Plugin).Assembly);
		Log.LogInfo($"Harmony patches applied: {harmony.GetPatchedMethods().Count()}");
	}
}

// BepInEx-config-backed knobs. Editable at BepInEx/config/com.bungus.everythingiscrab.pastplayercharactersasmobs.cfg
public class ModConfig {
	public ConfigEntry<int> MinLevel;
	public ConfigEntry<int> LevelsBetweenSnapshots;
	public ConfigEntry<int> PerBucketCap;
	public ConfigEntry<float> LossKeepChance;

	public ModConfig(ConfigFile cfg) {
		MinLevel = cfg.Bind("Snapshot", "MinLevel", 20,
			"Start capturing snapshots once player reaches this Evolution Level.");
		LevelsBetweenSnapshots = cfg.Bind("Snapshot", "LevelsBetweenSnapshots", 5,
			"Capture a snapshot every N levels after MinLevel.");
		PerBucketCap = cfg.Bind("Snapshot", "PerBucketCap", 200,
			"Max snapshots stored per level bucket. When full, the oldest 50% is candidate for random eviction.");
		LossKeepChance = cfg.Bind("Snapshot", "LossKeepChance", 0.25f,
			"On a non-winning run, each buffered snapshot is committed to disk with this chance.");
	}
}
