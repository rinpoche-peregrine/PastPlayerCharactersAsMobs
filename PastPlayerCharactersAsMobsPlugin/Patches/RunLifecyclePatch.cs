using System;
using EIC.Analytics;
using General;
using HarmonyLib;

namespace PastPlayerCharactersAsMobsPlugin.Patches;

[HarmonyPatch(typeof(RunIDTracker), nameof(RunIDTracker.MarkNewRunStarted))]
public static class RunStartedPatch {
	[HarmonyPostfix]
	public static void Postfix() {
		try {
			Plugin.Store.ResetRunBuffer();
			RunState.OnRunStarted();
			Plugin.Log.LogInfo($"Run started — RunId={RunState.CurrentRunId}");
		} catch (Exception ex) {
			Plugin.Log.LogError($"RunStartedPatch failed: {ex}");
		}
	}
}

[HarmonyPatch(typeof(RunIDTracker), nameof(RunIDTracker.MarkRunEnded))]
public static class RunEndedPatch {
	[HarmonyPostfix]
	public static void Postfix(ERunEndReason reason) {
		try {
			var isWin = reason == ERunEndReason.WonByKillingBoss
				|| reason == ERunEndReason.WonBySurviving;
			var buffered = Plugin.Store.RunBufferCount;
			var kept = Plugin.Store.CommitRunBuffer(isWin, Plugin.Config.LossKeepChance.Value);
			RunState.OnRunEnded();
			Plugin.Log.LogInfo($"Run ended ({reason}). isWin={isWin}, buffered={buffered}, committed={kept}, total stored={Plugin.Store.TotalSnapshots}");
		} catch (Exception ex) {
			Plugin.Log.LogError($"RunEndedPatch failed: {ex}");
		}
	}
}
