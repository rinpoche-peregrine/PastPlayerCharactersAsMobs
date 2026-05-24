using System;

namespace PastPlayerCharactersAsMobsPlugin;

// Per-run state owned by patches. Reset on MarkNewRunStarted.
public static class RunState {
	public static string CurrentRunId { get; private set; } = "";
	public static bool InRun { get; private set; }

	public static void OnRunStarted() {
		CurrentRunId = Guid.NewGuid().ToString("N");
		InRun = true;
	}

	public static void OnRunEnded() {
		InRun = false;
	}
}
