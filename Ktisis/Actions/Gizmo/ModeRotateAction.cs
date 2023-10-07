using Dalamud.Game.ClientState.Keys;

using Ktisis.Actions.Impl;
using Ktisis.Data.Config;
using Ktisis.Editing;
using Ktisis.ImGuizmo;
using Ktisis.Interface.Input.Keys;

namespace Ktisis.Actions.Gizmo; 

[Action("Gizmo_SetRotateMode")]
public class ModeRotateAction : ModeActionBase, IKeybind {
	protected override Operation TargetOp { get; init; } = Operation.ROTATE;
	
	public ModeRotateAction(ConfigService _cfg, EditorService _editor) : base(_cfg, _editor) {}

	public void BuildKeybind(HotkeyFactory hotkey) {
		hotkey.SetDefaultKey(VirtualKey.R, VirtualKey.CONTROL);
	}
}
