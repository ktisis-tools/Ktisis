using Dalamud.Game.ClientState.Keys;

using Ktisis.Data.Config.Actions;
using Ktisis.Editor.Actions.Input.Binds;
using Ktisis.Editor.Actions.Types;
using Ktisis.Editor.Selection;

namespace Ktisis.Editor.Actions.Handlers.Select;

[Action("Select_None")]
public class DeselectAction(IActionManager manager) : ActionBase(manager), IKeybind {
	private ISelectManager Selection => this.Manager.Context.Selection;
	
	public KeybindInfo Keybind { get; } = new() {
		Trigger = KeybindTrigger.OnDown,
		Default = new ActionKeybind {
			Enabled = true,
			Combo = new KeyCombo(VirtualKey.ESCAPE)
		}
	};

	public override bool CanInvoke() => this.Selection.Count > 0;
	
	public override bool Invoke() {
		if (!this.CanInvoke()) return false;
		this.Selection.Clear();
		return true;
	}
}
