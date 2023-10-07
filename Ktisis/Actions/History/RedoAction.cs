using Dalamud.Game.ClientState.Keys;

using Ktisis.Actions.Impl;
using Ktisis.Editing.History;
using Ktisis.Interface.Input.Keys;

namespace Ktisis.Actions.History; 

[Action("History_Redo")]
public class RedoAction : IAction, IKeybind {
	private readonly HistoryService _history;
	
	public RedoAction(HistoryService _history) {
		this._history = _history;
	}
	
	public bool CanInvoke() => this._history.CanRedo;

	public bool Invoke() {
		if (!CanInvoke()) return false;
		this._history.Redo();
		return true;
	}

	public void BuildKeybind(HotkeyFactory hotkey) {
		hotkey.SetDefaultKey(VirtualKey.Z, VirtualKey.CONTROL, VirtualKey.SHIFT);
	}
}
