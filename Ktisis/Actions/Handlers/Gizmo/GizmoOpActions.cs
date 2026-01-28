using Dalamud.Game.ClientState.Keys;

using Ktisis.Actions.Attributes;
using Ktisis.Actions.Binds;
using Ktisis.Actions.Types;
using Ktisis.Core.Types;
using Ktisis.Data.Config.Actions;
using Ktisis.ImGuizmo;

namespace Ktisis.Actions.Handlers.Gizmo;

public abstract class GizmoOpAction(IPluginContext ctx) : KeyAction(ctx) {
	protected abstract Operation TargetOp { get; init; }

	public override bool Invoke() {
		if (this.Context.Editor == null || this.Context.Editor.Selection.Count == 0)
			return false;
		this.Context.Config.File.Gizmo.Operation = this.TargetOp;
		return true;
	}
}

[Action("Gizmo_SetTranslateMode")]
public class OpTranslateAction(IPluginContext ctx) : GizmoOpAction(ctx) {
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
public class OpRotateAction(IPluginContext ctx) : GizmoOpAction(ctx) {
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
public class OpScaleAction(IPluginContext ctx) : GizmoOpAction(ctx) {
	protected override Operation TargetOp { get; init; } = Operation.SCALE | Operation.SCALE_U;
	
	public override KeybindInfo BindInfo { get; } = new() {
		Trigger = KeybindTrigger.OnDown,
		Default = new ActionKeybind {
			Enabled = true,
			Combo = new KeyCombo(VirtualKey.S, VirtualKey.CONTROL)
		}
	};
}

[Action("Gizmo_SetUniversalMode")]
public class OpUniversalAction(IPluginContext ctx) : GizmoOpAction(ctx) {
	protected override Operation TargetOp { get; init; } = Operation.UNIVERSAL;
	
	public override KeybindInfo BindInfo { get; } = new() {
		Trigger = KeybindTrigger.OnDown,
		Default = new ActionKeybind {
			Enabled = true,
			Combo = new KeyCombo(VirtualKey.U, VirtualKey.CONTROL)
		}
	};
}
