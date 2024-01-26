using Dalamud.Game.ClientState.Keys;

using Ktisis.Actions.Binds;
using Ktisis.Actions.Types;
using Ktisis.Core.Types;
using Ktisis.Data.Config.Actions;
using Ktisis.ImGuizmo;

namespace Ktisis.Actions.Handlers.Gizmo;

public abstract class ModeActionBase(IPluginContext ctx) : KeyAction(ctx) {
	protected abstract Operation TargetOp { get; init; }

	public override bool Invoke() {
		if (this.Context.Editor == null || this.Context.Editor.Selection.Count == 0)
			return false;
		this.Context.Config.File.Gizmo.Operation = this.TargetOp;
		return true;
	}
}

[Action("Gizmo_SetTranslateMode")]
public class ModeTranslateAction(IPluginContext ctx) : ModeActionBase(ctx) {
	protected override Operation TargetOp { get; init; } = Operation.TRANSLATE;
	
	public override KeybindInfo BindInfo { get; } = new() {
		Trigger = KeybindTrigger.OnDown,
		Default = new ActionKeybind {
			Enabled = true,
			Combo = new KeyCombo(VirtualKey.T, VirtualKey.CONTROL)
		}
	};
}

[Action("Gizmo_SetRotateMode")]
public class ModeRotateAction(IPluginContext ctx) : ModeActionBase(ctx) {
	protected override Operation TargetOp { get; init; } = Operation.ROTATE;

	public override KeybindInfo BindInfo { get; } = new() {
		Trigger = KeybindTrigger.OnDown,
		Default = new ActionKeybind {
			Enabled = true,
			Combo = new KeyCombo(VirtualKey.R, VirtualKey.CONTROL)
		}
	};
}

[Action("Gizmo_SetScaleMode")]
public class ModeScaleAction(IPluginContext ctx) : ModeActionBase(ctx) {
	protected override Operation TargetOp { get; init; } = Operation.SCALE;
	
	public override KeybindInfo BindInfo { get; } = new() {
		Trigger = KeybindTrigger.OnDown,
		Default = new ActionKeybind {
			Enabled = true,
			Combo = new KeyCombo(VirtualKey.S, VirtualKey.CONTROL)
		}
	};
}
