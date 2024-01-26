using Dalamud.Game.ClientState.Keys;

using Ktisis.Actions.Binds;
using Ktisis.Actions.Types;
using Ktisis.Core.Types;
using Ktisis.Data.Config.Actions;

namespace Ktisis.Actions.Handlers.History;

[Action("History_Redo")]
public class RedoAction(IPluginContext ctx) : ActionBase(ctx), IKeybind {
	public KeybindInfo Keybind { get; } = new() {
		Trigger = KeybindTrigger.OnDown,
		Default = new ActionKeybind {
			Enabled = true,
			Combo = new KeyCombo(VirtualKey.Z, VirtualKey.CONTROL, VirtualKey.SHIFT)
		}
	};

	public override bool CanInvoke() => this.Context.Editor is { Actions.History.CanRedo: true };
	
	public override bool Invoke() {
		if (!this.CanInvoke()) return false;
		this.Context.Editor!.Actions.History.Redo();
		return true;
	}
}
