using Dalamud.Game.ClientState.Keys;

using Ktisis.ImGuizmo;
using Ktisis.Editing;
using Ktisis.Config;
using Ktisis.Input.Factory;

namespace Ktisis.Input.Hotkeys; 

public class GizmoHotkeys {
	// Constructor

	private readonly ConfigService _cfg;
	private readonly EditorService _editor;
	
	public GizmoHotkeys(ConfigService _cfg, EditorService _editor) {
		this._cfg = _cfg;
		this._editor = _editor;
	}
	
	// Config

	private ConfigFile Config => this._cfg.Config;

	private void SetGizmoOp(Operation op) {
		if (this._editor.Selection.Count == 0)
			return;
		this.Config.Gizmo_Op = op;
	}
	
	// Hotkeys

	[Hotkey("Gizmo_Toggle", key: VirtualKey.G, mods: VirtualKey.CONTROL)]
	public bool GizmoToggle() {
		if (this.Config.Overlay_Visible)
			this.Config.Overlay_Gizmo = !this.Config.Overlay_Gizmo;
		return true;
	}
	
	[Hotkey("Gizmo_SetTranslateMode", key: VirtualKey.T, mods: VirtualKey.CONTROL)]
	public bool GizmoTranslate() {
		SetGizmoOp(Operation.TRANSLATE);
		return true;
	}
	
	[Hotkey("Gizmo_SetRotateMode", key: VirtualKey.R, mods: VirtualKey.CONTROL)]
	public bool SetRotateMode() {
		SetGizmoOp(Operation.ROTATE);
		return true;
	}

	[Hotkey("Gizmo_SetScaleMode", key: VirtualKey.S, mods: VirtualKey.CONTROL)]
	public bool SetScaleMode() {
		SetGizmoOp(Operation.SCALE);
		return true;
	}
}
