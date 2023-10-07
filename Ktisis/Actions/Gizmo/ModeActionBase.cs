using Ktisis.Actions.Impl;
using Ktisis.Editing;
using Ktisis.ImGuizmo;
using Ktisis.Data.Config;

namespace Ktisis.Actions.Gizmo; 

public abstract class ModeActionBase : IAction {
	protected abstract Operation TargetOp { get; init; }

	private readonly ConfigService _cfg;
	private readonly EditorService _editor;

	protected ModeActionBase(ConfigService _cfg, EditorService _editor) {
		this._cfg = _cfg;
		this._editor = _editor;
	}
	
	public bool Invoke() {
		if (this._editor.Selection.Count == 0)
			return false;
		this._cfg.Config.Gizmo_Op = this.TargetOp;
		return true;
	}
}
