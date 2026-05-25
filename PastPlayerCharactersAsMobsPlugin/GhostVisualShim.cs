using System;
using GameState;
using Il2CppInterop.Runtime;
using UnityEngine;
using World.Characters;
using World.Evolutions;
using World.Stats;
using World.Statuses;

namespace PastPlayerCharactersAsMobsPlugin;

// MonoBehaviour that implements IVisualStatCharacter so a cloned VisualController
// reads evolutions from our SimulatedEvolutionHandler instead of the live player.
// Registered with ClassInjector as implementing IVisualStatCharacter at plugin load.
public class GhostVisualShim : MonoBehaviour {
	public SimulatedEvolutionHandler Sim;
	public FactDB BorrowedFactDB;
	public AEntityStats BorrowedStats;
	public StatusController BorrowedStatusController;

	public GhostVisualShim(IntPtr ptr) : base(ptr) { }

	// IVisualStatCharacter virtuals - exposed with the names the IL2CPP interface expects
	public FactDB FactDB => BorrowedFactDB;
	public AEntityStats Stats => BorrowedStats;
	public IEvolutionHandler EvolutionHandler {
		get {
			if (Sim == null) return null;
			try { return Sim.Cast<IEvolutionHandler>(); } catch (Exception ex) {
				Plugin.Log.LogWarning($"GhostVisualShim cast sim->IEvolutionHandler failed: {ex.Message}");
				return null;
			}
		}
	}
	public StatusController StatusController => BorrowedStatusController;
}
