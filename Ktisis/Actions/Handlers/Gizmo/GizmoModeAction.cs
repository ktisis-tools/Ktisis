using Dalamud.Game.ClientState.Keys;

using Ktisis.Actions.Binds;
using Ktisis.Actions.Types;
using Ktisis.Core.Types;
using Ktisis.Data.Config.Actions;
using Ktisis.ImGuizmo;

namespace Ktisis.Actions.Handlers.Gizmo;

[Action("Gizmo_ToggleMode")]
public class GizmoModeAction(IPluginContext ctx) : KeyAction(ctx) {
	public override KeybindInfo BindInfo { get; } = new() {
		Trigger = KeybindTrigger.OnDown,
		Default = new ActionKeybind {
			Enabled = true,
			Combo = new KeyCombo(VirtualKey.X, VirtualKey.CONTROL)
		}
	};
	
	public override bool Invoke() {
		if (this.Context.Editor == null || this.Context.Editor.Selection.Count == 0)
			return false;
		// ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
		this.Context.Config.File.Gizmo.Mode ^= Mode.World;
		return true;
	}
}
