using Dalamud.Game.ClientState.Keys;
using Dalamud.Logging;

using Ktisis.History;
using Ktisis.Input.Factory;

namespace Ktisis.Input.Hotkeys; 

public class HistoryHotkeys {
	// Constructor

	private readonly HistoryService _history;

	public HistoryHotkeys(HistoryService _history) {
		this._history = _history;
	}
	
	// Hotkeys

	[Hotkey("History_Undo", key: VirtualKey.Z, mods: VirtualKey.CONTROL)]
	public bool HistoryUndo() {
		PluginLog.Information("me when I undo");
		if (!this._history.CanUndo) return false;
		this._history.Undo();
		return true;
	}

	[Hotkey("History_Redo", key: VirtualKey.Z, mods: new[] { VirtualKey.CONTROL, VirtualKey.SHIFT })]
	public bool HistoryRedo() {
		PluginLog.Information("me when I redo");
		if (!this._history.CanRedo) return false;
		this._history.Redo();
		return true;
	}
}
