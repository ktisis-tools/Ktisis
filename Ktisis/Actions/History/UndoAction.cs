using Dalamud.Game.ClientState.Keys;

using Ktisis.Actions.Impl;
using Ktisis.Editing.History;
using Ktisis.Interface.Input.Keys;

namespace Ktisis.Actions.History; 

[Action("History_Undo")]
public class UndoAction : IAction, IKeybind {
	private readonly HistoryService _history;
	
	public UndoAction(HistoryService _history) {
		this._history = _history;
	}
	
	public bool CanInvoke() => this._history.CanUndo;
    
	public bool Invoke() {
		if (!CanInvoke()) return false;
		this._history.Undo();
		return true;
	}

	public void BuildKeybind(HotkeyFactory hotkey) {
		hotkey.SetDefaultKey(VirtualKey.Z, VirtualKey.CONTROL);
	}
}
