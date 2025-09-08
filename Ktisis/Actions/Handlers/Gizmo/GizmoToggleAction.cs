using Dalamud.Game.ClientState.Keys;

using Ktisis.Actions.Attributes;
using Ktisis.Actions.Binds;
using Ktisis.Actions.Types;
using Ktisis.Core.Types;
using Ktisis.Data.Config.Actions;

namespace Ktisis.Actions.Handlers.Gizmo;

[Action("Gizmo_Toggle")]
public class GizmoToggleAction(IPluginContext ctx) : KeyAction(ctx) {
	public override KeybindInfo BindInfo { get; } = new() {
		Trigger = KeybindTrigger.OnDown,
		Default = new ActionKeybind {
			Enabled = true,
			Combo = new KeyCombo(VirtualKey.G, VirtualKey.CONTROL)
		}
	};

	public override bool CanInvoke() => this.Context.Editor != null;

	public override bool Invoke() {
		if (!this.CanInvoke()) return false;
		this.Context.Config.File.Gizmo.Visible ^= true;
		return true;
	}
}
