using Dalamud.Game.ClientState.Keys;

using Ktisis.Actions.Impl;
using Ktisis.Data.Config;
using Ktisis.Interface.Input.Keys;

namespace Ktisis.Actions.Gizmo; 

[Action("Gizmo_Toggle")]
public class GizmoToggleAction : IAction, IKeybind {
	private readonly ConfigService _cfg;
	
	public GizmoToggleAction(ConfigService _cfg) {
		this._cfg = _cfg;
	}

	private ConfigFile Config => this._cfg.Config;
	
	public bool Invoke() {
		if (!this.Config.Overlay_Visible)
			return false;
        this.Config.Overlay_Gizmo = !this.Config.Overlay_Gizmo;
		return true;
	}

	public void BuildKeybind(HotkeyFactory hotkey) {
		hotkey.SetDefaultKey(VirtualKey.G, VirtualKey.CONTROL);
	}
}
