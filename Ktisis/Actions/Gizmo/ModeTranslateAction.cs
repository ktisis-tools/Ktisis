using Dalamud.Game.ClientState.Keys;

using Ktisis.Actions.Impl;
using Ktisis.Data.Config;
using Ktisis.Editing;
using Ktisis.ImGuizmo;
using Ktisis.Interface.Input.Keys;

namespace Ktisis.Actions.Gizmo; 

[Action("Gizmo_SetTranslateMode")]
public class ModeTranslateAction : ModeActionBase, IKeybind {
	protected override Operation TargetOp { get; init; } = Operation.TRANSLATE;
	
	public ModeTranslateAction(ConfigService _cfg, Editor _editor) : base(_cfg, _editor) {}

	public void BuildKeybind(HotkeyFactory hotkey) {
		hotkey.SetDefaultKey(VirtualKey.T, VirtualKey.CONTROL);
	}
}
