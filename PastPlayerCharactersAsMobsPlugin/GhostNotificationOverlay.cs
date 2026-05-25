using System.Collections.Generic;
using UnityEngine;

namespace PastPlayerCharactersAsMobsPlugin;

// Persistent MonoBehaviour added to the BasePlugin's runtime GameObject.
// Renders fading notifications in the bottom-left of the screen when a ghost spawns.
public class GhostNotificationOverlay : MonoBehaviour {
	const float DurationSec = 6f;
	const int FontSize = 16;

	class Entry { public string Text; public float SpawnedAt; }
	static readonly List<Entry> _entries = new();

	public GhostNotificationOverlay(System.IntPtr ptr) : base(ptr) { }

	public static void Show(string text) {
		_entries.Add(new Entry { Text = text, SpawnedAt = Time.realtimeSinceStartup });
	}

	void OnGUI() {
		var now = Time.realtimeSinceStartup;
		_entries.RemoveAll(e => now - e.SpawnedAt > DurationSec);
		if (_entries.Count == 0) return;

		var style = new GUIStyle(GUI.skin.label) {
			fontSize = FontSize,
			fontStyle = FontStyle.Bold,
			alignment = TextAnchor.LowerLeft,
			wordWrap = false,
		};
		float x = 16;
		float y = Screen.height - 24 - (_entries.Count * (FontSize + 6));
		foreach (var e in _entries) {
			var t = (now - e.SpawnedAt) / DurationSec;
			float alpha = t < 0.7f ? 1f : 1f - ((t - 0.7f) / 0.3f);
			// Drop shadow for readability over any background.
			style.normal.textColor = new Color(0f, 0f, 0f, alpha);
			GUI.Label(new Rect(x + 2, y + 2, Screen.width - 32, FontSize + 6), e.Text, style);
			style.normal.textColor = new Color(1f, 0.85f, 0.85f, alpha);
			GUI.Label(new Rect(x, y, Screen.width - 32, FontSize + 6), e.Text, style);
			y += FontSize + 6;
		}
	}
}
