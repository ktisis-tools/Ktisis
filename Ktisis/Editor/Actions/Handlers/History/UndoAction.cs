using Dalamud.Game.ClientState.Keys;

using Ktisis.Data.Config.Actions;
using Ktisis.Editor.Actions.Input.Binds;
using Ktisis.Editor.Actions.Types;

namespace Ktisis.Editor.Actions.Handlers.History;

[Action("History_Undo")]
public class UndoAction(IActionManager manager) : ActionBase(manager), IKeybind {
	public KeybindInfo Keybind { get; } = new() {
		Trigger = KeybindTrigger.OnDown,
		Default = new ActionKeybind {
			Enabled = true,
			Combo = new KeyCombo(VirtualKey.Z, VirtualKey.CONTROL)
		}
	};
	
	private IHistoryManager History => this.Manager.History;

	public override bool CanInvoke() => this.History.CanUndo;
	
	public override bool Invoke() {
		if (!this.CanInvoke()) return false;
		this.History.Undo();
		return true;
	}
}
