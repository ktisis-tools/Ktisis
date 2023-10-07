using Dalamud.Game.ClientState.Keys;

using Ktisis.Actions.Impl;
using Ktisis.Data.Config;
using Ktisis.Editing;
using Ktisis.ImGuizmo;
using Ktisis.Interface.Input;
using Ktisis.Interface.Input.Keys;

namespace Ktisis.Actions.Gizmo; 

[Action("Gizmo_SetScaleMode")]
public class ModeScaleAction : ModeActionBase, IKeybind {
	protected override Operation TargetOp { get; init; } = Operation.SCALE;
	
	public ModeScaleAction(ConfigService _cfg, Editor _editor) : base(_cfg, _editor) {}

	public void BuildKeybind(HotkeyFactory hotkey) {
		hotkey.SetDefaultKey(VirtualKey.S, VirtualKey.CONTROL);
	}
}
