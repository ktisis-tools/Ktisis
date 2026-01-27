using Dalamud.Bindings.ImGuizmo;
using Dalamud.Game.ClientState.Keys;

using Ktisis.Actions.Attributes;
using Ktisis.Actions.Binds;
using Ktisis.Actions.Types;
using Ktisis.Core.Types;
using Ktisis.Data.Config.Actions;

namespace Ktisis.Actions.Handlers.Gizmo;

public abstract class GizmoOpAction(IPluginContext ctx) : KeyAction(ctx) {
	protected abstract ImGuizmoOperation TargetOp { get; init; }

	public override bool Invoke() {
		if (this.Context.Editor == null || this.Context.Editor.Selection.Count == 0)
			return false;
		this.Context.Config.File.Gizmo.Operation = this.TargetOp;
		return true;
	}
}

[Action("Gizmo_SetTranslateMode")]
public class OpTranslateAction(IPluginContext ctx) : GizmoOpAction(ctx) {
	protected override ImGuizmoOperation TargetOp { get; init; } = ImGuizmoOperation.Translate;
	
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
	protected override ImGuizmoOperation TargetOp { get; init; } = ImGuizmoOperation.Rotate;

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
	protected override ImGuizmoOperation TargetOp { get; init; } = ImGuizmoOperation.Scale | ImGuizmoOperation.Scaleu;
	
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
	protected override ImGuizmoOperation TargetOp { get; init; } = ImGuizmoOperation.Universal;
	
	public override KeybindInfo BindInfo { get; } = new() {
		Trigger = KeybindTrigger.OnDown,
		Default = new ActionKeybind {
			Enabled = true,
			Combo = new KeyCombo(VirtualKey.U, VirtualKey.CONTROL)
		}
	};
}