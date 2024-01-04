using Ktisis.Editor.Actions.Types;
using Ktisis.ImGuizmo;

namespace Ktisis.Editor.Actions.Handlers.Gizmo;

public abstract class ModeActionBase(IActionManager manager) : ActionBase(manager) {
	protected abstract Operation TargetOp { get; init; }

	public override bool Invoke() {
		if (this.Context.Selection.Count == 0)
			return false;
		this.Context.Config.Gizmo.Operation = this.TargetOp;
		return true;
	}
}

[Action("Gizmo_SetTranslateMode")]
public class ModeTranslateAction(IActionManager manager) : ModeActionBase(manager) {
	protected override Operation TargetOp { get; init; } = Operation.TRANSLATE;
}

[Action("Gizmo_SetRotateMode")]
public class ModeRotateAction(IActionManager manager) : ModeActionBase(manager) {
	protected override Operation TargetOp { get; init; } = Operation.ROTATE;
}

[Action("Gizmo_SetScaleMode")]
public class ModeScaleAction(IActionManager manager) : ModeActionBase(manager) {
	protected override Operation TargetOp { get; init; } = Operation.SCALE;
}
