using Dalamud.Game.ClientState.Keys;

using Ktisis.Actions.Attributes;
using Ktisis.Actions.Binds;
using Ktisis.Actions.Types;
using Ktisis.Core.Types;
using Ktisis.Data.Config.Actions;

namespace Ktisis.Actions.Handlers.Select;

[Action("Select_None")]
public class DeselectAction(IPluginContext ctx) : KeyAction(ctx) {
	public override KeybindInfo BindInfo { get; } = new() {
		Trigger = KeybindTrigger.OnDown,
		Default = new ActionKeybind {
			Enabled = true,
			Combo = new KeyCombo(VirtualKey.ESCAPE)
		}
	};

	public override bool CanInvoke() => this.Context.Editor is { Selection.Count: > 0 };
	
	public override bool Invoke() {
		if (!this.CanInvoke()) return false;
		this.Context.Editor!.Selection.Clear();
		return true;
	}
}
